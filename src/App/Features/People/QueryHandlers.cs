using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.People;

public class GetPersonQueryHandler(IPersonRepository personRepository) : IRequestHandler<GetPersonQuery, Person?>
{
    public async Task<Person?> Handle(GetPersonQuery request, CancellationToken cancellationToken)
    {
        return await personRepository.GetAsync(request.Id, cancellationToken);
    }
}

public class ListPeopleQueryHandler(IPersonRepository personRepository) : IRequestHandler<ListPeopleQuery, IReadOnlyList<Person>>
{
    public async Task<IReadOnlyList<Person>> Handle(ListPeopleQuery request, CancellationToken cancellationToken)
    {
        return await personRepository.ListAsync(cancellationToken);
    }
}
