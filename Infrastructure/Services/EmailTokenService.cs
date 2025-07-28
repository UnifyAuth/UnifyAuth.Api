using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Services;
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

        public EmailTokenService(UserManager<IdentityUserModel> userManager, ILogger<EmailTokenService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            //Debugging log
            _logger.LogDebug("Confirming email for UserId: {UserId} with Token: {Token}", confirmEmailDto.UserId, confirmEmailDto.Token);

            var identityUser = await _userManager.FindByIdAsync(confirmEmailDto.UserId.ToString());
            if (identityUser == null)
            {
                _logger.LogInformation("User not found with UserId: {UserId}", confirmEmailDto.UserId);
                return new ErrorResult("User not found. Please register or check your mail", "BadRequest");
            }

            if ((identityUser.EmailConfirmed))
            {
                _logger.LogInformation("Email already confirmed for UserId: {UserId}", confirmEmailDto.UserId);
                return new ErrorResult("Email Already Confirmed", "BadRequest");
            }

            var decodedToken = WebUtility.UrlDecode(confirmEmailDto.Token);
            var result = await _userManager.ConfirmEmailAsync(identityUser, decodedToken);

            if (!result.Succeeded)
            {
                var errors = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));
                _logger.LogWarning("Email confirmation failed for UserId: {UserId} with errors: {Errors}", confirmEmailDto.UserId, errors);
                return new ErrorResult($"Email confirmation failed \n{errors}","SystemError");
            }

            _logger.LogInformation("Email confirmed successfully for UserId: {UserId}", confirmEmailDto.UserId);
            return new SuccessResult("Email confirmation successful");
        }

        public async Task<IDataResult<ConfirmEmailDto>> GenerateEmailConfirmationToken(Guid userId)
        {
            //Debugging log
            _logger.LogDebug("Generating Email Confirmation Token with UserId: {UserId}", userId);

            var identityUser = await _userManager.FindByIdAsync(userId.ToString());
            if(identityUser == null)
            {
                _logger.LogInformation("User not found with UserId: {UserId}", userId);
                return new ErrorDataResult<ConfirmEmailDto>("User not found", "NotFound");
            }
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Email Confirmation Token Generation failed for User: {UserId} {Email}", identityUser.Id,identityUser.Email);
                return new ErrorDataResult<ConfirmEmailDto>("Failed to generate email confirmation token", "SystemError");
            }
            var encodedToken = WebUtility.UrlEncode(token);

            ConfirmEmailDto confirmEmailDto = new ConfirmEmailDto
            {
                UserId = userId,
                Token = encodedToken
            };
            _logger.LogInformation("Email Confirmation Token Generated Successfully for UserId: {UserId}", userId.ToString());
            return new SuccessDataResult<ConfirmEmailDto>(confirmEmailDto, "Email confirmation token generated successfully");
        }
    }
}
