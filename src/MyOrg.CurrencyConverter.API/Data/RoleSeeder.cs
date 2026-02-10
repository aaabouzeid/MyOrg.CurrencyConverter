using Microsoft.AspNetCore.Identity;
using Serilog;

namespace MyOrg.CurrencyConverter.API.Data;

/// <summary>
/// Role constants for the application
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";

    public static string[] AllRoles => [Admin, Manager, User];
}

/// <summary>
/// Seeds default roles and optionally a default admin user
/// </summary>
public class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, IConfiguration configuration)
    {
        Log.Information("Starting role seeding...");

        // Create all roles if they don't exist
        foreach (var role in AppRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (result.Succeeded)
                {
                    Log.Information("Role '{Role}' created successfully", role);
                }
                else
                {
                    Log.Error("Failed to create role '{Role}': {Errors}", role,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Log.Information("Role '{Role}' already exists", role);
            }
        }

        Log.Information("Role seeding completed");
    }

    public static async Task SeedDefaultAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        Log.Information("Checking for default admin user...");

        var adminEmail = configuration["DefaultAdmin:Email"];
        var adminPassword = configuration["DefaultAdmin:Password"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            Log.Warning("Default admin credentials not configured. Skipping admin user creation.");
            return;
        }

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                Log.Information("Default admin user created successfully");

                // Assign Admin role
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                Log.Information("Admin role assigned to default admin user");
            }
            else
            {
                Log.Error("Failed to create default admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            Log.Information("Default admin user already exists");

            // Ensure admin has the Admin role
            if (!await userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
                Log.Information("Admin role assigned to existing admin user");
            }
        }
    }
}
