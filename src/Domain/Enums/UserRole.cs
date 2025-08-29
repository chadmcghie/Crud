namespace Domain.Enums
{
    public static class UserRole
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static readonly IReadOnlyList<string> AllRoles = new List<string>
        {
            Admin,
            User
        };

        public static bool IsValidRole(string role)
        {
            return AllRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
        }
    }
}