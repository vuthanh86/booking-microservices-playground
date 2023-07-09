using System;
using System.Threading.Tasks;

using BuildingBlocks.EFCore;

using Identity.Identity.Constants;
using Identity.Identity.Models;

using Microsoft.AspNetCore.Identity;

namespace Identity.Data;

public class IdentityDataSeeder : IDataSeeder
{
    private readonly RoleManager<IdentityRole<long>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityDataSeeder(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<long>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAllAsync()
    {
        await SeedRoles();
        await SeedUsers();
    }

    private async Task SeedRoles()
    {
        if (await _roleManager.RoleExistsAsync(Constants.Role.Admin) == false)
            await _roleManager.CreateAsync(new IdentityRole<long>(Constants.Role.Admin));

        if (await _roleManager.RoleExistsAsync(Constants.Role.User) == false)
            await _roleManager.CreateAsync(new IdentityRole<long>(Constants.Role.User));
    }

    private async Task SeedUsers()
    {
        if (await _userManager.FindByNameAsync("samh") == null)
        {
            ApplicationUser user = new ApplicationUser
            {
                FirstName = "Sam",
                LastName = "H",
                UserName = "samh",
                Email = "sam@test.com",
                SecurityStamp = Guid.NewGuid().ToString()
            };

            IdentityResult result = await _userManager.CreateAsync(user, "Admin@123456");

            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, Constants.Role.Admin);
        }

        if (await _userManager.FindByNameAsync("meysamh2") == null)
        {
            ApplicationUser user = new ApplicationUser
            {
                FirstName = "Sam",
                LastName = "H",
                UserName = "samh2",
                Email = "sam2@test.com",
                SecurityStamp = Guid.NewGuid().ToString()
            };

            IdentityResult result = await _userManager.CreateAsync(user, "User@123456");

            if (result.Succeeded)
                await _userManager.AddToRoleAsync(user, Constants.Role.User);
        }
    }
}
