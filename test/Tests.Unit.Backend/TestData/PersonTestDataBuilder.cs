using Domain.Entities;

namespace Tests.Unit.Backend.TestData;

public class PersonTestDataBuilder
{
    private string _fullName = "John Doe";
    private string? _phone = "555-1234";
    private List<Role> _roles = new();

    public PersonTestDataBuilder WithFullName(string fullName)
    {
        _fullName = fullName;
        return this;
    }

    public PersonTestDataBuilder WithPhone(string? phone)
    {
        _phone = phone;
        return this;
    }

    public PersonTestDataBuilder WithRoles(params Role[] roles)
    {
        _roles = roles.ToList();
        return this;
    }

    public PersonTestDataBuilder WithoutRoles()
    {
        _roles.Clear();
        return this;
    }

    public Person Build()
    {
        var person = Person.Create(_fullName, _phone);

        if (_roles.Any())
        {
            person.UpdateRoles(_roles);
        }

        return person;
    }

    public static PersonTestDataBuilder Default() => new();
}

