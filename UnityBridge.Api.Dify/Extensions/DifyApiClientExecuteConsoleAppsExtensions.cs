using UnityBridge.Api.Dify.Models;

namespace UnityBridge.Api.Dify.Extensions;

public static class DifyApiClientExecuteConsoleAppsExtensions
{
    extension(DifyApiClient client)
    {
        /// <summary>
        /// <para>异步调用 [GET] /console/api/apps 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsResponse> ExecuteConsoleApiAppsAsync(ConsoleApiAppsRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "console", "api", "apps")
                .SetQueryParam("page", request.Page)
                .SetQueryParam("limit", request.Limit);

            if (request.Name is not null)
                flurlRequest.SetQueryParam("name", request.Name);

            if (request.IsCreatedByMe.HasValue)
                flurlRequest.SetQueryParam("is_created_by_me", request.IsCreatedByMe.Value);

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsResponse>(flurlRequest, data: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [GET] /console/api/apps/{app_id} 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidResponse> ExecuteConsoleApiAppsAppidAsync(ConsoleApiAppsAppidRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "console", "api", "apps", request.AppId);

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsAppidResponse>(flurlRequest, data: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [GET] /console/api/apps/{app_id}/export 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidExportResponse> ExecuteConsoleApiAppsAppidExportAsync(ConsoleApiAppsAppidExportRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "console", "api", "apps", request.AppId, "export");

            if (request.IncludeSecret.HasValue)
                flurlRequest.SetQueryParam("include_secret", request.IncludeSecret.Value);

            // 特殊处理：该接口可能返回 YAML 文本而不是 JSON
            // 这里我们尝试作为 JSON 请求，如果失败则捕获并处理文本
            // 但 Flurl 默认会尝试反序列化 JSON
            // 我们需要先获取字符串，然后手动处理
            using IFlurlResponse flurlResponse = await client.SendFlurlRequestAsync(flurlRequest, null, cancellationToken).ConfigureAwait(false);
            string text = await flurlResponse.GetStringAsync().ConfigureAwait(false);

            try 
            {
                // 尝试解析 JSON
                return client.JsonSerializer.Deserialize<ConsoleApiAppsAppidExportResponse>(text);
            }
            catch
            {
                // 解析失败，假设是 YAML 文本
                return new ConsoleApiAppsAppidExportResponse { RawText = text, Data = text };
            }
        }

        /// <summary>
        /// <para>异步调用 [POST] /console/api/apps/imports 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsImportsResponse> ExecuteConsoleApiAppsImportsAsync(ConsoleApiAppsImportsRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "console", "api", "apps", "imports");

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsImportsResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [GET] /console/api/apps/{app_id}/api-keys 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidApikeysResponse> ExecuteConsoleApiAppsAppidApikeysAsync(ConsoleApiAppsAppidApikeysRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "console", "api", "apps", request.AppId, "api-keys");

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsAppidApikeysResponse>(flurlRequest, data: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [POST] /console/api/apps/{app_id}/api-keys 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidApikeysCreateResponse> ExecuteConsoleApiAppsAppidApikeysCreateAsync(ConsoleApiAppsAppidApikeysCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "console", "api", "apps", request.AppId, "api-keys");

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsAppidApikeysCreateResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [DELETE] /console/api/apps/{app_id}/api-keys/{key_id} 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidApikeysKeyidResponse> ExecuteConsoleApiAppsAppidApikeysKeyidAsync(ConsoleApiAppsAppidApikeysKeyidRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Delete, "console", "api", "apps", request.AppId, "api-keys", request.KeyId);

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsAppidApikeysKeyidResponse>(flurlRequest, data: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}