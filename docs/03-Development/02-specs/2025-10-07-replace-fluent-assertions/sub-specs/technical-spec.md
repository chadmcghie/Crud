# Technical Specification

This is the technical specification for the spec detailed in @docs/03-Development/02-specs/2025-10-07-replace-fluent-assertions/spec.md

## Technical Requirements

### Migration Strategy

- **Phased Approach**: Migrate tests file-by-file to minimize risk and enable incremental testing
- **Validation**: Run full test suite after each file or logical group of files is migrated
- **Consistency**: Apply consistent conversion patterns across all test files

### Conversion Patterns

Replace FluentAssertions fluent syntax with xUnit Assert equivalents:

#### Equality Assertions
```csharp
// FluentAssertions → xUnit Assert
result.Should().Be(expected)              → Assert.Equal(expected, result)
result.Should().NotBe(unexpected)         → Assert.NotEqual(unexpected, result)
result.Should().BeEquivalentTo(expected)  → Assert.Equal(expected, result) // for value equality
```

#### Null Assertions
```csharp
// FluentAssertions → xUnit Assert
result.Should().BeNull()                  → Assert.Null(result)
result.Should().NotBeNull()               → Assert.NotNull(result)
```

#### Boolean Assertions
```csharp
// FluentAssertions → xUnit Assert
result.Should().BeTrue()                  → Assert.True(result)
result.Should().BeFalse()                 → Assert.False(result)
```

#### Collection Assertions
```csharp
// FluentAssertions → xUnit Assert
collection.Should().BeEmpty()             → Assert.Empty(collection)
collection.Should().NotBeEmpty()          → Assert.NotEmpty(collection)
collection.Should().HaveCount(n)          → Assert.Equal(n, collection.Count)
collection.Should().Contain(item)         → Assert.Contains(item, collection)
collection.Should().NotContain(item)      → Assert.DoesNotContain(item, collection)
```

#### String Assertions
```csharp
// FluentAssertions → xUnit Assert
str.Should().StartWith(prefix)            → Assert.StartsWith(prefix, str)
str.Should().EndWith(suffix)              → Assert.EndsWith(suffix, str)
str.Should().Contain(substring)           → Assert.Contains(substring, str)
str.Should().BeEmpty()                    → Assert.Empty(str)
```

#### Exception Assertions
```csharp
// FluentAssertions → xUnit Assert
action.Should().Throw<TException>()       → Assert.Throws<TException>(action)
  .WithMessage(message)                   → var ex = Assert.Throws<TException>(action);
                                             Assert.Contains(message, ex.Message)

action.Should().NotThrow()                → var exception = Record.Exception(action);
                                             Assert.Null(exception)
```

#### Type Assertions
```csharp
// FluentAssertions → xUnit Assert
obj.Should().BeOfType<T>()                → Assert.IsType<T>(obj)
obj.Should().BeAssignableTo<T>()          → Assert.IsAssignableFrom<T>(obj)
```

#### Comparison Assertions
```csharp
// FluentAssertions → xUnit Assert
value.Should().BeGreaterThan(n)           → Assert.True(value > n)
value.Should().BeLessThan(n)              → Assert.True(value < n)
value.Should().BeGreaterOrEqualTo(n)      → Assert.True(value >= n)
value.Should().BeLessOrEqualTo(n)         → Assert.True(value <= n)
value.Should().BeInRange(min, max)        → Assert.InRange(value, min, max)
```

### Project Configuration Changes

#### Tests.Unit.Backend.csproj
1. Remove `<PackageReference Include="FluentAssertions" Version="6.12.0" />`
2. Remove `<Using Include="FluentAssertions" />` from global usings

#### Tests.Integration.Backend.csproj
1. Remove `<PackageReference Include="FluentAssertions" Version="6.12.0" />`
2. Remove `<Using Include="FluentAssertions" />` from global usings

### Documentation Updates

#### tech-stack.md
Update the Testing section:
```markdown
**Testing**
- Unit Testing: xUnit
- Integration Testing: WebApplicationFactory with TestContainers
- E2E Testing: Playwright
- Code Coverage: Coverlet
```

Remove reference to FluentAssertions from the testing infrastructure description.

### Test Execution Strategy

1. **Incremental Migration**: Convert tests in logical groups (by feature or test class)
2. **Continuous Validation**: Run `dotnet test` after each migration batch to ensure no regressions
3. **Final Validation**: Execute full test suite (unit + integration) before completing the migration
4. **Code Formatting**: Run `dotnet format` after migration to ensure consistent code style

### Risk Mitigation

- **Test Coverage Preservation**: Ensure no tests are accidentally removed or disabled during migration
- **Assertion Logic Equivalence**: Verify that xUnit assertions maintain the same test intent as FluentAssertions
- **Build Verification**: Confirm all tests compile and pass after package removal
- **Documentation Review**: Update all references to FluentAssertions in documentation

### Performance Considerations

- **No Performance Impact**: xUnit Assert methods are as performant (or more so) than FluentAssertions
- **Reduced Package Load Time**: Eliminating FluentAssertions slightly reduces test assembly load time
- **Compilation Speed**: Fewer dependencies may slightly improve test project compilation time

### Rollback Plan

If issues arise during migration:
1. Revert changes to .csproj files to restore FluentAssertions package references
2. Restore global using directives
3. Revert individual test file changes using git
4. Re-run test suite to confirm restoration
