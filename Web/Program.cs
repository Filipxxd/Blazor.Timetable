using Timetable.Extensions;
using Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSchoolTimetable();

var app = builder.Build();

app.UseAntiforgery();

app.UseStaticFiles(); // req

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();