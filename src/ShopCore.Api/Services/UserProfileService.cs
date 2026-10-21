using Microsoft.EntityFrameworkCore;
using ShopCore.Api.Exceptions;
using ShopCore.Application.Contracts;
using ShopCore.Domain.Entities;
using ShopCore.Infrastructure.Persistence;

namespace ShopCore.Api.Services;

public sealed class UserProfileService(ShopCoreDbContext db) : IUserProfileService
{
    public async Task<UserProfileDto> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        return new UserProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.PhoneNumber);
    }

    public async Task<UserProfileDto> UpdateAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        await db.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.PhoneNumber);
    }

    public async Task<UserShippingAddressDto?> GetShippingAsync(Guid userId, CancellationToken cancellationToken)
    {
        var addr = await db.UserShippingAddresses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (addr is null)
        {
            return null;
        }

        return new UserShippingAddressDto(
            addr.Id,
            addr.UserId,
            addr.RecipientName,
            addr.Phone,
            addr.AddressLine1,
            addr.Ward,
            addr.District,
            addr.Province,
            addr.PostalCode);
    }

    public async Task<UserShippingAddressDto> UpsertShippingAsync(Guid userId, UpsertUserShippingAddressRequest request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var addr = await db.UserShippingAddresses
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

            if (addr is null)
            {
                addr = new UserShippingAddress
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    RecipientName = request.RecipientName.Trim(),
                    Phone = request.Phone.Trim(),
                    AddressLine1 = request.AddressLine1.Trim(),
                    Ward = request.Ward.Trim(),
                    District = request.District.Trim(),
                    Province = request.Province.Trim(),
                    PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim()
                };
                db.UserShippingAddresses.Add(addr);
            }
            else
            {
                addr.RecipientName = request.RecipientName.Trim();
                addr.Phone = request.Phone.Trim();
                addr.AddressLine1 = request.AddressLine1.Trim();
                addr.Ward = request.Ward.Trim();
                addr.District = request.District.Trim();
                addr.Province = request.Province.Trim();
                addr.PostalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
            }

            try
            {
                await db.SaveChangesAsync(cancellationToken);

                return new UserShippingAddressDto(
                    addr.Id,
                    addr.UserId,
                    addr.RecipientName,
                    addr.Phone,
                    addr.AddressLine1,
                    addr.Ward,
                    addr.District,
                    addr.Province,
                    addr.PostalCode);
            }
            catch (DbUpdateException ex)
            {
                if (IsUniqueConstraintViolation(ex) && attempt < 2)
                {
                    db.ChangeTracker.Clear();
                    continue;
                }
                throw;
            }
        }

        var finalAddr = await db.UserShippingAddresses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (finalAddr is null)
        {
            throw new InvalidOperationException("Failed to upsert shipping address.");
        }

        return new UserShippingAddressDto(
            finalAddr.Id,
            finalAddr.UserId,
            finalAddr.RecipientName,
            finalAddr.Phone,
            finalAddr.AddressLine1,
            finalAddr.Ward,
            finalAddr.District,
            finalAddr.Province,
            finalAddr.PostalCode);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null)
        {
            return false;
        }

        var message = inner.Message;
        return message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("IX_UserShippingAddresses_UserId", StringComparison.OrdinalIgnoreCase);
    }
}
