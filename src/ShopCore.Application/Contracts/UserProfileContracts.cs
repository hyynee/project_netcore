using System.ComponentModel.DataAnnotations;

namespace ShopCore.Application.Contracts;

public sealed record UserProfileDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? PhoneNumber);

public sealed record UpdateUserProfileRequest(
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [MaxLength(30)] string? PhoneNumber);

public sealed record UserShippingAddressDto(
    Guid Id,
    Guid UserId,
    string RecipientName,
    string Phone,
    string AddressLine1,
    string Ward,
    string District,
    string Province,
    string? PostalCode);

public sealed record UpsertUserShippingAddressRequest(
    [Required, MaxLength(200)] string RecipientName,
    [Required, MaxLength(30)] string Phone,
    [Required, MaxLength(500)] string AddressLine1,
    [Required, MaxLength(150)] string Ward,
    [Required, MaxLength(150)] string District,
    [Required, MaxLength(150)] string Province,
    [MaxLength(20)] string? PostalCode);
