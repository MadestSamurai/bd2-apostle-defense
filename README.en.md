# BD2 Apostle Defense

> **Disclaimer:** Using this assistant carries risks, including account penalties or bans, game errors, and data loss. This project is not affiliated with the game publisher and does not guarantee safe use. Assess the risks and follow the game's rules; you assume responsibility for all risks and consequences of using the tool.

English · [简体中文](README.md)

[Download Windows EXE](https://github.com/MadestSamurai/bd2-apostle-defense/releases/latest) · [Report an issue](https://github.com/MadestSamurai/bd2-apostle-defense/issues)

A standalone assistant for BrownDust II's Apostle Random Defense minigame on the Windows PC client. It targets two achievements: **clear wave 50** and **summon top-tier units**. It reads the live board and rules, then handles summons, upgrades, movement, rerolls, results and the next round. Chinese and English are built in; Python and other BD2 tools are not required.

## Download and start

Current version: **0.2.0**. Both editions have identical features and languages.

| Edition | Runtime requirement | Recommended for |
| --- | --- | --- |
| Portable | .NET included | Most users; run the EXE directly |
| Lite | .NET Desktop Runtime 8 x64 installed | A smaller download on an existing .NET setup |

The EXE runs on its own. ZIP bundles include both READMEs, licenses and maintenance documentation. Download one edition; verify it with `SHA256SUMS.txt` if desired.

1. Before upgrading, pause and close the old assistant, then restart the game normally to unload its component.
2. Enter the Apostle Defense lobby, open the assistant and click **Connect game**.
3. Choose goals, check your account and server progress, then click **Start**. By default it pursues wave 50 first, then focuses on top-tier summons.
4. Pause at any time, or select **Stop after this round**. Pausing the assistant does not pause the game timer.

Switch **简体中文 / English** from the top bar. The first launch follows your system language; later launches remember your choice. Switching does not alter the active task, interval or pending command. Names supplied by the game retain their original language.

## Board and decisions

- The 68-cell board shows actual positions, elements, tiers, enemies and move arrows. Click a unit to inspect its stats, range and route coverage. Inspection does not control the game; arrow keys navigate and Esc clears selection.
- English cells use Wa / Fi / Wi / Li / Da for Water / Fire / Wind / Light / Dark, followed by tier. Tooltips and selection details show full names.
- Ordinary waves compare executable moves and swaps, upgrades, summons and low-risk rerolls. A confirmed sale is followed by a refill before other spending.
- Boss waves use enemy position, speed and route to predict short-term damage and move attackers to stay in range. The planner handles multiple bosses, deducts displaced allies' losses and preserves attack progress. Static repositioning and selling are suspended during boss waves.
- Action interval: 100–3000 ms, default 500 ms. Commands wait for game confirmation rather than accumulating. Pursuit also checks the exact live target, wave and snapshot age.
- Results, Exit, confirmation and return to lobby follow the game's own flow. Achievement progress is read back from the server before continuing. Disabling auto-next still allows the current round to exit normally.

The planner uses short-horizon estimates. **It is not a full battle simulator and does not guarantee wave 50 or a specific summon.** Boss pursuit has offline regression coverage; actual results also depend on units, frame rate and latency.

## Connection, compatibility and diagnostics

- Windows x64 PC client only, one game process at a time. Enter the minigame before connecting. Other game popups may pause actions.
- The component is generated from the installed client's interfaces at connection time. Recognized renames and reordering are resolved; uncertain matches stop connection. This does not guarantee compatibility with every future update.
- No game DLLs, credentials, inventories, replays or private captures are included. Rules are read from the local installation.
- Settings, language and diagnostics live in `%LOCALAPPDATA%\BD2ApostleDefense`; use **Open diagnostics**. Existing private-version settings are retained. Diagnostics may contain account names, board state and local paths; remove personal information before sharing.
- If the heartbeat does not return, check whether the game exited, is still loading, or has an older component loaded. Restart the game normally before connecting a different tool version.

## Development

Project code is [MIT licensed](LICENSE). See [third-party notices](THIRD_PARTY_NOTICES.md).

With .NET 8 SDK, run `./build.ps1` in the repository; use `./package.ps1` for both editions. Normal builds and tests do not need a game installation. CI uses generated scenarios and never starts or injects a game. See [development](docs/DEVELOPMENT.md) and [localization](docs/LOCALIZATION.md).
