using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public class GetWallQueryHandler(IWallRepository wallRepository) : IRequestHandler<GetWallQuery, Wall?>
{
    public async Task<Wall?> Handle(GetWallQuery request, CancellationToken cancellationToken)
    {
        return await wallRepository.GetAsync(request.Id, cancellationToken);
    }
}

public class ListWallsQueryHandler(IWallRepository wallRepository) : IRequestHandler<ListWallsQuery, IReadOnlyList<Wall>>
{
    public async Task<IReadOnlyList<Wall>> Handle(ListWallsQuery request, CancellationToken cancellationToken)
    {
        return await wallRepository.ListAsync(cancellationToken);
    }
}
