using Microsoft.AspNetCore.Mvc;
using SmartBin.Application.Services;
using SmartBin.Domain.Models;
using SmartBin.Domain.Models.Dto;
using System.Security.Authentication;

namespace SmartBin.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger; 

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<User>>> Get()
    {
        _logger.LogInformation("Fetching all users from the database.");
        var users = await _userService.GetAllAsync();
        _logger.LogInformation("Successfully retrieved {Count} users.", users.Count);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> Get(string id)
    {
        _logger.LogInformation("Fetching user with ID: {UserId}", id);
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
        {
            _logger.LogWarning("User with ID: {UserId} not found.", id);
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> Post([FromBody] User user)
    {
        _logger.LogInformation("Creating a new user: {Nickname}", user.Nickname);
        var created = await _userService.CreateAsync(user);
        _logger.LogInformation("User created with ID: {UserId}", created.Id);
        return CreatedAtAction(nameof(Get), new { id = created.Id.ToString() }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Put(string id, [FromBody] User user)
    {
        _logger.LogInformation("Updating user with ID: {UserId}", id);
        await _userService.UpdateAsync(id, user);
        _logger.LogInformation("User {UserId} updated successfully.", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        _logger.LogInformation("Attempting to delete user with ID: {UserId}", id);
        await _userService.DeleteAsync(id);
        _logger.LogInformation("User {UserId} deleted.", id);
        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
    {
        _logger.LogInformation("Registration attempt for nickname: {Nickname}", registrationDto.Nickname);
        try
        {
            var tokenPair = await _userService.RegisterAsync(registrationDto);
            _logger.LogInformation("User {Nickname} registered successfully.", registrationDto.Nickname);
            return Ok(tokenPair);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Registration failed for {Nickname}: {Message}", registrationDto.Nickname, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
        _logger.LogInformation("Login attempt for user: {Nickname}", loginDto.Nickname);
        try
        {
            var tokenPair = await _userService.LoginAsync(loginDto.Nickname, loginDto.Password);
            _logger.LogInformation("User {Nickname} logged in successfully.", loginDto.Nickname);
            return Ok(tokenPair);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Unauthorized login attempt for user: {Nickname}. Reason: {Message}", loginDto.Nickname, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login for user: {Nickname}", loginDto.Nickname);
            return Problem("An internal error occurred.");
        }
    }
}