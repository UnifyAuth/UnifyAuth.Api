using Application.Common.Results.Abstracts;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ITwoFactorService
    {
        Task<IDataResult<AuthenticatorAppDto>> GenerateAuthenticatorKeyAndQrAsync(string userId);
        Task<IDataResult<string>> GenerateAuthenticationKey(User user, AuthenticationProviderType provider);
        Task<IResult> VerifyTwoFactorAuthenticationKey(string userId, VerifyTwoFactorDto verifyTwoFactorDto);
        Task<IResult> DisableUserTwoFactorAuthentication(string userId); 
    }
}
