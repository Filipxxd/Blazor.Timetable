using Blazor.Timetable.Common.Extensions;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();// req

builder.Services.AddBlazorTimetable(); // req

var app = builder.Build();

app.UseAntiforgery();

app.UseStaticFiles(); // req

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); // req

await app.RunAsync();