using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MyPIM.Data;
using MyPIM.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// MyPIM Services
builder.Services.AddSingleton<PimTableService>();
builder.Services.AddSingleton<IGraphService, AzureRbacGraphService>();
builder.Services.AddSingleton<IEventService, EventGridService>();
builder.Services.AddHostedService<RevocationWorker>();

// Azure AD Auth
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddControllersWithViews()
    .AddMicrosoftIdentityUI();

builder.Services.AddAuthorization(options =>
{
    // By default, all incoming requests will be authorized according to the default policy
    options.FallbackPolicy = options.DefaultPolicy;
});

// builder.Services.AddScoped<AuthenticationStateProvider, MockAuthStateProvider>(); // Removed Mock


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Seed Configuration
using (var scope = app.Services.CreateScope())
{
    var pimService = scope.ServiceProvider.GetRequiredService<PimTableService>();
    var existingConfigs = await pimService.GetConfigurationsAsync();
    
    // Seed Reader Role if not present
    var readerRoleId = "acdd72a7-3385-48ef-bd42-f606fba81ae7"; // Built-in Reader
    if (!existingConfigs.Any(c => c.RowKey == readerRoleId))
    {
        await pimService.SaveConfigurationAsync(new PimRoleConfiguration
        {
            RowKey = readerRoleId,
            RoleName = "Reader",
            DefaultDurationMinutes = 10,
            TargetScope = "/subscriptions/87f246bc-1398-416b-8263-c9b08d374e17/resourceGroups/rg-mypim-dev",
            IsEnabled = true
        });
    }
}

app.Run();
