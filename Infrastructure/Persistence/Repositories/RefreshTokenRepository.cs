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
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> GetRefreshTokenByTokenAsync(string token)
        {
            return await _context.RefreshTokens.Where(rt => rt.Token == token).FirstOrDefaultAsync();            
        }

        public async Task<IEnumerable<RefreshToken>> GetRefreshTokenByUserIdAsync(Guid userId)
        {
            return await _context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            await _context.SaveChangesAsync();
        }
    }
}
