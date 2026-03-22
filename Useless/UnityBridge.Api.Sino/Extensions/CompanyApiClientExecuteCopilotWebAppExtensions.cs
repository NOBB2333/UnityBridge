using UnityBridge.Api.Sino.Models;

namespace UnityBridge.Api.Sino.Extensions;

public static class CompanyApiClientExecuteCopilotWebAppExtensions
{
    extension(CompanyApiClient client)
    {
        /// <summary>
        /// <para>异步调用 [POST] /copilot-web-app/ocr 接口。</para>
        /// </summary>
        public async Task<CopilotWebAppOcrResponse> ExecuteCopilotWebAppOcrAsync(CopilotWebAppOcrRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "copilot-web-app", "ocr");

            if (request.FileBytes is null && string.IsNullOrEmpty(request.FilePath))
            {
                throw new ArgumentException("FileBytes or FilePath must be provided.");
            }

            string fileName = request.FileName ?? (string.IsNullOrEmpty(request.FilePath) ? "file" : System.IO.Path.GetFileName(request.FilePath));
            string contentType = request.ContentType ?? "application/octet-stream";

            using var httpContent = new MultipartFormDataContent();
            ByteArrayContent fileContent = request.FileBytes is not null
                ? new ByteArrayContent(request.FileBytes)
                : new ByteArrayContent(System.IO.File.ReadAllBytes(request.FilePath));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            httpContent.Add(fileContent, "file", fileName);

            if (!string.IsNullOrEmpty(request.FileName))
            {
                httpContent.Add(new StringContent(request.FileName!, Encoding.UTF8), "fileName");
            }

            return await client.SendFlurlRequestAsync<CopilotWebAppOcrResponse>(flurlRequest, httpContent, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [GET] /copilot-web-app/chat 接口。</para>
        /// <para>对应 curl.txt：健康度检查。</para>
        /// </summary>
        public async Task<CopilotWebAppHealthCheckResponse> ExecuteCopilotWebAppHealthCheckAsync(CopilotWebAppHealthCheckRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Get, "copilot-web-app", "chat")
                .WithHeader("Upgrade-Insecure-Requests", "1");

            using IFlurlResponse flurlResponse = await client.SendFlurlRequestAsync(flurlRequest, null, cancellationToken).ConfigureAwait(false);
            
            var response = new CopilotWebAppHealthCheckResponse();
            response.RawStatus = (int)flurlResponse.StatusCode;
            
            try
            {
                response.HtmlContent = await flurlResponse.GetStringAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore content parsing errors for health check
            }
            
            return response;
        }
    }
}
