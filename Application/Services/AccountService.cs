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
                return new ErrorDataResult<UserDto>(errorDataResult.Message!, errorDataResult.ErrorType!);

            UserDto userDto = _mapper.Map<UserDto>(result.Data);
            return new SuccessDataResult<UserDto>(userDto);
        }

        public async Task<IResult> UpdateUserAsync(UserUpdateDto userUpdateDto)
        {
            var user = await _userRepository.GetUserByIdAsync(userUpdateDto.Id.ToString());
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorResult(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);

            var validationResult = _userUpdateDtoValidator.Validate(userUpdateDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for user update: {Email} {Errors}",
                    user.Data!.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), AppError.Validation());
            }

            var result = await _userRepository.UpdateUserAsync(userUpdateDto);
            if (result is ErrorResult errorResult)
            {
                if (errorResult.Messages != null)
                    return new ErrorResult(errorResult.Messages, errorResult.ErrorType!);

                return new ErrorResult(errorResult.Message!, errorResult.ErrorType!);
            }
            return new SuccessResult("User updated successfully");
        }

        public async Task<IResult> SendEmailConfirmationLinkAsync(string userId, string email)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorResult(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);

            if (user.Data!.Email != email)
            {
                _logger.LogWarning("Email mismatch for provided email {ProvidedEmail}, registered email {RegisteredEmail}", email, user.Data.Email);
                return new ErrorResult("This email is not your registered email", AppError.BadRequest());
            }
            if (user.Data.EmailConfirmed == true)
            {
                _logger.LogInformation("Email already confirmed for email: {Email}", user.Data.Email);
                return new ErrorResult("Email already confirmed", AppError.BadRequest());
            }

            var emailConfirmationToken = await _emailTokenService.GenerateEmailConfirmationToken(user.Data);
            var frontendUrl = "http://localhost:4200/confirm-email";
            var emailContent = $"{frontendUrl}?userId={emailConfirmationToken.Data!.UserId}&token={emailConfirmationToken.Data.Token}";
            var emailResult = await _emailService.SendAsync(email, "Confirm your email", emailContent);

            return new SuccessResult("Email confirmation link sent successfully");
        }

        public async Task<IDataResult<TwoFactorConfigurationDto>> ConfigureTwoFactorAsync(string userId, AuthenticationProviderType provider)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorDataResult<TwoFactorConfigurationDto>(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);


            switch (provider)
            {
                case AuthenticationProviderType.None:
                    if (user.Data!.Preferred2FAProvider == AuthenticationProviderType.None)
                        return new ErrorDataResult<TwoFactorConfigurationDto>("You have not an authenticator option already", AppError.BadRequest());

                    var disableResult = await _twoFactorService.DisableUserTwoFactorAuthentication(userId);
                    return new SuccessDataResult<TwoFactorConfigurationDto>(new TwoFactorConfigurationDto());

                case AuthenticationProviderType.Authenticator:
                    if (user.Data!.Preferred2FAProvider == AuthenticationProviderType.Authenticator)
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Authenticator app is already set as the preferred two-factor authentication method.", AppError.BadRequest());

                    var authenticatorResult = await _twoFactorService.GenerateAuthenticatorKeyAndQrAsync(userId);
                    var twoFactorConfigurationDto = new TwoFactorConfigurationDto
                    {
                        SharedKey = authenticatorResult.Data!.SharedKey,
                        QrCodeUri = authenticatorResult.Data.QrCodeUri,
                        Provider = provider
                    };
                    return new SuccessDataResult<TwoFactorConfigurationDto>(twoFactorConfigurationDto);

                case AuthenticationProviderType.Email:
                    if (user.Data!.Preferred2FAProvider == AuthenticationProviderType.Email)
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Email is already set as the preferred two-factor authentication method.", AppError.BadRequest());

                    bool emailConfirmed = user.Data.EmailConfirmed ?? false;
                    if (!emailConfirmed)
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Email not confirmed. Please confirm your email before setting up two-factor authentication.", AppError.BadRequest());


                    var emailToken = await _twoFactorService.GenerateAuthenticationKey(user.Data, provider);
                    var emailContent = $"Your email verification code is: {emailToken.Data}. Please use this code to complete your two-factor configuration setup.";
                    var emailResult = await _emailService.SendAsync(user.Data.Email, "Two-Factor Configuration Code", emailContent);
                    var emailTwoFactorConfigurationDto = new TwoFactorConfigurationDto
                    {
                        SharedKey = "Check your email!",
                        Provider = provider
                    };
                    return new SuccessDataResult<TwoFactorConfigurationDto>(emailTwoFactorConfigurationDto);

                case AuthenticationProviderType.Phone:
                    if (user.Data!.Preferred2FAProvider == AuthenticationProviderType.Phone)
                        return new ErrorDataResult<TwoFactorConfigurationDto>("Phone is already set as the preferred two-factor authentication method.", AppError.BadRequest());

                    bool isPhoneNumberConfirmed = user.Data.PhoneNumberConfirmed ?? false;
                    if (!isPhoneNumberConfirmed)
                        return new ErrorDataResult<TwoFactorConfigurationDto>
                            ("Phone number not confirmed. Please confirm your phone number before setting up two-factor authentication.", AppError.BadRequest());

                    return new SuccessDataResult<TwoFactorConfigurationDto>(
                        new TwoFactorConfigurationDto
                        {
                            Provider = provider,
                            SharedKey = "SMS setup not implemented yet"
                        });
                default:
                    _logger.LogWarning("Unsupported two-factor authentication provider: {Provider}", provider);
                    return new ErrorDataResult<TwoFactorConfigurationDto>("Unsupported two-factor authentication provider", AppError.BadRequest());

            }
        }

        public async Task<IResult> VerifyTwoFactorConfiguration(string userId, VerifyTwoFactorConfigurationDto verifyTwoFactorConfigurationDto)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorResult(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);

            var verificationResult = await _twoFactorService.VerifyTwoFactorConfigurationKey(userId, verifyTwoFactorConfigurationDto);
            if (verificationResult is ErrorResult errorResult)
            {
                return new ErrorResult(errorResult.Message!, errorResult.ErrorType!);
            }
            return new SuccessResult("Two-factor authentication verified successfully");
        }

        public async Task<IResult> SendChangeEmailLinkAsync(string userId, string email)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorResult(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);
            
            if (user.Data!.Email == email)
                return new ErrorResult("This email is the same as your email", AppError.BadRequest());

            var userExist = await _userRepository.UserExistByEmail(email); // Check if the new email is already registered
            if (userExist.Success)
                return new ErrorResult("This email has been registered", AppError.BadRequest());

            var changeEmailToken = await _emailTokenService.GenerateChangeEmailToken(user.Data, email);
            var frontendUrl = "http://localhost:4200/settings/change-email/change-email-confirmation";
            var emailContent = string.Empty;
            emailContent = $"{frontendUrl}?userId={changeEmailToken.Data!.UserId}&token={changeEmailToken.Data.Token}";
            var emailResult = await _emailService.SendAsync(email, "Change your email", emailContent);
            return new SuccessResult("Change email link sent successfully");
        }
    }
}
