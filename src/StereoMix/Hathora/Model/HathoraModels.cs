using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace StereoMix.Hathora.Model;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum HathoraRegion
{
    Seattle,
    Los_Angeles,
    Washington_DC,
    Chicago,
    London,
    Frankfurt,
    Mumbai,
    Singapore,
    Tokyo,
    Sydney,
    Sao_Paulo,
    Dallas
}

public enum HathoraRoomReadyStatus
{
    Starting,
    Active
}

public enum HathoraRoomStatus
{
    Scheduling,
    Active,
    Suspended,
    Destroyed
}

public enum HathoraTransportType
{
    Tcp,
    Udp,
    Tls
}

public abstract class HathoraRequest
{
}

public abstract class HathoraResponse
{
    [JsonPropertyName("message")] public string? ErrorMessage { get; set; }
}

public class HathoraLoginResponse : HathoraResponse
{
    public required string Token { get; set; }
}

public class CreateRoomRequest : HathoraRequest
{
    public required string RoomId { get; set; }
    public required HathoraRegion Region { get; set; }
    public required string RoomConfig { get; set; }
}

public class CreateRoomResponse : HathoraResponse
{
    public required string ProcessId { get; set; }
    public required string RoomId { get; set; }
    public required HathoraRoomReadyStatus Status { get; set; }
    public ExposedPort? ExposedPort { get; set; }
    public List<ExposedPort>? AdditionalExposedPorts { get; set; }
}

public class GetRoomInfoRequest : HathoraRequest
{
    public required string AppId { get; set; }
    public required string RoomId { get; set; }
}

public class GetRoomInfoResponse : HathoraResponse
{
    public RoomAllocation? CurrentAllocation { get; set; }
    public required HathoraRoomStatus Status { get; set; }
    public List<RoomAllocation>? Allocations { get; set; }
    public required string RoomConfig { get; set; }
    public required string RoomId { get; set; }
    public required string AppId { get; set; }
}

public class GetConnectionInfoRequest : HathoraRequest
{
    public required string AppId { get; set; }
    public required string RoomId { get; set; }
}

public class GetConnectionInfoResponse : HathoraResponse
{
    public required string RoomId { get; set; }
    public required HathoraRoomReadyStatus Status { get; set; }
    public ExposedPort? ExposedPort { get; set; }
    public List<ExposedPort>? AdditionalExposedPorts { get; set; }
}

public class ExposedPort
{
    public required HathoraTransportType TransportType { get; set; }
    public required int Port { get; set; }
    public required string Host { get; set; }
    public required string Name { get; set; }
}

public class RoomAllocation
{
    public required DateTime UnscheduledAt { get; set; }
    public required DateTime ScheduledAt { get; set; }
    public required string ProcessId { get; set; }
    public required string RoomAllocationId { get; set; }
}
