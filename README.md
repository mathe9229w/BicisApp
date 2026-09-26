# BicisApp – Averías de estaciones (Examen Parcial)

**URL en Render:** https://bicisapp.onrender.com
**Repositorio:** https://github.com/mathe9229w/BicisApp

ASP.NET Core MVC (.NET 10) + Identity + EF Core/SQLite · Búsqueda **Algolia** · Caché **Redis** · WebSocket **PieHost** · Render (Docker).

Usuarios (contraseña `Examen2026!`): `supervisor@bicis.pe` (rol Supervisor, puede cerrar) y `operador@bicis.pe` (consulta).

## Ramas y PRs

| Rama | PR | Cambio | Título compartido |
|---|---|---|---|
| `feature/busqueda-algolia` (A) | #1 | Búsqueda en Algolia desde el servidor; solo muestra incidencias **abiertas existentes en la base** | Incidencias abiertas encontradas |
| `feature/cache-redis` (B) | #2 | Caché Redis 60 s del listado general; invalidación al cerrar; logs Redis/Base | Incidencias abiertas con consulta rápida |
| `feature/websocket-piehost` (C) | #3 | Evento `IncidenciaActualizada {Id, Estado}` publicado desde el servidor tras guardar; lista sin recargar; resincroniza al reconectar | Incidencias abiertas en tiempo real |

A, B y C nacen del **mismo commit inicial** de `main` (`d949194`). Orden de fusión: **A → B → C**, con merge commits (sin squash ni force push).

## Resolución de conflictos

1. **Primer conflicto (B con main que ya tenía A)** – commit `merge: incorporar main (A) en B…`
   - Controlador: con búsqueda vacía se usa el listado **cacheado en Redis**; con texto se consulta **Algolia directamente, sin la caché**.
   - Se conservan ambos servicios en `Program.cs` y ambas propiedades del ViewModel (`Busqueda`, `Origen`).
   - Título: *Incidencias abiertas encontradas con consulta rápida*.
2. **Segundo conflicto (C con main que ya tenía A+B)** – commit `merge: incorporar main (A+B) en C…`
   - Se inyectan los tres servicios (Algolia, caché, PieHost).
   - Orden al cerrar: **1) guardar en la base → 2) invalidar Redis → 3) publicar en PieHost**.
   - Título final: *Incidencias abiertas encontradas, con consulta rápida y en tiempo real*.

Historial: `git log --graph --oneline --all` (ver captura en la entrega).

## Variables de entorno (Render → Environment; nunca en el repo)

| Variable | Uso |
|---|---|
| `ConnectionStrings__DefaultConnection` | `Data Source=/var/data/bicis.db` |
| `Redis__ConnectionString` | `host:puerto,password=...,abortConnect=false` |
| `Algolia__AppId`, `Algolia__SearchKey`, `Algolia__AdminKey`, `Algolia__IndexName` | Búsqueda (SearchKey) e indexación inicial (AdminKey), solo en el servidor |
| `PieHost__ClusterId`, `PieHost__ApiKey`, `PieHost__ApiSecret`, `PieHost__Channel` | Publicar (servidor, con secret) y suscribirse (navegador, solo ApiKey) |
| `ASPNETCORE_ENVIRONMENT` | `Production` |

El `Dockerfile` expande `$PORT` en el comando de inicio.

## Pruebas en Render

1. **Login** con `supervisor@bicis.pe` → *Incidencias*.
2. **Redis**: abrir el listado dos veces → badge **Redis** y en logs `Listado de incidencias leído de REDIS`. Expira a los 60 s.
3. **Algolia**: buscar `Kennedy` o `frenos` → resultados del índice, solo abiertas.
4. **PieHost**: dos sesiones (supervisor y operador en incógnito). El supervisor cierra una incidencia → en la otra sesión la fila desaparece sin recargar. Logs: `cerrada en la base` → `Cache Redis invalidada` → `PieHost: publicado IncidenciaActualizada`.
5. Buscar de nuevo en Algolia la incidencia cerrada → **ya no aparece** (se filtra con el estado de la base).

## Commit desplegado

Render despliega automáticamente el último commit de `main` (ver Render → Deploys). El commit funcional final es el merge del PR #3 (`dcb6a65`).
