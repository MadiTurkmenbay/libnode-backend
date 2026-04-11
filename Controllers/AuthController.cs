using LibNode.Api.Models.DTOs;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibNode.Api.Controllers;

/// <summary>
/// Контроллер аутентификации: регистрация и вход.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Зарегистрировать нового пользователя.
    /// </summary>
    /// <param name="dto">Данные для регистрации.</param>
    /// <response code="201">Пользователь успешно создан.</response>
    /// <response code="409">Email или имя пользователя уже заняты.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _authService.RegisterAsync(dto, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Аутентифицировать пользователя и получить JWT токен.
    /// </summary>
    /// <param name="dto">Email и пароль.</param>
    /// <response code="200">Успешный вход. Возвращает JWT токен.</response>
    /// <response code="401">Неверный email или пароль.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(dto, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
