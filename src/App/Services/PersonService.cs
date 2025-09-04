using App.Abstractions;
using Domain.Entities;

namespace App.Services;

public class PersonService(IPersonRepository people, IRoleRepository roles) : IPersonService
{
    public async Task<Person?> GetAsync(Guid id, CancellationToken ct = default)
        => await people.GetAsync(id, ct);

    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default)
        => await people.ListAsync(ct);

    public async Task<Person> CreateAsync(string fullName, string? phone, IEnumerable<Guid>? roleIds = null, CancellationToken ct = default)
    {
        var person = new Person { FullName = fullName, Phone = phone };
        if (roleIds != null)
        {
            foreach (var roleId in roleIds)
            {
                var role = await roles.GetAsync(roleId, ct) ?? throw new KeyNotFoundException($"Role {roleId} not found");
                person.Roles.Add(role);
            }
        }
        return await people.AddAsync(person, ct);
    }

    public async Task UpdateAsync(Guid id, string fullName, string? phone, IEnumerable<Guid>? roleIds = null, CancellationToken ct = default)
    {
        var person = await people.GetAsync(id, ct) ?? throw new KeyNotFoundException($"Person {id} not found");
        person.FullName = fullName;
        person.Phone = phone;

        if (roleIds != null)
        {
            person.Roles.Clear();
            foreach (var roleId in roleIds)
            {
                var role = await roles.GetAsync(roleId, ct) ?? throw new KeyNotFoundException($"Role {roleId} not found");
                person.Roles.Add(role);
            }
        }

        await people.UpdateAsync(person, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
        => await people.DeleteAsync(id, ct);
}
