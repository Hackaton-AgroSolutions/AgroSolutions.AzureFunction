using AgroSolutions.AzureFunction.Domain.Common;

namespace AgroSolutions.AzureFunction.Domain.Events;

public record ReceivedSensorDataEvent(
    Guid SensorClientId,
    Guid CorrelationId,
    int FieldId,
    decimal PrecipitationMm,
    decimal WindSpeedKmh,
    float SoilPH,
    decimal AirTemperatureC,
    float AirHumidityPercent,
    float SoilMoisturePercent,
    float DataQualityScore) : IDomainEvent
{
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
}
