# Development / 开发

Requires Windows x64 and .NET 8 SDK. 普通构建不需要安装游戏。

```powershell
./build.ps1
./package.ps1 -Locked
```

The package script produces `dist/v0.2.1/`: Portable/Lite EXE and ZIP, `SHA256SUMS.txt`, `release.json`. It refuses to overwrite an existing release directory. Build products live in ignored `.build/`, `bin/` and `obj/` folders.

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
