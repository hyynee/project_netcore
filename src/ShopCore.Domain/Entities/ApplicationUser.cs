using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace ShopCore.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<IdempotencyRecord> IdempotencyRecords { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    [MaxLength(100)]
    public string? FirstName { get; set; }
    [MaxLength(100)]
    public string? LastName { get; set; }
    public UserShippingAddress? ShippingAddress { get; set; }
}
