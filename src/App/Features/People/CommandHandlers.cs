using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.People;

public class CreatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository) : IRequestHandler<CreatePersonCommand, Person>
{
    public async Task<Person> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = new Person { FullName = request.FullName, Phone = request.Phone };
        
        if (request.RoleIds != null)
        {
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken) 
                    ?? throw new KeyNotFoundException($"Role {roleId} not found");
                person.Roles.Add(role);
            }
        }
        
        return await personRepository.AddAsync(person, cancellationToken);
    }
}

public class UpdatePersonCommandHandler(IPersonRepository personRepository, IRoleRepository roleRepository) : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = await personRepository.GetAsync(request.Id, cancellationToken) 
            ?? throw new KeyNotFoundException($"Person {request.Id} not found");
            
        person.FullName = request.FullName;
        person.Phone = request.Phone;

        if (request.RoleIds != null)
        {
            person.Roles.Clear();
            foreach (var roleId in request.RoleIds)
            {
                var role = await roleRepository.GetAsync(roleId, cancellationToken) 
                    ?? throw new KeyNotFoundException($"Role {roleId} not found");
                person.Roles.Add(role);
            }
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
