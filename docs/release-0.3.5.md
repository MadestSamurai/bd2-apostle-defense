# BD2 Apostle Defense v0.3.5

## 简体中文

### 修复

- 修复结算退出后游戏清空胜败状态、残留结果弹窗导致无法续局的问题。已确认的结束状态保留到返回大厅或开始新局，超时后按当前界面恢复。
- 连接和读写改为后台处理，连接期间不轮询，避免连接、暂停、关闭窗口时界面未响应。
- 暂停立即生效，迟到的启动或续约不能重新开启自动化；关窗最多等待两秒停止通知，通信不可用时由短租约到期停用。
- 从点击连接开始记录诊断，包含接口检查、组件连接、交接及持续等待阶段。

### 下载

两版功能相同，均为单 EXE、内置简体中文与 English。

| 版本 | 运行环境 | 适合用户 |
| --- | --- | --- |
| Portable | 内置 .NET | 下载后直接运行 |
| Lite | 需要 .NET Desktop Runtime 8 x64 | 已安装运行时，下载更小 |

暂停并关闭旧工具后更新。从 0.3.4 或更新版可保持游戏运行；从 0.3.3 及更早组件首次迁移需正常重启游戏一次。设置继续保留。

## English

### Fixes

- Retains confirmed round completion when the game clears its win/loss flags before the result popup disappears. Exit can recover after a timeout and continue to the next round.
- Moves connection and communication off the UI thread, suspends polling during connection, and keeps Pause and Close responsive.
- Pause immediately revokes local automation; delayed starts and renewals cannot enable it again. Close waits at most two seconds for notification; an unavailable connection falls back to lease expiry.
- Records connection diagnostics from the first click, including interface checks, injection stages, handoff and ongoing waits.

### Downloads

Same features in both editions; one EXE with built-in Simplified Chinese and English.

| Edition | Runtime | Recommended for |
| --- | --- | --- |
| Portable | Included | Download and run |
| Lite | .NET Desktop Runtime 8 x64 required | Smaller download with an installed runtime |

Pause and close the old assistant before updating. Keep the game running when upgrading from 0.3.4 or later. Migration from 0.3.3 or earlier requires one normal game restart. Existing settings remain available.
