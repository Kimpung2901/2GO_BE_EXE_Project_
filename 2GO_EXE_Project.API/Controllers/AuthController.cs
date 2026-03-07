using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(request.Email);
        var hasPhone = !string.IsNullOrWhiteSpace(request.Phone);
        if (!hasEmail && !hasPhone)
        {
            return BadRequest("Vui lòng cung cấp email hoặc số điện thoại.");
        }
        if (hasEmail && !IsValidEmail(request.Email))
        {
            return BadRequest("Email không hợp lệ.");
        }
        if (hasPhone && !IsValidPhone(request.Phone))
        {
            return BadRequest("Số điện thoại phải gồm đúng 10 chữ số.");
        }
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Mật khẩu là bắt buộc.");
        }
        if (!IsValidPassword(request.Password))
        {
            return BadRequest("Mật khẩu phải có ít nhất 8 ký tự và bao gồm ít nhất 1 chữ cái và 1 chữ số.");
        }

        var result = await _authService.RegisterAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsValidIdentifier(request.Identifier))
        {
            return BadRequest("Tài khoản đăng nhập phải là email hoặc số điện thoại hợp lệ.");
        }

        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            // return 401 with user-friendly message instead of 500 stack trace
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token là bắt buộc.");
        }

        var result = await _authService.LogoutAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest("Refresh token là bắt buộc.");
        }

        try
        {
            var result = await _authService.RefreshTokenAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email là bắt buộc.");
        }
        if (!IsValidEmail(request.Email))
        {
            return BadRequest("Email không hợp lệ.");
        }

        var result = await _authService.VerifyEmailAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("resend-verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendVerifyEmail([FromBody] ResendVerifyEmailRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email là bắt buộc.");
        }
        if (!IsValidEmail(request.Email))
        {
            return BadRequest("Email không hợp lệ.");
        }

        var result = await _authService.ResendVerifyEmailAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email là bắt buộc.");
        }
        if (!IsValidEmail(request.Email))
        {
            return BadRequest("Email không hợp lệ.");
        }

        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return BadRequest("Mã xác thực là bắt buộc.");
        }
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest("Mật khẩu mới là bắt buộc.");
        }
        if (!IsValidPassword(request.NewPassword))
        {
            return BadRequest("Mật khẩu phải có ít nhất 8 ký tự và bao gồm ít nhất 1 chữ cái và 1 chữ số.");
        }
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest("Email là bắt buộc.");
        }
        if (!IsValidEmail(request.Email))
        {
            return BadRequest("Email không hợp lệ.");
        }

        var result = await _authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    private static bool IsValidIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return IsValidEmail(value) || IsValidPhone(value);
    }

    private static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase);
    }

    private static bool IsValidPassword(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8) return false;
        var hasLetter = value.Any(char.IsLetter);
        var hasDigit = value.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    private static bool IsValidPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        const string pattern = @"^[0-9]{10}$";
        return Regex.IsMatch(value, pattern);
    }

    [HttpPost("firebase-login")]
    [AllowAnonymous]
    public async Task<IActionResult> FirebaseLogin([FromBody] FirebaseLoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            return BadRequest("IdToken là bắt buộc.");
        }

        try
        {
            var result = await _authService.FirebaseLoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpPost("verify-phone-firebase")]
    [Authorize]
    public async Task<IActionResult> VerifyPhoneFirebase([FromBody] VerifyPhoneFirebaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.VerifyPhoneFirebaseAsync(User, request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("change-phone-firebase")]
    [Authorize]
    public async Task<IActionResult> ChangePhoneFirebase([FromBody] ChangePhoneFirebaseRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ChangePhoneFirebaseAsync(User, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _authService.GetCurrentUserAsync(User, cancellationToken);                       
        return Ok(result);
    }
}




