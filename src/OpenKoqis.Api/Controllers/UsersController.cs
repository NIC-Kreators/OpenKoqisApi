using System.Security.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenKoqis.Application.Services;
using OpenKoqis.Domain.Models;
using OpenKoqis.Domain.Models.Dto;

namespace OpenKoqis.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController(IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<User>>> GetAsync()
    {
        logger.LogInformation("Fetching all users from the database");
        var users = await userService.GetAllAsync();
        logger.LogInformation("Successfully retrieved {Count} users", users.Count);
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetByIdAsync(string id)
    {
        logger.LogInformation("Fetching user with ID: {UserId}", id);
        var user = await userService.GetByIdAsync(id);

        if (user == null)
        {
            logger.LogWarning("User with ID: {UserId} not found", id);
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost]
    public async Task<ActionResult<User>> PostAsync([FromBody] User user)
    {
        logger.LogInformation("Creating a new user: {Nickname}", user.Nickname);
        var created = await userService.CreateAsync(user);
        logger.LogInformation("User created with ID: {UserId}", created.Id);
        return CreatedAtAction(nameof(GetAsync), new
        {
            id = created.Id.ToString()
        }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutAsync(string id, [FromBody] User user)
    {
        logger.LogInformation("Updating user with ID: {UserId}", id);
        await userService.UpdateAsync(id, user);
        logger.LogInformation("User {UserId} updated successfully", id);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id)
    {
        logger.LogInformation("Attempting to delete user with ID: {UserId}", id);
        await userService.DeleteAsync(id);
        logger.LogInformation("User {UserId} deleted", id);
        return NoContent();
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync([FromBody] UserRegistrationDto registrationDto)
    {
        logger.LogInformation("Registration attempt for nickname: {Nickname}", registrationDto.Nickname);

        try
        {
            var tokenPair = await userService.RegisterAsync(registrationDto);
            logger.LogInformation("User {Nickname} registered successfully", registrationDto.Nickname);
            return Ok(tokenPair);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning("Registration failed for {Nickname}: {Message}", registrationDto.Nickname, ex.Message);
            return BadRequest(new
            {
                message = ex.Message
            });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync([FromBody] UserLoginDto loginDto)
    {
        logger.LogInformation("Login attempt for user: {Nickname}", loginDto.Nickname);

        try
        {
            var tokenPair = await userService.LoginAsync(loginDto.Nickname, loginDto.Password);
            logger.LogInformation("User {Nickname} logged in successfully", loginDto.Nickname);
            return Ok(tokenPair);
        }
        catch (AuthenticationException ex)
        {
            logger.LogWarning("Unauthorized login attempt for user: {Nickname}. Reason: {Message}", loginDto.Nickname, ex.Message);
            return Unauthorized(new
            {
                message = ex.Message
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for user: {Nickname}", loginDto.Nickname);
            return Problem("An internal error occurred.");
        }
    }
}
