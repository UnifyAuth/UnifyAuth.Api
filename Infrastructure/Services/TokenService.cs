using Application.Common.Security;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IConfiguration configuration, IMapper mapper, ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AccessToken> GenerateAccessToken(User user)
        {
            // Debugging log
            _logger.LogDebug("Creating JWT token for user with email: {Email}", user.Email);

            var identityUser = _mapper.Map<IdentityUserModel>(user);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, identityUser.Id.ToString()),
                new Claim(ClaimTypes.Email, identityUser.Email),
                new Claim(ClaimTypes.Name, identityUser.FirstName),
                new Claim(ClaimTypes.Surname, identityUser.LastName)
            };

            var jwt = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["Jwt:AccessTokenExpiration"])),
                notBefore: DateTime.UtcNow,
                signingCredentials: signingCredentials
                );
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.WriteToken(jwt);

            return new AccessToken
            {
                Token = token,
                Expiration = jwt.ValidTo
            };
        }

        public string GenerateRefreshToken()
        {
            _logger.LogDebug("Generating refresh token");
            var randomNumber = new byte[64];
            using var rgn = RandomNumberGenerator.Create();
            rgn.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }
    }
}
