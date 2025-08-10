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
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IEmailTokenService _emailTokenService;
        private readonly IEMailService _emailService;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthService(IValidator<RegisterDto> registerValidator, IUserRepository userRepository, IMapper mapper, IEMailService emailService, IEmailTokenService emailTokenService, ILogger<AuthService> logger, IValidator<LoginDto> loginValidator, ITokenService tokenService, IRefreshTokenRepository refreshTokenRepository)
        {
            _registerValidator = registerValidator;
            _userRepository = userRepository;
            _mapper = mapper;
            _emailService = emailService;
            _emailTokenService = emailTokenService;
            _logger = logger;
            _loginValidator = loginValidator;
            _tokenService = tokenService;
            _refreshTokenRepository = refreshTokenRepository;
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
            {
                if (errorDataResult.ErrorType == "NotFound")
                {
                    _logger.LogInformation("User not found for email: {Email}", loginDto.Email);
                    return new ErrorDataResult<TokenResultDto>("User not found", "NotFound");
                }
                else if (errorDataResult.ErrorType == "Unauthorized")
                {
                    _logger.LogInformation("Invalid password for email: {Email}", loginDto.Email);
                    return new ErrorDataResult<TokenResultDto>("Invalid password", "Unauthorized");
                }
            }

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
                _logger.LogInformation("Validation failed for {Email}: {Errors}",
                    registerDto.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), "BadRequest");
            }

            //Debugging log
            _logger.LogDebug("Mapping RegisterDto to User entity for {Email}", registerDto.Email);
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
            await _tokenService.UpdateRefreshToken(refreshToken, newRefreshTokenString);
            
            return new SuccessDataResult<TokenResultDto>(new TokenResultDto
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshTokenString,
                RefreshTokenExpiration = DateTime.UtcNow.AddDays(15),
            });
        }

        private async Task<IResult> SendEmailConfirmationLinkAsync(Guid userId, string email)
        {
            var emailConfirmationToken = await _emailTokenService.GenerateEmailConfirmationToken(userId);
            var emailContent = string.Empty;
            if (emailConfirmationToken is ErrorDataResult<ConfirmEmailDto> errorDataResult)
            {
                _logger.LogError("Failed to generate email confirmation token for user {UserId}: {Error}", userId, errorDataResult.Message);
                return new ErrorResult(errorDataResult.Message, errorDataResult.ErrorType);
            }

            var frontendUrl = "http://localhost:4200/confirm-email";
            emailContent = $"{frontendUrl}?userId={emailConfirmationToken.Data.UserId}&token={emailConfirmationToken.Data.Token}";

            await _emailService.SendAsync(email, "Confirm your email", emailContent);

            return new SuccessResult("Email confirmation link sent successfully");
        }
    }
}
