# Database Schema

This is the database schema implementation for the spec detailed in [spec.md](../spec.md)

## New Entity: PasswordResetToken

### Domain Entity (Domain Layer)

```csharp
namespace Domain.Entities.Authentication
{
    public class PasswordResetToken
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public string Token { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public bool IsUsed { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UsedAt { get; private set; }
        
        // Navigation property
        public User User { get; private set; }
    }
}
```

### Database Table: PasswordResetTokens

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | uniqueidentifier | PRIMARY KEY | Token identifier |
| UserId | uniqueidentifier | NOT NULL, FOREIGN KEY | Reference to Users table |
| Token | nvarchar(256) | NOT NULL, UNIQUE | The reset token string |
| ExpiresAt | datetime2 | NOT NULL | Token expiration time |
| IsUsed | bit | NOT NULL, DEFAULT 0 | Whether token has been used |
| CreatedAt | datetime2 | NOT NULL | Token creation time |
| UsedAt | datetime2 | NULL | When token was used |

### EF Core Configuration

```csharp
public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(256);
            
        builder.HasIndex(x => x.Token)
            .IsUnique();
            
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt })
            .HasDatabaseName("IX_PasswordResetTokens_UserId_ExpiresAt");
            
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Indexes and Constraints

1. **Primary Key**: Id
2. **Unique Index**: Token (for fast token lookup)
3. **Composite Index**: (UserId, ExpiresAt) for cleanup queries
4. **Foreign Key**: UserId → Users.Id with CASCADE DELETE

### Migration Requirements

```sql
CREATE TABLE PasswordResetTokens (
    Id uniqueidentifier NOT NULL PRIMARY KEY,
    UserId uniqueidentifier NOT NULL,
    Token nvarchar(256) NOT NULL,
    ExpiresAt datetime2 NOT NULL,
    IsUsed bit NOT NULL DEFAULT 0,
    CreatedAt datetime2 NOT NULL,
    UsedAt datetime2 NULL,
    CONSTRAINT FK_PasswordResetTokens_Users_UserId 
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IX_PasswordResetTokens_Token ON PasswordResetTokens(Token);
CREATE INDEX IX_PasswordResetTokens_UserId_ExpiresAt ON PasswordResetTokens(UserId, ExpiresAt);
```

### Data Retention

- Expired tokens older than 30 days should be cleaned up by a background job
- Used tokens can be retained for audit purposes but marked as used
- Consider implementing soft delete for audit trail

### Performance Considerations

- Token column indexed for O(1) lookup during validation
- Composite index on UserId and ExpiresAt for efficient cleanup queries
- Consider partitioning if token volume becomes very large
- Token string length of 256 chars allows for secure random tokens while maintaining index efficiency