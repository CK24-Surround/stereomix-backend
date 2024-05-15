namespace StereoMix.Edgegap;

public class SessionEnvironment
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public class DeployEnvironment
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public bool IsHidden { get; set; }
}

public class DeploymentFilter
{
    public string? Field { get; set; }
    public List<string>? Values { get; set; }
    public string? FilterType { get; set; }
}

public class Session
{
    public required string SessionId { get; set; }
    public required string Status { get; set; }
    public required bool Ready { get; set; }
    public required bool Linked { get; set; }
    public required string Kind { get; set; }
    public required int UserCount { get; set; }
}

public class DeploymentLocation
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Continent { get; set; }
    public string? AdministrativeDivision { get; set; }
    public string? Timezone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Message { get; set; }

    public bool HasLocation => City != null || Country != null || Continent != null || AdministrativeDivision != null || Timezone != null || Latitude != null || Longitude != null;
}

public class Selector
{
    public required string Tag { get; set; }
    public bool? TagOnly { get; set; }
    public SessionEnvironment? Env { get; set; }
}

public class ContainerLogStorage
{
    public bool Enabled { get; set; }
    public string? EndpointStorage { get; set; }
}

public class PortMapping
{
    public required int External { get; set; }
    public required int Internal { get; set; }
    public required string Protocol { get; set; }
    public required string Name { get; set; }
    public required bool TlsUpgrade { get; set; }
    public required string Link { get; set; }
    public int? Proxy { get; set; }
}

public class CreateDeploymentRequest
{
    public required string AppName { get; set; }
    public required string VersionName { get; set; }
    public required List<string> IpList { get; set; }
    public List<DeployEnvironment>? EnvVars { get; set; }
    public bool? IsPublicApp { get; set; }
    public List<DeploymentFilter>? Filters { get; set; }
}

public class CreateDeploymentResponse
{
    public required string RequestId { get; set; }
    public required string RequestDns { get; set; }
    public required string RequestApp { get; set; }
    public required string RequestVersion { get; set; }
    public required int RequestUserCount { get; set; }
}

public class DeploymentStatus
{
    public required string RequestId { get; set; }
    public required string Fqdn { get; set; }
    public required string AppName { get; set; }
    public required string AppVersion { get; set; }
    public required string CurrentStatus { get; set; }
    public required bool Running { get; set; }
    public required bool WhitelistingActive { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? RemovalTime { get; set; }
    public int? ElapsedTime { get; set; }
    public string? LastStatus { get; set; }
    public required bool Error { get; set; }
    public string? ErrorDetail { get; set; }
    public Dictionary<string, PortMapping>? Ports { get; set; }
    public string? PublicIp { get; set; }
    public List<Session>? Sessions { get; set; }
    public DeploymentLocation? Location { get; set; }
    public List<string>? Tags { get; set; }
    public int? Sockets { get; set; }
    public int? SocketsUsage { get; set; }
    public string? Command { get; set; }
    public string? Arguments { get; set; }
}

public class CreateSessionRequest
{
    public required string AppName { get; set; }
    public required string VersionName { get; set; }
    public required List<string> IpList { get; set; }
    public string? WebhookUrl { get; set; }
}

public class CreateSessionResponse
{
    public required string SessionId { get; set; }
    public string? CustomId { get; set; }
    public required string App { get; set; }
    public required string Version { get; set; }
    public string? DeploymentRequestId { get; set; }
    public List<Selector>? Selectors { get; set; }
    public string? WebhookUrl { get; set; }
}
