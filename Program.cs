using System.Globalization;
using BicisApp.Data;
using BicisApp.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// ---------- Base de datos SQLite + Identity ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bicis.db";
builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite(connectionString));
builder.Services.AddDefaultIdentity<IdentityUser>(o => o.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// ---------- Redis (cache distribuida + llaves de DataProtection) ----------
var redisCs = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisCs))
{
    var redisOptions = ConfigurationOptions.Parse(redisCs);
    redisOptions.AbortOnConnectFail = false;
    var redis = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.ConnectionMultiplexerFactory = () => Task.FromResult<IConnectionMultiplexer>(redis);
        o.InstanceName = "bicis:";
    });
    builder.Services.AddDataProtection().SetApplicationName("BicisApp")
        .PersistKeysToStackExchangeRedis(redis, "bicis:dataprotection-keys");
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// ---------- Configuración de servicios externos (variables de entorno) ----------
builder.Services.Configure<AlgoliaOptions>(builder.Configuration.GetSection("Algolia"));
builder.Services.AddHttpClient<IndexadorAlgolia>();

// ---------- Servicios de las preguntas (A, B, C) ----------
builder.Services.AddHttpClient<BusquedaAlgolia>();   // A: búsqueda en Algolia
builder.Services.AddScoped<CacheIncidencias>();       // B: cache Redis 60 s
builder.Services.Configure<PieHostOptions>(builder.Configuration.GetSection("PieHost"));
builder.Services.AddHttpClient<PublicadorPieHost>();  // C: WebSocket PieHost

builder.Services.AddControllersWithViews();
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
await DbSeeder.InicializarAsync(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
