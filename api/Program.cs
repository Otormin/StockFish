using api.Data;
using api.Interfaces;
using api.Models;
using api.Repository;
using api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
//for NLog
using NLog;
using NLog.Web;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

// Early init of NLog to allow startup and exception logging, before host is built
var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");

//try, catch, finally for NLog
try{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    //First step to adding controllers to program.cs
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    //to let swagger have jwt built into it
    builder.Services.AddSwaggerGen(option =>
    {
        option.SwaggerDoc("v1", new OpenApiInfo { Title = "StockFish", Version = "v1" });
        option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        option.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type=ReferenceType.SecurityScheme,
                        Id="Bearer"
                    }
                },
                new string[]{}
            }
        });
    });

    //install newtonsoft.json and microsoft.Aspnetcore.MVC.Newtonsoft and write this code to prevent object cycles
    builder.Services.AddControllers().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

    //add ApplicationDBContext and connection string
    builder.Services.AddDbContext<ApplicationDBContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    });

    //add this for Identity
    builder.Services.AddIdentity<AppUser, IdentityRole>(options => {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;

        //To stop attackers from bruteforcing their way into our API
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // How long they are locked out
        //The default amount of failed attempts is 5
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDBContext>();

    //add for JWT
    builder.Services.AddAuthentication(options => 
    {
        options.DefaultAuthenticateScheme = 
        options.DefaultChallengeScheme = 
        options.DefaultForbidScheme = 
        options.DefaultScheme = 
        options.DefaultSignInScheme = 
        options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options => 
    {
        // options.RequireHttpsMetadata = true;
        // options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidAudience = builder.Configuration["JWT:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JWT:SigningKey"])
            ),
            ClockSkew = TimeSpan.Zero
        }; 
    });

    //3rd step to adding controllers into your program.cs. Add before var app
    builder.Services.AddScoped<IStockRepository, StockRepository>();
    builder.Services.AddScoped<ICommentRepository, CommentRepository>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
    builder.Services.AddScoped<IFMPService, FMPService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ICommentService, CommentService>();
    builder.Services.AddScoped<IPortfolioService, PortfolioService>();
    builder.Services.AddScoped<IStockService, StockService>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();


    //for getting user data from the jwt when outside the controller 
    builder.Services.AddHttpContextAccessor();

    //for http client
    builder.Services.AddHttpClient<IFMPService, FMPService>();

    //Add RATE LIMITING SERVICES Before builder.Build
    //This does rate limiting per user
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            // Try to get the retry time default to 0 if not found
            context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter);
            
            // Ensure at least 1 second
            var retrySeconds = Math.Max(retryAfter.TotalSeconds, 1); 
            context.HttpContext.Response.Headers.RetryAfter = $"{retrySeconds}";

            var problemDetailsFactory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            
            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                context.HttpContext,
                StatusCodes.Status429TooManyRequests,
                title: "Too Many Requests",
                detail: $"Quota exceeded. Please try again after {retrySeconds} seconds."
            );

            await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: token);
        };

        options.AddPolicy("auth-limit", httpContext =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", 
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 3,
                    QueueLimit = 0
                })
        );

        options.AddPolicy("ip-sliding", httpContext =>
        {
            // High-level users like SuperAdmins get higher limits
            bool isPrivileged = httpContext.User.IsInRole("SuperAdmin") || httpContext.User.IsInRole("Admin");

            return RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", 
                factory: partition => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = isPrivileged ? 100 : 10, 
                    Window = TimeSpan.FromSeconds(10),
                    SegmentsPerWindow = 5,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = isPrivileged ? 10 : 2
                });
        });

        // This creates a policy that looks at the user's IP Address
        options.AddPolicy("fixed", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", 
                factory: partition => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10, // Allow 10 requests 
                    Window = TimeSpan.FromSeconds(10), // Every 10 seconds
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }
            )
        );
    });

    //this does rate limiting for the server
    /*
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddFixedWindowLimiter(policyName: "fixed", options =>
        {
            options.PermitLimit = 5; // Allow 5 requests
            options.Window = TimeSpan.FromSeconds(10); // Every 10 seconds
            options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            options.QueueLimit = 2; // Allow 2 extra requests to wait in line
        });
    }); */

    var app = builder.Build();

    //This is to get the users IP address instead of the IP address of your hosting provider
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    //dont forget to uncomment this
    //app.UseHttpsRedirection();

    //always use CORS after the httpsDirection
    app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            //.WithOrigins("https://localhost:44351")
            .SetIsOriginAllowed(origin => true));

    //Add RATE LIMITING MIDDLEWARE (After CORS, Before Auth)
    app.UseRateLimiter();

    //add for JWT
    app.UseAuthentication();
    app.UseAuthorization();

    //Second step to adding controllers to program.cs
    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    LogManager.Shutdown();
}

/*
This code allows you to put the rate limiting middleware above the CORS middleware saving server resources and allowing 
CORS to get the error and display it on the frontend

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.OnRejected = async (context, token) =>
    {
        var httpContext = context.HttpContext;

        // 1. MANUALLY ADD CORS HEADERS
        // This ensures React can read the 429 error even if the 
        // global app.UseCors() hasn't executed yet.
        httpContext.Response.Headers.AccessControlAllowOrigin = "*"; // Or your specific React URL
        httpContext.Response.Headers.AccessControlAllowMethods = "GET, POST, PUT, DELETE, OPTIONS";
        httpContext.Response.Headers.AccessControlAllowHeaders = "Content-Type, Authorization";

        // 2. SET RETRY METADATA
        context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter);
        var retrySeconds = Math.Max(retryAfter.TotalSeconds, 1);
        httpContext.Response.Headers.RetryAfter = $"{retrySeconds}";

        // 3. CREATE PROBLEM DETAILS
        var problemDetailsFactory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            detail: $"Rate limit exceeded. Try again in {retrySeconds} seconds."
        );

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: token);
    };

    // ... your policies (ip-sliding, etc.)
});

*/