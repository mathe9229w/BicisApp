namespace BicisApp.Models;

public class IncidenciasViewModel
{
    public List<IncidenciaDto> Incidencias { get; set; } = new();

    /// <summary>Texto buscado en Algolia (vacío = listado habitual).</summary>
    public string? Busqueda { get; set; }
}
