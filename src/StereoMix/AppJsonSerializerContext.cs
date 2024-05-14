using System.Text.Json;
using System.Text.Json.Serialization;
using StereoMix.Edgegap.Model;
using StereoMix.Hathora.Model;

namespace StereoMix;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
    UseStringEnumConverter = true,
    Converters =
    [
        typeof(NullableDateTimeJsonConverter),
        typeof(NullableIntJsonConverter),
        typeof(NullableDoubleJsonConverter)
        // typeof(JsonStringEnumConverter<HathoraRegion>),
        // typeof(HathoraRoomReadyStatusConverter),
        // typeof(HathoraRoomStatusConverter),
        // typeof(HathoraTransportTypeConverter)
    ])]
[JsonSerializable(typeof(CreateDeploymentRequest))]
[JsonSerializable(typeof(CreateDeploymentResponse))]
[JsonSerializable(typeof(DeploymentStatus))]
[JsonSerializable(typeof(CreateRoomRequest))]
[JsonSerializable(typeof(CreateRoomResponse))]
[JsonSerializable(typeof(GetRoomInfoRequest))]
[JsonSerializable(typeof(GetRoomInfoResponse))]
[JsonSerializable(typeof(GetConnectionInfoRequest))]
[JsonSerializable(typeof(GetConnectionInfoResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}

public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override bool HandleNull => true;

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TryGetDateTime(out var dateTime) ? dateTime : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}

public class NullableIntJsonConverter : JsonConverter<int?>
{
    public override bool HandleNull => true;

    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TryGetInt32(out var value) ? value : null;
    }

    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}

public class NullableDoubleJsonConverter : JsonConverter<double?>
{
    public override bool HandleNull => true;

    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TryGetDouble(out var value) ? value : null;
    }

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
    }
}
