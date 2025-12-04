using UnityBridge.Api.Sino.Models;

namespace UnityBridge.Api.Sino.Extensions;

public static class CompanyApiClientExecuteLangwellDocServerExtensions
{
    extension(CompanyApiClient client)
    {
        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-doc-server/knowledge/lib/individual/add 接口。</para>
        /// </summary>
        public async Task<LangwellDocServerKnowledgeLibIndividualAddResponse> ExecuteLangwellDocServerKnowledgeLibIndividualAddAsync(LangwellDocServerKnowledgeLibIndividualAddRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-doc-server", "knowledge", "lib", "individual", "add");
            return await client.SendFlurlRequestAsJsonAsync<LangwellDocServerKnowledgeLibIndividualAddResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-doc-server/knowledge/lib/individual/page 接口。</para>
        /// </summary>
        public async Task<LangwellDocServerKnowledgeLibIndividualPageResponse> ExecuteLangwellDocServerKnowledgeLibIndividualPageAsync(LangwellDocServerKnowledgeLibIndividualPageRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-doc-server", "knowledge", "lib", "individual", "page");
            return await client.SendFlurlRequestAsJsonAsync<LangwellDocServerKnowledgeLibIndividualPageResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-doc-server/knowledge/lib/all/listBackAll 接口。</para>
        /// </summary>
        public async Task<LangwellDocServerKnowledgeLibAllListBackAllResponse> ExecuteLangwellDocServerKnowledgeLibAllListBackAllAsync(LangwellDocServerKnowledgeLibAllListBackAllRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-doc-server", "knowledge", "lib", "all", "listBackAll");
            return await client.SendFlurlRequestAsJsonAsync<LangwellDocServerKnowledgeLibAllListBackAllResponse>(flurlRequest, data: request, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-doc-server/knowledge/file/individual/upload 接口。</para>
        /// </summary>
        public async Task<LangwellDocServerKnowledgeFileIndividualUploadResponse> ExecuteLangwellDocServerKnowledgeFileIndividualUploadAsync(LangwellDocServerKnowledgeFileIndividualUploadRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrEmpty(request.LibraryId)) throw new ArgumentException("LibraryId is required.", nameof(request));

            bool hasBytes = request.FileBytes is not null;
            if (!hasBytes && string.IsNullOrEmpty(request.FilePath))
                throw new ArgumentException("FileBytes or FilePath must be provided.");

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-doc-server", "knowledge", "file", "individual", "upload");

            using var formContent = new MultipartFormDataContent();
            string fileName = request.FileName ?? (hasBytes ? "file" : System.IO.Path.GetFileName(request.FilePath!) );
            string contentType = request.ContentType ?? "application/octet-stream";

            ByteArrayContent fileContent = hasBytes
                ? new ByteArrayContent(request.FileBytes!)
                : new ByteArrayContent(System.IO.File.ReadAllBytes(request.FilePath!));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(fileContent, "file", fileName);

            formContent.Add(new StringContent(fileName, Encoding.UTF8), "fileName");
            formContent.Add(new StringContent(request.LibraryId, Encoding.UTF8), "libId");

            if (request.ExtraFormFields is not null)
            {
                foreach ((string key, string value) in request.ExtraFormFields)
                {
                    formContent.Add(new StringContent(value, Encoding.UTF8), key);
                }
            }

            return await client.SendFlurlRequestAsync<LangwellDocServerKnowledgeFileIndividualUploadResponse>(flurlRequest, formContent, cancellationToken).ConfigureAwait(false);
        }
    }
}
