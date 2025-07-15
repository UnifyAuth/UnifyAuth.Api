using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using FluentValidation;
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

        public AuthService(IValidator<RegisterDto> validator, IUserRepository userRepository, IMapper mapper, IEMailService emailService, IEmailTokenService emailTokenService)
        {
            _validator = validator;
            _userRepository = userRepository;
            _mapper = mapper;
            _emailService = emailService;
            _emailTokenService = emailTokenService;
        }

        public async Task<IResult> RegisterAsync(RegisterDto registerDto)
        {
            var validationResult = _validator.Validate(registerDto);
            if (!validationResult.IsValid)
            {
                var errorResults = validationResult.ToDictionary();
                return new ErrorResult("Registration failed. Please check the inputs.", errorResults, "BadRequest");
            }
            User user = _mapper.Map<User>(registerDto);
            var result = await _userRepository.CreateUserAsync(user, registerDto.Password);
            if (!result.Success)
            {
                if (result is ErrorDataResult<User> errorDataResult)
                {
                    return new ErrorResult(errorDataResult.Message, errorDataResult.ErrorType);
                }
            }

            var emailConfirmationToken = await _emailTokenService.GenerateEmailConfirmationToken(result.Data.Id);
            var emailContent = string.Empty;
            if (emailConfirmationToken.Success)
            {
                emailContent = $"UserId: {emailConfirmationToken.Data.UserId}/nToken: {emailConfirmationToken.Data.Token}";
            }
            else
            {
                return new ErrorResult("Failed to generate email confirmation token", "SystemError");
            }
            var emailResult = await _emailService.SendAsync(registerDto.Email, "Confirm your email", emailContent);
            if (!emailResult.Success)
            {
                return new ErrorResult(emailResult.Message, "SystemError");
            }

            return new SuccessResult("User registered successfully");
        }
    }
}
