using Domain.Entities;
using MediatR;

namespace App.Features.Windows;

public record GetWindowQuery(Guid Id) : IRequest<Window?>;

public record ListWindowsQuery : IRequest<IReadOnlyList<Window>>;
