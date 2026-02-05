using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Helpers;

namespace api.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);
        Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
        Task<RefreshToken> FindHashedToken(string hashedToken);
    }
}