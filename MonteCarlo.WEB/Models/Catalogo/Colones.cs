using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MonteCarlo.WEB.Models.Catalogo;

/// <summary>
/// Formato y lectura de montos en colones costarricenses (CRC): simbolo ₡, espacio como separador
/// de miles y coma decimal; sin decimales cuando el monto es entero (HU-CON-001).
/// </summary>
public static class Colones
{
    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CR");
    private static readonly Regex MilesConPunto = new(@"^\d{1,3}(\.\d{3})+(,\d+)?$", RegexOptions.Compiled);
    private static readonly Regex Numero = new(@"^\d+(\.\d+)?$", RegexOptions.Compiled);

    /// <summary>3500 → ₡3 500; 3500.5 → ₡3 500,5.</summary>
    public static string Formato(decimal monto) => "₡" + monto.ToString("#,##0.##", Cultura);

    /// <summary>Numero plano para volver a mostrar un monto en un campo de texto (3500.5 → "3500,5").</summary>
    public static string ParaCampo(decimal monto) => monto.ToString("0.##", Cultura);

    /// <summary>
    /// Acepta «3500», «3 500», «3.500», «3500,50» y «₡3 500». Rechaza signos negativos, exponentes y texto.
    /// </summary>
    public static bool TryParse(string? texto, out decimal monto)
    {
        monto = 0;
        if (string.IsNullOrWhiteSpace(texto))
            return false;

        var limpio = Regex.Replace(texto, @"[₡\s ]", "");
        limpio = MilesConPunto.IsMatch(limpio)
            ? limpio.Replace(".", "").Replace(',', '.')
            : limpio.Replace(',', '.');

        return Numero.IsMatch(limpio)
            && decimal.TryParse(limpio, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out monto);
    }
}

/// <summary>Valida que un texto sea un monto en colones valido (hasta 2 decimales, no mayor al limite de la base).</summary>
[AttributeUsage(AttributeTargets.Property)]
public class ColonesAttribute : ValidationAttribute
{
    public const decimal Maximo = 99999999.99m;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Required se encarga del campo vacio.
        if (value is not string texto || string.IsNullOrWhiteSpace(texto))
            return ValidationResult.Success;

        if (!Colones.TryParse(texto, out var monto))
            return new ValidationResult("Ingresa un precio válido en colones, por ejemplo 3500.");

        if (monto > Maximo)
            return new ValidationResult($"El precio no puede superar {Colones.Formato(Maximo)}.");

        if (decimal.Round(monto, 2) != monto)
            return new ValidationResult("El precio admite como máximo 2 decimales.");

        return ValidationResult.Success;
    }
}
