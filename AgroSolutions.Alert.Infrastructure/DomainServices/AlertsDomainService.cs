using AgroSolutions.Alert.Domain.DomainServices.Interfaces;
using AgroSolutions.Alert.Domain.Events;
using AgroSolutions.Alert.Infrastructure.Interfaces;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core.Flux.Domain;
using InfluxDB.Client.Writes;
using Serilog;

namespace AgroSolutions.Alert.Infrastructure.DomainServices;

public class AlertsDomainService(IInfluxDbService influxDb) : IAlertsDomainService
{
    private readonly IInfluxDbService _influxDb = influxDb;
    private readonly string _bucket = Environment.GetEnvironmentVariable("InfluxDB:Bucket")!;

    public async Task<byte> CheckAllRulesAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        if (await CheckDroughtAlertAsync(receivedSensorDataEvent))
            return 1;

        if (await CheckPlagueRiskAsync(receivedSensorDataEvent))
            return 2;

        if (await CheckSensorWithLowDataQualityAsync(receivedSensorDataEvent))
            return 3;

        if (await CheckHeatWaveAsync(receivedSensorDataEvent))
            return 4;

        if (await CheckHighProbabilityOfFungalDiseasesAsync(receivedSensorDataEvent))
            return 5;

        if (await CheckHighAcidityWithPotentialForReducedNutrientAbsorptionAsync(receivedSensorDataEvent))
            return 6;

        return 0;
    }

    // Rule number 1: If soil moisture is below 30% in the last 24 hours → "Drought Alert"
    private async Task<bool> CheckDroughtAlertAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        IEnumerable<FluxTable> tables = await _influxDb.QueryAsync($"from(bucket: \"{_bucket}\")"+
            "   |> range(start: -24h)"+
            "   |> filter(fn: (r) => r._measurement == \"agro_sensors\")"+
            "   |> filter(fn: (r) => r.field_id == \"{receivedSensorDataEvent.FieldId}\")"+
            "   |> filter(fn: (r) => r._field == \"soil_moisture_percent\")"+
            "   |> min()"
        );
        if (float.Parse(tables.SelectMany(t => t.Records).FirstOrDefault()?.Values["_value"].ToString() ?? "0") >= 30)
            return false;

        Log.Warning("The field with ID {FieldId} and the sensor with ID {SensorClientId} are at risk of drying out.", receivedSensorDataEvent.FieldId, receivedSensorDataEvent.SensorClientId);
        PointData alertPointData = PointData
            .Measurement("alerts")
            .Tag("sensor_client_id", receivedSensorDataEvent.SensorClientId.ToString())
            .Tag("field_id", receivedSensorDataEvent.FieldId.ToString())
            .Field("message", $"The field with ID {receivedSensorDataEvent.FieldId} and the sensor with ID {receivedSensorDataEvent.SensorClientId} are at risk of drying out!")
            .Timestamp(receivedSensorDataEvent.Timestamp, WritePrecision.Ns);

        await _influxDb.WritePointDataAsync(alertPointData);
        return true;
    }

    // Rule number 2: If the average temperature over the last 12 hours is between 22°C and 30°C and the average air humidity over the last 12 hours is greater than 80% → "Plague Risk"
    private async Task<bool> CheckPlagueRiskAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        IEnumerable<FluxTable> tables = await _influxDb.QueryAsync($@"temp =
            from(bucket: ""{_bucket}"")
                |> range(start: -12h)
                |> filter(fn: (r) => r.sensor_client_id == ""{receivedSensorDataEvent.SensorClientId}"" and r._field == ""temperature"")
                |> mean()

            humidity =
                from(bucket: ""{_bucket}"")
                    |> range(start: -12h)
                    |> filter(fn: (r) => r.sensor_client_id == ""{receivedSensorDataEvent.SensorClientId}"" and r._field == ""humidity"")
                    |> mean()

            join(tables: {{temp: temp, humidity: humidity}}, on: [""_start"",""_stop""])
                |> map(fn: (r) => ({{
                    _value: r.temp._value >= 22.0 and 
                            r.temp._value <= 30.0 and 
                            r.humidity._value > 80.0
            }}))");

        if (!tables.SelectMany(t => t.Records).Any())
            return false;

        Log.Warning("The field with ID {FieldId} and the sensor with ID {SensorClientId} present a pest.", receivedSensorDataEvent.FieldId, receivedSensorDataEvent.SensorClientId);
        PointData alertPointData = PointData
            .Measurement("alerts")
            .Tag("sensor_client_id", receivedSensorDataEvent.SensorClientId.ToString())
            .Tag("field_id", receivedSensorDataEvent.FieldId.ToString())
            .Field("message", $"The field with ID {receivedSensorDataEvent.SensorClientId} and the sensor with ID {receivedSensorDataEvent.FieldId} present a pest!")
            .Timestamp(receivedSensorDataEvent.Timestamp, WritePrecision.Ns);
        await _influxDb.WritePointDataAsync(alertPointData);
        return true;
    }

    // Rule number 3: If data quality score < 70 in the last 6 hours → "Sensor with Low Data Quality"
    private async Task<bool> CheckSensorWithLowDataQualityAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        IEnumerable<FluxTable> tables = await _influxDb.QueryAsync($@"from(bucket: ""{_bucket}"")
            |> range(start: -6h)
            |> filter(fn: (r) => r._measurement == ""agro_sensors"")
            |> filter(fn: (r) => r.sensor_client_id == ""{receivedSensorDataEvent.SensorClientId}"")
            |> filter(fn: (r) => r._field == ""data_quality_score"")
            |> min()");

        if (float.Parse(tables.SelectMany(t => t.Records).FirstOrDefault()?.Values["_value"].ToString() ?? "0") >= 70)
            return false;

        Log.Warning("The Sensor with Id {SensorClientId} in the Field with Id {FieldId} has low data quality.", receivedSensorDataEvent.SensorClientId, receivedSensorDataEvent.FieldId);
        PointData alertPointData = PointData
            .Measurement("alerts")
            .Tag("sensor_client_id", receivedSensorDataEvent.SensorClientId.ToString())
            .Tag("field_id", receivedSensorDataEvent.FieldId.ToString())
            .Field("message", $"The Sensor with Id {receivedSensorDataEvent.SensorClientId} in the Field with Id {receivedSensorDataEvent.FieldId} has low data quality!")
            .Timestamp(receivedSensorDataEvent.Timestamp, WritePrecision.Ns);
        await _influxDb.WritePointDataAsync(alertPointData);
        return true;
    }

    // Rule number 4: If the air temperature is above 35°C for 3 consecutive days and no have chanfe of rain in nexts 24 hours → "Heat Wave – Potential Impact on Production"
    private async Task<bool> CheckHeatWaveAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        IEnumerable<FluxTable> tables = await _influxDb.QueryAsync($@"from(bucket: ""{_bucket}"")
            |> range(start: -3d)
            |> filter(fn: (r) => r._measurement == ""agro_sensors"")
            |> filter(fn: (r) => r.sensor_client_id == ""{receivedSensorDataEvent.SensorClientId}"")
            |> filter(fn: (r) => r._field == ""air_temperature_c"")
            |> aggregateWindow(every: 1d, fn: min, createEmpty: false)
            |> filter(fn: (r) => r._value > 35)");
        IEnumerable<FluxTable> tablesWeather = await _influxDb.QueryAsync($@"import ""experimental""
from(bucket: ""{_bucket}"")
    |> range(start: now(), stop: experimental.addDuration(d: 3d, to: now()))
    |> filter(fn: (r) => r._measurement == ""weather_forecast"")
    |> filter(fn: (r) => r.city == ""sao_paulo"")
    |> filter(fn: (r) => r._field == ""rain_probability"")
    |> max()");
        double maxRainProbability = tablesWeather
            .SelectMany(t => t.Records.Select(r => Convert.ToDouble(r.GetValue())))
            .FirstOrDefault();

        if (!(tables.SelectMany(t => t.Records).Count() == 3 && maxRainProbability < 60))
            return false;

        Log.Warning("The Sensor with Id {SensorClientId} in the Field with Id {FieldId} detected an upcoming heat wave.", receivedSensorDataEvent.SensorClientId, receivedSensorDataEvent.FieldId);
        PointData alertPointData = PointData
            .Measurement("alerts")
            .Tag("sensor_client_id", receivedSensorDataEvent.SensorClientId.ToString())
            .Tag("field_id", receivedSensorDataEvent.FieldId.ToString())
            .Field("message", $"The Sensor with Id {receivedSensorDataEvent.SensorClientId} in the Field with Id {receivedSensorDataEvent.FieldId} detected an upcoming heat wave.")
            .Timestamp(receivedSensorDataEvent.Timestamp, WritePrecision.Ns);
        await _influxDb.WritePointDataAsync(alertPointData);
        return true;
    }

    // Rule number 5: If soil moisture is above 70% and air humidity is above 85% and the air temperature is between 20°C and 30°C for 8 hours → "High Probability of Fungal Diseases"
    private async Task<bool> CheckHighProbabilityOfFungalDiseasesAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        IEnumerable<FluxTable> tables = tables = await _influxDb.QueryAsync($@"from(bucket:""{_bucket}"")
            |> range(start: -8h)
            |> filter(fn: (r) => r._measurement == ""agro_sensors"")
            |> filter(fn: (r) => r.sensor_client_id == ""{receivedSensorDataEvent.SensorClientId}"")
            |> filter(fn: (r) =>
                r._field == ""soil_moisture_percent"" or
                r._field == ""air_humidity_percent"" or
                r._field == ""air_temperature_c"")
            |> pivot(rowKey: [""_time""], columnKey: [""_field""], valueColumn: ""_value"")
            |> filter(fn: (r) =>
                r.soil_moisture_percent > 70 and
                r.air_humidity_percent > 85 and
                r.air_temperature_c >= 20 and
                r.air_temperature_c <= 30)");

        if (tables.SelectMany(t => t.Records).ToList().Count == 0)
            return false;

        Log.Warning("The Sensor with Id {SensorClientId} in the Field with Id {FieldId} detected a High Probability of Fungal Diseases.", receivedSensorDataEvent.SensorClientId, receivedSensorDataEvent.FieldId);
        PointData alertPointData = PointData
            .Measurement("alerts")
            .Tag("sensor_client_id", receivedSensorDataEvent.SensorClientId.ToString())
            .Tag("field_id", receivedSensorDataEvent.FieldId.ToString())
            .Field("message", $"The Sensor with Id {receivedSensorDataEvent.SensorClientId} in the Field with Id {receivedSensorDataEvent.FieldId} detected a High Probability of Fungal Diseases.")
            .Timestamp(receivedSensorDataEvent.Timestamp, WritePrecision.Ns);
        await _influxDb.WritePointDataAsync(alertPointData);
        return true;
    }

    // Rule number 6: If the soil pH is less than 5.0 and the soil moisture is greater than 60% → "High Acidity with Potential for Reduced Nutrient Absorption"
    private async Task<bool> CheckHighAcidityWithPotentialForReducedNutrientAbsorptionAsync(ReceivedSensorDataEvent receivedSensorDataEvent)
    {
        if (!(receivedSensorDataEvent.SoilPH < 5 && receivedSensorDataEvent.SoilMoisturePercent > 60))
            return false;

        Log.Warning("The Field with Id {FieldId} and with Sensor with Id {SensorClientId} is at risk of high acidity with potential for reduced nutrient absorption.", receivedSensorDataEvent.FieldId, receivedSensorDataEvent.SensorClientId);
        PointData alertPointData = PointData
            .Measurement("alerts")
            .Tag("sensor_client_id", receivedSensorDataEvent.SensorClientId.ToString())
            .Tag("field_id", receivedSensorDataEvent.FieldId.ToString())
            .Field("message", $"The Field with Id {receivedSensorDataEvent.FieldId} and with Sensor Id {receivedSensorDataEvent.SensorClientId} is at risk of high acidity with potential for reduced nutrient absorption!")
            .Timestamp(receivedSensorDataEvent.Timestamp, WritePrecision.Ns);
        await _influxDb.WritePointDataAsync(alertPointData);
        return true;
    }
}
