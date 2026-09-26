namespace BicisApp.Services;

/// <summary>Configuración de Algolia (variables Algolia__*). Las claves nunca se envían al navegador.</summary>
public class AlgoliaOptions
{
    public string AppId { get; set; } = string.Empty;
    public string SearchKey { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
    public string IndexName { get; set; } = "incidencias";

    public bool Configurado => !string.IsNullOrWhiteSpace(AppId) && !string.IsNullOrWhiteSpace(SearchKey);
}
