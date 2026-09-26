using BicisApp.Data;
using BicisApp.Models;
using BicisApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BicisApp.Controllers;

[Authorize]
[Route("Operaciones")]
public class OperacionesController(
    ApplicationDbContext db,
    CacheIncidencias cache,
    ILogger<OperacionesController> logger) : Controller
{
    // GET /Operaciones/Incidencias
    [HttpGet("Incidencias")]
    public async Task<IActionResult> Incidencias()
    {
        // B: listado general cacheado 60 s en Redis
        var (abiertas, origen) = await cache.ObtenerAsync(ConsultarAbiertasAsync);
        return View(new IncidenciasViewModel { Incidencias = abiertas, Origen = origen });
    }

    // POST /Operaciones/Incidencias/5/Cerrar  (solo Supervisor)
    [HttpPost("Incidencias/{id:int}/Cerrar")]
    [Authorize(Roles = DbSeeder.RolSupervisor)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id)
    {
        var incidencia = await db.Incidencias.FindAsync(id);
        if (incidencia is null) return NotFound();
        if (incidencia.Estado == EstadoIncidencia.Cerrada)
        {
            TempData["Error"] = $"La incidencia #{id} ya estaba cerrada.";
            return RedirectToAction(nameof(Incidencias));
        }

        incidencia.Estado = EstadoIncidencia.Cerrada;
        incidencia.FechaCierre = DateTime.UtcNow;
        await db.SaveChangesAsync();
        logger.LogInformation("Incidencia {Id} cerrada en la base de datos", id);

        // B: invalidar la clave del listado ANTES de volver a consultarlo
        await cache.InvalidarAsync();

        TempData["Exito"] = $"Incidencia #{id} cerrada.";
        return RedirectToAction(nameof(Incidencias));
    }

    private Task<List<IncidenciaDto>> ConsultarAbiertasAsync() =>
        db.Incidencias.AsNoTracking()
            .Where(i => i.Estado == EstadoIncidencia.Abierta)
            .OrderBy(i => i.Prioridad).ThenBy(i => i.Id)
            .Select(i => new IncidenciaDto
            {
                Id = i.Id,
                Estacion = i.Estacion,
                Descripcion = i.Descripcion,
                Prioridad = i.Prioridad,
                Estado = i.Estado,
                FechaReporte = i.FechaReporte
            })
            .ToListAsync();
}
