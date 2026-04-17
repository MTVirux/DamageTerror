# CLAUDE.md — DamageTerror

## Project Overview

DamageTerror is a Dalamud plugin for FFXIV that provides a native ImGui damage meter overlay powered by IINACT. It displays real-time DPS/HPS/DTPS with encounter history, skill breakdowns, graph views, buff/debuff tracking, and configurable themes.

- **Author**: MTVirux
- **Framework**: Dalamud Plugin API Level 14, .NET 10, ImGui
- **Command**: `/dt` — toggle meter; `/dt config` — toggle config; `/dt toggle <group>` — toggle popout group

## Build

```powershell
.\scripts\build\debug.ps1     # Debug build (from repo root)
.\scripts\build\release.ps1   # Release build
```

Build always runs from the repo root: `C:\Users\Virux\FFXIVWorkspace\DamageTerror`

## Project Structure

```
DamageTerror/              # Main plugin project
  Core/                    # Plugin entry point (DamageTerrorPlugin.cs)
  Configuration.cs         # All config properties (~600 lines), Newtonsoft.Json serialized
  Enums/                   # BarColumn, SortField, ViewMode, TabFilterMode, etc.
  Models/                  # CombatantEntry, CombatEncounter, EncounterSnapshot, MeterTab, ThemePreset, etc.
  Services/                # DataService, EncounterStore, SkillTracker, StatusTracker, GraphDataTracker, WebSocket/IPC sources
  Gui/
    MainWindow/            # MainWindow, CombatantBarComponent, GraphViewComponent, PopoutTabWindow, etc.
    ConfigWindow/          # ConfigWindow, tabs: General, Display, Appearance, MeterTabs, Layout, SampleData, History
  Helpers/                 # Parsers, formatters, lookup tables (JobDataTable, DoT potencies, positionals, EncounterSearchHelper)
  Presets/                 # BuiltInPresets — 7 theme presets (Default, Kagerou, Ember, Horizoverlay, MopiMopi, Ikegami, NextUI)
ECommons/                  # Submodule — shared Dalamud utilities
OtterGui/                  # Submodule — ImGui utilities
NightmareUI/               # Submodule — UI components (DO NOT EDIT)
NightmareUI.OtterGuiWrapper/ # Submodule (DO NOT EDIT)
FFXIVClientStructs/        # Submodule — game struct definitions
```

## Architecture

- **No DI framework** — services are manually constructed in `DamageTerrorPlugin` and passed by reference.
- **Data flow**: IINACT → IPC or WebSocket → `DataSourceDispatcher` → `DataService` → `EncounterStore` / `SkillTracker` / `GraphDataTracker` / `StatusTracker`
- **Rendering**: `MainWindow.Draw()` iterates `config.Layout` elements. Each element delegates to a component (EncounterHeaderComponent, CombatantBarComponent, etc.).
- **Threading**: Data arrives on background threads; all mutable shared state in services is guarded by `lock(syncLock)`.
- **Serialization**: Newtonsoft.Json with `TolerantEnumConverters` for forward/backward compat. Collection properties on Configuration use `[JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]`.

## Conventions

- **Config defaults**: Font sizes use `FontDefaults.BaseSizePt` (14f) constant in Configuration.cs (`file static class`).
- **Enum serialization**: Never rename enum values — they're persisted in user config JSON.
- **Submodules**: Never edit files in ECommons, OtterGui, NightmareUI, NightmareUI.OtterGuiWrapper, FFXIVClientStructs.
- **Logging**: Use `ServiceManager.PluginLog` (static accessor) — no constructor-injected IPluginLog.
- **ImGui IDs**: All interactive ImGui widgets use `##` suffixed IDs to avoid conflicts.
- **Sealed classes**: All non-abstract, non-inherited classes are `sealed`. Always seal new classes unless designing for inheritance.
- **Readonly structs**: Value types that don't need mutation after construction (e.g., `SkillUseEvent`, `GraphSample`, `StatusClassification`, `ActiveBuff`) use `readonly struct` with `init` properties and `with` expressions for "mutations".
- **Model properties**: Model classes use auto-properties (`{ get; set; }`) not public fields, even for simple data holders (e.g., `StatusApplication`).
- **Global usings**: All `DamageTerror.*` namespaces are imported in `GlobalUsings.cs`. Never add per-file `using DamageTerror.{Enums|Helpers|Services|Models|Jobs|Presets|Core};`.
- **Compiled regex**: Static regex patterns use `RegexOptions.Compiled` and are cached as `static readonly` fields (e.g., `PresetManager.InvalidFileNameRegex`).
- **Comments**: Only add comments that explain *why*, document magic numbers, or describe non-obvious game mechanics. Do not add comments that restate what code does.

## Key Patterns

- `EncounterStore.isStaleDataSuppressed`: Set after user removes active encounter; drops incoming data until a genuinely new encounter starts.
- `CombatDataParser.SanitizeNumericToken()`: Shared null/special-case handling for all numeric parse methods.
- `WebSocketDataSource`: Auto-reconnect with exponential backoff (1s initial, 30s max). Max message size 10 MB.
- `GraphDataTracker.ValidationThreshold` (5%): Max divergence between log-line and CombatData totals before correction.
- `StatusTracker`: Hardcoded status IDs are game-version-dependent; kept in code with job-name comments.
- `SkillTracker.GetEventsWithFallback()`: Shared helper for live-events-with-seeded-fallback pattern (used by GetSkillEvents, GetDamageTakenEvents, GetItemEvents).
- `SkillTracker.IncrementStatusStackCount()`: Shared helper for counting status stacks (skill issue / damage down).
- `SampleCombatSimulator`: Uses `Random.Shared` (thread-safe, .NET 6+) — never instantiate `new Random()`.
- `JobDataTable`: Single source of truth for all job abbreviations, full names, roles, colors, and ClassJob IDs. `JobColorHelper` and `JobIconHelper` delegate to it.
- `JobColorHelper.GetEffectiveJobColor()`: Resolves per-job or role-based colors from config. `GetBarColor()` derives a dimmed variant.
- `JobIconHelper`: Named constants `PlainIconOffset`, `FramedIconOffset`, `LimitBreakIconId` for icon resolution.
- `SkillTracker`: Named constants `CritFlag` (0x20), `DirectHitFlag` (0x40), `DotOutlierLowThreshold`, `DotOutlierHighThreshold`, `DotMinHitsForOutlierFilter` for combat data parsing.
- `EncounterSearchHelper.MatchesFilter()`: Shared zone/title/player/job search logic used by EncounterHeaderComponent and EncounterHistoryTab.
- `DamageTerrorPlugin.SafeDispose()`: Each disposal step is wrapped individually so one failure doesn't block others.
- `EncounterSnapshot.EnsureCaseInsensitive<TValue>()`: Generic helper for rebuilding deserialized dictionaries with `OrdinalIgnoreCase` comparer.
- `DataService.FinalizeOutgoingEncounter()`: Shared helper that captures final skills & graph data before resetting trackers.
- `SkillTracker.GetCountLocked()`: Shared helper for single-value dictionary lookups under syncLock.
- `DataService.lastCombatDataTicks`: Thread-safe via `Interlocked.Read/Exchange` (cross-thread read in CheckStaleness / write in OnCombatData).
- `IpcDataSource.connected`: Marked `volatile` for cross-thread visibility.
- `PresetManager.SaveCustomPreset()`: Atomic write pattern (temp file + `File.Move`).

## Known DRY Debt

- **ThemePreset ↔ Configuration**: ~400 properties duplicated; `ApplyTo()`/`CreateFromConfig()` maintain 80+ manual assignments. High-effort refactor — consider shared base class when adding properties becomes painful.
- **AppearanceTab graph config**: `DrawDetailsTab` and `DrawGraphViewTab` share ~70% identical graph-settings widgets (~200 LOC). Extract shared `DrawGraphSettings()` helper when adding new graph options.
- **Graph array building**: `CombatantDetailPanel.DrawGraphTab` and `GraphViewComponent.Render` share ~200 LOC of graph array building code. Extract shared `GraphRenderHelper.BuildGraphArrays()` when modifying graph logic.
- **BuiltInPresets**: 7 presets × ~300 LOC each. Consider base preset + override pattern when adding new presets.
- **AppearanceTab slider/color widgets**: `DrawBarsTab` has 500+ LOC of repeated ImGui slider/color picker patterns. Consider data-driven config widget helper when adding new appearance options.
- **MainWindow ↔ PopoutTabWindow**: Component initialization, `Draw()` orchestration, and `DrawCombatantBars()` are near-identical (~100 LOC). Extract to shared `MeterWindowHelper.RenderMeterContent()` / `RenderCombatantBars()`.
- **GraphSettings factory methods**: `GraphSettings.FromGraphView()` and `FromDetail()` are 18 identical assignments each. Consider generic factory with prefix parameter.
- **SkillMarkerConfig triplets**: 6 separate `SkillMarkerConfig` properties (DPS/HPS/DTPS × Detail/GraphView) in both Configuration and ThemePreset. Consider `Dictionary<MetricType, SkillMarkerConfig>` per context.
- **CombatantBarComponent.GetColumnDisplayValue()**: 50+ case switch could use dictionary dispatch for extensibility.
- **CombatantDetailPanel**: 1000+ LOC file with 5 embedded tab renderers (Details, Skills, Graph, Buffs, Items). Extract each to separate component class.
- **AppearanceTab**: 2000+ LOC file with ~12 config sections. Split into per-section tab files (Presets, Bars, Colors, StatusBar, Tooltip, Details, Graph, Fonts).
- **MeterTab.Clone()**: Manually copies 40+ properties. Consider implementing a reflection-based or source-generated deep clone.
