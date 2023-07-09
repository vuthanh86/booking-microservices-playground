using System.Security.Claims;
using System.Threading.Tasks;

using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

using Identity.Identity.Models;

using Microsoft.AspNetCore.Identity;

namespace Identity;

public class UserValidator : IResourceOwnerPasswordValidator
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserValidator(SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
    {
        ApplicationUser user = await _userManager.FindByNameAsync(context.UserName);

        SignInResult signIn = await _signInManager.PasswordSignInAsync(
                                                                       user,
                                                                       context.Password,
                                                                       true,
                                                                       true);

        if (signIn.Succeeded)
        {
            string userId = user!.Id.ToString();

            // context set to success
            context.Result = new GrantValidationResult(
                                                       userId,
                                                       "custom",
                                                       new Claim[]
                                                       {
                                                           new(ClaimTypes.NameIdentifier, userId),
                                                           new(ClaimTypes.Name, user.UserName)
                                                       }
                                                      );

            return;
        }

        // context set to Failure
        context.Result = new GrantValidationResult(
                                                   TokenRequestErrors.UnauthorizedClient, "Invalid Credentials");
    }
}
