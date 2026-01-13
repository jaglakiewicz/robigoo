/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

#endregion

namespace Server.Services
{
    #region Interfaces

    public interface ITokenService
    {
        string GenerateAccessToken(long userId, string login, string role);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        int GetAccessTokenExpirationMinutes();
        int GetRefreshTokenExpirationDays();
    }

    #endregion

    public class TokenService : ITokenService
    {
        #region Declarations

        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenExpirationMinutes;
        private readonly int _refreshTokenExpirationDays;

        #endregion

        #region Constructor

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
            
            _secretKey = configuration["Jwt:Key"] 
                ?? throw new InvalidOperationException("JWT Key not configured. Set Jwt:Key in appsettings.json or environment variables.");
            
            _issuer = configuration["Jwt:Issuer"] ?? "Robigoo";
            _audience = configuration["Jwt:Audience"] ?? "RobigooUsers";
            _accessTokenExpirationMinutes = configuration.GetValue<int>("Jwt:AccessTokenExpirationMinutes", 30);
            _refreshTokenExpirationDays = configuration.GetValue<int>("Jwt:RefreshTokenExpirationDays", 7);

            // Validate key length (minimum 256 bits = 32 bytes for HS256)
            if (Encoding.UTF8.GetBytes(_secretKey).Length < 32)
            {
                throw new InvalidOperationException("JWT Key must be at least 256 bits (32 characters) for HS256.");
            }
        }

        #endregion

        #region Methods - Public

        /// <summary>
        /// Generates a short-lived JWT access token.
        /// </summary>
        public string GenerateAccessToken(long userId, string login, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var claims = new List<Claim>
            {
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.Name, login),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a cryptographically secure refresh token.
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        /// <summary>
        /// Validates a JWT token and returns the claims principal.
        /// </summary>
        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets claims from an expired token (for refresh token flow).
        /// </summary>
        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_secretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = false // Allow expired tokens
                }, out SecurityToken validatedToken);

                var jwtToken = validatedToken as JwtSecurityToken;
                if (jwtToken == null || !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        public int GetAccessTokenExpirationMinutes() => _accessTokenExpirationMinutes;
        public int GetRefreshTokenExpirationDays() => _refreshTokenExpirationDays;

        #endregion

        #region Legacy Support

        /// <summary>
        /// Legacy method for backward compatibility.
        /// </summary>
        [Obsolete("Use GenerateAccessToken instead")]
        public string GenerateToken(long userId, string login, string role)
        {
            return GenerateAccessToken(userId, login, role);
        }

        #endregion
    }
}
