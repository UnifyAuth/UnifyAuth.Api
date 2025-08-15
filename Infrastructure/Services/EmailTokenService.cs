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

        public async Task<IResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            //Debugging log
            _logger.LogDebug("Confirming email for UserId: {UserId} with Token: {Token}", confirmEmailDto.UserId, confirmEmailDto.Token);

            var identityUser = await _userManager.FindByIdAsync(confirmEmailDto.UserId.ToString());
            if (identityUser == null)
            {
                _logger.LogInformation("User not found with UserId: {UserId}", confirmEmailDto.UserId);
                return new ErrorResult("User not found. Please register or check your mail", "NotFound");
            }

            if ((identityUser.EmailConfirmed))
            {
                _logger.LogInformation("Email already confirmed for UserId: {UserId}", confirmEmailDto.UserId);
                return new ErrorResult("Email Already Confirmed", "BadRequest");
            }

            var decodedToken = Uri.UnescapeDataString(confirmEmailDto.Token);
            var result = await _userManager.ConfirmEmailAsync(identityUser, decodedToken);

            if (!result.Succeeded)
            {
                if(result.Errors.Any(e => e.Code == "InvalidToken"))
                {
                    _logger.LogWarning("Invalid token for UserId: {UserId}", confirmEmailDto.UserId);
                    return new ErrorResult("Invalid token. Please request a new confirmation link.", "InvalidToken");
                }
                else if(result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
                {
                    _logger.LogWarning("Concurrency failure for UserId: {UserId}", confirmEmailDto.UserId);
                    return new ErrorResult("Concurrency failure. Please try again later.", "ConcurrencyFailure");
                }
                _logger.LogWarning("Email confirmation failed for UserId: {UserId} with error: {Error}", 
                    confirmEmailDto.UserId, 
                    result.Errors.Select(e => e.Code.FirstOrDefault()));
                return new ErrorResult("An error occurred while email confirmation. Please try again later","SystemError");
            }

            _logger.LogInformation("Email confirmed successfully for UserId: {UserId}", confirmEmailDto.UserId);
            return new SuccessResult("Email confirmation successful");
        }

        public async Task<IDataResult<ConfirmEmailDto>> GenerateEmailConfirmationToken(User user)
        {
            //Debugging log
            _logger.LogDebug("Generating Email Confirmation Token with UserEmail: {UserEmail}", user.Email);

            IdentityUserModel identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Email Confirmation Token Generation failed for User: {UserId} {Email}", identityUser.Id,identityUser.Email);
                return new ErrorDataResult<ConfirmEmailDto>("Failed to generate email confirmation token", "TokenGenerationError");
            }
            var encodedToken = WebUtility.UrlEncode(token);

            ConfirmEmailDto confirmEmailDto = new ConfirmEmailDto
            {
                UserId = identityUser.Id,
                Token = encodedToken
            };
            _logger.LogInformation("Email Confirmation Token Generated Successfully for UserEmail: {UserEmail}", identityUser.Email);
            return new SuccessDataResult<ConfirmEmailDto>(confirmEmailDto, "Email confirmation token generated successfully");
        }

        public async Task<IDataResult<ResetPasswordLinkDto>> GenerateResetPasswordToken(User user)
        {
            //Debugging log
            _logger.LogDebug("Generating Reset Password Token with UserEmail: {UserEmail}", user.Email);
           
            IdentityUserModel identityUser = _mapper.Map<IdentityUserModel>(user);
            var token = await _userManager.GeneratePasswordResetTokenAsync(identityUser);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Reset Password Token Generation failed for User: {UserId} {Email}", identityUser.Id, identityUser.Email);
                return new ErrorDataResult<ResetPasswordLinkDto>("Failed to generate reset password token", "TokenGenerationError");
            }
            var encodedToken = WebUtility.UrlEncode(token);
            ResetPasswordLinkDto resetPasswordDto = new ResetPasswordLinkDto
            {
                UserId = user.Id,
                Token = encodedToken
            };
            _logger.LogInformation("Reset Password Token Generated Successfully for UserEmail: {UserEmail}", user.Email);
            return new SuccessDataResult<ResetPasswordLinkDto>(resetPasswordDto, "Reset password token generated successfully");
        }
    }
}
