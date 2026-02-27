using UnityBridge.Crawler.BiliBili.Models;
using System.Text.Json;

namespace UnityBridge.Crawler.BiliBili.Extensions;

/// <summary>
/// BiliClient 登录相关扩展方法。
/// </summary>
public static class BiliClientExecuteAuthExtensions
{
    extension(BiliClient client)
    {
        /// <summary>
        /// <para>异步调用 [GET] /x/passport-login/web/qrcode/generate 接口。</para>
        /// <para>生成二维码登录链接。</para>
        /// </summary>
        public async Task<BiliQrCodeGenerateResponse> ExecuteQrCodeGenerateAsync(
            BiliQrCodeGenerateRequest request, CancellationToken ct = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            var source = string.IsNullOrWhiteSpace(request.Source) ? "main-fe-header" : request.Source;
            var url = $"{BiliEndpoints.PASSPORT}/x/passport-login/web/qrcode/generate?source={Uri.EscapeDataString(source)}";

            IFlurlRequest flurlRequest = client.FlurlClient.Request(url)
                .WithHeader("Accept", "application/json, text/plain, */*")
                .WithHeader("Content-Type", "application/json;charset=UTF-8")
                .WithHeader("Cookie", client.Cookies)
                .WithHeader("Origin", BiliEndpoints.WEB)
                .WithHeader("Referer", BiliEndpoints.WEB)
                .WithHeader("User-Agent", client.ClientOptions.UserAgent);
            flurlRequest.Verb = HttpMethod.Get;

            using IFlurlResponse response = await client.SendFlurlRequestAsync(flurlRequest, null, ct);
            var json = await response.GetStringAsync();
            return ParseGenerateResponse(json);
        }

        /// <summary>
        /// <para>异步调用 [GET] /x/passport-login/web/qrcode/poll 接口。</para>
        /// <para>轮询二维码登录状态。</para>
        /// </summary>
        public async Task<BiliQrCodePollResponse> ExecuteQrCodePollAsync(
            BiliQrCodePollRequest request, CancellationToken ct = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.QrcodeKey)) throw new ArgumentNullException(nameof(request.QrcodeKey));

            var source = string.IsNullOrWhiteSpace(request.Source) ? "main-fe-header" : request.Source;
            var url = $"{BiliEndpoints.PASSPORT}/x/passport-login/web/qrcode/poll?source={Uri.EscapeDataString(source)}&qrcode_key={Uri.EscapeDataString(request.QrcodeKey)}";

            IFlurlRequest flurlRequest = client.FlurlClient.Request(url)
                .WithHeader("Accept", "application/json, text/plain, */*")
                .WithHeader("Content-Type", "application/json;charset=UTF-8")
                .WithHeader("Cookie", client.Cookies)
                .WithHeader("Origin", BiliEndpoints.WEB)
                .WithHeader("Referer", BiliEndpoints.WEB)
                .WithHeader("User-Agent", client.ClientOptions.UserAgent);
            flurlRequest.Verb = HttpMethod.Get;

            using IFlurlResponse response = await client.SendFlurlRequestAsync(flurlRequest, null, ct);
            var json = await response.GetStringAsync();
            return ParsePollResponse(json);
        }
    }

    private static BiliQrCodeGenerateResponse ParseGenerateResponse(string json)
    {
        var result = new BiliQrCodeGenerateResponse();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("code", out var code))
        {
            result.Code = code.GetInt32();
        }

        if (root.TryGetProperty("message", out var message))
        {
            result.Message = message.GetString();
        }

        if (root.TryGetProperty("data", out var data))
        {
            result.Data = new BiliQrCodeGenerateData
            {
                Url = data.TryGetProperty("url", out var url) ? url.GetString() : null,
                QrcodeKey = data.TryGetProperty("qrcode_key", out var key) ? key.GetString() : null
            };
        }

        return result;
    }

    private static BiliQrCodePollResponse ParsePollResponse(string json)
    {
        var result = new BiliQrCodePollResponse();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("code", out var code))
        {
            result.Code = code.GetInt32();
        }

        if (root.TryGetProperty("message", out var message))
        {
            result.Message = message.GetString();
        }

        if (root.TryGetProperty("data", out var data))
        {
            result.Data = new BiliQrCodePollData
            {
                Code = data.TryGetProperty("code", out var dCode) ? dCode.GetInt32() : -1,
                Message = data.TryGetProperty("message", out var dMessage) ? dMessage.GetString() : null,
                Url = data.TryGetProperty("url", out var dUrl) ? dUrl.GetString() : null,
                RefreshToken = data.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null,
                Timestamp = data.TryGetProperty("timestamp", out var ts) ? ts.GetInt64() : null
            };
        }

        return result;
    }
}
