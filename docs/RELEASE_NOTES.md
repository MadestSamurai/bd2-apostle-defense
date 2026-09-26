# BD2 Apostle Defense v0.3.3

## 简体中文

### 更新内容

- 修复胜败结算后长时间卡住：游戏正常关闭战斗连接时，助手继续完成结算和退出。
- 修复旧指令迟迟没有确认时一直等待的问题；恢复后根据最新界面继续，保留自动化开关。
- 改善文件短暂占用时的恢复，避免诊断文件写入失败阻断状态同步；错误记录增加文件路径和详细原因。
- 已经返回大厅或开始退出时及时识别状态，不重复点击退出。中英文界面同步更新。

### 下载

| 版本 | 运行环境 | 建议 |
| --- | --- | --- |
| **Portable** | 自带 .NET，无需另装运行库 | 大多数用户 |
| **Lite** | 需要 .NET Desktop Runtime 8 x64 | 已安装桌面运行库、希望减小下载体积 |

两版功能相同，内置简体中文／English。EXE 可独立使用；ZIP 附带双语说明与许可证。用 `SHA256SUMS.txt` 核对下载。

### 升级

暂停并关闭旧工具，正常重启游戏，再打开新版连接。已有设置保留。本次包含连接组件修复，需要重启游戏才能加载新版。

作者发布版免费。第三方收费不代表作者参与、背书或提供服务。[使用说明与风险提示](https://github.com/MadestSamurai/bd2-apostle-defense/blob/main/README.md)。

## English

### Changes

- Fixes long stalls after victory or defeat: the assistant continues settlement and Exit when the game normally closes the completed round's battle connection.
- Recovers commands that were not accepted or whose readback never arrived, then resumes from the current screen without turning automation off.
- Adds bounded recovery from temporary file contention. Failed diagnostic writes no longer block state updates, and errors include file paths and detailed causes.
- Recognizes arrival in the lobby and exits already in progress without clicking Exit twice. Both UI languages are updated.

### Downloads

| Build | Runtime | Recommended for |
| --- | --- | --- |
| **Portable** | Includes .NET; no separate runtime needed | Most users |
| **Lite** | Requires .NET Desktop Runtime 8 x64 | Smaller download when the desktop runtime is installed |

Both builds have identical features and include Simplified Chinese / English. EXEs run independently; ZIPs include bilingual documentation and licenses. Verify downloads with `SHA256SUMS.txt`.

### Upgrade

Pause and close the old assistant, restart the game normally, then connect with the new version. Existing settings are retained. This update replaces the connection component, so restarting the game is required.

Official releases are free. Third-party fees do not imply the author's involvement, endorsement or support. [Usage and risk notice](https://github.com/MadestSamurai/bd2-apostle-defense/blob/main/README.en.md).
