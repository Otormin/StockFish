using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Helpers;
using api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Repository
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly ApplicationDBContext _context;
        public RefreshTokenRepository(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
            return refreshToken;
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            _context.RefreshTokens.Update(refreshToken);            
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> FindHashedToken(string hashedToken)
        {
            var retrievedToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == hashedToken);
            if(retrievedToken == null)
            {
                return null;
            }

            return retrievedToken;
        }
    }
}