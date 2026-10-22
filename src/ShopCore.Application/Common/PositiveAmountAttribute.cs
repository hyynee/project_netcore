using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ShopCore.Application.Common;

/// <summary>
/// Culture independent validation for money amounts.
/// <see cref="RangeAttribute"/> parses its string bounds with <see cref="System.Globalization.CultureInfo.CurrentCulture"/>,
/// which throws when the server runs under a culture that uses "," as the decimal separator.
/// </summary>
/// <remarks>
/// The amount must also be a whole number because the price columns are stored as
/// <c>decimal(18,0)</c>. A fractional value would be rounded silently by SQL Server.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PositiveAmountAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value switch
    {
        null => true,
        decimal amount => IsWholePositive(amount),
        _ => false
    };

    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.InvariantCulture, "{0} must be a whole number greater than zero.", name);

    public static bool IsWholePositive(decimal amount) => amount > 0m && decimal.Truncate(amount) == amount;
}
