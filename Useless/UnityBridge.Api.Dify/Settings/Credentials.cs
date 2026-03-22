namespace UnityBridge.Api.Dify.Settings;

public class Credentials
{
    /// <summary>
    /// 初始化客户端时 <see cref="DifyApiClientOptions.AppKey"/> 的副本。
    /// </summary>
    public string AppKey { get; }

    internal Credentials(DifyApiClientOptions options)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));

        AppKey = options.AppKey;
    }
}