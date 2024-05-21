namespace Edgegap.Model;

public abstract class EdgegapRequest
{
}

public abstract class EdgegapResponse
{
}

public class EdgegapErrorResponse
{
    public required string Message { get; set; }
    public Dictionary<string, string>? Errors { get; set; }
}
