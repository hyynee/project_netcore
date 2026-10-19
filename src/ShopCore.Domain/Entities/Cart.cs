namespace ShopCore.Domain.Entities;

public sealed class Cart : Entity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public int Version { get; set; }
    public byte[] RowVersion { get; set; } = [];
    public ICollection<CartItem> Items { get; set; } = [];
}
