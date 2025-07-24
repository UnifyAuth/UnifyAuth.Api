using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Services;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Identity;
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

        public EmailTokenService(UserManager<IdentityUserModel> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            var identityUser = await _userManager.FindByIdAsync(confirmEmailDto.UserId.ToString());
            if (identityUser == null)
            {
                return new ErrorResult("User not found. Please register or check your mail", "BadRequest");
            }

            if ((identityUser.EmailConfirmed))
            {
                return new ErrorResult("Email Already Confirmed", "BadRequest");
            }

            var decodedToken = WebUtility.UrlDecode(confirmEmailDto.Token);
            var result = await _userManager.ConfirmEmailAsync(identityUser, decodedToken);

            if (!result.Succeeded)
            {
                var errors = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));
                return new ErrorResult($"Email doğrulama başarısız \n{errors}","SystemError");
            }

            return new SuccessResult("Email doğrulama başarılı");
        }

        public async Task<IDataResult<ConfirmEmailDto>> GenerateEmailConfirmationToken(Guid userId)
        {
            var identityUser = await _userManager.FindByIdAsync(userId.ToString());
            if(identityUser == null)
            {
                return new ErrorDataResult<ConfirmEmailDto>("User not found", "NotFound");
            }
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(identityUser);
            if (string.IsNullOrEmpty(token))
            {
                return new ErrorDataResult<ConfirmEmailDto>("Failed to generate email confirmation token", "SystemError");
            }
            var encodedToken = WebUtility.UrlEncode(token);

            ConfirmEmailDto confirmEmailDto = new ConfirmEmailDto
            {
                UserId = userId,
                Token = encodedToken
            };
            return new SuccessDataResult<ConfirmEmailDto>(confirmEmailDto, "Email confirmation token generated successfully");
        }
    }
}
