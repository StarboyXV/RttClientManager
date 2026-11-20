using Rtt.ClientManager.Bussiness.Services;
using Rtt.ClientManager.Data;
using Rtt.ClientManager.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Register Connection Factory
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(connectionString));

// Register Data Repositories
builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ILookupRepository, LookupRepository>();

// Register Business Services
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<ICsvExportService, CsvExportService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();