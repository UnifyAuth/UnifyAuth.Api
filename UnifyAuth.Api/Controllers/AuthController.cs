using Application.Common.Results.Concrete;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace UnifyAuth.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authManager;
        private readonly IEmailTokenService _emailTokenService;

        public AuthController(IAuthService authManager, IEmailTokenService emailTokenService)
        {
            _authManager = authManager;
            _emailTokenService = emailTokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var registerResult = await _authManager.RegisterAsync(registerDto);
            if (!registerResult.Success)
            {
                if (registerResult is ErrorResult errorResult)
                {
                    if (errorResult.ErrorType == "SystemError")
                    {
                        return StatusCode(500, errorResult.Message);
                    }
                    else if (errorResult.ErrorType == "BadRequest")
                    {
                        return BadRequest(new
                        {
                            message = errorResult.Message,
                            errorType = errorResult.ErrorType,
                            messages = errorResult.Messages
                        });
                    }
                }
            }
            return Ok(new { Message = registerResult.Message });
        }

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            var result = await _emailTokenService.ConfirmEmail(confirmEmailDto);
            if (!result.Success)
            {
                if (result is ErrorResult errorResult)
                {
                    if (errorResult.ErrorType == "SystemError")
                    {
                        return StatusCode(500, errorResult.Message);
                    }
                    else if (errorResult.ErrorType == "BadRequest")
                    {
                        return BadRequest(new
                        {
                            message = errorResult.Message,
                            errorType = errorResult.ErrorType,
                            messages = errorResult.Messages
                        });
                    }
                }
            }
            return Ok(new { Message = result.Message });
        }
    }
}
