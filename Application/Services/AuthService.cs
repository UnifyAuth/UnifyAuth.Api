using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Common.Security;
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
    public class AuthService : IAuthService
    {
        private readonly IValidator<RegisterDto> _registerValidator;
        private readonly IValidator<LoginDto> _loginValidator;
        private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEMailService _emailService;
        private readonly IPasswordService _passwordService;
        private readonly ITwoFactorService _twoFactorService;

        public AuthService(IValidator<RegisterDto> registerValidator, IUserRepository userRepository, IMapper mapper, ILogger<AuthService> logger, IValidator<LoginDto> loginValidator, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, IEMailService emailService, IPasswordService passwordService, IValidator<ResetPasswordDto> resetPasswordValidator, ITwoFactorService twoFactorService)
        {
            _registerValidator = registerValidator;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
            _loginValidator = loginValidator;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _emailService = emailService;
            _passwordService = passwordService;
            _resetPasswordValidator = resetPasswordValidator;
            _twoFactorService = twoFactorService;
        }

        public async Task<IDataResult<LoginResponseDto>> LoginAsyncWithJWT(LoginDto loginDto)
        {
            var validationResult = _loginValidator.Validate(loginDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for {Email}: {Errors}",
                    loginDto.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorDataResult<LoginResponseDto>(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), AppError.BadRequest());
            }

            var userChecking = await _userRepository.CheckRegisteredUserAsync(loginDto.Email, loginDto.Password);
            if (userChecking is ErrorDataResult<User> errorDataResult)
                return new ErrorDataResult<LoginResponseDto>(errorDataResult.Message!, errorDataResult.ErrorType!);

            if (userChecking.Data!.TwoFactorEnabled)
            {
                var provider = userChecking.Data.Preferred2FAProvider;
                var sendResult = await GenerateAndSendTwoFactorCode(userChecking.Data, provider);
                if (!sendResult.Success)
                {
                    return new ErrorDataResult<LoginResponseDto>(sendResult.Message!, sendResult.ErrorType!);
                }
                return new SuccessDataResult<LoginResponseDto>(new LoginResponseDto
                {
                    UserId = userChecking.Data.Id.ToString(),
                    IsTowFactorRequired = true,
                    Provider = userChecking.Data.Preferred2FAProvider.ToString(),
                    TokenResultDto = null
                });
            }

            var tokenResultDto = await GenerateAccessTokenAndRefreshToken(userChecking.Data);
            var loginResponseDto = new LoginResponseDto
            {
                IsTowFactorRequired = false,
                Provider = string.Empty,
                TokenResultDto = tokenResultDto
            };

            return new SuccessDataResult<LoginResponseDto>(loginResponseDto);
        }

        public async Task<IDataResult<LoginResponseDto>> LoginWithGoogle(ExternalLoginDto externalLoginDto)
        {
            User user = new User();
            var userExist = await _userRepository.GetUserByExternalProviderAsync(externalLoginDto);
            if (!userExist.Success)
            {
                var userExistEmail = await _userRepository.UserExistByEmail(externalLoginDto.Email);
                if (userExistEmail.Success)
                {
                    return new ErrorDataResult<LoginResponseDto>("User with this email already exists. Please login using your email and password.", AppError.BadRequest());
                }

                var newUser = new User
                {
                    Email = externalLoginDto.Email,
                    FirstName = externalLoginDto.FirstName,
                    LastName = externalLoginDto.LastName,
                    PhoneNumber = externalLoginDto.PhoneNumber,
                    EmailConfirmed = true // Since it's from Google, we can consider email as confirmed
                };

                var createUserResult = await _userRepository.CreateUserWithExternalLogin(newUser, externalLoginDto);
                if (!createUserResult.Success)
                {
                    if(createUserResult.Messages != null && createUserResult.Messages.Length > 1)
                    {
                        return new ErrorDataResult<LoginResponseDto>(createUserResult.Messages, createUserResult.ErrorType!);
                    }
                    return new ErrorDataResult<LoginResponseDto>(createUserResult.Message!, createUserResult.ErrorType!);
                }
                user = createUserResult.Data!;
            }else
            {
                user = userExist.Data!;
            }

            var tokenResultDto = await GenerateAccessTokenAndRefreshToken(user);
            var loginResponseDto = new LoginResponseDto
            {
                IsTowFactorRequired = false,
                Provider = string.Empty,
                TokenResultDto = tokenResultDto
            };

            return new SuccessDataResult<LoginResponseDto>(loginResponseDto);
        }
        private async Task<IResult> GenerateAndSendTwoFactorCode(User user, AuthenticationProviderType provider)
        {
            switch (provider)
            {
                case AuthenticationProviderType.Authenticator:
                    // For authenticator app, no need to send code, user will get it from their app
                    return new SuccessResult("Use your authenticator app to get the code");
                case AuthenticationProviderType.Email:
                    var emailToken = await _twoFactorService.GenerateAuthenticationKey(user, provider);
                    var emailContent = $"Your email verification code is: {emailToken.Data}. Please use this code to complete your two-factor authentication setup.";
                    var emailResult = await _emailService.SendAsync(user.Email, "Two-Factor Authentication Code", emailContent);
                    return new SuccessResult("Two-factor code sent via email");
                case AuthenticationProviderType.Phone:
                    // Authentication via SMS is not implemented yet
                    return new SuccessResult("SMS provider is not implemented yet");
                default:
                    _logger.LogWarning("Unsupported two-factor authentication provider: {Provider}", provider);
                    return new ErrorDataResult<TwoFactorConfigurationDto>("Unsupported two-factor authentication provider", AppError.BadRequest());
            }
        }

        public async Task<IDataResult<LoginResponseDto>> VerifyTwoFactorAuthentication(VerifyTwoFactorAuthenticationDto verifyTwoFactorAuthenticationDto)
        {
            var user = await _userRepository.GetUserByIdAsync(verifyTwoFactorAuthenticationDto.UserId);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorDataResult<LoginResponseDto>(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);

            var verificationResult = await _twoFactorService.VerifyTwoFactorAuthenticationKey(verifyTwoFactorAuthenticationDto);
            if (verificationResult is ErrorResult errorResult)
                return new ErrorDataResult<LoginResponseDto>(errorResult.Message!, errorResult.ErrorType!);

            var tokenResultDto = await GenerateAccessTokenAndRefreshToken(user.Data!);
            return new SuccessDataResult<LoginResponseDto>(new LoginResponseDto
            {
                IsTowFactorRequired = false,
                Provider = string.Empty,
                TokenResultDto = tokenResultDto
            });
        }

        private async Task<TokenResultDto> GenerateAccessTokenAndRefreshToken(User user)
        {
            AccessToken accessToken = _tokenService.GenerateAccessToken(user);
            string refreshTokenString = _tokenService.GenerateRefreshToken();
            RefreshToken refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(15), // Set expiration to 15 days
                Created = DateTime.UtcNow
            };
            await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken);
            _logger.LogInformation("Refresh token created and stored database successfully for user: {Email}", user.Email);
            return new TokenResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                RefreshTokenExpiration = refreshToken.Expires
            };
        }

        public async Task<IResult> RegisterAsync(RegisterDto registerDto)
        {
            var validationResult = _registerValidator.Validate(registerDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for register: {Email} {Errors}",
                    registerDto.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), AppError.BadRequest());
            }

            User user = _mapper.Map<User>(registerDto);
            var result = await _userRepository.CreateUserAsync(user, registerDto.Password);
            if (result is ErrorDataResult<User> errorDataResult)
            {
                if (errorDataResult.Messages != null && errorDataResult.Messages!.Length > 1)
                {
                    return new ErrorResult(errorDataResult.Messages, errorDataResult.ErrorType!);
                }
                return new ErrorResult(errorDataResult.Message!, errorDataResult.ErrorType!);
            }

            _logger.LogInformation("User registered successfully with Email: {Email}", registerDto.Email);
            return new SuccessResult("User registered successfully");
        }

        public async Task<IDataResult<TokenResultDto>> RefreshAccessToken(string refreshTokenString)
        {
            var refreshToken = await _refreshTokenRepository.GetRefreshTokenByTokenAsync(refreshTokenString);
            if (refreshToken is ErrorDataResult<RefreshToken> errorDataResult)
                return new ErrorDataResult<TokenResultDto>(errorDataResult.Message!, errorDataResult.ErrorType!);

            var tokenValidationResult = _tokenService.ValidateRefreshTokenAsync(refreshToken.Data!);
            if (tokenValidationResult is ErrorResult errorResult)
                return new ErrorDataResult<TokenResultDto>(errorResult.Message!, errorResult.ErrorType!);

            var user = await _userRepository.GetUserByIdAsync(refreshToken.Data!.UserId.ToString());
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorDataResult<TokenResultDto>(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);

            var accessToken = _tokenService.GenerateAccessToken(user.Data!);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();
            refreshToken.Data.Token = newRefreshTokenString;
            refreshToken.Data.Expires = DateTime.UtcNow.AddDays(15); // Set expiration to 15 days
            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken.Data);

            return new SuccessDataResult<TokenResultDto>(new TokenResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Data.Token,
                RefreshTokenExpiration = refreshToken.Data.Expires,
            });
        }

        public async Task<IResult> Logout(string refreshTokenString)
        {
            var refreshToken = await _refreshTokenRepository.GetRefreshTokenByTokenAsync(refreshTokenString);
            if (refreshToken is null) return new ErrorResult("No refresh token found for user", AppError.NotFound());

            refreshToken.Data!.Revoked = true;
            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken.Data);
            return new SuccessResult("User logged out successfully");
        }

        public async Task<IResult> SendResetPasswordLink(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorResult(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);

            var resetPasswordToken = await _passwordService.GenerateResetPasswordToken(user.Data!);
            var frontendUrl = "http://localhost:4200/reset-password";
            var emailContent = string.Empty;
            emailContent = $"{frontendUrl}?userId={resetPasswordToken.Data!.UserId}&token={resetPasswordToken.Data.Token}";
            await _emailService.SendAsync(email, "Reset Password", emailContent);

            return new SuccessResult("Reset password link sent successfully");
        }

        public async Task<IResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var validationResult = _resetPasswordValidator.Validate(resetPasswordDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for reset password: {Errors}",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), AppError.Validation());
            }
            var user = await _userRepository.GetUserByIdAsync(resetPasswordDto.UserId);
            if (user is ErrorDataResult<User> userErrorDataResult)
                return new ErrorResult(userErrorDataResult.Message!, userErrorDataResult.ErrorType!);


            var resetPasswordResult = await _passwordService.ResetPassword(user.Data!, resetPasswordDto.Token, resetPasswordDto.NewPassword);
            if (resetPasswordResult is ErrorResult resetPasswordErrorResult)
            {
                return new ErrorResult(resetPasswordErrorResult.Message!, resetPasswordErrorResult.ErrorType!);
            }
            return new SuccessResult("Password reset successfully");
        }
    }
}
