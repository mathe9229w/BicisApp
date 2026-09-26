namespace BicisApp.Services;

/// <summary>Configuración de PieHost / PieSocket (variables PieHost__*).</summary>
public class PieHostOptions
{
    /// <summary>Cluster ID (ej. "s1234") o dominio completo del cluster (ej. "s1234.nyc1.piesocket.com").</summary>
    public string ClusterId { get; set; } = string.Empty;
    /// <summary>API Key pública del cluster (la usa también el navegador para suscribirse).</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>API Secret: solo en el servidor para publicar.</summary>
    public string ApiSecret { get; set; } = string.Empty;
    public string Channel { get; set; } = "incidencias";

    public string Dominio => ClusterId.Contains('.') ? ClusterId : $"{ClusterId}.piesocket.com";
    public bool Configurado => !string.IsNullOrWhiteSpace(ClusterId) && !string.IsNullOrWhiteSpace(ApiKey);
}
