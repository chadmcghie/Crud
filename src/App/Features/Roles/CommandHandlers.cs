using App.Abstractions;
using Domain.Entities;
using MediatR;

namespace App.Features.Roles;

public class CreateRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<CreateRoleCommand, Role>
{
    public async Task<Role> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        // ensure unique by name
        var existing = await roleRepository.GetByNameAsync(request.Name, cancellationToken);
        if (existing is not null)
            return existing;

        return await roleRepository.AddAsync(new Role { Name = request.Name, Description = request.Description }, cancellationToken);
    }
}

public class UpdateRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<UpdateRoleCommand>
{
    public async Task Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetAsync(request.Id, cancellationToken) 
            ?? throw new KeyNotFoundException($"Role {request.Id} not found");
        role.Name = request.Name;
        role.Description = request.Description;
        await roleRepository.UpdateAsync(role, cancellationToken);
    }
}

public class DeleteRoleCommandHandler(IRoleRepository roleRepository) : IRequestHandler<DeleteRoleCommand>
{
    public async Task Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        await roleRepository.DeleteAsync(request.Id, cancellationToken);
    }
}
