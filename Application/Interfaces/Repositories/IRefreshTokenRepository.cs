using Application.Common.Results.Abstracts;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IRefreshTokenRepository
    {
        Task AddRefreshTokenAsync(RefreshToken refreshToken);
        Task<IDataResult<RefreshToken>> GetRefreshTokenByTokenAsync(string token);
        Task<IDataResult<IEnumerable<RefreshToken>>> GetRefreshTokenByUserIdAsync(Guid userId);
        Task UpdateRefreshTokenAsync(RefreshToken refreshToken);

    }
}
