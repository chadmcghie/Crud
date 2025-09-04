using Domain.Entities;
using MediatR;

namespace App.Features.People;

public record GetPersonQuery(Guid Id) : IRequest<Person?>;

public record ListPeopleQuery : IRequest<IReadOnlyList<Person>>;
