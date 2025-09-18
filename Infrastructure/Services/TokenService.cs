using Application.Common.Results.Abstracts;
using Application.Common.Results.Concrete;
using Application.Common.Security;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using Infrastructure.Common.IdentityModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public TokenService(IConfiguration configuration, IMapper mapper, ILogger<TokenService> logger, IRefreshTokenRepository refreshTokenRepository)
        {
            _configuration = configuration;
            _mapper = mapper;
            _logger = logger;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public AccessToken GenerateAccessToken(User user)
        {
            // Validate configuration settings
            var jwtSecretKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var expirationSetting = _configuration["Jwt:AccessTokenExpiration"];

            if (string.IsNullOrEmpty(jwtSecretKey))
                throw new InvalidOperationException("JWT Key is not configured in application settings.");
            if (string.IsNullOrEmpty(jwtIssuer))
                throw new InvalidOperationException("JWT Issuer is not configured in application settings.");
            if (string.IsNullOrEmpty(jwtAudience))
                throw new InvalidOperationException("JWT Audience is not configured in application settings.");
            if (string.IsNullOrEmpty(expirationSetting))
                throw new InvalidOperationException("JWT AccessTokenExpiration is not configured in application settings.");


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName)
            };

            var jwt = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(expirationSetting)),
                notBefore: DateTime.UtcNow,
                signingCredentials: signingCredentials
                );
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.WriteToken(jwt);

            _logger.LogInformation("Access token generated for user: {UserEmail}", user.Email);
            return new AccessToken
            {
                Token = token,
                Expiration = jwt.ValidTo
            };
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rgn = RandomNumberGenerator.Create();
            rgn.GetBytes(randomNumber);

            return Convert.ToBase64String(randomNumber);
        }

        public IResult ValidateRefreshTokenAsync(RefreshToken token)
        {
            if (token.Expires < DateTime.UtcNow)
            {
                _logger.LogInformation("Refresh token expired: {Token}", token.Token);
                return new ErrorResult("Refresh token expired", AppError.Unauthorized());
            }

            if (token.Revoked)
            {
                _logger.LogInformation("Refresh token has been revoked: {Token}", token.Token);
                return new ErrorResult("Refresh token has been revoked", AppError.Unauthorized());
            }
            return new SuccessResult("Refresh token is valid");
        }

        public async Task<IResult> UpdateRefreshToken(RefreshToken refreshToken, string refreshTokenString)
        {
            // Debugging log
            _logger.LogDebug("Updating refresh token for token: {RefreshTokenString}", refreshToken.Token);

            refreshToken.Token = refreshTokenString;
            await _refreshTokenRepository.UpdateRefreshTokenAsync(refreshToken);
            _logger.LogInformation("Refresh token updated successfully new token: {RefreshTokenString}", refreshTokenString);
            return new SuccessResult("Refresh token updated successfully");
        }
    }
}
