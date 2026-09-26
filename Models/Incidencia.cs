using System.ComponentModel.DataAnnotations;

namespace BicisApp.Models;

public enum EstadoIncidencia { Abierta = 0, Cerrada = 1 }

public enum PrioridadIncidencia { Alta = 1, Media = 2, Baja = 3 }

public class Incidencia
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Estacion { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    public PrioridadIncidencia Prioridad { get; set; } = PrioridadIncidencia.Media;

    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    public DateTime FechaReporte { get; set; } = DateTime.UtcNow;

    public DateTime? FechaCierre { get; set; }
}

/// <summary>Datos que se muestran en la pantalla (serializable).</summary>
public class IncidenciaDto
{
    public int Id { get; set; }
    public string Estacion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public PrioridadIncidencia Prioridad { get; set; }
    public EstadoIncidencia Estado { get; set; }
    public DateTime FechaReporte { get; set; }
}
