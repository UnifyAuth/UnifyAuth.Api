using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
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
        private readonly IEmailTokenService _emailTokenService;
        private readonly IPasswordService _passwordService;

        public AuthService(IValidator<RegisterDto> registerValidator, IUserRepository userRepository, IMapper mapper, ILogger<AuthService> logger, IValidator<LoginDto> loginValidator, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository, IEMailService emailService, IEmailTokenService emailTokenService, IPasswordService passwordService, IValidator<ResetPasswordDto> resetPasswordValidator)
        {
            _registerValidator = registerValidator;
            _userRepository = userRepository;
            _mapper = mapper;
            _logger = logger;
            _loginValidator = loginValidator;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
            _emailService = emailService;
            _emailTokenService = emailTokenService;
            _passwordService = passwordService;
            _resetPasswordValidator = resetPasswordValidator;
        }

        public async Task<IDataResult<TokenResultDto>> LoginAsyncWithJWT(LoginDto loginDto)
        {
            _logger.LogDebug("User login with email: {Email}", loginDto.Email);

            var validationResult = _loginValidator.Validate(loginDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for {Email}: {Errors}",
                    loginDto.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorDataResult<TokenResultDto>(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), "BadRequest");
            }

            var userChecking = await _userRepository.CheckRegisteredUserAsync(loginDto.Email, loginDto.Password);
            if (userChecking is ErrorDataResult<User> errorDataResult)
               return new ErrorDataResult<TokenResultDto>(errorDataResult.Message, errorDataResult.ErrorType);
            

            AccessToken accessToken = await _tokenService.GenerateAccessToken(userChecking.Data);
            _logger.LogInformation("JWT token created successfully for user: {Email}", loginDto.Email);

            string refreshTokenString = _tokenService.GenerateRefreshToken();
            RefreshToken refreshToken = new RefreshToken
            {
                Token = refreshTokenString,
                UserId = userChecking.Data.Id,
                Expires = DateTime.UtcNow.AddDays(15), // Set expiration to 15 days
                Created = DateTime.UtcNow
            };
            await _refreshTokenRepository.AddRefreshTokenAsync(refreshToken);
            _logger.LogInformation("Refresh token created and stored database successfully for user: {Email}", loginDto.Email);

            TokenResultDto tokenResultDto = new TokenResultDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenString,
                RefreshTokenExpiration = refreshToken.Expires
            };

            return new SuccessDataResult<TokenResultDto>(tokenResultDto);
        }

        public async Task<IResult> RegisterAsync(RegisterDto registerDto)
        {
            //Debugging log
            _logger.LogDebug("Registering user with email: {Email}", registerDto.Email);

            var validationResult = _registerValidator.Validate(registerDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for register: {Email} {Errors}",
                    registerDto.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), "BadRequest");
            }

            User user = _mapper.Map<User>(registerDto);
            var result = await _userRepository.CreateUserAsync(user, registerDto.Password);
            if (result is ErrorDataResult<User> errorDataResult)
            {
                if (errorDataResult.Messages.Length > 1)
                {
                    _logger.LogInformation("User creation failed for {Email} with multiple errors: {Errors}",
                        registerDto.Email,
                        errorDataResult.Messages);
                    return new ErrorResult(errorDataResult.Messages, errorDataResult.ErrorType);
                }
                _logger.LogInformation("User creation failed for {Email} with error: {Error}",
                    registerDto.Email,
                    errorDataResult.Message);
                return new ErrorResult(errorDataResult.Message, errorDataResult.ErrorType);
            }
            
            _logger.LogInformation("User registered successfully with Email: {Email}", registerDto.Email);
            return new SuccessResult("User registered successfully");
        }

        public async Task<IDataResult<TokenResultDto>> RefreshAccessToken(string refreshTokenString)
        {
            _logger.LogDebug("Refreshing access token for refresh token: {RefreshToken}", refreshTokenString);

            var refreshToken = await _refreshTokenRepository.GetRefreshTokenByTokenAsync(refreshTokenString);
            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", refreshTokenString);
                return new ErrorDataResult<TokenResultDto>("Refresh token not found", "Unauthorized");
            }

            var tokenValidationResult = await _tokenService.ValidateRefreshTokenAsync(refreshToken);
            if (tokenValidationResult is ErrorResult errorResult)
            {
                return new ErrorDataResult<TokenResultDto>(errorResult.Message, errorResult.ErrorType);
            }

            var identityUser = await _userRepository.GetUserByIdAsync(refreshToken.UserId.ToString());
            var user = _mapper.Map<User>(identityUser.Data);
            var accessToken = await _tokenService.GenerateAccessToken(user);
            var newRefreshTokenString = _tokenService.GenerateRefreshToken();

            refreshToken.Token = newRefreshTokenString;
            refreshToken.Expires = DateTime.UtcNow.AddDays(15); // Set expiration to 15 days

            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken);

            return new SuccessDataResult<TokenResultDto>(new TokenResultDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenString,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(15),
            });
        }

        public async Task<IResult> Logout(string refreshTokenString)
        {
            _logger.LogDebug("Logging out for refresh token: {RefreshToken}", refreshTokenString);
            var refreshToken = await _refreshTokenRepository.GetRefreshTokenByTokenAsync(refreshTokenString);
            if(refreshToken is null) return new ErrorResult("No refresh token found for user", "NotFound");

            refreshToken.Revoked = true;
            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken);
            return new SuccessResult("User logged out successfully");
        }

        public async Task<IResult> SendResetPasswordLink(string email)
        {
            var user = await _userRepository.GetUserByEmailAsync(email);
            if (user is ErrorDataResult<User> userErrorDataResult)
            {
                if (userErrorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogInformation("User not found with email: {Email}", email);
                    return new ErrorResult(userErrorDataResult.Message, userErrorDataResult.ErrorType);
                }
            }

            var resetPasswordToken = await _emailTokenService.GenerateResetPasswordToken(user.Data);
            if (resetPasswordToken is ErrorDataResult<ResetPasswordLinkDto> tokenErrorDataResult)
                return new ErrorResult(tokenErrorDataResult.Message, tokenErrorDataResult.ErrorType);

            var frontendUrl = "http://localhost:4200/reset-password";
            var emailContent = string.Empty;
            emailContent = $"{frontendUrl}?userId={resetPasswordToken.Data.UserId}&token={resetPasswordToken.Data.Token}";

            var emailResult = await _emailService.SendAsync(email, "Reset Password", emailContent);
            if (emailResult is ErrorResult mailErrorResult) return new ErrorResult(mailErrorResult.Message, mailErrorResult.ErrorType);

            return new SuccessResult("Reset password link sent successfully");
        }

        public async Task<IResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            var validationResult = _resetPasswordValidator.Validate(resetPasswordDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for reset password: {Errors}",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), "BadRequest");
            }
            var user = await _userRepository.GetUserByIdAsync(resetPasswordDto.UserId);
            if(user is ErrorDataResult<User> userErrorDataResult)
            {
                if(userErrorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogWarning("User not found with UserId: {UserId}", resetPasswordDto.UserId);
                    return new ErrorResult(userErrorDataResult.Message, userErrorDataResult.ErrorType);
                }
            }

            var resetPasswordResult = await _passwordService.ResetPassword(user.Data,resetPasswordDto.Token,resetPasswordDto.NewPassword);
            if(resetPasswordResult is ErrorResult resetPasswordErrorResult)
            {
                if(resetPasswordErrorResult.ErrorType == "BadRequest")
                {
                    return new ErrorResult(resetPasswordErrorResult.Message, resetPasswordErrorResult.ErrorType);
                }
            }
            return new SuccessResult("Password reset successfully");
        }
    }
}
