using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public record GetWallQuery(Guid Id) : IRequest<Wall?>;

public record ListWallsQuery : IRequest<IReadOnlyList<Wall>>;
