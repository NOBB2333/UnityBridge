using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace UnityBridge.Crawler;

public sealed class CrawlerCommandRegistry
{
    private readonly Dictionary<string, RegisteredActionDefinition> _actionsByName;
    private readonly Dictionary<string, string> _actionAliases;
    private readonly Dictionary<string, RegisteredPlatformDefinition> _platformsByName;
    private readonly Dictionary<string, string> _platformAliases;
    private readonly Dictionary<(string Action, string Platform), RegisteredPlatformAction> _actionsByPlatform;

    private CrawlerCommandRegistry(
        Dictionary<string, RegisteredActionDefinition> actionsByName,
        Dictionary<string, string> actionAliases,
        Dictionary<string, RegisteredPlatformDefinition> platformsByName,
        Dictionary<string, string> platformAliases,
        Dictionary<(string Action, string Platform), RegisteredPlatformAction> actionsByPlatform)
    {
        _actionsByName = actionsByName;
        _actionAliases = actionAliases;
        _platformsByName = platformsByName;
        _platformAliases = platformAliases;
        _actionsByPlatform = actionsByPlatform;
    }

    public IReadOnlyCollection<RegisteredActionDefinition> Actions => _actionsByName.Values;
    public IReadOnlyCollection<RegisteredPlatformDefinition> Platforms => _platformsByName.Values;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "CLI scanning relies on rooted crawler assemblies preserved by rd.xml.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "CLI entry types are discovered from the rooted crawler assembly and only require public static methods.")]
    public static CrawlerCommandRegistry Create(Assembly assembly)
    {
        var actionsByName = new Dictionary<string, RegisteredActionDefinition>(StringComparer.OrdinalIgnoreCase);
        var actionAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var platformsByName = new Dictionary<string, RegisteredPlatformDefinition>(StringComparer.OrdinalIgnoreCase);
        var platformAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["all"] = "all"
        };
        var actionsByPlatform = new Dictionary<(string Action, string Platform), RegisteredPlatformAction>();

        foreach (var type in assembly.GetTypes()
                     .Where(static t => t.GetCustomAttribute<CrawlerPlatformAttribute>() is not null))
        {
            var platformAttribute = type.GetCustomAttribute<CrawlerPlatformAttribute>()!;
            var platformName = NormalizeKey(platformAttribute.Name);
            var displayName = platformAttribute.DisplayName.Trim();

            if (platformsByName.TryGetValue(platformName, out var existingPlatform))
            {
                if (!string.Equals(existingPlatform.DisplayName, displayName, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"平台 {platformName} 的显示名称定义冲突：{existingPlatform.DisplayName} / {displayName}");
                }
            }
            else
            {
                existingPlatform = new RegisteredPlatformDefinition(platformName, displayName);
                platformsByName[platformName] = existingPlatform;
            }

            RegisterAlias(platformAliases, platformName, platformName, "平台");
            foreach (var alias in platformAttribute.Aliases.Where(static alias => !string.IsNullOrWhiteSpace(alias)))
            {
                RegisterAlias(platformAliases, alias, platformName, "平台");
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                         .Where(static m => m.GetCustomAttributes<CrawlerActionAttribute>().Any()))
            {
                ValidateMethod(type, method);
                var executor = CreateExecutor(method);

                foreach (var actionAttribute in method.GetCustomAttributes<CrawlerActionAttribute>())
                {
                    var actionName = NormalizeKey(actionAttribute.Name);
                    if (string.IsNullOrWhiteSpace(actionName))
                    {
                        throw new InvalidOperationException($"{type.FullName}.{method.Name} 的动作名称不能为空。");
                    }

                    var actionDefinition = RegisterAction(actionsByName, actionAliases, actionName, actionAttribute);
                    var key = (actionDefinition.Name, existingPlatform.Name);
                    if (actionsByPlatform.ContainsKey(key))
                    {
                        throw new InvalidOperationException(
                            $"动作 {actionDefinition.Name} 在平台 {existingPlatform.Name} 上重复注册。");
                    }

                    actionsByPlatform[key] = new RegisteredPlatformAction(
                        actionDefinition.Name,
                        existingPlatform.Name,
                        existingPlatform.DisplayName,
                        executor);
                }
            }
        }

        return new CrawlerCommandRegistry(actionsByName, actionAliases, platformsByName, platformAliases, actionsByPlatform);
    }

    public bool TryResolveAction(string input, out RegisteredActionDefinition action)
    {
        var key = NormalizeKey(input);
        if (_actionAliases.TryGetValue(key, out var canonical)
            && _actionsByName.TryGetValue(canonical, out action!))
        {
            return true;
        }

        action = null!;
        return false;
    }

    public string NormalizePlatform(string input)
    {
        var key = NormalizeKey(input);
        return _platformAliases.TryGetValue(key, out var canonical) ? canonical : key;
    }

    public bool TryGetActionForPlatform(string action, string platform, out RegisteredPlatformAction registeredAction)
        => _actionsByPlatform.TryGetValue((NormalizeKey(action), NormalizeKey(platform)), out registeredAction!);

    public IReadOnlyList<RegisteredPlatformAction> GetActionsForAllPlatforms(string action)
        => _actionsByPlatform.Values
            .Where(item => string.Equals(item.Action, NormalizeKey(action), StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Platform, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public RegisteredPlatformDefinition? TryGetPlatform(string platform)
    {
        _platformsByName.TryGetValue(NormalizeKey(platform), out var registeredPlatform);
        return registeredPlatform;
    }

    private static RegisteredActionDefinition RegisterAction(
        IDictionary<string, RegisteredActionDefinition> actionsByName,
        IDictionary<string, string> actionAliases,
        string actionName,
        CrawlerActionAttribute attribute)
    {
        if (!actionsByName.TryGetValue(actionName, out var existing))
        {
            existing = new RegisteredActionDefinition(
                actionName,
                attribute.PlatformArgumentIndex,
                attribute.PlatformOptional,
                attribute.SupportsAllPlatforms,
                attribute.RunInParallelForAll);
            actionsByName[actionName] = existing;

            RegisterAlias(actionAliases, actionName, actionName, "动作");
            foreach (var alias in attribute.Aliases.Where(static alias => !string.IsNullOrWhiteSpace(alias)))
            {
                RegisterAlias(actionAliases, alias, actionName, "动作");
            }

            return existing;
        }

        if (existing.PlatformArgumentIndex != attribute.PlatformArgumentIndex
            || existing.PlatformOptional != attribute.PlatformOptional
            || existing.SupportsAllPlatforms != attribute.SupportsAllPlatforms
            || existing.RunInParallelForAll != attribute.RunInParallelForAll)
        {
            throw new InvalidOperationException($"动作 {actionName} 的注册配置不一致。");
        }

        RegisterAlias(actionAliases, actionName, actionName, "动作");
        foreach (var alias in attribute.Aliases.Where(static alias => !string.IsNullOrWhiteSpace(alias)))
        {
            RegisterAlias(actionAliases, alias, actionName, "动作");
        }

        return existing;
    }

    private static void RegisterAlias(
        IDictionary<string, string> aliasMap,
        string alias,
        string canonical,
        string scope)
    {
        var key = NormalizeKey(alias);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (aliasMap.TryGetValue(key, out var existing) && !string.Equals(existing, canonical, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{scope}别名 {alias} 同时指向 {existing} 和 {canonical}。");
        }

        aliasMap[key] = canonical;
    }

    private static void ValidateMethod(Type type, MethodInfo method)
    {
        if (!method.IsStatic)
        {
            throw new InvalidOperationException($"{type.FullName}.{method.Name} 必须是 static 方法。");
        }

        if (!typeof(Task).IsAssignableFrom(method.ReturnType))
        {
            throw new InvalidOperationException($"{type.FullName}.{method.Name} 返回值必须是 Task 或 Task<T>。");
        }

        var parameters = method.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(CrawlerCommandContext))
        {
            throw new InvalidOperationException(
                $"{type.FullName}.{method.Name} 参数签名必须是 (CrawlerCommandContext ctx)。");
        }
    }

    private static Func<CrawlerCommandContext, Task> CreateExecutor(MethodInfo method)
    {
        return async context =>
        {
            var result = method.Invoke(null, [context]);
            if (result is not Task task)
            {
                throw new InvalidOperationException($"{method.DeclaringType?.FullName}.{method.Name} 未返回 Task 实例。");
            }

            await task;
        };
    }

    private static string NormalizeKey(string value) => value.Trim().ToLowerInvariant();
}

public sealed record RegisteredActionDefinition(
    string Name,
    int PlatformArgumentIndex,
    bool PlatformOptional,
    bool SupportsAllPlatforms,
    bool RunInParallelForAll
);

public sealed record RegisteredPlatformDefinition(
    string Name,
    string DisplayName
);

public sealed record RegisteredPlatformAction(
    string Action,
    string Platform,
    string PlatformDisplayName,
    Func<CrawlerCommandContext, Task> ExecuteAsync
);
