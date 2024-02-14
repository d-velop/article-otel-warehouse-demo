# Article listings

## listing 1

```cs
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
```

## listing 2

```cs
using System.Diagnostics.Metrics;

namespace WarehouseManager.Services;

public class CustomMetricsService
{
    public const string MeterName = "CustomMetrics";
    public readonly Meter Meter;
    public readonly Counter<int> GoodsEnvelope;

    public CustomMetricsService()
    {
        Meter = new Meter(MeterName);
        GoodsEnvelope = Meter.CreateCounter<int>("GoodsEnvelope");
    }
}
```

## listing 3

```cs
customMetricsService.GoodsEnvelope.Add(warehouseItem.ItemQty,
  new KeyValuePair<string, object?>("envelope_type", "in"));
```

## listing 4

```cs
// Create Warehouse Value Gauge Metric
app.Services.GetRequiredService<CustomMetricsService>().Meter
  .CreateObservableGauge<long>("WarehouseValue", () =>
{
   var sum = 0;

   using (var scope = app.Services.CreateScope())
   {
       var warehouseDbContext = scope.ServiceProvider
         .GetRequiredService<WarehouseSqliteDbContext>()!;
       foreach (var item in warehouseDbContext.WarehouseItems.ToList())
       {
           sum += item.ItemQty * item.ItemValue;
       }
   }

   return sum;
});
```

## listing 5

```yaml
receivers:
  otlp:
    protocols:
      grpc:

exporters:
  prometheus:
    endpoint: "0.0.0.0:8889"

processors:
  batch:
  metricstransform:
    transforms:
      - include: .*
        match_type: regexp
        action: update
        operations:
          - action: add_label
            new_label: deployment
            new_value: green

# . . . omitted

service:
  # . . . omitted
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch, metricstransform]
      exporters: [prometheus]
```

## listing 6

```shell
curl --location --request POST 'http://localhost:5086/warehouseitem' \
  --header 'Content-Type: application/json' \
  --data-raw '{
    "name": "Red Bricks Type 42",
    "itemValue": "4",
    "itemQty": 250000
  }'
```

## listing 7

```shell
curl --location --request DELETE
  'http://localhost:5086/warehouseitem/25acc7c1-6417-4f11-91bf-8db2d9f1ad86'
```

## listing 8

```yaml
  pipelines:
    # This pipeline collects metrics from the WarehouseManager app
    metrics/warehousemanager:
      receivers: [otlp]
      processors: [k8sattributes, metricstransform, batch]
      exporters: [prometheusremotewrite]
    # This pipeline collects cluster level metrics
    metrics/cluster:
      receivers: [k8s_cluster, kubeletstats]
      processors: [batch]
      exporters: [prometheusremotewrite]
```
