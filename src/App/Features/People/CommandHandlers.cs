using App.Abstractions;
using Domain.Entities;
using MediatR;

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

public class UpdatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository) : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        // Load existing person to work with tracked entity
        var person = await personRepository.GetAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Person {request.Id} not found");

        // Update person properties using domain methods
        person.UpdateFullName(request.FullName);
        person.UpdatePhone(request.Phone);

        if (request.RoleIds != null)
        {
            // Load all required roles first to validate they exist
            var newRoles = new List<Role>();
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken)
                    ?? throw new ArgumentException($"Role {roleId} not found");
                newRoles.Add(role);
            }

            // Update roles using domain method
            person.UpdateRoles(newRoles);
        }

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
