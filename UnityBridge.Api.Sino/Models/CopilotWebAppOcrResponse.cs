namespace UnityBridge.Api.Sino.Models
{
    /// <summary>
    /// <para>表示 [POST] /copilot-web-app/ocr 接口的响应。</para>
    /// </summary>
    public class CopilotWebAppOcrResponse : CompanyApiResponse
    {
        /// <summary>
        /// 获取或设置响应数据。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("data")]
        public object? Data { get; set; }
    }
}
