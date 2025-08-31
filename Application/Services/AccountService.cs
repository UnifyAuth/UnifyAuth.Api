using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Common.Validators;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccountService> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<UserUpdateDto> _userUpdateDtoValidator;
        private readonly IEMailService _emailService;
        private readonly IEmailTokenService _emailTokenService;
        private readonly ITwoFactorService _twoFactorService;

        public AccountService(IUserRepository userRepository, ILogger<AccountService> logger, IMapper mapper, IValidator<UserUpdateDto> userUpdateDtoValidator, IEmailTokenService emailTokenService, IEMailService emailService, ITwoFactorService twoFactorService)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _userUpdateDtoValidator = userUpdateDtoValidator;
            _emailTokenService = emailTokenService;
            _emailService = emailService;
            _twoFactorService = twoFactorService;
        }

        public async Task<IDataResult<UserDto>> GetUserInfos(string userId)
        {
            var result = await _userRepository.GetUserByIdAsync(userId);
            if (result is ErrorDataResult<User> errorDataResult)
            {
                _logger.LogInformation("Error retrieving user info for userId {UserId}: {Error}", userId, errorDataResult.Message);
                return new ErrorDataResult<UserDto>(errorDataResult.Message, errorDataResult.ErrorType);
            }

            UserDto userDto = _mapper.Map<UserDto>(result.Data);
            return new SuccessDataResult<UserDto>(userDto, "User information retrieved successfully");
        }

        public async Task<IResult> UpdateUserAsync(UserUpdateDto userUpdateDto)
        {
            var user = await _userRepository.GetUserByIdAsync(userUpdateDto.Id.ToString());
            if(user is ErrorDataResult<User> userErrorDataResult)
            {
                if(userErrorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogWarning("User not found for update with Id: {UserId}", userUpdateDto.Id);
                    return new ErrorResult(userErrorDataResult.Message, userErrorDataResult.ErrorType);
                }
            }
            var validationResult = _userUpdateDtoValidator.Validate(userUpdateDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for user update: {Email} {Errors}",
                    user.Data.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), "BadRequest");
            }

            var result = await _userRepository.UpdateUserAsync(userUpdateDto);
            if (result is ErrorResult errorResult)
            {
                if (errorResult.Messages.Length > 1)
                {
                    _logger.LogInformation("User updating failed for {Email} with multiple errors: {Errors}",
                        user.Data.Email,
                        errorResult.Messages);
                    return new ErrorResult(errorResult.Messages, errorResult.ErrorType);
                }
                _logger.LogInformation("User updating failed for {Email} with error: {Error}",
                    user.Data.Email,
                    errorResult.Message);
                return new ErrorResult(errorResult.Message, errorResult.ErrorType);
            }

            _logger.LogInformation("User updated successfully with Email: {Email}", user.Data.Email);
            return new SuccessResult("User updated successfully");
        }

        public async Task<IResult> SendEmailConfirmationLinkAsync(Guid userId, string email)
        {
            var user = await _userRepository.GetUserByIdAsync(userId.ToString());
            if (user is ErrorDataResult<User> userErrorDataResult)
            {
                if (userErrorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogWarning("User not found for Id: {UserId}", userId);
                    return new ErrorResult(userErrorDataResult.Message, userErrorDataResult.ErrorType);
                }
            }
            var emailConfirmationToken = await _emailTokenService.GenerateEmailConfirmationToken(user.Data);
            if (emailConfirmationToken is ErrorDataResult<ConfirmEmailDto> errorDataResult)
            {
                return new ErrorResult(errorDataResult.Message, errorDataResult.ErrorType);
            }

            var frontendUrl = "http://localhost:4200/confirm-email";
            var emailContent = string.Empty;
            emailContent = $"{frontendUrl}?userId={emailConfirmationToken.Data.UserId}&token={emailConfirmationToken.Data.Token}";

            var emailResult = await _emailService.SendAsync(email, "Confirm your email", emailContent);
            if (emailResult is ErrorResult mailErrorResult)
            {
                return new ErrorResult(mailErrorResult.Message, mailErrorResult.ErrorType);
            }

            return new SuccessResult("Email confirmation link sent successfully");
        }

        public async Task<IDataResult<TwoFactorConfigurationDto>> ConfigureTwoFactorAsync(string userId, AuthenticationProviderType provider)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is ErrorDataResult<User> userErrorDataResult)
            {
                if (userErrorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogWarning("User not found for Id: {UserId}", userId);
                    return new ErrorDataResult<TwoFactorConfigurationDto>(userErrorDataResult.Message, userErrorDataResult.ErrorType);
                }
            }

            switch (provider)
            {
                case AuthenticationProviderType.None:
                    if(user.Data.Preferred2FAProvider == AuthenticationProviderType.None)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>("You have not an authenticator option already", "BadRequest");
                    }
                    var disableResult = await _twoFactorService.DisableUserTwoFactorAuthentication(userId);
                    if(disableResult is ErrorResult errorResult)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>(errorResult.Message, errorResult.ErrorType);
                    }
                    return new SuccessDataResult<TwoFactorConfigurationDto>(new TwoFactorConfigurationDto(),disableResult.Message);

                case AuthenticationProviderType.Authenticator:
                    if (user.Data.Preferred2FAProvider == AuthenticationProviderType.Authenticator)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Authenticator app is already set as the preferred two-factor authentication method.", "BadRequest");
                    }
                    var authenticatorResult = await _twoFactorService.GenerateAuthenticatorKeyAndQrAsync(userId);
                    if (authenticatorResult is ErrorDataResult<AuthenticatorAppDto> errorDataResult)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>(errorDataResult.Message, errorDataResult.ErrorType);
                    }
                    var twoFactorConfigurationDto = new TwoFactorConfigurationDto
                    {
                        SharedKey = authenticatorResult.Data.SharedKey,
                        QrCodeUri = authenticatorResult.Data.QrCodeUri,
                        Provider = provider
                    };
                    return new SuccessDataResult<TwoFactorConfigurationDto>(twoFactorConfigurationDto);

                case AuthenticationProviderType.Email:
                    if (user.Data.Preferred2FAProvider == AuthenticationProviderType.Email)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Email is already set as the preferred two-factor authentication method.", "BadRequest");
                    }
                    bool emailConfirmed = user.Data.EmailConfirmed ?? false;
                    if (!emailConfirmed)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Email not confirmed. Please confirm your email before setting up two-factor authentication.", "BadRequest");
                    }
                    var emailToken = await _twoFactorService.GenerateAuthenticationKey(user.Data, provider);
                    if (emailToken is ErrorDataResult<string> emailErrorDataResult)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>(emailErrorDataResult.Message, emailErrorDataResult.ErrorType);
                    }
                    var emailContent = $"Your email verification code is: {emailToken.Data}. Please use this code to complete your two-factor authentication setup.";
                    var emailResult = await _emailService.SendAsync(user.Data.Email, "Two-Factor Authentication Code", emailContent);
                    if (emailResult is ErrorResult emailErrorResult)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>(emailErrorResult.Message, emailErrorResult.ErrorType);
                    }

                    var emailTwoFactorConfigurationDto = new TwoFactorConfigurationDto
                    {
                        SharedKey = "Check your email!",
                        Provider = provider
                    };
                    return new SuccessDataResult<TwoFactorConfigurationDto>(emailTwoFactorConfigurationDto);

                case AuthenticationProviderType.Phone:
                    if (user.Data.Preferred2FAProvider == AuthenticationProviderType.Phone)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Phone is already set as the preferred two-factor authentication method.", "BadRequest");
                    }
                    bool isPhoneNumberConfirmed = user.Data.PhoneNumberConfirmed ?? false;
                    if (!isPhoneNumberConfirmed)
                    {
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Phone number not confirmed. Please confirm your phone number before setting up two-factor authentication.", "BadRequest");
                    }
                    return new SuccessDataResult<TwoFactorConfigurationDto>(
                        new TwoFactorConfigurationDto
                        {
                            Provider = provider,
                            SharedKey = "SMS setup not implemented yet"
                        });
                default:
                    _logger.LogWarning("Unsupported two-factor authentication provider: {Provider}", provider);
                    return new ErrorDataResult<TwoFactorConfigurationDto>("Unsupported two-factor authentication provider", "BadRequest");

            }
        }

        public async Task<IResult> VerifyTwoFactorAuthentication(string userId, VerifyTwoFactorDto verifyTwoFactorDto)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if(user is ErrorDataResult<User> userErrorDataResult)
            {
                if (userErrorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogWarning("User not found for Id: {UserId}", userId);
                    return new ErrorResult(userErrorDataResult.Message, userErrorDataResult.ErrorType);
                }
            }

            var verificationResult = await _twoFactorService.VerifyTwoFactorAuthenticationKey(userId, verifyTwoFactorDto);
            if(verificationResult is ErrorResult errorResult)
            {
                return new ErrorResult(errorResult.Message, errorResult.ErrorType);
            }

            return new SuccessResult("Two-factor authentication verified successfully");
        }
    }
}
