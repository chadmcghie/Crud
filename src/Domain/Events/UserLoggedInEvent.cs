namespace Domain.Events
{
    public class UserLoggedInEvent
    {
        public Guid UserId { get; }
        public string Email { get; }
        public DateTime LoggedInAt { get; }

        public UserLoggedInEvent(Guid userId, string email, DateTime loggedInAt)
        {
            UserId = userId;
            Email = email;
            LoggedInAt = loggedInAt;
        }
    }
}
