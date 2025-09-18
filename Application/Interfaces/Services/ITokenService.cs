using Application.Common.Results.Abstracts;
using Application.Common.Security;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ITokenService
    {
        AccessToken GenerateAccessToken(User user);
        IResult ValidateRefreshTokenAsync(RefreshToken refreshToken);
        Task<IResult> UpdateRefreshToken(RefreshToken refreshToken, string refreshTokenString);
        string GenerateRefreshToken();
    }
}
