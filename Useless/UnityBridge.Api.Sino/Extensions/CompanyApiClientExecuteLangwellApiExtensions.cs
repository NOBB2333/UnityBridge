using UnityBridge.Api.Sino.Events;
using UnityBridge.Api.Sino.Models;

namespace UnityBridge.Api.Sino.Extensions;

public static class CompanyApiClientExecuteLangwellApiExtensions
{
    extension(CompanyApiClient client)
    {
        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-ins-server/dify/broker/formData 接口。</para>
        /// </summary>
        public async Task<LangwellInsServerDifyBrokerFormDataResponse> ExecuteLangwellApiLangwellInsServerDifyBrokerFormDataAsync(LangwellInsServerDifyBrokerFormDataRequest request, CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            bool hasBytes = request.FileBytes is not null;
            if (!hasBytes && string.IsNullOrEmpty(request.FilePath))
                throw new ArgumentException("FileBytes or FilePath must be provided.");

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-ins-server", "dify", "broker", "formData");

            using var formContent = new MultipartFormDataContent();
            string fileName = request.FileName ?? (hasBytes ? "file" : System.IO.Path.GetFileName(request.FilePath!) );
            string contentType = request.ContentType ?? "application/octet-stream";

            ByteArrayContent fileContent = hasBytes
                ? new ByteArrayContent(request.FileBytes!)
                : new ByteArrayContent(System.IO.File.ReadAllBytes(request.FilePath!));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(fileContent, "file", fileName);

            formContent.Add(new StringContent(request.Path, Encoding.UTF8), "path");
            formContent.Add(new StringContent(request.AgentId, Encoding.UTF8), "agentId");
            formContent.Add(new StringContent(request.User, Encoding.UTF8), "user");

            if (!string.IsNullOrEmpty(request.LibraryName))
                formContent.Add(new StringContent(request.LibraryName, Encoding.UTF8), "libName");
            if (!string.IsNullOrEmpty(request.LibraryDescription))
                formContent.Add(new StringContent(request.LibraryDescription, Encoding.UTF8), "libDesc");

            formContent.Add(new StringContent(request.Flag, Encoding.UTF8), "flag");

            return await client.SendFlurlRequestAsync<LangwellInsServerDifyBrokerFormDataResponse>(flurlRequest, formContent, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-ins-server/dify/broker/agent/stream 接口，并以 SSE 流的方式返回事件。</para>
        /// </summary>
        public async IAsyncEnumerable<AgentStreamEvent> ExecuteLangwellApiLangwellInsServerDifyBrokerAgentStreamAsync(LangwellApiLangwellInsServerDifyBrokerAgentStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-ins-server", "dify", "broker", "agent", "stream")
                .WithHeader("Accept", "text/event-stream");

            using var httpContent = new StringContent(client.JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            using IFlurlResponse flurlResponse = await client.SendFlurlRequestAsync(flurlRequest, httpContent, cancellationToken).ConfigureAwait(false);

            using Stream stream = await flurlResponse.GetStreamAsync().ConfigureAwait(false);
            using StreamReader reader = new StreamReader(stream);

            string? eventName = null;
            string? eventId = null;
            StringBuilder? dataBuilder = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line is null)
                    break;

                if (string.IsNullOrEmpty(line))
                {
                    if (eventName is not null || eventId is not null || dataBuilder is not null)
                    {
                        yield return new AgentStreamEvent
                        {
                            Event = eventName,
                            Id = eventId,
                            Data = dataBuilder?.ToString()
                        };

                        eventName = null;
                        eventId = null;
                        dataBuilder = null;
                    }

                    continue;
                }

                if (line.StartsWith(":", StringComparison.Ordinal))
                    continue;

                int colonIndex = line.IndexOf(':');
                string field = colonIndex >= 0 ? line.Substring(0, colonIndex) : line;
                string value = colonIndex >= 0 ? line.Substring(colonIndex + 1).TrimStart() : string.Empty;

                switch (field)
                {
                    case "event":
                        eventName = value;
                        break;

                    case "id":
                        eventId = value;
                        break;

                    case "data":
                        dataBuilder ??= new StringBuilder();
                        if (dataBuilder.Length > 0)
                            dataBuilder.Append('\n');
                        dataBuilder.Append(value);
                        break;
                }
            }

            if (eventName is not null || eventId is not null || dataBuilder is not null)
            {
                yield return new AgentStreamEvent
                {
                    Event = eventName,
                    Id = eventId,
                    Data = dataBuilder?.ToString()
                };
            }
        }
    }
}