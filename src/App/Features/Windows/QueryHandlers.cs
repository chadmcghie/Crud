using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public class GetWindowQueryHandler(IWindowService windowService) : IRequestHandler<GetWindowQuery, Window?>
{
    public async Task<Window?> Handle(GetWindowQuery request, CancellationToken cancellationToken)
    {
        return await windowService.GetAsync(request.Id, cancellationToken);
    }
}

public class ListWindowsQueryHandler(IWindowService windowService) : IRequestHandler<ListWindowsQuery, IReadOnlyList<Window>>
{
    public async Task<IReadOnlyList<Window>> Handle(ListWindowsQuery request, CancellationToken cancellationToken)
    {
        return await windowService.ListAsync(cancellationToken);
    }
}
