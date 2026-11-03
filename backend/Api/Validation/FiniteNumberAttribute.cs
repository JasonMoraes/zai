using System.ComponentModel.DataAnnotations;

namespace Api.Validation;

/// <summary>
/// Rejects infinities and NaN. System.Text.Json reads an out-of-range literal such as <c>1e400</c> as
/// <see cref="double.PositiveInfinity"/>, which is malformed input (400), not a value to apply business rules to.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class FiniteNumberAttribute() : ValidationAttribute("Wartość musi być skończoną liczbą.")
{
    public override bool IsValid(object? value) => value is not double number || double.IsFinite(number);
}
