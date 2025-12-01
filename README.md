## UnityBridge

UnityBridge 是一个针对 Dify / 企业内部 AI 应用平台的 **命令行桥接工具**，用于在本地便捷地完成应用的导入导出、API Key 管理、编码检测等常见运维 / 开发工作，并内置试用期与离线授权机制。

- **下载（导出）应用**：从 Dify/控制台导出指定应用配置到本地文件。
- **上传（导入）应用**：将本地的应用 JSON 包上传到平台，实现应用迁移 / 备份。
- **检测文件/文件夹编码**：批量检测并显示文件或目录的文本编码。
- **生成 API Key**：基于已有应用生成/管理 API Key。
- **获取应用详情**：根据 App ID 拉取并显示应用详细信息。
- **试用期与离线授权**：内置 30 天试用期，支持基于机器码的离线激活 key。

> 以上功能对应控制台中的菜单 1 ～ 7，入口代码见 `UnityBridge/Program.cs`。

---

## 环境要求

- .NET 6 Runtime 或 SDK（建议安装 SDK，方便本地开发调试）
- 可访问目标 Dify / 企业内部 AI 应用平台的网络环境

---

## 本地运行

在仓库根目录执行：

```bash
dotnet run --project UnityBridge/UnityBridge.csproj
```

启动后会看到类似菜单：

- 1. 下载 (导出) 应用
- 2. 上传 (导入) 应用
- 3. 检测文件/文件夹编码
- 4. 生成 API Key
- 5. 获取应用详情
- 6. 查看试用期信息
- 7. 测试本机授权 (显示机器码并生成测试激活 key)

按提示输入编号回车即可。

> 提示：连接地址、请求头（含 Token / Cookie 等）统一保存在 `UnityBridge/Configuration/*.json` 中（例如 `Download.json` 里的 `Download` / `Upload` 节），程序启动时通过 Options 自动加载，保持节名称与文件中的对象名称一致即可无需硬编码，使用前请根据自己的环境修改这些 JSON。

---

## Docker 运行

仓库中已提供基础的 Docker 支持：

- `UnityBridge/Dockerfile`：构建 `UnityBridge` 控制台应用镜像。
- `compose.yaml`：示例 docker-compose 配置。

在仓库根目录执行：

```bash
docker compose build
docker compose run --rm unitybridge
```

进入容器后，会自动启动 UnityBridge 控制台程序，与本地运行效果一致。

---

## 试用期与授权说明

UnityBridge 内置了一个简单的试用/授权系统（见 `UnityBridge/Helpers/TrialManager.cs`）：

- **首次运行**：自动初始化 30 天试用期，并在控制台打印开始时间和到期时间。
- **每次启动**：在试用期内会显示剩余时间；到期后会提示输入激活 key。
- **配置存储**：试用信息加密保存在程序运行目录的 `Const/trial.dat` 中。
- **本机授权**：
  - 通过菜单 **7) 测试本机授权** 可以查看“机器码”（硬件指纹）。
  - 机器码由多种硬件标识组合而成：在 Windows 下优先读取 WMI 提供的整机 UUID、主板/BIOS 序列号、CPU ID、硬盘序列；无法获取时再退回到机器名、系统目录、CPU 核数及首个可用网卡的 MAC 地址，再整体做 MD5 得到 32 位 Hex。
  - 将机器码发给发行方，即可由发行方生成对应的激活 key。
  - 在试用到期提示时输入 key 即可完成离线激活。

> 注意：当前实现仅用于小工具 / 内部场景示例，正式商业环境建议配合服务端授权、日志审计等更完善的机制。

---

## 解决 “git push -u origin master” 报错的小提示

本仓库默认分支是 **`main`**，推送时应使用：

```bash
git push -u origin main
```

之后直接执行：

```bash
git push
```

即可推送到远程。
