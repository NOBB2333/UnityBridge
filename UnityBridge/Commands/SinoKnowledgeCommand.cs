using System.Text;
using UnityBridge.Api.Sino;
using UnityBridge.Api.Sino.Extensions;
using UnityBridge.Api.Sino.Models;

namespace UnityBridge.Commands;

/// <summary>
/// 基于 Company API 的知识库辅助命令，封装 curl.txt 中的场景。
/// </summary>
public static class SinoKnowledgeCommand
{
    /// <summary>
    /// 对应 curl.txt #27：分页查看个人知识库列表。
    /// </summary>
    public static async Task ListIndividualLibrariesAsync(CompanyApiClient client, string? keyword = null,
        int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var request = new LangwellDocServerKnowledgeLibIndividualPageRequest
        {
            PageNumber = 1,
            PageSize = pageSize,
            Entity = new LangwellDocServerKnowledgeLibIndividualPageRequest.Types.Filter
            {
                LibName = keyword ?? string.Empty
            }
        };

        var response = await client.ExecuteLangwellDocServerKnowledgeLibIndividualPageAsync(request, cancellationToken)
            .ConfigureAwait(false);
        if (!response.IsSuccessful())
        {
            Console.WriteLine($"查询失败：{response.ErrorCode} {response.ErrorMessage ?? "未知错误"}");
            return;
        }

        if (response.Data?.Records is not { Count: > 0 } records)
        {
            Console.WriteLine("没有匹配的知识库。");
            return;
        }

        var table = BuildLibraryTable(records);
        Console.WriteLine(table);
        Console.WriteLine(
            $"总数: {response.Data.Total ?? "?"} | 当前页: {response.Data.Current ?? "?"}/{response.Data.Pages ?? "?"}");
    }

    /// <summary>
    /// 对应 curl.txt #372：拉取全部企业/个人知识库概览。
    /// </summary>
    public static async Task ShowLibraryOverviewAsync(CompanyApiClient client,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        var response = await client
            .ExecuteLangwellDocServerKnowledgeLibAllListBackAllAsync(
                new LangwellDocServerKnowledgeLibAllListBackAllRequest(), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessful())
        {
            Console.WriteLine($"获取概览失败：{response.ErrorCode} {response.ErrorMessage ?? "未知错误"}");
            return;
        }

        PrintSection("个人知识库", response.Data?.IndividualQuery?.KnowledgeLibraries);
        PrintSection("企业知识库", response.Data?.EnterpriseQuery?.KnowledgeLibraries);
        PrintSection("项目知识库", response.Data?.ProjectQuery?.KnowledgeLibraries);
    }

    private static string BuildLibraryTable(
        IList<LangwellDocServerKnowledgeLibIndividualPageResponse.Types.Record> records)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ID\t名称\t描述\t类型\t文件大小\t知识数量");
        foreach (var record in records)
        {
            builder.Append(record.Id ?? "-").Append('\t')
                .Append(record.Name ?? "-").Append('\t')
                .Append(string.IsNullOrWhiteSpace(record.Description) ? "-" : record.Description).Append('\t')
                .Append(record.DataType ?? "-").Append('\t')
                .Append(record.AllFileSize?.ToString() ?? "-").Append('\t')
                .Append(record.KnowledgeCount ?? "-").AppendLine();
        }

        return builder.ToString();
    }

    private static void PrintSection(string title,
        IList<LangwellDocServerKnowledgeLibAllListBackAllResponse.Types.KnowledgeLibInfo>? libs)
    {
        Console.WriteLine($"\n== {title} ==");
        if (libs is not { Count: > 0 })
        {
            Console.WriteLine("暂无数据。");
            return;
        }

        foreach (var lib in libs)
        {
            Console.WriteLine(
                $"- {lib?.Name ?? "(未命名)"} ({lib?.Id ?? "无 ID"}) | 数据类型: {lib?.DataType ?? "-"} | 文件总量: {lib?.AllFileSize?.ToString() ?? "-"}");
        }
    }
}