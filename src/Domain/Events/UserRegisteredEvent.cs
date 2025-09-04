namespace Domain.Events
{
    public class UserRegisteredEvent
    {
        public Guid UserId { get; }
        public string Email { get; }
        public DateTime RegisteredAt { get; }

        public UserRegisteredEvent(Guid userId, string email, DateTime registeredAt)
        {
            UserId = userId;
            Email = email;
            RegisteredAt = registeredAt;
        }
    }
}