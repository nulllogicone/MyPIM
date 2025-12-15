using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MyPIM.Data;
using MyPIM.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor(options => 
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
});

// MyPIM Services
builder.Services.AddScoped<PimDataService>();
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
    // Policy: only members of configured security group can access admin features
    options.AddPolicy("AdminGroup", policy =>
        policy.RequireAssertion(ctx =>
        {
            var groupId = builder.Configuration["AdminSecurityGroupId"]; // Object ID of the Entra ID security group
            if (string.IsNullOrWhiteSpace(groupId))
            {
                return false;
            }

            return ctx.User.HasClaim(c =>
                (c.Type == "groups" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups")
                && string.Equals(c.Value, groupId, StringComparison.OrdinalIgnoreCase));
        }));
});

// Database registration
var sqlConn = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(sqlConn))
{
    builder.Services.AddDbContext<MyPimDbContext>(options => options.UseSqlServer(sqlConn));
}
else
{
    // Attempt to load from Key Vault in production
    var vaultUri = builder.Configuration["KeyVault:VaultUri"] ?? builder.Configuration["KeyVault__VaultUri"];
    var secretName = builder.Configuration["KeyVault:SqlConnectionSecretName"] ?? builder.Configuration["KeyVault__SqlConnectionSecretName"] ?? "SqlConnectionString";
    if (!string.IsNullOrWhiteSpace(vaultUri))
    {
        var client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        try
        {
            var secret = client.GetSecret(secretName);
            var kvConn = secret.Value.Value;
            if (!string.IsNullOrWhiteSpace(kvConn))
            {
                builder.Services.AddDbContext<MyPimDbContext>(options => options.UseSqlServer(kvConn));
            }
        }
        catch
        {
            // Swallow and continue without DB if KV is not accessible
        }
    }
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
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
    // Optionally apply EF Core migrations at startup
    var db = scope.ServiceProvider.GetService<MyPimDbContext>();
    if (db != null)
    {
        await db.Database.MigrateAsync();

        // Seed SQL configuration: Scope + Reader Role
        var defaultArmScope = "/subscriptions/87f246bc-1398-416b-8263-c9b08d374e17/resourceGroups/rg-mypim-dev";
        var scopeEntity = await db.Scopes.FirstOrDefaultAsync(s => s.ArmScope == defaultArmScope);
        if (scopeEntity == null)
        {
            scopeEntity = new Scope { Id = Guid.NewGuid(), ArmScope = defaultArmScope };
            db.Scopes.Add(scopeEntity);
        }

        var readerRoleIdGuid = Guid.Parse("acdd72a7-3385-48ef-bd42-f606fba81ae7");
        var readerRole = await db.Roles.FirstOrDefaultAsync(r => r.Id == readerRoleIdGuid);
        if (readerRole == null)
        {
            readerRole = new Role { Id = readerRoleIdGuid, Name = "Reader", ScopeId = scopeEntity.Id };
            db.Roles.Add(readerRole);
        }

        await db.SaveChangesAsync();
    }

    // Publish App Started Event
    var eventService = scope.ServiceProvider.GetRequiredService<IEventService>();
    await eventService.PublishAppStartedAsync();
}

app.Run();
