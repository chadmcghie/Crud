namespace Domain.Events
{
    public class TokenRefreshedEvent
    {
        public Guid UserId { get; }
        public DateTime RefreshedAt { get; }
        public string OldTokenId { get; }
        public string NewTokenId { get; }

        public TokenRefreshedEvent(Guid userId, DateTime refreshedAt, string oldTokenId, string newTokenId)
        {
            UserId = userId;
            RefreshedAt = refreshedAt;
            OldTokenId = oldTokenId;
            NewTokenId = newTokenId;
        }
    }
}
