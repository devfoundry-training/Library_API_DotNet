using System.Text.Json;
using System.Text.Json.Serialization;
using Library.Api;
using Library.Api.Data;
using Library.Api.Data.Repositories;
using Library.Api.Endpoints;
using Library.Api.Errors;
using Library.Api.Services;
using Library.Api.Today;

var settings = LibrarySettings.FromEnvironment();
var database = DatabaseUrl.Parse(settings.DatabaseUrl);

// Content root next to the DLL, so appsettings.json is found whatever folder the app starts from.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
});

// Listen on PORT: on every interface inside a container (the official .NET images set
// DOTNET_RUNNING_IN_CONTAINER), only on this machine otherwise.
var host = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ? "0.0.0.0" : "localhost";
builder.WebHost.UseUrls($"http://{host}:{settings.Port}");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

builder.Services.AddSingleton(settings);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IToday, RequestToday>();

// Repository layer, then service layer.
builder.Services.AddLibraryDatabase(database);
builder.Services.AddScoped<BookRepository>();
builder.Services.AddScoped<MemberRepository>();
builder.Services.AddScoped<LoanRepository>();
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<LoanService>();

var app = builder.Build();

await app.Services.PrepareLibraryDatabaseAsync();

app.UseMiddleware<ErrorHandlingMiddleware>();

// Work out today for every request, so a bad X-Today is a 400 on every endpoint.
app.Use((context, next) =>
{
    _ = context.RequestServices.GetRequiredService<IToday>().Value;
    return next(context);
});

app.MapHealthEndpoints();
app.MapBookEndpoints();
app.MapMemberEndpoints();
app.MapLoanEndpoints();
if (settings.TestMode)
{
    app.MapTestEndpoints();
}

app.Run();
