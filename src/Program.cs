using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MyPIM.Data;
using MyPIM.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// MyPIM Services
builder.Services.AddSingleton<PimTableService>();
builder.Services.AddSingleton<IGraphService, MockGraphService>();
builder.Services.AddHostedService<RevocationWorker>();

// Fake Auth for Mock
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies");
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, MockAuthStateProvider>();


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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
