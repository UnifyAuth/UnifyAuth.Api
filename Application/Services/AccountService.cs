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
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AccountService> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<UserDto> _userValidator;
        private readonly IEMailService _emailService;
        private readonly IEmailTokenService _emailTokenService;

        public AccountService(IUserRepository userRepository, ILogger<AccountService> logger, IMapper mapper, IValidator<UserDto> userValidator, IEmailTokenService emailTokenService, IEMailService emailService)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _userValidator = userValidator;
            _emailTokenService = emailTokenService;
            _emailService = emailService;
        }

        public async Task<IDataResult<UserDto>> GetUserInfos(string userId)
        {
            var result = await _userRepository.GetUserByIdAsync(userId);
            if(result is ErrorDataResult<User> errorDataResult)
            {
                _logger.LogInformation("Error retrieving user info for userId {UserId}: {Error}", userId, errorDataResult.Message);
                return new ErrorDataResult<UserDto>(errorDataResult.Message, errorDataResult.ErrorType);
            }

            UserDto userDto = _mapper.Map<UserDto>(result.Data);
            return new SuccessDataResult<UserDto>(userDto, "User information retrieved successfully");
        }

        public async Task<IResult> UpdateUserAsync(UserDto userDto)
        {
            var validationResult = _userValidator.Validate(userDto);
            if (!validationResult.IsValid)
            {
                _logger.LogInformation("Validation failed for user update: {Email} {Errors}",
                    userDto.Email,
                    validationResult.Errors.Select(e => e.ErrorMessage).ToArray());
                return new ErrorResult(validationResult.Errors.Select(e => e.ErrorMessage).ToArray(), "BadRequest");
            }

            User user = _mapper.Map<User>(userDto);
            var result = await _userRepository.UpdateUserAsync(user);
            if (result is ErrorResult errorResult)
            {
                if (errorResult.Messages.Length > 1)
                {
                    _logger.LogInformation("User updating failed for {Email} with multiple errors: {Errors}",
                        userDto.Email,
                        errorResult.Messages);
                    return new ErrorResult(errorResult.Messages, errorResult.ErrorType);
                }
                _logger.LogInformation("User updating failed for {Email} with error: {Error}",
                    userDto.Email,
                    errorResult.Message);
                return new ErrorResult(errorResult.Message, errorResult.ErrorType);
            }

            _logger.LogInformation("User updated successfully with Email: {Email}", userDto.Email);
            return new SuccessResult("User updated successfully");
        }

        public async Task<IResult> SendEmailConfirmationLinkAsync(Guid userId, string email)
        {
            var user = await _userRepository.GetUserByIdAsync(userId.ToString());
            if(user is ErrorDataResult<User> userErrorDataResult)
            {
                if(userErrorDataResult.ErrorType == "NotFound")
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
            if(emailResult is ErrorResult mailErrorResult)
            {
                return new ErrorResult(mailErrorResult.Message, mailErrorResult.ErrorType);
            }

            return new SuccessResult("Email confirmation link sent successfully");
        }
    }
}
