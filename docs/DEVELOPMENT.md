# Development / 开发

Requires Windows x64 and .NET 8 SDK. 普通构建不需要安装游戏。

```powershell
./build.ps1
./package.ps1 -Locked
```

The package script produces `dist/v0.2.2/`: Portable/Lite EXE and ZIP, `SHA256SUMS.txt`, `release.json`. It refuses to overwrite an existing release directory. Build products live in ignored `.build/`, `bin/` and `obj/` folders.

- `shared/`: snapshot and command protocol, guards and native flow.
- `planner/`: ordinary-wave assignment and short-horizon boss pursuit.
- `core/`: process connection and single-command controller.
- `hook/`: owned source compiled against the user's local client at connection time.
- `compatibility/`: interface metadata matching, compilation and embedded contract; no proprietary assemblies.
- `desktop/`, `localization/`: WPF interface, source-message catalogs and reversible rendering.
- `tests/`: generated scenarios, assignment oracle, controller regressions and localization coverage.
- `compatibility-tests/`: generated assemblies test renaming, reordering and refusal of ambiguous matches.

Private account captures and table exports are not fixtures in this repository. Tests never connect to a running game. Native integration can be checked offline with an installed client:

```powershell
./BD2ApostleDefense.exe --check-client "C:\path\to\Managed" "C:\test-output"
```

This compiles the component and writes a report; it does not inject it. `--smoke <output>` renders a generated board, exercises both languages and controller behavior, then exits. `--identity <file>` writes the embedded version and fingerprint.

Tag `v<version>` only after local checks. GitHub Actions builds without game installation and creates a **draft** release. Download that exact CI artifact, verify checksums, inspect both packaged UIs and test local client compilation before publishing the draft. Never replace already-public assets with different bytes.

跨版本兼容以可靠的接口匹配为前提；不确定时拒绝注入。不要将离线模型结果表述为实机通关保证。请勿把游戏DLL、账户数据、运行日志或私有采集提交到仓库。

## Recovery behavior in 0.2.2

Runtime5 separates native-action timeouts from unclassified exceptions. A timeout finishes the command without latching a component fault. The controller retains enabled intent, reads the current state, and delays repeated failures by 2, 4, 8, 16 and at most 30 seconds. A successful action or new room resets its delay. Movement failures cool the affected unit, including reverse swaps, so summons and other units remain available. Local movement has a two-second readback deadline; settlement retains its thirty-second deadline. There is never more than one in-flight command.

Lobby arrival acknowledges an exit even if a popup overlays the lobby. Cancelled matching and match-failure screens supersede obsolete actions and refill intent. Only a native `EventPopupUI` over the defense lobby, with no other blocking modal and a positive `CanCloseUI`, can use the normal Back handler. Unknown modals wait. A submitted exit (`Exiting`) is never clicked again; a slow achievement read attaches to the existing request. Account/process changes, incompatible interfaces and unclassified component exceptions still require intervention.

`RecoveryTests` uses generated states to cover delayed receipts, movement starvation, cancelled matching, event dismissal, unknown modals, slow exits and manual stop. Packaged smoke checks cover the key transitions. Local-client compilation verifies the interface contract without injection. Continuous live recovery remains a separate runtime acceptance check; offline tests do not prove uninterrupted gameplay.

## 文档格式 / Documentation format

README、仓库简介和 Release 统一遵循 [Publication style](PUBLICATION_STYLE.md)。新版本从 [Release template](RELEASE_TEMPLATE.md) 开始，更新 [当前版本说明](RELEASE_NOTES.md) 后再打包。

Use the shared format for READMEs, repository descriptions and releases. Update both languages and release notes before packaging.
