using System.Text.Json;
using System.Text.Json.Serialization;

using Edgegap.Model;

namespace Edgegap;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    [
        typeof(NullableIntJsonConverter),
        typeof(NullableDoubleJsonConverter),
        typeof(NullableDateTimeJsonConverter),
        typeof(EdgegapEnumJsonConverter<EdgegapDeploymentFilterFieldType>),
        typeof(EdgegapEnumJsonConverter<EdgegapDeploymentFilterType>),
        typeof(EdgegapEnumJsonConverter<EdgegapApSortStrategyType>),
        typeof(EdgegapDeploymentStatusTypeJsonConverter)
    ])]
[JsonSerializable(typeof(EdgegapCreateDeploymentRequest))]
[JsonSerializable(typeof(EdgegapCreateDeploymentResponse))]
[JsonSerializable(typeof(EdgegapGetDeploymentStatusResponse))]
[JsonSerializable(typeof(EdgegapDeleteDeploymentResponse))]
[JsonSerializable(typeof(EdgegapErrorResponse))]
public partial class EdgegapJsonSerializerContext : JsonSerializerContext
{
}

public class EdgegapEnumJsonConverter<TEnum>() : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.SnakeCaseLower)
    where TEnum : struct, Enum;

public class EdgegapDeploymentStatusTypeJsonConverter : JsonConverter<EdgegapDeploymentStatusType>
{
    public override EdgegapDeploymentStatusType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Failed to parse EdgegapDeploymentStatusType. Token type is not string.");
        }

        var str = reader.GetString();
        return str switch
        {
            "Status.INITIALIZING" => EdgegapDeploymentStatusType.Initializing,
            "Status.SEEKING" => EdgegapDeploymentStatusType.Seeking,
            "Status.SEEKED" => EdgegapDeploymentStatusType.Seeked,
            "Status.SCANNING" => EdgegapDeploymentStatusType.Scanning,
            "Status.DEPLOYING" => EdgegapDeploymentStatusType.Deploying,
            "Status.READY" => EdgegapDeploymentStatusType.Ready,
            "Status.TERMINATED" => EdgegapDeploymentStatusType.Terminated,
            "Status.ERROR" => EdgegapDeploymentStatusType.Error,
            _ => throw new JsonException($"Failed to parse EdgegapDeploymentStatusType. Unexpected value: {str}")
        };
    }

    public override void Write(Utf8JsonWriter writer, EdgegapDeploymentStatusType value, JsonSerializerOptions options)
    {
        var str = value switch
        {
            EdgegapDeploymentStatusType.Initializing => "Status.INITIALIZING",
            EdgegapDeploymentStatusType.Seeking => "Status.SEEKING",
            EdgegapDeploymentStatusType.Seeked => "Status.SEEKED",
            EdgegapDeploymentStatusType.Scanning => "Status.SCANNING",
            EdgegapDeploymentStatusType.Deploying => "Status.DEPLOYING",
            EdgegapDeploymentStatusType.Ready => "Status.READY",
            EdgegapDeploymentStatusType.Terminated => "Status.TERMINATED",
            EdgegapDeploymentStatusType.Error => "Status.ERROR",
            EdgegapDeploymentStatusType.Unspecified => throw new ArgumentException("Unexpected status type"),
            _ => throw new ArgumentOutOfRangeException(nameof(value))
        };
    }
}

public class NullableDateTimeJsonConverter : JsonConverter<DateTime?>
{
    public override bool HandleNull => true;

    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            return null;
        }

        return reader.TryGetDateTime(out var dateTime) ? dateTime : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

public class NullableIntJsonConverter : JsonConverter<int?>
{
    public override bool HandleNull => true;

    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            return null;
        }

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
        if (reader.TokenType != JsonTokenType.Number)
        {
            return null;
        }

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
