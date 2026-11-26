using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

// helper for mapping the database to the model
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = Host.CreateApplicationBuilder(args);

// logs go to stdrr. Stdout are reserved for mcp protocol messages
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

var connenctionString =
    builder.Configuration["CYKELSERVICE_DB"]
    ?? throw new InvalidOperationException("Missing connection string for CYKELSERVICE_DB");

builder.Services.AddSingleton(NpgsqlDataSource.Create(connenctionString));
builder.Services.AddSingleton<IRepairQueries, RepairQueries>();
builder.Services.AddSingleton<ICustomerQueries, CustomerQueries>();

builder.Services.AddMcpServer().WithStdioServerTransport().WithToolsFromAssembly();

await builder.Build().RunAsync();
