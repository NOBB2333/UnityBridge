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
        /// <para>该接口不需要请求体，按照控制台行为发送空 Body。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidApikeysCreateResponse> ExecuteConsoleApiAppsAppidApikeysCreateAsync(ConsoleApiAppsAppidApikeysCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "console", "api", "apps", request.AppId, "api-keys");

            // 按浏览器的 curl 行为，不发送 JSON Body（Content-Length: 0）
            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsAppidApikeysCreateResponse>(flurlRequest, data: null, cancellationToken: cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// <para>异步调用 [POST] /console/api/apps/{app_id}/workflows/publish 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidWorkflowsPublishResponse> ExecuteConsoleApiAppsAppidWorkflowsPublishAsync(ConsoleApiAppsAppidWorkflowsPublishRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "console", "api", "apps", request.AppId, "workflows", "publish");

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiAppsAppidWorkflowsPublishResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [GET] /console/api/apps/{app_id}/workflows/publish 接口。</para>
        /// <para>该接口返回复杂的工作流结构，响应中的 Data 字段包含 JsonElement 以便手动解析。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiAppsAppidWorkflowsPublishGetResponse> ExecuteConsoleApiAppsAppidWorkflowsPublishGetAsync(ConsoleApiAppsAppidWorkflowsPublishGetRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "console", "api", "apps", request.AppId, "workflows", "publish");

            // 特殊处理：该接口返回复杂的工作流结构，需要手动解析
            using IFlurlResponse flurlResponse = await client.SendFlurlRequestAsync(flurlRequest, null, cancellationToken).ConfigureAwait(false);
            string text = await flurlResponse.GetStringAsync().ConfigureAwait(false);

            try
            {
                // 尝试解析 JSON，使用 JsonSerializer 创建独立的 JsonElement
                var jsonElement = client.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(text);
                return new ConsoleApiAppsAppidWorkflowsPublishGetResponse
                {
                    Data = jsonElement,
                    RawText = text
                };
            }
            catch
            {
                // 解析失败，返回原始文本（Data 为默认值）
                return new ConsoleApiAppsAppidWorkflowsPublishGetResponse
                {
                    RawText = text
                };
            }
        }

        /// <summary>
        /// <para>异步调用 [POST] /console/api/workspaces/current/tool-provider/workflow/create 接口。</para>
        /// <para>创建工作流工具提供者。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateResponse> ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowCreateAsync(ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "console", "api", "workspaces", "current", "tool-provider", "workflow", "create");

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiWorkspacesCurrentToolProviderWorkflowCreateResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [GET] /console/api/workspaces/current/tool-provider/workflow/get 接口。</para>
        /// <para>获取工作流工具提供者信息。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConsoleApiWorkspacesCurrentToolProviderWorkflowGetResponse> ExecuteConsoleApiWorkspacesCurrentToolProviderWorkflowGetAsync(ConsoleApiWorkspacesCurrentToolProviderWorkflowGetRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "console", "api", "workspaces", "current", "tool-provider", "workflow", "get")
                .SetQueryParam("workflow_app_id", request.WorkflowAppId);

            return await client.SendFlurlRequestAsJsonAsync<ConsoleApiWorkspacesCurrentToolProviderWorkflowGetResponse>(flurlRequest, data: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}