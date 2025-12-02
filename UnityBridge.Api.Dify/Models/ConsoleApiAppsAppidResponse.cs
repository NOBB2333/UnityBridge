namespace UnityBridge.Api.Dify.Models;

/// <summary>
/// <para>表示 [GET] /console/api/apps/{app_id} 接口的响应。</para>
/// </summary>
public class ConsoleApiAppsAppidResponse : DifyApiResponse
{
    /// <summary>
    /// 获取或设置应用 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    /// <summary>
    /// 获取或设置应用名称。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    /// <summary>
    /// 获取或设置应用描述。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 获取或设置应用模式。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>
    /// 获取或设置图标类型。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon_type")]
    public string? IconType { get; set; }

    /// <summary>
    /// 获取或设置图标。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon")]
    public string? Icon { get; set; }

    /// <summary>
    /// 获取或设置图标背景。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon_background")]
    public string? IconBackground { get; set; }

    /// <summary>
    /// 获取或设置图标 URL。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    /// <summary>
    /// 获取或设置标签列表（可能为字符串或对象，保持 JsonElement 以兼容不同结构）。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("tags")]
    public System.Text.Json.JsonElement[]? Tags { get; set; }

    /// <summary>
    /// 获取或设置是否启用站点。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("enable_site")]
    public bool? EnableSite { get; set; }

    /// <summary>
    /// 获取或设置是否启用 API。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("enable_api")]
    public bool? EnableApi { get; set; }

    /// <summary>
    /// 获取或设置模型配置。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("model_config")]
    public System.Text.Json.JsonElement? ModelConfig { get; set; }

    /// <summary>
    /// 获取或设置工作流信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("workflow")]
    public WorkflowInfo? Workflow { get; set; }

    /// <summary>
    /// 获取或设置站点信息。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("site")]
    public SiteInfo? Site { get; set; }

    /// <summary>
    /// 获取或设置 API 基础 URL。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("api_base_url")]
    public string? ApiBaseUrl { get; set; }

    /// <summary>
    /// 获取或设置是否使用图标作为答案图标。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("use_icon_as_answer_icon")]
    public bool? UseIconAsAnswerIcon { get; set; }

    /// <summary>
    /// 获取或设置创建者 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("created_by")]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 获取或设置创建时间戳。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("created_at")]
    public long? CreatedAt { get; set; }

    /// <summary>
    /// 获取或设置更新者 ID。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updated_by")]
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// 获取或设置更新时间戳。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
    public long? UpdatedAt { get; set; }

    /// <summary>
    /// 获取或设置已删除的工具列表。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("deleted_tools")]
    public string[]? DeletedTools { get; set; }

    /// <summary>
    /// 获取或设置访问模式。
    /// </summary>
    [System.Text.Json.Serialization.JsonPropertyName("access_mode")]
    public string? AccessMode { get; set; }

    /// <summary>
    /// 工作流信息。
    /// </summary>
    public class WorkflowInfo
    {
        /// <summary>
        /// 获取或设置工作流 ID。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string? Id { get; set; }

        /// <summary>
        /// 获取或设置创建者 ID。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 获取或设置创建时间戳。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }

        /// <summary>
        /// 获取或设置更新者 ID。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_by")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// 获取或设置更新时间戳。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
        public long? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 站点信息。
    /// </summary>
    public class SiteInfo
    {
        /// <summary>
        /// 获取或设置访问令牌。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        /// <summary>
        /// 获取或设置代码。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("code")]
        public string? Code { get; set; }

        /// <summary>
        /// 获取或设置标题。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// 获取或设置图标类型。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("icon_type")]
        public string? IconType { get; set; }

        /// <summary>
        /// 获取或设置图标。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("icon")]
        public string? Icon { get; set; }

        /// <summary>
        /// 获取或设置图标背景。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("icon_background")]
        public string? IconBackground { get; set; }

        /// <summary>
        /// 获取或设置图标 URL。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        /// <summary>
        /// 获取或设置描述。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// 获取或设置默认语言。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("default_language")]
        public string? DefaultLanguage { get; set; }

        /// <summary>
        /// 获取或设置聊天颜色主题。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("chat_color_theme")]
        public string? ChatColorTheme { get; set; }

        /// <summary>
        /// 获取或设置聊天颜色主题是否反转。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("chat_color_theme_inverted")]
        public bool? ChatColorThemeInverted { get; set; }

        /// <summary>
        /// 获取或设置自定义域名。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customize_domain")]
        public string? CustomizeDomain { get; set; }

        /// <summary>
        /// 获取或设置版权信息。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("copyright")]
        public string? Copyright { get; set; }

        /// <summary>
        /// 获取或设置隐私政策。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("privacy_policy")]
        public string? PrivacyPolicy { get; set; }

        /// <summary>
        /// 获取或设置自定义免责声明。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("custom_disclaimer")]
        public string? CustomDisclaimer { get; set; }

        /// <summary>
        /// 获取或设置自定义令牌策略。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customize_token_strategy")]
        public string? CustomizeTokenStrategy { get; set; }

        /// <summary>
        /// 获取或设置提示是否公开。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("prompt_public")]
        public bool? PromptPublic { get; set; }

        /// <summary>
        /// 获取或设置应用基础 URL。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("app_base_url")]
        public string? AppBaseUrl { get; set; }

        /// <summary>
        /// 获取或设置是否显示工作流步骤。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("show_workflow_steps")]
        public bool? ShowWorkflowSteps { get; set; }

        /// <summary>
        /// 获取或设置是否使用图标作为答案图标。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("use_icon_as_answer_icon")]
        public bool? UseIconAsAnswerIcon { get; set; }

        /// <summary>
        /// 获取或设置创建者 ID。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_by")]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// 获取或设置创建时间戳。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("created_at")]
        public long? CreatedAt { get; set; }

        /// <summary>
        /// 获取或设置更新者 ID。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_by")]
        public string? UpdatedBy { get; set; }

        /// <summary>
        /// 获取或设置更新时间戳。
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("updated_at")]
        public long? UpdatedAt { get; set; }
    }
}