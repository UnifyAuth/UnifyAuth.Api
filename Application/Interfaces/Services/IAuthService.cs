using Application.Common.Results.Abstracts;
using Application.Common.Security;
using Application.DTOs;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IAuthService
    {
        Task<IResult> RegisterAsync(RegisterDto registerDto);
        Task<IDataResult<TokenResultDto>> LoginAsyncWithJWT(LoginDto loginDto);
        Task<IDataResult<TokenResultDto>> RefreshAccessToken(string refreshTokenString);
        Task<IResult> Logout(string refreshTokenString);
    }
}
