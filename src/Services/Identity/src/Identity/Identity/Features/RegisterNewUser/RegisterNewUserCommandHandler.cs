using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ardalis.GuardClauses;

using BuildingBlocks.Contracts.EventBus.Messages;
using BuildingBlocks.Core;
using BuildingBlocks.Core.CQRS;

using Identity.Identity.Dtos;
using Identity.Identity.Exceptions;
using Identity.Identity.Models;

using Microsoft.AspNetCore.Identity;

namespace Identity.Identity.Features.RegisterNewUser;

public class RegisterNewUserCommandHandler : ICommandHandler<RegisterNewUserCommand, RegisterNewUserResponseDto>
{
    private readonly IEventDispatcher _eventDispatcher;
    private readonly UserManager<ApplicationUser> _userManager;

    public RegisterNewUserCommandHandler(UserManager<ApplicationUser> userManager,
        IEventDispatcher eventDispatcher)
    {
        _userManager = userManager;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<RegisterNewUserResponseDto> Handle(RegisterNewUserCommand command,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(command, nameof(command));

        ApplicationUser applicationUser = new ApplicationUser
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
            UserName = command.Username,
            Email = command.Email,
            PasswordHash = command.Password,
            PassPortNumber = command.PassportNumber
        };

        IdentityResult identityResult = await _userManager.CreateAsync(applicationUser, command.Password);
        IdentityResult roleResult = await _userManager.AddToRoleAsync(applicationUser, Constants.Constants.Role.User);

        if (identityResult.Succeeded == false)
            throw new RegisterIdentityUserException(string.Join(',', identityResult.Errors.Select(e => e.Description)));

        if (roleResult.Succeeded == false)
            throw new RegisterIdentityUserException(string.Join(',', roleResult.Errors.Select(e => e.Description)));

        await _eventDispatcher.SendAsync(new UserCreated(applicationUser.Id,
                                                         applicationUser.FirstName + " " + applicationUser.LastName,
                                                         applicationUser.PassPortNumber),
                                         cancellationToken: cancellationToken);

        return new RegisterNewUserResponseDto
        {
            Id = applicationUser.Id,
            FirstName = applicationUser.FirstName,
            LastName = applicationUser.LastName,
            Username = applicationUser.UserName,
            PassportNumber = applicationUser.PassPortNumber
        };
    }
}
