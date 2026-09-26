using BicisApp.Models;
using BicisApp.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BicisApp.Data;

/// <summary>Crea la base, el rol Supervisor, usuarios y las incidencias de prueba (y las carga en Algolia).</summary>
public static class DbSeeder
{
    public const string RolSupervisor = "Supervisor";
    public const string Password = "Examen2026!";

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var cs = db.Database.GetConnectionString();
        if (!string.IsNullOrWhiteSpace(cs))
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(new SqliteConnectionStringBuilder(cs).DataSource));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }
        await db.Database.EnsureCreatedAsync();

        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var users = sp.GetRequiredService<UserManager<IdentityUser>>();
        if (!await roles.RoleExistsAsync(RolSupervisor)) await roles.CreateAsync(new IdentityRole(RolSupervisor));

        var supervisor = await CrearUsuarioAsync(users, "supervisor@bicis.pe");
        if (!await users.IsInRoleAsync(supervisor, RolSupervisor)) await users.AddToRoleAsync(supervisor, RolSupervisor);
        await CrearUsuarioAsync(users, "operador@bicis.pe");

        if (!await db.Incidencias.AnyAsync())
        {
            db.Incidencias.AddRange(
                new Incidencia { Estacion = "Estación Parque Kennedy", Descripcion = "Candado del anclaje 3 no libera la bicicleta", Prioridad = PrioridadIncidencia.Alta },
                new Incidencia { Estacion = "Estación Larcomar", Descripcion = "Pantalla táctil del tótem apagada", Prioridad = PrioridadIncidencia.Media },
                new Incidencia { Estacion = "Estación Plaza San Martín", Descripcion = "Llanta ponchada en bicicleta B-102", Prioridad = PrioridadIncidencia.Baja },
                new Incidencia { Estacion = "Estación Parque Kennedy", Descripcion = "Frenos desgastados en bicicleta B-215", Prioridad = PrioridadIncidencia.Alta },
                new Incidencia { Estacion = "Estación Barranco", Descripcion = "Cadena suelta en bicicleta B-330", Prioridad = PrioridadIncidencia.Media },
                new Incidencia { Estacion = "Estación Barranco", Descripcion = "Lector de tarjeta sin respuesta", Prioridad = PrioridadIncidencia.Alta },
                new Incidencia { Estacion = "Estación Miraflores Centro", Descripcion = "Luz delantera rota en bicicleta B-018", Prioridad = PrioridadIncidencia.Baja },
                new Incidencia { Estacion = "Estación San Isidro Golf", Descripcion = "Anclaje 7 bloqueado", Prioridad = PrioridadIncidencia.Media },
                new Incidencia { Estacion = "Estación Larcomar", Descripcion = "Timbre roto en bicicleta B-077", Prioridad = PrioridadIncidencia.Baja },
                new Incidencia { Estacion = "Estación Plaza San Martín", Descripcion = "Asiento flojo en bicicleta B-141", Prioridad = PrioridadIncidencia.Media, Estado = EstadoIncidencia.Cerrada, FechaCierre = DateTime.UtcNow });
            await db.SaveChangesAsync();
        }

        // Índice de Algolia con las incidencias de prueba
        var indexador = sp.GetRequiredService<IndexadorAlgolia>();
        await indexador.IndexarAsync(await db.Incidencias.AsNoTracking().ToListAsync());
    }

    private static async Task<IdentityUser> CrearUsuarioAsync(UserManager<IdentityUser> users, string email)
    {
        var u = await users.FindByEmailAsync(email);
        if (u is not null) return u;
        u = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
        var r = await users.CreateAsync(u, Password);
        if (!r.Succeeded) throw new InvalidOperationException(string.Join("; ", r.Errors.Select(e => e.Description)));
        return u;
    }
}
