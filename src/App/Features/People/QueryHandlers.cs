using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.People;

public class GetPersonQueryHandler(IPersonService personService) : IRequestHandler<GetPersonQuery, Person?>
{
    public async Task<Person?> Handle(GetPersonQuery request, CancellationToken cancellationToken)
    {
        return await personService.GetAsync(request.Id, cancellationToken);
    }
}

public class ListPeopleQueryHandler(IPersonService personService) : IRequestHandler<ListPeopleQuery, IReadOnlyList<Person>>
{
    public async Task<IReadOnlyList<Person>> Handle(ListPeopleQuery request, CancellationToken cancellationToken)
    {
        return await personService.ListAsync(cancellationToken);
    }
}
