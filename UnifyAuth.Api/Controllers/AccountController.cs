using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using UnifyAuth.Api.Extensions;

namespace UnifyAuth.Api.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IEmailTokenService _emailTokenService;

        public AccountController(IAccountService accountService, IEmailTokenService emailTokenService)
        {
            _accountService = accountService;
            _emailTokenService = emailTokenService;
        }

        [HttpGet("profile")]
        public async Task<IResult> GetUserProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _accountService.GetUserInfos(userId!);

            if (!result.Success)
                return result.ToProblemDetails();

            return Results.Ok(result.Data);
        }

        [HttpPut("edit-profile")]
        public async Task<IResult> UpdateUserProfile([FromBody] UserUpdateDto userUpdateDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userUpdateDto.Id != Guid.Parse(userId!))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You are not allowed to update this user.",
                    type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "User not authorized to update this profile"
                    });
            }
            var result = await _accountService.UpdateUserAsync(userUpdateDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            return Results.Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("send-email-confirmation-link")]
        public async Task<IResult> SendEmailConfirmationLink([FromBody] EmailDto emailDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _accountService.SendEmailConfirmationLinkAsync(userId!, emailDto.Email);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            return Results.Ok(new { message = "Email confirmation link sent successfully" });
        }

        [HttpPost("confirm-email")]
        public async Task<IResult> ConfirmEmail(ConfirmEmailTokenDto confirmEmailDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (confirmEmailDto.UserId != Guid.Parse(userId!))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You are not allowed to update this user.",
                    type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "User not authorized to update this profile"
                    });
            }
            var result = await _emailTokenService.ConfirmEmail(confirmEmailDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }

            return Results.Ok(new { Message = result.Message });
        }

        [HttpPost("configure-2fa")]
        public async Task<IResult> ConfigureTwoFactorAuthentication([FromQuery, BindRequired] AuthenticationProviderType provider)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _accountService.ConfigureTwoFactorAsync(userId!, provider);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            return Results.Ok(result.Data);
        }

        [HttpPost("verify-2fa-configuration")]
        public async Task<IResult> VerifyTwoFactorConfiguration([FromBody] VerifyTwoFactorConfigurationDto verifyTwoFactorConfigurationDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var result = await _accountService.VerifyTwoFactorConfiguration(userId, verifyTwoFactorConfigurationDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            return Results.Ok(new { message = "Two-factor authentication verified successfully" });
        }

        [HttpPost("send-change-email-link")]
        public async Task<IResult> SendChangeEmailLink(EmailDto email)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var result = await _accountService.SendChangeEmailLinkAsync(userId, email.Email);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            return Results.Ok(new { message = "Change email link sent successfully" });
        }

        [HttpPost("verify-change-email")]
        public async Task<IResult> VerifyChangeEmail(ChangeEmailTokenDto changeEmailTokenDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (changeEmailTokenDto.UserId != Guid.Parse(userId!))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Forbidden",
                    detail: "You are not allowed to update this user.",
                    type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.3",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "User not authorized to update this profile"
                    });
            }
            var result = await _emailTokenService.VerifyChangeEmailToken(changeEmailTokenDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }

            return Results.Ok(new { Message = result.Message });
        }
    }
}
