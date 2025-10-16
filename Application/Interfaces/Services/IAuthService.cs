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
        Task<IDataResult<LoginResponseDto>> LoginAsyncWithJWT(LoginDto loginDto);
        Task<IDataResult<LoginResponseDto>> VerifyTwoFactorAuthentication(VerifyTwoFactorAuthenticationDto verifyTwoFactorAuthenticationDto);
        Task<IDataResult<TokenResultDto>> RefreshAccessToken(string refreshTokenString);
        Task<IResult> Logout(string refreshTokenString);
        Task<IResult> SendResetPasswordLink(string email);
        Task<IResult> ResetPassword(ResetPasswordDto resetPasswordDto);
        Task<IDataResult<LoginResponseDto>> LoginWithGoogle(ExternalLoginDto externalLoginDto);
    }
}
