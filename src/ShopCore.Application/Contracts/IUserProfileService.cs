namespace ShopCore.Application.Contracts;

public interface IUserProfileService
{
    Task<UserProfileDto> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserProfileDto> UpdateAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken);
    Task<UserShippingAddressDto?> GetShippingAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserShippingAddressDto> UpsertShippingAsync(Guid userId, UpsertUserShippingAddressRequest request, CancellationToken cancellationToken);
}
