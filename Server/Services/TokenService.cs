/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

#endregion

namespace Server.Services
{
    #region Interfaces

    public interface ITokenService
    {
        string GenerateToken(long userId, string login, string role);
        ClaimsPrincipal? ValidateToken(string token);
    }

    #endregion

    public class TokenService : ITokenService
    {
        #region Declarations

        private readonly IConfiguration _configuration;
        private const string SecretKey = "SuperSecretKeyForJWTTokenGenerationThisIsVeryLongAndSecure2025!";

        #endregion

        #region Constructor

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        #endregion

        #region Properties

        #endregion

        #region Methods - Public

        public string GenerateToken(long userId, string login, string role)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("userId", userId.ToString()),
                    new Claim(ClaimTypes.Name, login),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("role", role)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(SecretKey);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Methods - Private

        #endregion
    }
}
