using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private UserManager<IdentityUserModel> _userManager;
        private IMapper _mapper;
        private ILogger<TwoFactorService> _logger;

        public TwoFactorService(UserManager<IdentityUserModel> userManager, IMapper mapper, ILogger<TwoFactorService> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IDataResult<AuthenticatorAppDto>> GenerateAuthenticatorKeyAndQrAsync(string userId)
        {
            var identityUser = await _userManager.FindByIdAsync(userId);
            var resetResult = await _userManager.ResetAuthenticatorKeyAsync(identityUser!);
            if (!resetResult.Succeeded)
            {
                _logger.LogError("Failed to reset authenticator key for user: {Email}", identityUser!.Email);
                throw new Exception("Failed to reset authenticator key");
            }
            var key = await _userManager.GetAuthenticatorKeyAsync(identityUser!);
            if (key == null)
            {
                _logger.LogError("Failed to retrieve authenticator key for user: {Email}", identityUser!.Email);
                throw new Exception("Failed to retrieve authenticator key");
            }
            string issuer = "UnifyAuth";
            string email = identityUser!.Email!;
            var qrCodeUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";

            var authenticatorAppDto = new AuthenticatorAppDto
            {
                SharedKey = key,
                QrCodeUri = qrCodeUri
            };
            return new SuccessDataResult<AuthenticatorAppDto>(authenticatorAppDto);
        }

        public async Task<IDataResult<string>> GenerateAuthenticationKey(User user, AuthenticationProviderType provider)
        {
            var identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GenerateTwoFactorTokenAsync(identityUser, provider.ToString());
            if (token == null)
            {
                _logger.LogError("Failed to generate authentication key for Email: {Email}", user.Email);
                throw new Exception("Failed to generate authentication key");
            }
            return new SuccessDataResult<string>(token);
        }

        public async Task<IResult> VerifyTwoFactorConfigurationKey(string userId, VerifyTwoFactorConfigurationDto verifyTwoFactorDto)
        {
            var identityUser = await _userManager.FindByIdAsync(userId);
            var userTwoFaProvider = identityUser!.Preferred2FAProvider; // if user has authenticator app provider and now wants to set email or sms, we need to remove the authenticator key following lines
            identityUser!.Preferred2FAProvider = verifyTwoFactorDto.Provider;
            var isVerified = await _userManager.VerifyTwoFactorTokenAsync(identityUser!, verifyTwoFactorDto.Provider.ToString(), verifyTwoFactorDto.Key);
            if (!isVerified)
            {
                _logger.LogInformation("Two-factor authentication key verification failed for Email: {Email}", identityUser!.Email);
                return new ErrorResult("Invalid two-factor authentication key", AppError.BadRequest());
            }
            var updateUserPreferred2FAProvider = await _userManager.UpdateAsync(identityUser);
            if (!updateUserPreferred2FAProvider.Succeeded)
            {
                throw new Exception("Failed to update user's preferred two-factor authentication provider");
            }

            if (!identityUser!.TwoFactorEnabled)
            {
                await _userManager.SetTwoFactorEnabledAsync(identityUser, true);
            }

            if (verifyTwoFactorDto.Provider != AuthenticationProviderType.Authenticator && userTwoFaProvider == AuthenticationProviderType.Authenticator)
            {
                await RemoveUserAuthenticationToken(identityUser);
            }
            return new SuccessResult("Two-factor configuration key verified successfully");
        }
        public async Task<IResult> VerifyTwoFactorAuthenticationKey(VerifyTwoFactorAuthenticationDto verifyTwoFactorAuthenticationDto)
        {
            var identityUser = await _userManager.FindByIdAsync(verifyTwoFactorAuthenticationDto.UserId);
            var isVerified = await _userManager.VerifyTwoFactorTokenAsync(identityUser!, verifyTwoFactorAuthenticationDto.Provider, verifyTwoFactorAuthenticationDto.Key);
            if (!isVerified)
            {
                _logger.LogInformation("Two-factor authentication key verification failed for Email: {Email}", identityUser!.Email);
                return new ErrorResult("Invalid two-factor authentication key", AppError.BadRequest());
            }
            return new SuccessResult("Two-factor authentication key verified successfully");
        }

        public async Task<IResult> DisableUserTwoFactorAuthentication(string userId)
        {
            var identityUser = await _userManager.FindByIdAsync(userId);
            var twoFaProvider = identityUser!.Preferred2FAProvider;
            identityUser!.Preferred2FAProvider = AuthenticationProviderType.None;
            var updateResult = await _userManager.SetTwoFactorEnabledAsync(identityUser!, false);
            if (!updateResult.Succeeded)
            {
                _logger.LogError("Failed to disable two-factor authentication for Email: {Email}", identityUser!.Email);
                throw new Exception("Failed to disable two-factor authentication");
            }
            if (twoFaProvider == AuthenticationProviderType.Authenticator)
                await RemoveUserAuthenticationToken(identityUser!);

            return new SuccessResult("Two-factor authentication disabled successfully");
        }

        private async Task<IResult> RemoveUserAuthenticationToken(IdentityUserModel identityUser)
        {
            var result = await _userManager.RemoveAuthenticationTokenAsync(identityUser, "[AspNetUserStore]", "AuthenticatorKey");
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to remove authenticator key for user: {Email}", identityUser.Email);
                throw new Exception("Failed to remove authenticator key");
            }
            return new SuccessResult("Authenticator key removed successfully");
        }
    }
}
