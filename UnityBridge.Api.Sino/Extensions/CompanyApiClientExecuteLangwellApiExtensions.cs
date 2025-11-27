using UnityBridge.Api.Sino.Events;
using UnityBridge.Api.Sino.Models;

namespace UnityBridge.Api.Sino.Extensions;

public static class CompanyApiClientExecuteLangwellApiExtensions
{
    extension(CompanyApiClient client)
    {
        /// <summary>
        /// <para>异步调用 [POST] /langwell-api/langwell-ins-server/dify/broker/agent/stream 接口。</para>
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<AgentStreamEvent> ExecuteLangwellApiLangwellInsServerDifyBrokerAgentStreamAsync(LangwellApiLangwellInsServerDifyBrokerAgentStreamRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (client is null) throw new ArgumentNullException(nameof(client));
            if (request is null) throw new ArgumentNullException(nameof(request));

            IFlurlRequest flurlRequest = client.CreateFlurlRequest(request, HttpMethod.Post, "langwell-api", "langwell-ins-server", "dify", "broker", "agent", "stream");

            using IFlurlResponse flurlResponse = await client.SendFlurlRequestAsync(flurlRequest, new StringContent(client.JsonSerializer.Serialize(request), System.Text.Encoding.UTF8, System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json")), cancellationToken).ConfigureAwait(false);
            
            using Stream stream = await flurlResponse.GetStreamAsync().ConfigureAwait(false);
            using StreamReader reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null && !cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // 简单的 SSE 解析
                // 格式通常是:
                // event: message
                // data: {...}
                // 
                // 或者直接是 data: {...}
                // 这里我们假设每行都是一个完整的事件或者需要累积
                // 但为了简化，我们假设 Rust 代码中的 AgentStreamEvent 结构对应的是 SSE 的 data 部分被解析后的结果
                // 或者整个 SSE 消息被解析为一个对象
                
                // 根据 Rust 代码：
                // pub struct AgentStreamEvent {
                //     pub event: Option<String>,
                //     pub data: Option<String>,
                //     pub id: Option<String>,
                // }
                // 这看起来像是直接解析 SSE 的每一行，或者是解析 SSE 的一个完整块
                
                // 如果返回的是标准的 SSE 格式：
                // data: {"event": "message", "answer": "hello", ...}
                
                // 我们尝试解析每一行
                if (line.StartsWith("data:"))
                {
                    string data = line.Substring(5).Trim();
                    // 这里 data 可能是一个 JSON 字符串，也可能只是普通字符串
                    // 如果 Rust 模型中的 data 是 Option<String>，那么它可能就是原始数据
                    
                    // 我们构造一个 AgentStreamEvent
                    yield return new AgentStreamEvent { Data = data };
                }
                else if (line.StartsWith("event:"))
                {
                    string eventName = line.Substring(6).Trim();
                    yield return new AgentStreamEvent { Event = eventName };
                }
                else if (line.StartsWith("id:"))
                {
                    string id = line.Substring(3).Trim();
                    yield return new AgentStreamEvent { Id = id };
                }
            }
        }
    }
}
