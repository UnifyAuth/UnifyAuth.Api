using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Services;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if(string.IsNullOrEmpty(userId))
            {
                return NotFound(new { message = "User nof found" });
            }
            var result = await _accountService.GetUserInfos(userId);

            if (result is ErrorDataResult<User> errorDataResult)
            {
                if (errorDataResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorDataResult.Message });
                }
            }
            return Ok(result.Data);
        }

        [HttpPut("edit-profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserDto userDto)
        {
            if (userDto == null || string.IsNullOrEmpty(userDto.Id.ToString()) || userDto.Id.ToString() == "00000000-0000-0000-0000-000000000000")
            {
                return BadRequest(new { message = "Invalid user data" });
            }
            var result = await _accountService.UpdateUserAsync(userDto);

            if (result is ErrorResult errorResult)
            {
                if (errorResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorResult.Message });
                }
                else if(errorResult.ErrorType == "BadRequest")
                {
                    return BadRequest(new { message = errorResult.Messages });
                }
            }
            return Ok(new { message = "Profile updated successfully" });
        }

        [HttpPost("send-email-confirmation-link")]
        public async Task<IActionResult> SendEmailConfirmationLink([FromBody] EmailDto emailDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound(new { message = "User not found" });
            }

            var result = await _accountService.SendEmailConfirmationLinkAsync(Guid.Parse(userId), emailDto.Email);
            if (result is ErrorResult errorResult)
            {
                if (errorResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorResult.Message });
                }
                else if (errorResult.ErrorType == "TokenGenerationError")
                {
                    return StatusCode(500, new { message = errorResult.Message });
                }
                else if (errorResult.ErrorType == "EmailError")
                {
                    return StatusCode(500, new { message = errorResult.Message });
                }
            }
            return Ok(new { message = "Email confirmation link sent successfully" });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            if (confirmEmailDto == null || string.IsNullOrEmpty(confirmEmailDto.UserId.ToString()) || string.IsNullOrEmpty(confirmEmailDto.Token))
            {
                return BadRequest(new { message = "Invalid confirmation data" });
            }
            var result = await _emailTokenService.ConfirmEmail(confirmEmailDto);

            if (result is ErrorResult errorResult)
            {
                if (errorResult.ErrorType == "BadRequest")
                {
                    return BadRequest(new { message = errorResult.Message});
                }
                else if(errorResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorResult.Message });
                }
                else if (errorResult.ErrorType == "InvalidToken")
                {
                    return BadRequest(new { message = errorResult.Message });
                }
                else if(errorResult.ErrorType == "ConcurrencyFailure")
                {
                    return StatusCode(409, new { message = errorResult.Message });
                }
                return StatusCode(500, new { message = errorResult.Message });
            }

            return Ok(new { Message = result.Message });
        }
    }
}
