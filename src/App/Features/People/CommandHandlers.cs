using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.People;

public class CreatePersonCommandHandler(IPersonService personService) : IRequestHandler<CreatePersonCommand, Person>
{
    public async Task<Person> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        return await personService.CreateAsync(
            request.FullName,
            request.Phone,
            request.RoleIds ?? [],
            cancellationToken
        );
    }
}

public class UpdatePersonCommandHandler(IPersonService personService) : IRequestHandler<UpdatePersonCommand>
{
    public async Task Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        await personService.UpdateAsync(
            request.Id,
            request.FullName,
            request.Phone,
            request.RoleIds ?? [],
            cancellationToken
        );
    }
}

public class DeletePersonCommandHandler(IPersonService personService) : IRequestHandler<DeletePersonCommand>
{
    public async Task Handle(DeletePersonCommand request, CancellationToken cancellationToken)
    {
        await personService.DeleteAsync(request.Id, cancellationToken);
    }
}
