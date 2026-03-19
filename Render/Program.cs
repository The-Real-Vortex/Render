using MudBlazor.Services;
using Render.Client.Pages;
using Render.Components;
using Microsoft.EntityFrameworkCore;
using Render.Data;
using Microsoft.AspNetCore.Components;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Render.Server.Services;
using Render.Client.State;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMudServices();

builder.Services.AddScoped<RegisterState>();
builder.Services.AddScoped<LoginState>();
builder.Services.AddScoped<CreatePostState>();

builder.Services.AddControllers();

builder.Services.AddMemoryCache();

// Configure Redis sharded cache — distributes keys evenly across all databases (0–15)
var redisSettings = builder.Configuration.GetSection("RedisSettings");
var redisConn = redisSettings.GetValue<string>("ConnectionString") ?? "localhost:6379";
var redisDatabaseCount = redisSettings.GetValue<int?>("DatabaseCount") ?? 16;
var redisUsername = redisSettings.GetValue<string>("Username");
var redisPassword = redisSettings.GetValue<string>("Password");

var redisOptions = ConfigurationOptions.Parse(redisConn);
redisOptions.AbortOnConnectFail = false;

if (!string.IsNullOrWhiteSpace(redisUsername))
{
    redisOptions.User = redisUsername;
}

if (!string.IsNullOrWhiteSpace(redisPassword))
{
    redisOptions.Password = redisPassword;
}

var multiplexer = ConnectionMultiplexer.Connect(redisOptions);
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
builder.Services.AddSingleton<IShardedCache>(new ShardedCache(multiplexer, redisDatabaseCount));

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetRequiredService<NavigationManager>().BaseUri)
});

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ILikeService, LikeService>();
builder.Services.AddScoped<Render.Data.DataSeeder>();

builder.Services.AddHttpClient("picsum", client =>
{
    client.BaseAddress = new Uri("https://picsum.photos/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Render API",
        Version = "v1",
        Description = "API Dokumentation für Render"
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddAuthenticationCore();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        // Return 401/403 JSON instead of redirecting
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Render.Client._Imports).Assembly);

app.Run();