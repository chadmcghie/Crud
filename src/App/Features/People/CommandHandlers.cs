using App.Abstractions;
using App.Services;
using Domain.Entities;
using MediatR;

namespace App.Features.People;

public class CreatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository, IRowVersionService rowVersionService) : IRequestHandler<CreatePersonCommand, Person>
{
    public async Task<Person> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = new Person
        {
            FullName = request.FullName,
            Phone = request.Phone,
            RowVersion = rowVersionService.GenerateInitialVersion()
        };

        if (request.RoleIds != null)
        {
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken)
                    ?? throw new ArgumentException($"Role {roleId} not found");
                person.Roles.Add(role);
            }
        }

        return await personRepository.AddAsync(person, cancellationToken);
    }
}

public class UpdatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository, IRowVersionService rowVersionService) : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        // Load existing person with current state for proper concurrency control
        var person = await personRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {request.Id} not found");

        // Manual concurrency check - validate client's RowVersion against current entity
        if (request.RowVersion != null && person.RowVersion != null)
        {
            if (!request.RowVersion.SequenceEqual(person.RowVersion))
            {
                throw new InvalidOperationException("The person was modified by another user. Please refresh and try again.");
            }
        }

        // Update person properties
        person.FullName = request.FullName;
        person.Phone = request.Phone;

        if (request.RoleIds != null)
        {
            // For many-to-many relationships, EF Core requires explicit tracking of changes
            // Remove existing roles first
            var rolesToRemove = person.Roles.ToList();
            foreach (var role in rolesToRemove)
            {
                person.Roles.Remove(role);
            }

            // Add new roles
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken)
                    ?? throw new ArgumentException($"Role {roleId} not found");
                person.Roles.Add(role);
            }
        }

        // Generate new RowVersion before saving
        person.RowVersion = rowVersionService.GenerateNewVersion();

        await personRepository.UpdateAsync(person, cancellationToken);
    }
}

public class DeletePersonCommandHandler(IPersonRepository personRepository) : IRequestHandler<DeletePersonCommand>
{
    public async Task Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        await personRepository.DeleteAsync(request.Id, cancellationToken);
    }
}
