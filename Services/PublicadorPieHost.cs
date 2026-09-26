using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace BicisApp.Services;

/// <summary>Publica eventos en el canal de PieHost desde el SERVIDOR (API /api/publish con key + secret).</summary>
public class PublicadorPieHost(HttpClient http, IOptions<PieHostOptions> options, ILogger<PublicadorPieHost> logger)
{
    public const string EventoIncidenciaActualizada = "IncidenciaActualizada";
    private readonly PieHostOptions _opt = options.Value;

    public async Task PublicarIncidenciaActualizadaAsync(int id, string estado, CancellationToken ct = default)
    {
        if (!_opt.Configurado || string.IsNullOrWhiteSpace(_opt.ApiSecret))
        {
            logger.LogWarning("PieHost no configurado: no se publica {Evento}", EventoIncidenciaActualizada);
            return;
        }

        var body = new
        {
            key = _opt.ApiKey,
            secret = _opt.ApiSecret,
            channelId = _opt.Channel,
            message = new
            {
                @event = EventoIncidenciaActualizada,
                data = new { Id = id, Estado = estado }
            }
        };

        try
        {
            var resp = await http.PostAsJsonAsync($"https://{_opt.Dominio}/api/publish", body, ct);
            var texto = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
                logger.LogInformation("PieHost: publicado {Evento} Id={Id} Estado={Estado} en canal '{Canal}'", EventoIncidenciaActualizada, id, estado, _opt.Channel);
            else
                logger.LogError("PieHost respondió {Status}: {Cuerpo}", (int)resp.StatusCode, texto);
        }
        catch (Exception ex)
        {
            // El estado ya está guardado; los clientes lo recuperan al reconectar.
            logger.LogError(ex, "No se pudo publicar en PieHost");
        }
    }
}
