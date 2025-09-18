using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class EmailTokenService : IEmailTokenService
    {
        private readonly UserManager<IdentityUserModel> _userManager;
        private readonly ILogger<EmailTokenService> _logger;
        private readonly IMapper _mapper;

        public EmailTokenService(UserManager<IdentityUserModel> userManager, ILogger<EmailTokenService> logger, IMapper mapper)
        {
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IResult> ConfirmEmail(ConfirmEmailTokenDto emailTokenDto)
        {
            var identityUser = await _userManager.FindByIdAsync(emailTokenDto.UserId.ToString());
            if (identityUser == null)
            {
                _logger.LogWarning("User not found with UserId: {UserId}", emailTokenDto.UserId);
                return new ErrorResult("User not found. Please register or check your mail", AppError.NotFound());
            }

            if ((identityUser.EmailConfirmed))
            {
                _logger.LogInformation("Email already confirmed for User: {UserEmail}", identityUser.Email);
                return new ErrorResult("Email Already Confirmed", AppError.BadRequest());
            }

            var decodedToken = Uri.UnescapeDataString(emailTokenDto.Token);
            var result = await _userManager.ConfirmEmailAsync(identityUser, decodedToken);

            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                {
                    _logger.LogWarning("Concurrency failure for User: {USerEmail}", identityUser.Email);
                    return new ErrorResult("Your account was changed at the same time, so this action could not be completed. Try again.", AppError.ConcurrencyFailure());
                }
                _logger.LogWarning("Invalid token for User: {UserEmail}", identityUser.Email);
                return new ErrorResult("Invalid token. Please request a new confirmation link.", AppError.BadRequest());
            }

            _logger.LogInformation("Email confirmed successfully for Email: {Email}", identityUser.Email);
            return new SuccessResult("Email confirmation successful");
        }

        public async Task<IDataResult<ConfirmEmailTokenDto>> GenerateEmailConfirmationToken(User user)
        {
            IdentityUserModel identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            var encodedToken = WebUtility.UrlEncode(token);
            ConfirmEmailTokenDto emailTokenDto = new ConfirmEmailTokenDto
            {
                UserId = identityUser.Id,
                Token = encodedToken
            };
            _logger.LogInformation("Email Confirmation Token Generated Successfully for UserEmail: {UserEmail}", identityUser.Email);
            return new SuccessDataResult<ConfirmEmailTokenDto>(emailTokenDto);
        }

        public async Task<IDataResult<ConfirmEmailTokenDto>> GenerateChangeEmailToken(User user, string newEmail)
        {
            IdentityUserModel identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GenerateChangeEmailTokenAsync(identityUser, newEmail);
            var encodedToken = WebUtility.UrlEncode(token);
            ConfirmEmailTokenDto emailTokenDto = new ConfirmEmailTokenDto
            {
                UserId = identityUser.Id,
                Token = encodedToken
            };
            _logger.LogInformation("Change email Token Generated Successfully for UserEmail: {UserEmail}", identityUser.Email);
            return new SuccessDataResult<ConfirmEmailTokenDto>(emailTokenDto);
        }

        public async Task<IResult> VerifyChangeEmailToken(ChangeEmailTokenDto changeEmailTokenDto)
        {
            var identityUser = await _userManager.FindByIdAsync(changeEmailTokenDto.UserId.ToString());
            if (identityUser == null)
            {
                _logger.LogWarning("User not found with UserId: {UserId}", changeEmailTokenDto.UserId);
                return new ErrorResult("User not found. Please register or check your mail", AppError.NotFound());
            }
            var decodedToken = Uri.UnescapeDataString(changeEmailTokenDto.Token);
            identityUser.UserName = changeEmailTokenDto.Email;
            var result = await _userManager.ChangeEmailAsync(identityUser, changeEmailTokenDto.Email!, decodedToken);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                {
                    _logger.LogWarning("Concurrency failure for UserId: {UserId}", changeEmailTokenDto.UserId);
                    return new ErrorResult("Your account was changed at the same time, so this action could not be completed. Try again.", AppError.ConcurrencyFailure());
                }
                _logger.LogWarning("Invalid token for UserId: {UserId}", changeEmailTokenDto.UserId);
                return new ErrorResult("Invalid token. Please request a new confirmation link.", AppError.BadRequest());
            }
            _logger.LogInformation("Email changed successfully for User: {Email}", identityUser.Email);
            return new SuccessResult("Email changed successfully");
        }
    }
}
