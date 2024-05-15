using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace StereoMix.Hathora;

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

public class HathoraCreateRoomRequest : HathoraRequest
{
    public required string RoomId { get; set; }
    public required HathoraRegion Region { get; set; }
    public required string RoomConfig { get; set; }
}

public class HathoraCreateRoomResponse : HathoraResponse
{
    public required string ProcessId { get; set; }
    public required string RoomId { get; set; }
    public required HathoraRoomReadyStatus Status { get; set; }
    public HathoraExposedPort? ExposedPort { get; set; }
    public List<HathoraExposedPort>? AdditionalExposedPorts { get; set; }
}

public class HathoraGetRoomInfoRequest : HathoraRequest
{
    public required string AppId { get; set; }
    public required string RoomId { get; set; }
}

public class HathoraGetRoomInfoResponse : HathoraResponse
{
    public HathoraRoomAllocation? CurrentAllocation { get; set; }
    public required HathoraRoomStatus Status { get; set; }
    public List<HathoraRoomAllocation>? Allocations { get; set; }
    public required string RoomConfig { get; set; }
    public required string RoomId { get; set; }
    public required string AppId { get; set; }
}

public class HathoraGetConnectionInfoRequest : HathoraRequest
{
    public required string AppId { get; set; }
    public required string RoomId { get; set; }
}

public class HathoraGetConnectionInfoResponse : HathoraResponse
{
    public required string RoomId { get; set; }
    public required HathoraRoomReadyStatus Status { get; set; }
    public HathoraExposedPort? ExposedPort { get; set; }
    public List<HathoraExposedPort>? AdditionalExposedPorts { get; set; }
}

public class HathoraExposedPort
{
    public required HathoraTransportType TransportType { get; set; }
    public required int Port { get; set; }
    public required string Host { get; set; }
    public required string Name { get; set; }
}

public class HathoraRoomAllocation
{
    public required DateTime UnscheduledAt { get; set; }
    public required DateTime ScheduledAt { get; set; }
    public required string ProcessId { get; set; }
    public required string RoomAllocationId { get; set; }
}
