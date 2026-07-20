using DigitalBookLibrary.Application.Common;
using DigitalBookLibrary.Application.DTOs.Auth;
using DigitalBookLibrary.Application.Interfaces;
using DigitalBookLibrary.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DigitalBookLibrary.WebAPI.Controllers
{
    /// <summary>Rate-limited: these endpoints are the ones worth brute-forcing.</summary>
    [ApiController]
    [Route("api/auth")]
    [EnableRateLimiting("auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _auth;
        private readonly ICurrentUser _currentUser;

        public AuthController(AuthService auth, ICurrentUser currentUser)
        {
            _auth = auth;
            _currentUser = currentUser;
        }

        /// <summary>Register a new member account.</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
        {
            var user = await _auth.RegisterAsync(dto, cancellationToken);
            return Ok(ApiResponse.Ok(user, ResponseCodes.Registered));
        }

        /// <summary>Log in with email/username + password; returns access + refresh tokens.</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto, CancellationToken cancellationToken)
        {
            var result = await _auth.LoginAsync(dto, cancellationToken);
            return Ok(ApiResponse.Ok(result, ResponseCodes.LoggedIn));
        }

        /// <summary>Exchange a valid refresh token for a new token pair (rotation).</summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequestDto dto, CancellationToken cancellationToken)
        {
            var result = await _auth.RefreshAsync(dto, cancellationToken);
            return Ok(ApiResponse.Ok(result, ResponseCodes.TokenRefreshed));
        }

        /// <summary>Revoke the supplied refresh token.</summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshRequestDto dto, CancellationToken cancellationToken)
        {
            await _auth.LogoutAsync(dto, cancellationToken);
            return Ok(ApiResponse.Ok(ResponseCodes.LoggedOut));
        }

        /// <summary>Get the current authenticated user's profile.</summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me(CancellationToken cancellationToken)
        {
            var profile = await _auth.GetProfileAsync(_currentUser.RequireUserId(), cancellationToken);
            return Ok(ApiResponse.Ok(profile));
        }
    }
}
