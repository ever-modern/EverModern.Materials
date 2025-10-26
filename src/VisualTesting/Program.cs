using DestallMaterials.Blazor.Components.Services.UI;
using VisualTesting.Components;

var builder = WebApplication.CreateBuilder(args);

IServiceCollection services = builder.Services;

services.AddRazorComponents()
    .AddInteractiveServerComponents();

var clickCatcher = new GlobalClickCatcher();
services.AddSingleton<IGlobalClickCatcher>(clickCatcher);
services.AddSingleton<IGlobalClickInvoker>(clickCatcher);
services.AddScoped<IUiManipulator, JsUiManipulator>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
