using Application.Common.Results.Abstracts;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly UnifyAuthContext _context;
        private readonly ILogger<RefreshTokenRepository> _logger;
        public RefreshTokenRepository(UnifyAuthContext context, ILogger<RefreshTokenRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            _logger.LogDebug("Adding refresh token for user ID: {UserId}", refreshToken.UserId);
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> GetRefreshTokenByTokenAsync(string token)
        {
            _logger.LogDebug("Retrieving refresh token for token: {Token}", token);
            return await _context.RefreshTokens.Where(rt => rt.Token == token).FirstOrDefaultAsync();            
        }

        public async Task<IEnumerable<RefreshToken>> GetRefreshTokenByUserIdAsync(Guid userId)
        {
            _logger.LogDebug("Retrieving refresh tokens for user ID: {UserId}", userId);
            return await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            _logger.LogDebug("Updating refresh token for user ID: {UserId}", refreshToken.UserId);
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }
    }
}
