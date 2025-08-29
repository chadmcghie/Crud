# Database Schema

This is the database schema implementation for the spec detailed in @.agent-os/specs/2025-08-29-jwt-authentication/spec.md

## Entity Definitions

### User Entity (Domain Layer)
```csharp
public class User : Entity<Guid>, IAggregateRoot
{
    public Email Email { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public HashSet<string> Roles { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public List<RefreshToken> RefreshTokens { get; private set; }
}
```

### RefreshToken Entity (Domain Layer)
```csharp
public class RefreshToken : Entity<Guid>
{
    public string Token { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.UtcNow;
}
```

## Database Tables

### Users Table
```sql
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(128) NOT NULL,
    Roles NVARCHAR(MAX), -- JSON array of roles
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

CREATE INDEX IX_Users_Email ON Users(Email);
```

### RefreshTokens Table
```sql
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Token NVARCHAR(256) NOT NULL,
    UserId UNIQUEIDENTIFIER NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    RevokedAt DATETIME2 NULL,
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE INDEX IX_RefreshTokens_Token ON RefreshTokens(Token);
CREATE INDEX IX_RefreshTokens_UserId ON RefreshTokens(UserId);
CREATE INDEX IX_RefreshTokens_ExpiresAt ON RefreshTokens(ExpiresAt);
```

## Entity Framework Core Configuration

### UserConfiguration
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Email)
            .HasConversion(
                email => email.Value,
                value => new Email(value))
            .HasMaxLength(256)
            .IsRequired();
        
        builder.HasIndex(x => x.Email)
            .IsUnique();
        
        builder.Property(x => x.PasswordHash)
            .HasConversion(
                hash => hash.Value,
                value => new PasswordHash(value))
            .HasMaxLength(128)
            .IsRequired();
        
        builder.Property(x => x.Roles)
            .HasConversion(
                roles => JsonSerializer.Serialize(roles, (JsonSerializerOptions)null),
                json => JsonSerializer.Deserialize<HashSet<string>>(json, (JsonSerializerOptions)null))
            .HasColumnType("nvarchar(max)");
        
        builder.HasMany(x => x.RefreshTokens)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### RefreshTokenConfiguration
```csharp
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Token)
            .HasMaxLength(256)
            .IsRequired();
        
        builder.HasIndex(x => x.Token);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.ExpiresAt);
        
        builder.HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId);
    }
}
```

## Migration Script

```csharp
public partial class AddAuthenticationTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Email = table.Column<string>(maxLength: 256, nullable: false),
                PasswordHash = table.Column<string>(maxLength: 128, nullable: false),
                Roles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                Token = table.Column<string>(maxLength: 256, nullable: false),
                UserId = table.Column<Guid>(nullable: false),
                ExpiresAt = table.Column<DateTime>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                RevokedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_Token",
            table: "RefreshTokens",
            column: "Token");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserId",
            table: "RefreshTokens",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_ExpiresAt",
            table: "RefreshTokens",
            column: "ExpiresAt");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RefreshTokens");
        migrationBuilder.DropTable(name: "Users");
    }
}
```

## Rationale

### Design Decisions
- **Email as unique identifier**: Using email ensures unique user identification and simplifies the login process
- **Roles as JSON**: Storing roles as JSON provides flexibility for role management without additional tables
- **Refresh tokens as separate table**: Allows multiple tokens per user and easy revocation management
- **Cascade delete**: Ensures refresh tokens are automatically cleaned up when users are deleted

### Performance Considerations
- **Indexed Email column**: Optimizes user lookup during login operations
- **Indexed Token column**: Ensures fast token validation during refresh operations
- **Indexed ExpiresAt column**: Enables efficient cleanup of expired tokens
- **PasswordHash storage**: BCrypt hashes are stored efficiently while maintaining security

### Data Integrity Rules
- **Email uniqueness**: Enforced at database level to prevent duplicate accounts
- **Foreign key constraints**: Ensures referential integrity between users and tokens
- **Non-nullable fields**: Critical fields like Email and PasswordHash cannot be null
- **Token expiration tracking**: ExpiresAt field ensures tokens can be validated and cleaned up