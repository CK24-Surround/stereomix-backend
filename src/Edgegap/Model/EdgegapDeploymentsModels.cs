namespace Edgegap.Model;

public class EdgegapDeployEnvironment
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public bool IsHidden { get; set; }
}

public enum EdgegapDeploymentFilterType
{
    Any,
    All,
    Not
}

public enum EdgegapApSortStrategyType
{
    Basic,
    Weighted
}

public enum EdgegapDeploymentStatusType
{
    Unspecified,
    Initializing,
    Seeking,
    Seeked,
    Scanning,
    Deploying,
    Ready,
    Terminated,
    Error
}

public class EdgegapDeploymentFilter
{
    public required string Field { get; set; }
    public required List<string> Values { get; set; }
    public required EdgegapDeploymentFilterType FilterType { get; set; }
}

public class EdgegapDeploymentLocation
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Continent { get; set; }
    public string? AdministrativeDivision { get; set; }
    public string? Timezone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public bool HasLocation =>
        City != null ||
        Country != null ||
        Continent != null ||
        AdministrativeDivision != null ||
        Timezone != null ||
        Latitude != null ||
        Longitude != null;
}

public class EdgegapContainerLogStorage
{
    public bool Enabled { get; set; }
    public string? EndpointStorage { get; set; }
}

public class EdgegapPortMapping
{
    public required int External { get; set; }
    public required int Internal { get; set; }
    public required string Protocol { get; set; }
    public required string Name { get; set; }
    public required bool TlsUpgrade { get; set; }
    public required string Link { get; set; }
    public int? Proxy { get; set; }
}

public class EdgegapDeploymentStatus
{
    public required string RequestId { get; set; }
    public required string Fqdn { get; set; }
    public required string AppName { get; set; }
    public required string AppVersion { get; set; }
    public required EdgegapDeploymentStatusType CurrentStatus { get; set; }
    public required bool Running { get; set; }
    public required bool WhitelistingActive { get; set; }
    public required DateTime? StartTime { get; set; }
    public DateTime? RemovalTime { get; set; }
    public required int? ElapsedTime { get; set; }
    public EdgegapDeploymentStatusType LastStatusType { get; set; }
    public required bool Error { get; set; }
    public string? ErrorDetail { get; set; }
    public Dictionary<string, EdgegapPortMapping>? Ports { get; set; }
    public string? PublicIp { get; set; }
    public EdgegapDeploymentLocation? Location { get; set; }
    public List<string>? Tags { get; set; }
    public int? Sockets { get; set; }
    public int? SocketsUsage { get; set; }
    public string? Command { get; set; }
    public string? Arguments { get; set; }
}

public class EdgegapCreateDeploymentRequest : EdgegapRequest
{
    public required string AppName { get; set; }
    public string? VersionName { get; set; }
    public List<string>? IpList { get; set; }
    public List<EdgegapDeployEnvironment>? EnvVars { get; set; }
    public string? WebhookUrl { get; set; }
    public List<string>? Tags { get; set; }
    public EdgegapContainerLogStorage? ContainerLogStorage { get; set; }
    public List<EdgegapDeploymentFilter>? Filters { get; set; }
    public EdgegapApSortStrategyType EdgegapApSortStrategyType { get; set; }
    public string? Command { get; set; }
    public string? Arguments { get; set; }
}

public class EdgegapCreateDeploymentResponse : EdgegapResponse
{
    public required string RequestId { get; set; }
    public required string RequestDns { get; set; }
    public required string RequestApp { get; set; }
    public required string RequestVersion { get; set; }
    public required int RequestUserCount { get; set; }

    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Continent { get; set; }
    public string? AdministrativeDivision { get; set; }
    public List<string>? Tags { get; set; }
    public EdgegapContainerLogStorage? ContainerLogStorage { get; set; }
}

public class EdgegapGetDeploymentStatusResponse : EdgegapDeploymentStatus
{
}

public class EdgegapDeleteDeploymentResponse : EdgegapResponse
{
    public required string Message { get; set; }
    public EdgegapDeploymentStatus? DeploymentSummary { get; set; }
}
