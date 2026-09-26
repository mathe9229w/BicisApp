using System.Text.Json;
using BicisApp.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace BicisApp.Services;

/// <summary>Cache Redis (60 s) del listado general de incidencias abiertas.</summary>
public class CacheIncidencias(IDistributedCache cache, ILogger<CacheIncidencias> logger)
{
    public const string Clave = "incidencias:abiertas";
    public static readonly TimeSpan Duracion = TimeSpan.FromSeconds(60);

    /// <summary>Lee de Redis; si no hay, consulta la base y guarda por 60 s. Registra el origen en logs.</summary>
    public async Task<(List<IncidenciaDto> Items, string Origen)> ObtenerAsync(Func<Task<List<IncidenciaDto>>> consultarBase)
    {
        try
        {
            var json = await cache.GetStringAsync(Clave);
            if (json is not null)
            {
                logger.LogInformation("Listado de incidencias leído de REDIS (clave {Clave})", Clave);
                return (JsonSerializer.Deserialize<List<IncidenciaDto>>(json) ?? new(), "Redis");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error leyendo Redis; se usa la base de datos");
        }

        var items = await consultarBase();
        logger.LogInformation("Listado de incidencias leído de la BASE DE DATOS; se guarda en Redis por {Segundos} s", Duracion.TotalSeconds);
        try
        {
            await cache.SetStringAsync(Clave, JsonSerializer.Serialize(items),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = Duracion });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error escribiendo en Redis");
        }
        return (items, "Base de datos");
    }

    public async Task InvalidarAsync()
    {
        try
        {
            await cache.RemoveAsync(Clave);
            logger.LogInformation("Cache Redis invalidada (clave {Clave})", Clave);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error invalidando Redis");
        }
    }
}
