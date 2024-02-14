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
