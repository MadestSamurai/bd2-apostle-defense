# BD2 Apostle Defense v0.3.0

## 简体中文

### 更新内容

- 改善网络短暂波动时的接续，减少外部探测误触发的断连；恢复期间保留任务，等待连接和盘面同步后继续。
- 增加网络状态和诊断记录，区分短暂探测失败与真正断线。
- 新增「好运模式」，默认关闭，每次开启需确认风险；重启工具后恢复关闭。
- 中英文界面同步更新，继续提供 Portable／Lite 双版本。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 内置 .NET 运行时 | 首次使用推荐，下载即用 |
| **Lite** | 需安装 [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/download/dotnet/8.0) | 已安装运行时，下载更小 |

适用于 Windows x64。EXE 可独立运行，ZIP 附说明与许可证；使用 `SHA256SUMS.txt` 校验下载。两版功能相同，均内置简体中文／English。

### 升级

暂停并关闭旧工具，正常重启游戏，再打开新版连接。本机设置保留；具体功能与设置迁移见上面的更新内容。

[使用说明与风险声明](https://github.com/MadestSamurai/bd2-apostle-defense/blob/main/README.md)

## English

### Changes

- Improves recovery from brief network interruptions and reduces disconnects caused by external connectivity checks. Tasks wait for the connection and board to synchronize, then continue.
- Adds network status and diagnostics to distinguish temporary probe failures from actual disconnections.
- Adds **Lucky mode**, off by default. Each activation requires risk confirmation; restarting the assistant turns it off.
- Updates both interface languages and retains Portable / Lite editions.

### Downloads

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| **Portable** | .NET included | Most users; download and run |
| **Lite** | [.NET Desktop Runtime 8 x64](https://dotnet.microsoft.com/download/dotnet/8.0) | Smaller download if the runtime is installed |

For Windows x64. EXEs run on their own; ZIPs include documentation and licenses. Verify downloads against `SHA256SUMS.txt`. Both editions have the same features and include Simplified Chinese / English.

### Upgrade

Pause and close the old assistant, restart the game normally, then connect with the new version. Local preferences are retained; see Changes above for feature and setting migrations.

[Usage and risk disclaimer](https://github.com/MadestSamurai/bd2-apostle-defense/blob/main/README.en.md)
