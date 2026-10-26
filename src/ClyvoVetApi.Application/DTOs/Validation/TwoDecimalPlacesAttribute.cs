using System.ComponentModel.DataAnnotations;

namespace ClyvoVetApi.DTOs.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class TwoDecimalPlacesAttribute : ValidationAttribute
{
    public TwoDecimalPlacesAttribute() : base("Informe um valor com no máximo duas casas decimais.") { }

    public override bool IsValid(object? value) =>
        value is null || value is decimal number && decimal.Round(number, 2) == number;
}
