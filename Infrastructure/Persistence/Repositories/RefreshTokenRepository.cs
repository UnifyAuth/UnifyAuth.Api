using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
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
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<IDataResult<RefreshToken>> GetRefreshTokenByTokenAsync(string token)
        {
            var result = await _context.RefreshTokens.Where(rt => rt.Token == token).FirstOrDefaultAsync();
            if (result == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", token);
                return new ErrorDataResult<RefreshToken>("Refresh token not found", AppError.NotFound());
            }
            return new SuccessDataResult<RefreshToken>(result);
        }

        public async Task<IDataResult<IEnumerable<RefreshToken>>> GetRefreshTokenByUserIdAsync(Guid userId)
        {
            var result = await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
            if(result == null || result.Count == 0) return new ErrorDataResult<IEnumerable<RefreshToken>>("Refresh tokens not found for the user", AppError.NotFound());
            return new SuccessDataResult<IEnumerable<RefreshToken>>(result);
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.SaveChangesAsync();
        }
    }
}
