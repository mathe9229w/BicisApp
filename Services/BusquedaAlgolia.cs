using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace BicisApp.Services;

/// <summary>Consulta el índice de Algolia desde el SERVIDOR (la clave nunca llega al navegador).</summary>
public class BusquedaAlgolia(HttpClient http, IOptions<AlgoliaOptions> options, ILogger<BusquedaAlgolia> logger)
{
    private readonly AlgoliaOptions _opt = options.Value;

    /// <summary>Devuelve los Id de incidencias cuyo nombre de estación o descripción coinciden con el texto.</summary>
    public async Task<List<int>> BuscarIdsAsync(string texto, CancellationToken ct = default)
    {
        if (!_opt.Configurado) throw new InvalidOperationException("Algolia no está configurado (Algolia__AppId / Algolia__SearchKey).");

        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://{_opt.AppId}-dsn.algolia.net/1/indexes/{Uri.EscapeDataString(_opt.IndexName)}/query")
        {
            Content = JsonContent.Create(new
            {
                query = texto,
                hitsPerPage = 100,
                restrictSearchableAttributes = new[] { "estacion", "descripcion" }
            })
        };
        req.Headers.Add("X-Algolia-Application-Id", _opt.AppId);
        req.Headers.Add("X-Algolia-API-Key", _opt.SearchKey);

        var resp = await http.SendAsync(req, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogError("Algolia respondió {Status}: {Cuerpo}", (int)resp.StatusCode, json);
            throw new InvalidOperationException("Error consultando Algolia.");
        }

        using var doc = JsonDocument.Parse(json);
        var ids = new List<int>();
        foreach (var hit in doc.RootElement.GetProperty("hits").EnumerateArray())
        {
            if (hit.TryGetProperty("objectID", out var oid) && int.TryParse(oid.GetString(), out var id))
                ids.Add(id);
        }
        logger.LogInformation("Algolia '{Texto}': {Cantidad} coincidencias en el índice", texto, ids.Count);
        return ids;
    }
}
