using App.Abstractions;
using Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace App.Features.People;

public class CreatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository) : IRequestHandler<CreatePersonCommand, Person>
{
    public async Task<Person> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = Person.Create(request.FullName, request.Phone);

        if (request.RoleIds != null)
        {
            var roles = new List<Role>();
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken)
                    ?? throw new ArgumentException($"Role {roleId} not found");
                roles.Add(role);
            }
            person.UpdateRoles(roles);
        }

        return await personRepository.AddAsync(person, cancellationToken);
    }
}

public class UpdatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository, ILogger<UpdatePersonCommandHandler> logger) : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        // Load existing person to work with tracked entity
        var person = await personRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {request.Id} not found");

        logger.LogInformation("UPDATE PERSON: Loaded person {PersonId} with {RoleCount} roles: {Roles}",
            person.Id, person.Roles.Count, string.Join(", ", person.Roles.Select(r => $"{r.Name}({r.Id})")));

        // Update person properties using domain methods
        person.UpdateFullName(request.FullName);
        person.UpdatePhone(request.Phone);

        if (request.RoleIds != null)
        {
            logger.LogInformation("UPDATE PERSON: Request has {RoleIdCount} role IDs: {RoleIds}",
                request.RoleIds.Count(), string.Join(", ", request.RoleIds));

            // Load all required roles first to validate they exist
            var newRoles = new List<Role>();
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken)
                    ?? throw new ArgumentException($"Role {roleId} not found");
                logger.LogInformation("UPDATE PERSON: Loaded role {RoleName}({RoleId})", role.Name, role.Id);
                newRoles.Add(role);
            }

            logger.LogInformation("UPDATE PERSON: About to call UpdateRoles with {NewRoleCount} roles", newRoles.Count);

            // Update roles using domain method
            person.UpdateRoles(newRoles);

            logger.LogInformation("UPDATE PERSON: After UpdateRoles, person has {RoleCount} roles: {Roles}",
                person.Roles.Count, string.Join(", ", person.Roles.Select(r => $"{r.Name}({r.Id})")));
        }

        await personRepository.UpdateAsync(person, cancellationToken);

        logger.LogInformation("UPDATE PERSON: Completed update for person {PersonId}", person.Id);
    }
}

public class DeletePersonCommandHandler(IPersonRepository personRepository) : IRequestHandler<DeletePersonCommand>
{
    public async Task Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        await personRepository.DeleteAsync(request.Id, cancellationToken);
    }
}
