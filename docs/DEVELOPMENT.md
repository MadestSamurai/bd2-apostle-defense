# Development / 开发

Requires Windows x64 and .NET 8 SDK. 普通构建不需要安装游戏。

```powershell
./build.ps1
./package.ps1 -Locked
```

The package script produces `dist/v0.3.3/`: Portable/Lite EXE and ZIP, `SHA256SUMS.txt`, `release.json`. It refuses to overwrite an existing release directory. Build products live in ignored `.build/`, `bin/` and `obj/` folders.

- `shared/`: snapshot and command protocol, guards and native flow.
- `planner/`: ordinary-wave assignment and short-horizon boss pursuit.
- `core/`: process connection and single-command controller.
- `hook/`: owned source compiled against the user's local client at connection time.
- `compatibility/`: interface metadata matching, compilation and embedded contract; no proprietary assemblies.
- `desktop/`, `localization/`: WPF interface, source-message catalogs and reversible rendering.
- `tests/`: generated scenarios, assignment oracle, controller regressions and localization coverage.
- `native-tests/`: actual Harmony adapters tested against isolated synthetic transport and summon methods.
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

## Connection recovery in 0.3.0

The network adapter is resolved from the installed client through the same compatibility contract as gameplay inputs. Only the owned defense TCP connection is observed. Complete packets from its current transport, including native heartbeat responses, provide health evidence; broadcasts and retired transports do not. The external connectivity probe gets a bounded grace period, while true socket loss and explicit server-close packets retain native handling. Recovery requires stable traffic and a room acknowledgment after reconnect. No packet payloads or settlement results are rewritten.

The controller retains one pending command during recovery, renews its lease and pauses its readback deadline. After recovery it reconciles the actual receipt before planning again. Network diagnostics rotate at 2 MiB. Leaving the owned monitor or unloading restores its original timeout.

Lucky mode defaults to off and requires a new confirmation every time it is enabled. Consent is session-only, and disabling it updates the active lease immediately, even if another input is invalid. Its native adapter uses a thread-local scope around the tool's normal summon action and restores that scope in a `finally` block. Generated regressions check isolation, rollback, total preservation and unchanged game tables. These checks and offline client compilation do not establish continuous live gameplay reliability.

## 文档格式 / Documentation format

README、仓库简介和 Release 统一遵循 [Publication style](PUBLICATION_STYLE.md)。新版本从 [Release template](RELEASE_TEMPLATE.md) 开始，更新 [当前版本说明](RELEASE_NOTES.md) 后再打包。

Use the shared format for READMEs, repository descriptions and releases. Update both languages and release notes before packaging.

## Settlement recovery in 0.3.3

Runtime7 distinguishes `RoomEnded` from own-player death. The completed room no longer needs battle TCP recovery; an unfinished disconnected room still blocks game inputs. The frame pump reads the room lifecycle before considering network recovery. Scene receipts and obsolete board actions are reconciled before the network wait gate.

Snapshots expose `AcceptedCommand`. Missing acceptance after a fresh snapshot is more than four seconds newer than the original decision, or accepted readback beyond its execution deadline plus five seconds, requests `CancelThrough`. The component acknowledges retirement before the controller issues another command. This retires the tool's waiting state; it does not undo a native action already submitted to the game. Decision timestamps are never refreshed to make stale commands look valid. Genuine live-network recovery pauses the accepted operation's watchdog. Receipt processing and manual pause remain available.

Atomic writes retry replacement three times with bounded 20/40 ms delays, preserve the previous complete file on failure and remove temporary files. Hook command reads, snapshot publication, action evidence, flow logs, network logs and heartbeat publication run as isolated I/O steps. Each process writes rotating `io-errors-<pid>.log` diagnostics with path, operation, exception, HResult and stack; matching failures are limited to one entry every 15 seconds. All blocking I/O remains off Unity's frame thread.

`SettlementRecoveryTests`, `AtomicFilesTests` and actual network-adapter regressions cover normal terminal disconnect, own-player death, late/lost acknowledgments, a four-hour simulated wait, cancellation barriers, lobby transitions, real network recovery, file locks, partial serialization, concurrent readers/writers and logging failure isolation. Generated fixtures contain no user logs. Continuous live cross-round verification is still required separately.
