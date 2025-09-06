using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public class GetWindowQueryHandler(IWindowRepository windowRepository) : IRequestHandler<GetWindowQuery, Window?>
{
    public async Task<Window?> Handle(GetWindowQuery request, CancellationToken cancellationToken)
    {
        return await windowRepository.GetAsync(request.Id, cancellationToken);
    }
}

public class ListWindowsQueryHandler(IWindowRepository windowRepository) : IRequestHandler<ListWindowsQuery, IReadOnlyList<Window>>
{
    public async Task<IReadOnlyList<Window>> Handle(ListWindowsQuery request, CancellationToken cancellationToken)
    {
        return await windowRepository.ListAsync(cancellationToken);
    }
}
