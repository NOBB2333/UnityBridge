using UnityBridge.Api.Sino.Models;

namespace UnityBridge.Api.Sino.Extensions;

public static class CompanyApiClientExecuteCopilotWebAppExtensions
{
    extension(CompanyApiClient client)
    {
        /// <summary>
        /// <para>异步调用 [POST] /copilot-web-app/ocr 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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
            if (request.FileBytes is not null)
            {
                httpContent.Add(new ByteArrayContent(request.FileBytes), "file", fileName);
            }
            else
            {
                // 注意：这里需要确保 FilePath 是有效的本地路径
                // 如果是在 Web 环境中，通常推荐使用 FileBytes
                var fileContent = new ByteArrayContent(System.IO.File.ReadAllBytes(request.FilePath));
                httpContent.Add(fileContent, "file", fileName);
            }
            
            // 如果有其他字段，也可以添加到 httpContent

            using IFlurlResponse flurlResponse = await client.SendFlurlRequestAsync(flurlRequest, httpContent, cancellationToken).ConfigureAwait(false);
            return await client.WrapFlurlResponseAsJsonAsync<CopilotWebAppOcrResponse>(flurlResponse, cancellationToken).ConfigureAwait(false);
        }
    }
}
