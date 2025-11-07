using Application.DTOs;
using Application.Interfaces.Services;
using Infrastructure.Common.IdentityModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UnifyAuth.Api.Extensions;

namespace UnifyAuth.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly SignInManager<IdentityUserModel> _signInManager;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, SignInManager<IdentityUserModel> signInManager, IConfiguration config, ILogger<AuthController> logger)
        {
            _authService = authService;
            _signInManager = signInManager;
            _config = config;
            _logger = logger;
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

            if (result.Data!.IsTowFactorRequired)
            {
                return Results.Ok(new
                {
                    userId = result.Data.UserId,
                    isTwoFactorRequired = true,
                    provider = result.Data.Provider,
                    accessToken = string.Empty,
                });
            }
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.Data.TokenResultDto!.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", result.Data.TokenResultDto.RefreshToken, cookieOptions);
            return Results.Ok(new
            {
                isTwoFactorRequired = false,
                provider = string.Empty,
                accessToken = result.Data.TokenResultDto.AccessToken
            });
        }

        [HttpPost("login-2fa")]
        public async Task<IResult> LoginWith2FA(VerifyTwoFactorAuthenticationDto verifyTwoFactorAuthenticationDto)
        {
            var result = await _authService.VerifyTwoFactorAuthentication(verifyTwoFactorAuthenticationDto);
            if (!result.Success)
            {
                return result.ToProblemDetails();
            }
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.Data!.TokenResultDto!.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", result.Data.TokenResultDto.RefreshToken, cookieOptions);
            return Results.Ok(result.Data.TokenResultDto.AccessToken);
        }

        [HttpGet("login/google")]
        public IResult LoginGoogle([FromQuery] string origin)
        {
            if (!IsAllowedOrigin(origin))
            {
                _logger.LogWarning("Google login request from unknown origin: {origin}", origin);
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "BadRequest",
                    detail: "Origin is null or not allowed.",
                    type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
                    extensions: new Dictionary<string, object?>
                    {
                        ["error"] = "Origin is not allowed."
                    });

            }

            var redirectUrl = Url.Action(nameof(GoogleCallback));
            var props = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
            props.Items["origin"] = origin;

            return Results.Challenge(props, new[] { GoogleDefaults.AuthenticationScheme });
        }

        [HttpGet("login/google/callback")]
        public async Task<IResult> GoogleCallback(CancellationToken cancellationToken)
        {
            var authResult = await _signInManager.GetExternalLoginInfoAsync();
            if (authResult == null)
            {
                return Results.Content(PopupScript(null, false, error: "Login failed. Please try again."), "text/html");
            }

            var props = authResult.AuthenticationProperties ?? new AuthenticationProperties();
            props.Items.TryGetValue("origin", out var origin);

            if (string.IsNullOrWhiteSpace(origin) || !IsAllowedOrigin(origin))
            {
                _logger.LogWarning("Google callback request from unknown origin: {origin}", origin);
                return Results.Content(PopupScript(null, false, error: "Unauthorized origin!"), "text/html");
            }

            var userEmail = authResult.Principal.FindFirstValue(ClaimTypes.Email);
            var providerKey = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userEmail))
            {
                return Results.Content(PopupScript(origin, false, error: "Email not found. Please check your google account."), "text/html");
            }
            if (string.IsNullOrWhiteSpace(providerKey))
            {
                _logger.LogError("Google provider key is null");
                return Results.Content(PopupScript(origin, false, error: "Something went wrong. Please try again later."), "text/html");
            }

            var externalLoginDto = new ExternalLoginDto
            {
                Provider = authResult.LoginProvider,
                ProviderKey = authResult.ProviderKey,
                Email = authResult.Principal.FindFirstValue(ClaimTypes.Email)!,
                FirstName = authResult.Principal.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                LastName = authResult.Principal.FindFirstValue(ClaimTypes.Surname) ?? string.Empty,
                PhoneNumber = authResult.Principal.FindFirstValue(ClaimTypes.MobilePhone) ?? string.Empty
            };

            var result = await _authService.LoginWithGoogle(externalLoginDto);
            if (!result.Success)
            {
                return Results.Content(PopupScript(origin, false, error: result.Message ?? result.Messages!.FirstOrDefault()), "text/html");
            }

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = result.Data!.TokenResultDto!.RefreshTokenExpiration
            };
            Response.Cookies.Append("refreshToken", result.Data.TokenResultDto.RefreshToken, cookieOptions);
            return Results.Content(PopupScript(origin, true, jwt: result.Data.TokenResultDto.AccessToken.Token), "text/html");
        }

        private bool IsAllowedOrigin(string origin)
        {
            if (string.IsNullOrEmpty(origin)) return false;
            var allowedOrigin = _config["AllowedOrigin"];
            if (string.IsNullOrWhiteSpace(allowedOrigin))
            {
                return false;
            }
            bool isAllowedOrigin = string.Equals(origin, allowedOrigin, StringComparison.OrdinalIgnoreCase);
            return isAllowedOrigin;
        }

        private static string PopupScript(string? origin, bool success, string? jwt = null, string? error = null)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "google-login-result",
                success,
                jwt,
                error
            });

            var target = string.IsNullOrEmpty(origin) ? "null" : $"`{origin}`";

            return $@"
                <!doctype html>
                <html><body>
                <script>
                (function(){{
                  try {{
                    const data = {payload};
                    if (window.opener && {target} !== null) {{
                      window.opener.postMessage(data, {target});
                    }}
                  }} catch(e) {{}}
                  window.close();
                }})();
                </script>
                </body></html>";
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
                Expires = result.Data!.RefreshTokenExpiration
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

        [HttpPost("cookie-test/set")]
        public IResult TestCookieSet()
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddMinutes(5)
            };
            Response.Cookies.Append("testCookie", "testValue", cookieOptions);
            return Results.Ok();
        }

        [HttpGet("cookie-test/get")]
        public IResult TestCookieCheck()
        {
            var cookie = Request.Cookies["testCookie"];
            var hasTestCookie = !string.IsNullOrEmpty(cookie);
            return Results.Ok(new { hasTestCookie });
        }
    }
}
