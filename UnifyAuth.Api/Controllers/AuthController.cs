using Application.Common.Results.Concrete;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailDto confirmEmailDto)
        {
            var result = await _emailTokenService.ConfirmEmail(confirmEmailDto);

            if (result is ErrorResult errorResult)
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

            return Ok(new { Message = result.Message });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var loginResult = await _authService.LoginAsyncWithJWT(loginDto);
            if(loginResult is ErrorDataResult<TokenResultDto> errorDataResult)
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
                else if(errorDataResult.ErrorType == "Unauthorized")
                {
                    return Unauthorized(new { message = errorDataResult.Message });
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
            return Ok(new { hasRefreshCookie = !string.IsNullOrEmpty(cookie)});
        }
    }
}
