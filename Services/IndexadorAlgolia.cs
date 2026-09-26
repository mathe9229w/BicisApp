using System.Net.Http.Json;
using BicisApp.Models;
using Microsoft.Extensions.Options;

namespace BicisApp.Services;

/// <summary>Carga/actualiza las incidencias en el índice de Algolia (REST, clave de administración solo en servidor).</summary>
public class IndexadorAlgolia(HttpClient http, IOptions<AlgoliaOptions> options, ILogger<IndexadorAlgolia> logger)
{
    private readonly AlgoliaOptions _opt = options.Value;

    public async Task IndexarAsync(IEnumerable<Incidencia> incidencias, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.AppId) || string.IsNullOrWhiteSpace(_opt.AdminKey))
        {
            logger.LogWarning("Algolia sin AppId/AdminKey: no se indexa.");
            return;
        }

        var body = new
        {
            requests = incidencias.Select(i => new
            {
                action = "updateObject",
                body = new
                {
                    objectID = i.Id.ToString(),
                    id = i.Id,
                    estacion = i.Estacion,
                    descripcion = i.Descripcion,
                    prioridad = i.Prioridad.ToString(),
                    estado = i.Estado.ToString()
                }
            })
        };

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://{_opt.AppId}.algolia.net/1/indexes/{Uri.EscapeDataString(_opt.IndexName)}/batch")
        {
            Content = JsonContent.Create(body)
        };
        req.Headers.Add("X-Algolia-Application-Id", _opt.AppId);
        req.Headers.Add("X-Algolia-API-Key", _opt.AdminKey);

        try
        {
            var resp = await http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
                logger.LogInformation("Algolia: {Cantidad} incidencias indexadas en '{Indice}'", incidencias.Count(), _opt.IndexName);
            else
                logger.LogError("Algolia respondió {Status}: {Cuerpo}", (int)resp.StatusCode, await resp.Content.ReadAsStringAsync(ct));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo indexar en Algolia");
        }
    }
}
