/*
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
 * ==================== ROBIGOO FIELD SPRAYER CONTROL STATION ====================
*/

#region Imports

using Server.Data;
using Server.Models;
using Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

#endregion //Imports

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        #region Declarations

        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;
        private readonly ITokenService _tokenService;

        #endregion //Declarations

        #region Constructor

        public AuthController(AppDbContext context, IPasswordService passwordService, ITokenService tokenService)
        {
            _context = context;
            _passwordService = passwordService;
            _tokenService = tokenService;
        }

        #endregion //Constructor

        #region Properties

        #endregion //Properties

        #region Methods - Public

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (string.IsNullOrEmpty(dto.Login) || string.IsNullOrEmpty(dto.Password))
                return BadRequest(new { message = "Login i hasło są wymagane" });

            var user = _context.Users.FirstOrDefault(u => u.Login == dto.Login);
            if (user == null || !_passwordService.VerifyPassword(dto.Password, user.PasswordHash))
                return Unauthorized(new { message = "Błędny login lub hasło" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Użytkownik jest nieaktywny" });

            user.LastLoginAt = DateTime.UtcNow;
            _context.SaveChanges();

            var token = _tokenService.GenerateAccessToken(user.Id, user.Login, user.Role);
            var response = new
            {
                token = token,
                user = new LoginResponseDto
                {
                    UserId = user.Id,
                    Login = user.Login,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    PermissionNumber = user.PermissionNumber,
                    Role = user.Role,
                    AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
                }
            };

            return Ok(response);
        }

        [HttpGet("me")]
        public IActionResult GetCurrentUser()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out long userId))
                return Unauthorized();

            var user = _context.Users.Find(userId);
            if (user == null)
                return Unauthorized();

            var response = new LoginResponseDto
            {
                UserId = user.Id,
                Login = user.Login,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                PermissionNumber = user.PermissionNumber,
                Role = user.Role,
                AvatarBase64 = user.AvatarData != null ? Convert.ToBase64String(user.AvatarData) : null
            };

            return Ok(response);
        }

        #endregion

        #region Methods - Private

        #endregion
    }
}

