using Application.Common.Results.Concrete;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace UnifyAuth.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailTokenService _emailTokenService;
        private readonly ITokenService _tokenService;

        public AuthController(IAuthService authService, IEmailTokenService emailTokenService, ITokenService tokenService)
        {
            _authService = authService;
            _emailTokenService = emailTokenService;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto registerDto)
        {
            var registerResult = await _authService.RegisterAsync(registerDto);

            if (registerResult is ErrorResult errorResult)
            {
                if (errorResult.ErrorType == "BadRequest")
                {
                    return BadRequest(new
                    {
                        message = errorResult.Message,
                        errorType = errorResult.ErrorType,
                        messages = errorResult.Messages
                    });
                }
            }

            return Ok(new { Message = registerResult.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var loginResult = await _authService.LoginAsyncWithJWT(loginDto);
            if (loginResult is ErrorDataResult<TokenResultDto> errorDataResult)
            {
                if (errorDataResult.ErrorType == "BadRequest")
                {
                    return BadRequest(new
                    {
                        message = errorDataResult.Message,
                        errorType = errorDataResult.ErrorType,
                        messages = errorDataResult.Messages
                    });
                }
                else if (errorDataResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorDataResult.Message });
                }
                else if (errorDataResult.ErrorType == "BadRequest")
                {
                    return BadRequest(new { message = errorDataResult.Message });
                }
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = loginResult.Data.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", loginResult.Data.RefreshToken, cookieOptions);

            return Ok(loginResult.Data.AccessToken);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var oldRefreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(oldRefreshToken))
            {
                return Unauthorized(new { message = "Refresh token is missing." });
            }

            var refreshToken = await _authService.RefreshAccessToken(oldRefreshToken);
            if (refreshToken is ErrorDataResult<TokenResultDto> errorDataResult)
            {
                if (errorDataResult.ErrorType == "Unauthorized")
                {
                    return Unauthorized(new { message = errorDataResult.Message });
                }
            }
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = refreshToken.Data.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", refreshToken.Data.RefreshToken, cookieOptions);

            return Ok(refreshToken.Data.AccessToken);
        }

        [HttpGet("has-refresh-cookie")]
        public IActionResult HasRefreshCookie()
        {
            var cookie = Request.Cookies["refreshToken"];
            return Ok(new { hasRefreshCookie = !string.IsNullOrEmpty(cookie) });
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
                return NoContent();

            var logoutResult = await _authService.Logout(refreshToken);
            if (logoutResult is ErrorResult errorResult)
            {
                if (errorResult.ErrorType == "NotFound")
                    return NoContent();
            }
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UnixEpoch
            };
            Response.Cookies.Append("refreshToken", string.Empty, cookieOptions);

            return NoContent();
        }

        [HttpPost("send-reset-password-link")]
        public async Task<IActionResult> SendResetPasswordLink(EmailDto emailDto)
        {
            if (string.IsNullOrEmpty(emailDto.Email))
            {
                return BadRequest(new { message = "Email is required." });
            }
            var result = await _authService.SendResetPasswordLink(emailDto.Email);
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
            return Ok(new { message = "Reset password link sent successfully." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            if (resetPasswordDto == null || string.IsNullOrEmpty(resetPasswordDto.Token) || string.IsNullOrEmpty(resetPasswordDto.NewPassword))
            {
                return BadRequest(new { message = "Invalid reset password data." });
            }
            var result = await _authService.ResetPassword(resetPasswordDto);
            if (result is ErrorResult errorResult)
            {
                if (errorResult.ErrorType == "BadRequest")
                {
                    return BadRequest(new { message = errorResult.Message });
                }
                else if (errorResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorResult.Message });
                }
            }
            return Ok(new { message = "Password reset successfully." });
        }
    }
}
