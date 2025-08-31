using Application.Common.Results.Abstracts;
using Application.DTOs;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IAccountService
    {
        Task<IDataResult<UserDto>> GetUserInfos(string userId);
        Task<IResult> UpdateUserAsync(UserUpdateDto userUpdateDto);
        Task<IResult> SendEmailConfirmationLinkAsync(Guid userId, string email);
        Task<IDataResult<TwoFactorConfigurationDto>> ConfigureTwoFactorAsync(string userId, AuthenticationProviderType provider);
        Task<IResult> VerifyTwoFactorAuthentication(string userId,VerifyTwoFactorDto verifyTwoFactorDto);
    }
}
