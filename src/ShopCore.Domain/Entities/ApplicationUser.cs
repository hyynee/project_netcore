namespace ShopCore.Domain.Entities;

public sealed class ApplicationUser : Entity
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<IdempotencyRecord> IdempotencyRecords { get; set; } = [];
}
