using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
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
        private readonly IValidator<RegisterDto> _validator;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IEmailTokenService _emailTokenService;
        private readonly IEMailService _emailService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IValidator<RegisterDto> validator, IUserRepository userRepository, IMapper mapper, IEMailService emailService, IEmailTokenService emailTokenService, ILogger<AuthService> logger)
        {
            _validator = validator;
            _userRepository = userRepository;
            _mapper = mapper;
            _emailService = emailService;
            _emailTokenService = emailTokenService;
            _logger = logger;
        }

        public async Task<IResult> RegisterAsync(RegisterDto registerDto)
        {
            //Debugging log
            _logger.LogDebug("Registering user with email: {Email}", registerDto.Email);

            var validationResult = _validator.Validate(registerDto);
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
                if (errorDataResult.Messages != null && errorDataResult.Messages.Length > 1)
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


            var sendEmailConfirmationResult = await SendEmailConfirmationLinkAsync(result.Data.Id, registerDto.Email);
            if (sendEmailConfirmationResult is ErrorResult emailError)
            {
                return new ErrorResult(emailError.Message, emailError.ErrorType);
            }

            return new SuccessResult("User registered successfully");
        }

        private async Task<IResult> SendEmailConfirmationLinkAsync(Guid userId, string email)
        {
            var emailConfirmationToken = await _emailTokenService.GenerateEmailConfirmationToken(userId);
            var emailContent = string.Empty;
            if (emailConfirmationToken.Success)
            {
                var frontendUrl = "http://localhost:4200/confirm-email";
                emailContent = $"{frontendUrl}?userId={emailConfirmationToken.Data.UserId}&token={emailConfirmationToken.Data.Token}";
            }
            else
            {
                return new ErrorResult("Failed to generate email confirmation token", "SystemError");
            }
            var emailResult = await _emailService.SendAsync(email, "Confirm your email", emailContent);
            if (!emailResult.Success)
            {
                return new ErrorResult(emailResult.Message, "SystemError");
            }
            return new SuccessResult("Email confirmation link sent successfully");
        }
    }
}
