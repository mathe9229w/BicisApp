namespace BicisApp.Models;

public class IncidenciasViewModel
{
    public List<IncidenciaDto> Incidencias { get; set; } = new();

    /// <summary>Origen del listado: "Redis" o "Base de datos".</summary>
    public string? Origen { get; set; }
}
