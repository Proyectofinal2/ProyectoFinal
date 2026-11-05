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