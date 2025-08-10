using Application.Common.Results.Concrete;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier).Value;

            var result = await _accountService.GetUserInfos(userId);
            
            if(result is ErrorDataResult<User> errorDataResult)
            {
                if(errorDataResult.ErrorType == "NotFound")
                {
                    return NotFound(new { message = errorDataResult.Message });
                }
            }
            return Ok(result.Data);
        }
    }
}
