namespace UnityBridge.Core;

/// <summary>
/// SKIT.FlurlHttpClient 客户端接口。
/// </summary>
public interface ICommonClient : IDisposable
{
    /// <summary>
    /// 获取当前客户端使用的 <see cref="IFlurlClient"/> 对象。
    /// </summary>
    IFlurlClient FlurlClient { get; }
}