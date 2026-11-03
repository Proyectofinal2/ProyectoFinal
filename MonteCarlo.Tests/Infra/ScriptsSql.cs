using System.Text.RegularExpressions;

namespace MonteCarlo.Tests.Infra;

/// <summary>Lee los scripts de MonteCarlo.API/Data y los adapta (en memoria) a una base temporal.</summary>
public static class ScriptsSql
{
    public static string Leer(string archivo)
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null)
        {
            var ruta = Path.Combine(directorio.FullName, "MonteCarlo.API", "Data", archivo);
            if (File.Exists(ruta)) return File.ReadAllText(ruta);
            directorio = directorio.Parent;
        }
        throw new FileNotFoundException($"No se encontro MonteCarlo.API/Data/{archivo} subiendo desde {AppContext.BaseDirectory}.");
    }

    /// <summary>
    /// Cambia DB_ID('MonteCarlo'), CREATE DATABASE MonteCarlo y USE MonteCarlo por la base temporal, y aborta si
    /// queda cualquier referencia a la base real. Devuelve los lotes (separados por lineas GO).
    /// </summary>
    public static List<string> Transformar(string script, string nombreBase)
    {
        if (!Regex.IsMatch(nombreBase, "^MonteCarlo_test_[0-9a-f]{8}$"))
            throw new InvalidOperationException($"Nombre de base no permitido: {nombreBase}.");

        var adaptado = Regex.Replace(script, @"DB_ID\('MonteCarlo'\)", $"DB_ID('{nombreBase}')");
        adaptado = Regex.Replace(adaptado, @"CREATE\s+DATABASE\s+MonteCarlo\b", $"CREATE DATABASE {nombreBase}", RegexOptions.IgnoreCase);
        adaptado = Regex.Replace(adaptado, @"\bUSE\s+MonteCarlo\b", $"USE {nombreBase}", RegexOptions.IgnoreCase);

        if (Regex.IsMatch(adaptado, @"DB_ID\('MonteCarlo'\)|CREATE\s+DATABASE\s+MonteCarlo\b|\bUSE\s+MonteCarlo\b|\[MonteCarlo\]", RegexOptions.IgnoreCase))
            throw new InvalidOperationException("El script adaptado aun referencia la base real MonteCarlo: se aborta.");

        return Lotes(adaptado);
    }

    public static List<string> Lotes(string script) =>
        Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();
}
