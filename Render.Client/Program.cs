using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Render.Client.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<RegisterState>();
builder.Services.AddScoped<LoginState>();
builder.Services.AddScoped<CreatePostState>();

builder.Services.AddMudServices(); 



await builder.Build().RunAsync();