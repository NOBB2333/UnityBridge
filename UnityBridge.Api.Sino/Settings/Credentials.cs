namespace UnityBridge.Api.Sino.Settings
{
    public class Credentials
    {
        /// <summary>
        /// 初始化客户端时 <see cref="CompanyApiClientOptions.AccessToken"/> 的副本。
        /// </summary>
        public string AccessToken { get; }

        internal Credentials(CompanyApiClientOptions options)
        {
            if (options is null) throw new ArgumentNullException(nameof(options));

            AccessToken = options.AccessToken;
        }
    }
}
