using Application.Common.Results.Concrete;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using UnifyAuth.Api.Extensions;

namespace UnifyAuth.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IResult> Register(RegisterDto registerDto)
        {
            var result = await _authService.RegisterAsync(registerDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            return Results.Ok();
        }

        [HttpPost("login")]
        public async Task<IResult> Login(LoginDto loginDto)
        {
            var result = await _authService.LoginAsyncWithJWT(loginDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.Data.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", result.Data.RefreshToken, cookieOptions);

            return Results.Ok(result.Data.AccessToken);
        }

        [HttpPost("refresh-token")]
        public async Task<IResult> RefreshToken()
        {
            var oldRefreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(oldRefreshToken))
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Refresh token is missing.",
                    type: "https://datatracker.ietf.org/doc/html/rfc7235#section-3.1",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "Refresh token is missing."
                    });
            }

            var result = await _authService.RefreshAccessToken(oldRefreshToken);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.Data.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", result.Data.RefreshToken, cookieOptions);

            return Results.Ok(result.Data.AccessToken);
        }

        [HttpGet("has-refresh-cookie")]
        public IResult HasRefreshCookie()
        {
            var cookie = Request.Cookies["refreshToken"];
            return Results.Ok(new { hasRefreshCookie = !string.IsNullOrEmpty(cookie) });
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var result = await _authService.Logout(refreshToken!);
            if (!result.Success)
                return result.ToProblemDetails();
            
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UnixEpoch
            };
            Response.Cookies.Append("refreshToken", string.Empty, cookieOptions);

            return Results.NoContent();
        }

        [HttpPost("send-reset-password-link")]
        public async Task<IResult> SendResetPasswordLink(EmailDto emailDto)
        {
            if (string.IsNullOrEmpty(emailDto.Email))
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "BadRequest",
                    detail: "Email is required",
                    type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "Email is missing."
                    });

            var result = await _authService.SendResetPasswordLink(emailDto.Email);
            if (!result.Success)
                return result.ToProblemDetails();

            return Results.Ok(new { message = "Reset password link sent successfully." });
        }

        [HttpPost("reset-password")]
        public async Task<IResult> ResetPassword(ResetPasswordDto resetPasswordDto)
        {
            if (resetPasswordDto == null || string.IsNullOrEmpty(resetPasswordDto.Token) || string.IsNullOrEmpty(resetPasswordDto.NewPassword))
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "BadRequest",
                    detail: "New password is required",
                    type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "New password is required."
                    }
                    );
            
            var result = await _authService.ResetPassword(resetPasswordDto);
            if (!result.Success)
                return result.ToProblemDetails();
            
            return Results.Ok(new { message = "Password reset successfully." });
        }
    }
}
