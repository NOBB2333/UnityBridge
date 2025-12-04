namespace UnityBridge.Api.Sino.Settings
{
    public class Credentials
    {
        /// <summary>
        /// 初始化客户端时 <see cref="CompanyApiClientOptions.Token"/> 的副本。
        /// </summary>
        public string? Token { get; }

        /// <summary>
        /// 初始化客户端时 <see cref="CompanyApiClientOptions.OverToken"/> 的副本。
        /// </summary>
        public string? OverToken { get; }

        /// <summary>
        /// 初始化客户端时 <see cref="CompanyApiClientOptions.TenantId"/> 的副本。
        /// </summary>
        public string? TenantId { get; }

        internal Credentials(CompanyApiClientOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            Token = options.Token;
            OverToken = options.OverToken;
            TenantId = options.TenantId;
        }
    }
}
