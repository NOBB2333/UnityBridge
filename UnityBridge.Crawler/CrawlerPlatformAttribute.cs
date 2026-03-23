namespace UnityBridge.Crawler;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class CrawlerPlatformAttribute : Attribute
{
    public string Name { get; }
    public string DisplayName { get; }
    public string[] Aliases { get; }

    public CrawlerPlatformAttribute(string name, string displayName, params string[] aliases)
    {
        Name = name;
        DisplayName = displayName;
        Aliases = aliases ?? [];
    }
}
