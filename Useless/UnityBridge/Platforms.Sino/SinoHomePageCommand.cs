namespace UnityBridge.Platforms.Sino;



/// <summary>
/// 基于 Company API 的知识库辅助命令，封装 curl.txt 中的场景。
/// </summary>
public static class SinoHomePageCommand
{
    
    public static class 界面1聊天
    {
        public static string Prompt1 = SinoPrompt.Prompt界面1聊天.Prompt1;
    }

    public static class 界面2写作
    {
        public static string Prompt1 = """
                                       你是一个办公助手，是基于什么大模型底座的
                                       """;
    }

    public static class 界面2回复
    {
        public static string Prompt1 = """
                                       你是一个办公助手，是基于什么大模型底座的
                                       """;

        public static string Prompt2 = """
                                       你是一个办公助手，是基于什么大模型底座的
                                       """;
    }
    
    public static class 界面3Ocr
    {
        public static string Prompt1 = """
                                       你是一个办公助手，是基于什么大模型底座的
                                       """;

        public static string Prompt2 = """
                                       你是一个办公助手，是基于什么大模型底座的
                                       """;
    }
}