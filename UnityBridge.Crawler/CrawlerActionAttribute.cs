namespace UnityBridge.Crawler;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class CrawlerActionAttribute : Attribute
{
    public string Name { get; }
    public string[] Aliases { get; init; } = [];
    public int PlatformArgumentIndex { get; init; }
    public bool PlatformOptional { get; init; }
    public bool SupportsAllPlatforms { get; init; }
    public bool RunInParallelForAll { get; init; }

    public CrawlerActionAttribute(string name)
    {
        Name = name;
    }
}
