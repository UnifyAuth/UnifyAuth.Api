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
                _logger.LogInformation("User not found with UserId: {UserId}", emailTokenDto.UserId);
                return new ErrorResult("User not found. Please register or check your mail", "NotFound");
            }

            if ((identityUser.EmailConfirmed))
            {
                _logger.LogInformation("Email already confirmed for UserId: {UserId}", emailTokenDto.UserId);
                return new ErrorResult("Email Already Confirmed", "BadRequest");
            }

            var decodedToken = Uri.UnescapeDataString(emailTokenDto.Token);
            var result = await _userManager.ConfirmEmailAsync(identityUser, decodedToken);

            if (!result.Succeeded)
            {
                if(result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    _logger.LogWarning("Invalid token for UserId: {UserId}", emailTokenDto.UserId);
                    return new ErrorResult("Invalid token. Please request a new confirmation link.", "InvalidToken");
                }
                else if(result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                {
                    _logger.LogWarning("Concurrency failure for UserId: {UserId}", emailTokenDto.UserId);
                    return new ErrorResult("Concurrency failure. Please try again later.", "ConcurrencyFailure");
                }
                _logger.LogWarning("Email confirmation failed for UserId: {UserId} with error: {Error}",
                    emailTokenDto.UserId, 
                    result.Errors.Select(e => e.Code.FirstOrDefault()));
                return new ErrorResult("An error occurred while email confirmation. Please try again later","SystemError");
            }

            _logger.LogInformation("Email confirmed successfully for Email: {Email}", identityUser.Email);
            return new SuccessResult("Email confirmation successful");
        }

        public async Task<IDataResult<ConfirmEmailTokenDto>> GenerateEmailConfirmationToken(User user)
        {
            IdentityUserModel identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Email Confirmation Token Generation failed for User: {UserId} {Email}", identityUser.Id,identityUser.Email);
                return new ErrorDataResult<ConfirmEmailTokenDto>("Failed to generate email confirmation token", "TokenGenerationError");
            }
            var encodedToken = WebUtility.UrlEncode(token);

            ConfirmEmailTokenDto emailTokenDto = new ConfirmEmailTokenDto
            {
                UserId = identityUser.Id,
                Token = encodedToken
            };
            _logger.LogInformation("Email Confirmation Token Generated Successfully for UserEmail: {UserEmail}", identityUser.Email);
            return new SuccessDataResult<ConfirmEmailTokenDto>(emailTokenDto, "Email confirmation token generated successfully");
        }

        public async Task<IDataResult<ConfirmEmailTokenDto>> GenerateChangeEmailToken(User user, string newEmail)
        {
            IdentityUserModel identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GenerateChangeEmailTokenAsync(identityUser,newEmail);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Email Confirmation Token Generation failed for User: {UserId} {Email}", identityUser.Id, identityUser.Email);
                return new ErrorDataResult<ConfirmEmailTokenDto>("Failed to generate email confirmation token", "TokenGenerationError");
            }
            var encodedToken = WebUtility.UrlEncode(token);

            ConfirmEmailTokenDto emailTokenDto = new ConfirmEmailTokenDto
            {
                UserId = identityUser.Id,
                Token = encodedToken
            };

            _logger.LogInformation("Change email Token Generated Successfully for UserEmail: {UserEmail}", identityUser.Email);
            return new SuccessDataResult<ConfirmEmailTokenDto>(emailTokenDto, "Change email token generated successfully");
        }

        public async Task<IResult> VerifyChangeEmailToken(ChangeEmailTokenDto changeEmailTokenDto)
        {
            var identityUser = await _userManager.FindByIdAsync(changeEmailTokenDto.UserId.ToString());
            if (identityUser == null)
            {
                _logger.LogWarning("User not found with UserId: {UserId}", changeEmailTokenDto.UserId);
                return new ErrorResult("User not found. Please register or check your mail", "NotFound");
            }
            var decodedToken = Uri.UnescapeDataString(changeEmailTokenDto.Token);
            identityUser.UserName = changeEmailTokenDto.Email;
            var result = await _userManager.ChangeEmailAsync(identityUser, changeEmailTokenDto.Email!, decodedToken);
            if (!result.Succeeded)
            {
                if (result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    _logger.LogWarning("Invalid token for UserId: {UserId}", changeEmailTokenDto.UserId);
                    return new ErrorResult("Invalid token. Please request a new confirmation link.", "InvalidToken");
                }
                else if (result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                {
                    _logger.LogWarning("Concurrency failure for UserId: {UserId}", changeEmailTokenDto.UserId);
                    return new ErrorResult("Concurrency failure. Please try again later.", "ConcurrencyFailure");
                }
                _logger.LogWarning("Email confirmation failed for UserId: {UserId} with error: {Error}",
                    changeEmailTokenDto.UserId,
                    result.Errors.Select(e => e.Code.FirstOrDefault()));
                return new ErrorResult("An error occurred while email confirmation. Please try again later", "SystemError");
            }
            _logger.LogInformation("Email changed successfully for User: {Email}", identityUser.Email);
            return new SuccessResult("Email changed successfully");
        }
    }
}
