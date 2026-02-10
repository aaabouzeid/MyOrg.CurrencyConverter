using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyOrg.CurrencyConverter.API.Infrastructure.Data;

namespace MyOrg.CurrencyConverter.API.Controllers;

/// <summary>
/// Controller for managing user roles
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class RoleController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RoleController> _logger;

    public RoleController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RoleController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Get all available roles (Admin only)
    /// </summary>
    [HttpGet("all")]
    [Authorize(Roles = AppRoles.Admin)]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        return Ok(new { roles });
    }

    /// <summary>
    /// Get roles for a specific user (Admin only)
    /// </summary>
    [HttpGet("user/{email}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> GetUserRoles(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            email = user.Email,
            roles
        });
    }

    /// <summary>
    /// Assign a role to a user (Admin only)
    /// </summary>
    [HttpPost("assign")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!await _roleManager.RoleExistsAsync(request.Role))
        {
            return BadRequest(new { message = $"Role '{request.Role}' does not exist" });
        }

        if (await _userManager.IsInRoleAsync(user, request.Role))
        {
            return BadRequest(new { message = $"User already has role '{request.Role}'" });
        }

        var result = await _userManager.AddToRoleAsync(user, request.Role);

        if (result.Succeeded)
        {
            _logger.LogInformation("Role '{Role}' assigned to user '{Email}' by '{Admin}'",
                request.Role, request.Email, User.Identity?.Name);

            return Ok(new
            {
                message = $"Role '{request.Role}' assigned successfully",
                email = user.Email,
                roles = await _userManager.GetRolesAsync(user)
            });
        }

        return BadRequest(new
        {
            message = "Failed to assign role",
            errors = result.Errors.Select(e => e.Description)
        });
    }

    /// <summary>
    /// Remove a role from a user (Admin only)
    /// </summary>
    [HttpPost("remove")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!await _userManager.IsInRoleAsync(user, request.Role))
        {
            return BadRequest(new { message = $"User does not have role '{request.Role}'" });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, request.Role);

        if (result.Succeeded)
        {
            _logger.LogInformation("Role '{Role}' removed from user '{Email}' by '{Admin}'",
                request.Role, request.Email, User.Identity?.Name);

            return Ok(new
            {
                message = $"Role '{request.Role}' removed successfully",
                email = user.Email,
                roles = await _userManager.GetRolesAsync(user)
            });
        }

        return BadRequest(new
        {
            message = "Failed to remove role",
            errors = result.Errors.Select(e => e.Description)
        });
    }

    /// <summary>
    /// Get current user's roles (available to all authenticated users)
    /// </summary>
    [HttpGet("my-roles")]
    [Authorize] // Override class-level Admin requirement to allow any authenticated user
    public async Task<IActionResult> GetMyRoles()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new
        {
            email = user.Email,
            roles
        });
    }
}

public class AssignRoleRequest
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
