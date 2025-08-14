using BeachHouse.UI.Components;
using BeachHouse.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SqlDataAccess>();
builder.Services.AddScoped<BrokerageService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<IDataIngestionService, DataIngestionService>();
builder.Services.AddScoped<IBacktestService, BacktestService>(); // <-- Add this line

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
