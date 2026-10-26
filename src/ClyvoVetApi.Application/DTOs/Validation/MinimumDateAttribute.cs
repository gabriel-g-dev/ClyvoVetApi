using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class MinimumDateAttribute(int year, int month, int day) : ValidationAttribute
{
    private readonly DateTime _minimum = new(year, month, day);

    public override bool IsValid(object? value) => value is DateTime date && date >= _minimum;
}
