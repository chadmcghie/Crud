using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Walls;

public class GetWallQueryHandler(IWallService wallService) : IRequestHandler<GetWallQuery, Wall?>
{
    public async Task<Wall?> Handle(GetWallQuery request, CancellationToken cancellationToken)
    {
        return await wallService.GetAsync(request.Id, cancellationToken);
    }
}

public class ListWallsQueryHandler(IWallService wallService) : IRequestHandler<ListWallsQuery, IReadOnlyList<Wall>>
{
    public async Task<IReadOnlyList<Wall>> Handle(ListWallsQuery request, CancellationToken cancellationToken)
    {
        return await wallService.ListAsync(cancellationToken);
    }
}
