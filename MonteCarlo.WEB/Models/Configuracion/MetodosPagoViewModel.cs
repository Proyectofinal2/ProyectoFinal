namespace MonteCarlo.WEB.Models.Configuracion;

public class MetodoPagoAdminApiResponse
{
    public int IdMetodoPago { get; set; }
    public string Nombre { get; set; } = "";
    public bool Activo { get; set; }
}

public class MetodosPagoViewModel
{
    public List<MetodoPagoAdminApiResponse> Metodos { get; set; } = [];
}

public class HistorialMetodoPagoApiResponse
{
    public int IdHistorialMetodoPago { get; set; }
    public int IdMetodoPago { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string? NombreAnterior { get; set; }
    public string NombreNuevo { get; set; } = string.Empty;
    public bool? ActivoAnterior { get; set; }
    public bool ActivoNuevo { get; set; }
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string RolUsuario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
}

public class UsuarioHistorialMetodoPagoApiResponse
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

public class PaginaHistorialMetodoPagoApiResponse
{
    public List<HistorialMetodoPagoApiResponse> Items { get; set; } = [];
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; }
    public int Total { get; set; }
    public int TotalPaginas { get; set; }
}

public class HistorialMetodosPagoViewModel
{
    public string? Busqueda { get; set; }
    public int? IdMetodoPago { get; set; }
    public int? IdUsuario { get; set; }
    public string? Accion { get; set; }
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public string? Error { get; set; }
    public List<MetodoPagoAdminApiResponse> Metodos { get; set; } = [];
    public List<UsuarioHistorialMetodoPagoApiResponse> Usuarios { get; set; } = [];
    public PaginaHistorialMetodoPagoApiResponse Pagina { get; set; } = new();

    public bool TieneFiltros =>
        !string.IsNullOrWhiteSpace(Busqueda) || IdMetodoPago.HasValue || IdUsuario.HasValue ||
        !string.IsNullOrWhiteSpace(Accion) || Desde.HasValue || Hasta.HasValue;
}
