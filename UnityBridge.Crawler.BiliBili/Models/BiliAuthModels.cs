namespace UnityBridge.Crawler.BiliBili.Models;

/// <summary>二维码生成请求。</summary>
public class BiliQrCodeGenerateRequest : BiliRequest
{
    public string Source { get; set; } = "main-fe-header";
}

/// <summary>二维码生成响应。</summary>
public class BiliQrCodeGenerateResponse : BiliResponse
{
    [JsonPropertyName("data")]
    public BiliQrCodeGenerateData? Data { get; set; }
}

public class BiliQrCodeGenerateData
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("qrcode_key")]
    public string? QrcodeKey { get; set; }
}

/// <summary>二维码轮询请求。</summary>
public class BiliQrCodePollRequest : BiliRequest
{
    public string Source { get; set; } = "main-fe-header";
    public string QrcodeKey { get; set; } = string.Empty;
}

/// <summary>二维码轮询响应。</summary>
public class BiliQrCodePollResponse : BiliResponse
{
    [JsonPropertyName("data")]
    public BiliQrCodePollData? Data { get; set; }
}

public class BiliQrCodePollData
{
    /// <summary>
    /// 状态码：0 登录成功；86038 二维码已失效；86090 已扫码未确认；86101 未扫码。
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// 登录成功后会返回回调 URL，通常带登录 Cookie 参数。
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }
}
