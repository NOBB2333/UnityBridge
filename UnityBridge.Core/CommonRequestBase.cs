namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 请求基类。
/// </summary>
public abstract class CommonRequestBase : ICommonRequest
{
    /// <inheritdoc/>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual int? Timeout { get; set; }
}