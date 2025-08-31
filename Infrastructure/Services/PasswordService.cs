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
    public class PasswordService : IPasswordService
    {
        private readonly UserManager<IdentityUserModel> _userManager;
        private readonly ILogger<PasswordService> _logger;
        private readonly IMapper _mapper;

        public PasswordService(UserManager<IdentityUserModel> userManager, ILogger<PasswordService> logger, IMapper mapper)
        {
            _userManager = userManager;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<IResult> ResetPassword(User user, string token, string newPassword)
        {
            // Debugging log
            _logger.LogDebug("Resetting password for Email: {Email} with Token: {Token}", user.Email, token);
            var identityUser = await _userManager.FindByIdAsync(user.Id.ToString());
            if (identityUser == null)
            {
                _logger.LogInformation("User not found with UserId: {UserId}", user.Id);
                return new ErrorResult("User not found. Please register or check your mail", "NotFound");
            }

            var isSame = await _userManager.CheckPasswordAsync(identityUser, newPassword);
            if (isSame)
            {
                _logger.LogDebug("The new password is the same as the old password for Email: {Email}", user.Email);
                return new ErrorResult("The new password cannot to be the same as the old password","BadRequest");
            }

            var result = await _userManager.ResetPasswordAsync(identityUser, token, newPassword);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed to reset password for Email: {Email} with error: {Error}", 
                    user.Email, 
                    result.Errors.Select(e => e.Description).FirstOrDefault());
                return new ErrorResult(result.Errors.Select(e => e.Description).FirstOrDefault(), "BadRequest");
            }

            return new SuccessResult("Password reset successfully");
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
