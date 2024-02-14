using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using WarehouseManager;
using WarehouseManager.DataAccessLayer;
using WarehouseManager.Services;

const string sourceName = "WarehouseManager";

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json");
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSqlite<WarehouseSqliteDbContext>(builder.Configuration.GetConnectionString("SqliteDatabase"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(sourceName).AddTelemetrySdk())
    .WithMetrics(metrics => metrics
        .AddRuntimeInstrumentation()
        .AddMeter(CustomMetricsService.MeterName)
        .AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("AppSettings:OtelEndpoint"));
            otlpOptions.Protocol = OtlpExportProtocol.Grpc;
        })
    );

builder.Services.AddSingleton<CustomMetricsService>();

var app = builder.Build();
await EnsureDbAsync(app.Services);

// Create Warehouse Value Gauge Metric
app.Services.GetRequiredService<CustomMetricsService>().Meter.CreateObservableGauge<long>("WarehouseValue", () =>
{
    var sum = 0;

    using (var scope = app.Services.CreateScope())
    {
        var warehouseDbContext = scope.ServiceProvider.GetRequiredService<WarehouseSqliteDbContext>()!;
        foreach (var item in warehouseDbContext.WarehouseItems.ToList())
        {
            sum += item.ItemQty * item.ItemValue;
        }
    }

    return sum;
});

// Request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(sourceName);
    logger.LogInformation("Start processing request {method} {url}", context.Request.Method, context.Request.Path);

    await next();

    logger.LogInformation("Finished processing request {method} {url} {statusCode}", context.Request.Method, context.Request.Path, context.Response.StatusCode);
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = string.Empty;
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WarehouseManager API v1");
});

app.MapGet("/warehouseitem", async (WarehouseSqliteDbContext db) =>
{
    var warehouseItems = await db.WarehouseItems.OrderBy(p => p.Name).ToListAsync();

    return warehouseItems;
})
.WithName(EndpointNames.GetWarehouseItems);

app.MapGet("/warehouseitem/{id:guid}", async (Guid id, WarehouseSqliteDbContext db) =>
{
    var warehouseItem = await db.WarehouseItems.FindAsync(id);
    if (warehouseItem is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(warehouseItem);
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.WithName(EndpointNames.GetWarehouseItem);

app.MapPost("warehouseitem", async (HttpRequest req, WarehouseSqliteDbContext db, CustomMetricsService customMetricsService) =>
{
    if (!req.HasJsonContentType())
    {
        return Results.BadRequest();
    }

    var warehouseItem = await req.ReadFromJsonAsync<WarehouseItem>();

    if (warehouseItem is null)
    {
        return Results.BadRequest();
    }

    var id = Guid.NewGuid();
    warehouseItem.Id = id;

    db.WarehouseItems.Add(warehouseItem);
    await db.SaveChangesAsync();

    customMetricsService.GoodsEnvelope.Add(warehouseItem.ItemQty,
        new KeyValuePair<string, object?>("envelope_type", "in"));

    return Results.CreatedAtRoute(EndpointNames.GetWarehouseItem, new { id }, warehouseItem);
})
.WithName(EndpointNames.UploadWarehouseItem)
.Produces(StatusCodes.Status201Created)
.Produces(StatusCodes.Status400BadRequest);

app.MapDelete("/warehouseitem/{id:guid}", async (Guid id, WarehouseSqliteDbContext db, CustomMetricsService customMetricsService) =>
{
    var warehouseItem = await db.WarehouseItems.FindAsync(id);
    if (warehouseItem is null)
    {
        return Results.NotFound();
    }

    db.WarehouseItems.Remove(warehouseItem);
    await db.SaveChangesAsync();

    customMetricsService.GoodsEnvelope.Add(warehouseItem.ItemQty,
        new KeyValuePair<string, object?>("envelope_type", "out"));

    return Results.NoContent();
})
.WithName(EndpointNames.DeleteWarehouseItem)
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

app.MapGet("/health_check", () => { })
.WithName(EndpointNames.HealthCheck)
.Produces(StatusCodes.Status200OK);


app.Run("http://*:5086");

static async Task EnsureDbAsync(IServiceProvider services)
{
    using var db = services.CreateScope().ServiceProvider.GetRequiredService<WarehouseSqliteDbContext>();
    await db.Database.EnsureCreatedAsync();
}
