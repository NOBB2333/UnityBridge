namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 响应基类。
/// </summary>
public abstract class CommonResponseBase : ICommonResponse
{
    /// <summary>
    /// 
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual int RawStatus { get; internal set; }

    /// <summary>
    /// 
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual IDictionary<string, string> RawHeaders { get; internal set; } = new Dictionary<string, string>();

    /// <summary>
    /// 
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public virtual byte[] RawBytes { get; internal set; } = Array.Empty<byte>();

    /// <inheritdoc/>
    public virtual int GetRawStatus()
    {
        return RawStatus;
    }

    /// <inheritdoc/>
    public virtual IDictionary<string, string> GetRawHeaders()
    {
        return RawHeaders;
    }

    /// <inheritdoc/>
    public virtual byte[] GetRawBytes()
    {
        return RawBytes;
    }

    /// <inheritdoc/>
    public virtual bool IsSuccessful()
    {
        return GetRawStatus() >= 200 && GetRawStatus() < 300;
    }
}