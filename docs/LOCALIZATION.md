# Localization / 翻译

The application contains `zh-CN` and `en-US`. Chinese systems default to Chinese; other systems default to English. The explicit selection is saved independently in `language.json`, so switching cannot mutate controller leases, goals or action intervals.

`localization/zh-CN.json` maps source messages to themselves. `en-US.json` has identical keys. Full messages take precedence; formatted legacy diagnostics use longest-first literal fragments in one pass. The WPF adapter retains the source value, watches dynamic updates and detaches listeners when board cells are rebuilt. Protocol snapshots and evidence logs are not translated. Game account names and selected unit names bypass translation.

When adding UI or diagnostics, add both catalog entries. `LocalizationTests` uses Roslyn and XAML parsing to check source coverage, culture defaults, preference persistence and setting isolation. Packaged GUI checks cover live switching, dynamic decisions, exact name preservation, minimum-window tile sizing, listener cleanup and the Chinese roundtrip.

中文与英文同包维护，不按语言拆分发布。英文棋盘采用Wa／Fi／Wi／Li／Da短标签，完整含义显示在详情和提示中。新增较长翻译后需检查最小窗口；不要为了翻译重写正在运行的操作配置。
