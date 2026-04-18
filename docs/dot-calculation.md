# DoT Calculation Methods

Damage Terror provides two DoT (Damage over Time) calculation modes that control how aggregate tick damage from ACT network log lines is attributed to individual DoT sources. The mode is selected in **Settings → General → DoT calculation**.

FFXIV's combat log does not report per-DoT tick damage. Instead, log line Type 24 (DoTHoT) reports the **total** tick damage dealt to a target across all active DoTs in a single aggregate value. This creates an attribution problem: if a target has three DoTs from different sources, the game only tells you the combined tick. The DoT calculation mode determines how that combined tick is split.

---

## Mode Overview

| Mode | Config Value | Description |
|------|:---:|-------------|
| **DamageTerror Refined** | `Refined = 0` | Potency-weighted distribution with per-tick low-byte snapping, dynamic crit/DH estimation, and per-application crit rate refinement. Default mode. |
| **IINACT / ACT (Trust Parser)** | `Iinact = 1` | Trusts the parser's own DoT attribution with no additional refinement. |
| ~~Plugin~~ | ~~`Plugin = 2`~~ | Deprecated. Auto-migrated to Refined on config load. See [Deprecated: Plugin Mode](#deprecated-plugin-mode). |

---

## IINACT / ACT (Trust Parser)

The simplest mode. Damage Terror defers to the parser (IINACT or ACT) for all DoT attribution.

### What happens

1. **No low-byte extraction**: The 0x0E/0x0F effect scan in Type 21/22 lines is skipped entirely (gated by `config.DotCalcMode != DotCalcMode.Iinact`). No `DamageLowByte` or `CritLowByte` data is captured on status applications.
2. **No coefficient calibration**: `CalibrateFromDotHit()` is never called. No per-potency damage coefficient is computed.
3. **No per-application crit rate**: Since no `CritLowByte` data exists, per-tick crit rate refinement is unavailable.
4. **No SnapToLowByte**: Without low-byte data, the integer-LSB snapping step is skipped.
5. **Status tracking still runs**: `StatusTracker` still classifies statuses and tracks active DoTs/HoTs on targets. This data is used for the Buffs/Debuffs tab and status display.
6. **Type 24 distribution**: Aggregate tick lines still pass through `ProcessDoTHoTLine()`. Potency weights are still computed from the potency table, but without calibration data or low-byte refinement, the distribution relies solely on raw potency ratios. The named source from the log line receives primary attribution.

### When to use

- When you trust your parser's DoT simulation and want exact parity with ACT/IINACT numbers.
- When debugging discrepancies between Damage Terror and your parser.
- When running parsers that already perform their own sophisticated DoT attribution.

### Limitations

- ACT's DoT simulation is known to misattribute ticks when multiple DoTs from different sources overlap on the same target.
- No per-tick accuracy improvement from game-data low bytes.
- Ground-effect DoTs (Salted Earth, Doton, Honing Dance) may lump together or misattribute entirely.

---

## DamageTerror Refined

The default mode. A four-stage pipeline that builds per-combatant damage profiles, extracts hidden game data from status application effects, calibrates per-potency coefficients from initial hits, and distributes aggregate ticks using potency-weighted shares refined by low-byte snapping and per-application crit rates.

### Stage A — Per-Combatant Stat Accumulation

**Entry point**: `SkillTracker.AccumulateCombatantStats()`, called for every non-auto-attack Type 21/22 damage hit.

Every damage event feeds a `CombatantDotStats` struct per source combatant. The goal is to build a statistical profile of each player's damage output so that DoT tick estimates can be accurately scaled.

#### Crit/DH Stripping

Raw damage values include crit and direct hit multipliers. To get a base damage estimate, these are analytically stripped:

```
if (isCrit)   baseDmg /= critMulti
if (isDH)     baseDmg /= 1.25
```

The crit multiplier is resolved in preference order:
1. **Dynamic crit multiplier** — computed from the ratio of average crit damage to average non-crit damage for that combatant. Requires ≥5 non-crit hits and ≥3 crit hits. Validated to the range [1.3, 1.8]; values outside this range fall back to the default.
2. **Default** — `DefaultCritMulti = 1.65`, a reasonable mid-range estimate.

#### Outlier Filter

Once a combatant has accumulated ≥ `DotMinHitsForOutlierFilter` (10) hits, incoming hits are checked against the running average base damage:

- **Low threshold**: 30% of running average (`DotOutlierLowThreshold = 0.3`)
- **High threshold**: 300% of running average (`DotOutlierHighThreshold = 3.0`)

Hits outside this range are discarded to prevent statistical pollution from limit breaks, overkill, or other anomalous values.

#### Tracked Statistics

| Field | Description |
|-------|-------------|
| `TotalBaseDamage` | Sum of crit/DH-stripped base damage |
| `TotalHits` | Total qualifying hit count |
| `CritHits` | Hits that were critical |
| `DHHits` | Hits that were direct hits |
| `NonCritDHStripped` | DH-stripped damage pool for non-crit hits only |
| `NonCritCount` | Non-crit sample count |
| `CritDHStripped` | DH-stripped damage pool for crit hits only |
| `CritCountForMulti` | Crit sample count for dynamic multiplier |

Derived properties:
- `AverageBaseDmgPerHit` = `TotalBaseDamage / TotalHits`
- `CritRate` = `CritHits / TotalHits`
- `DHRate` = `DHHits / TotalHits`
- `DynamicCritMulti` = `(CritDHStripped / CritCountForMulti) / (NonCritDHStripped / NonCritCount)` (when sufficient data)
- `HasData` = `TotalHits >= 3`

### Stage B — Coefficient Calibration

**Entry point**: `SkillTracker.CalibrateFromDotHit()`, called when a Type 21/22 line both deals damage AND applies a known DoT status.

When an ability deals damage and simultaneously applies a DoT (detected via 0x0E/0x0F effect scan in the same action packet), and that DoT has a catalogued initial hit potency > 0 in the potency table, the calibration system computes:

```
coefficient = baseDmg / initialHitPotency
```

Where `baseDmg` is the crit/DH-stripped base damage from Stage A. This coefficient represents the per-potency damage value for that combatant given their current gear, buffs, and stats.

The coefficient is stored as a running average in `CombatantDotStats.CalibrationSum / CalibrationCount`. A single calibration sample (`CalibrationCount >= 1`) is sufficient to activate calibrated mode for that combatant.

#### Why This Works

FFXIV's damage formula is `Damage = Potency × f(stats, buffs, etc.)`. The unknown factor `f(...)` is constant across all potency-based calculations for a given player at a given moment. By observing the initial hit of a DoT application (where both the damage and the potency are known), the coefficient can be extracted and applied to estimate tick damage for any DoT at any potency.

#### Coefficient Resolution Order

When computing tick weights in Stage D, the coefficient for a given source is resolved:
1. **Calibrated coefficient** — from that combatant's own `CalibratedCoeff` if `HasCalibration` is true.
2. **Shared fallback** — if the source has no calibration data, borrows the coefficient from another calibrated combatant in the encounter (imperfect but better than nothing).
3. **Potency-only fallback** — if no calibration data exists anywhere, uses raw potency values as weights (equivalent to assuming all combatants have identical stats).

### Stage C — Low-Byte Extraction

**Entry point**: 0x0E/0x0F effect scan within `SkillTracker.ProcessLogLine()` for Type 21/22 lines.

When an ability applies a status effect (buff or debuff), the ACT network log line contains hidden data in the effect's flag and value fields that encode information about the expected tick damage.

#### What Gets Extracted

From each 0x0E (status on target) or 0x0F (status on caster) effect entry:

| Field | Extraction | Meaning |
|-------|-----------|---------|
| `appliedStatusId` | `(valueField >> 16) & 0xFFFF` | The status ID being applied |
| `damageLowByte` | `(flags >> 8) & 0xFF` | Least significant byte of the expected non-crit tick damage |
| `critLowByte` | `(flags >> 16) & 0xFF` | Per-application crit rate × 1000 (e.g., 200 = 20.0%). Overflows at 25.6% |

#### Data Flow

1. Extracted values are stored in `pendingLowBytes[(source, target, statusId)]`.
2. When the matching Type 26 (GainsEffect) log line arrives, the pending data is consumed and attached to the `ActiveStatus` record via `StatusTracker.OnStatusGained()`.
3. The `ActiveStatus` struct carries `DamageLowByte`, `CritLowByte`, and `HasLowByteData` for the lifetime of the status.
4. When the status is used in Stage D's weight calculation, this data enables SnapToLowByte and per-application crit rate refinement.

#### Why Low Bytes Matter

The low byte of the expected tick damage is deterministic — it comes directly from the server's damage formula for that specific status application with the player's exact stats and buffs at application time. Even though we only get one byte (0–255), it constrains the estimated tick value to one of at most three candidates, dramatically reducing estimation error.

The per-application crit rate captures the player's crit stat at application time, which may differ from their running average if buffs have changed. This allows per-tick crit scaling that tracks buff windows accurately.

### Stage D — Tick Distribution

**Entry point**: `SkillTracker.ProcessDoTHoTLine()`, called for every Type 24 (DoTHoT) log line.

This is where aggregate tick damage is split across individual DoT sources.

#### 1. Source Collection

For the tick's target:
- Queries `StatusTracker.GetActiveStatuses(target)` for all currently active DoT/HoT statuses.
- Queries `StatusTracker.GetRecentlyRemovedDoTs(target)` for statuses that fell off within the last 6 seconds (`RecentlyRemovedGraceSec = 6f`). This grace window covers late-arriving ticks for statuses that expired between the server tick and the log line.

#### 2. Ground-Effect Branching

Type 24 lines include an `effectId` field:
- **Non-zero effectId**: This is a ground-effect tick (Salted Earth, Doton, Honing Dance). Attribution goes only to the source's active ground-effect skill, queried via `StatusTracker.GetActiveGroundEffectDots()`. Ground effects have a separate pending registration system (`PendingGroundEffectTimeoutSec = 5f`) for the gap between skill use and status appearance.
- **Zero effectId**: This is a standard aggregate tick across all non-ground DoTs/HoTs on the target.

#### 3. Fallback

If no active or recently-removed statuses are found (common when the plugin starts mid-fight), the total tick amount is attributed to the named source from the log line with a generic "DoT" or "HoT" label.

#### 4. Weight Calculation

**Entry point**: `SkillTracker.CalculateTickWeight()`, called per source-skill per tick.

For each active DoT/HoT on the target, a weight is computed:

```
baseWeight = coefficient × tickPotency
```

Where:
- `coefficient` is resolved per Stage B's preference order.
- `tickPotency` comes from the potency table (`DotPotencyTable.GetTickPotency(statusId)`), defaulting to 50 for unknown statuses.

**Low-byte snapping** (Refined only): If the status has `HasLowByteData`, the base weight is passed through `SnapToLowByte()` to constrain it to an integer whose least significant byte matches the stored `DamageLowByte`. See [SnapToLowByte Algorithm](#snaptolobyte-algorithm).

**Per-application crit rate** (Refined only): If `CritLowByte > 0` and the decoded rate is ≤ 25.5% (overflow boundary), the per-application crit rate replaces the running average crit rate. Otherwise falls back to the combatant's `CritRate` from Stage A.

**Crit and DH scaling**:
```
critFactor = 1.0 + critRate × (critMulti - 1.0)
dhFactor   = 1.0 + dhRate × 0.25
weight     = baseWeight × critFactor × dhFactor
```

DH factor is excluded for HoTs (HoTs cannot direct hit in FFXIV).

#### 5. Proportional Distribution

With weights computed for all sources, the aggregate tick is split:

```
share[i] = totalAmount × (weight[i] / totalWeight)
```

The last source receives the remainder (`totalAmount - sum(share[0..n-2])`) to avoid rounding loss.

#### 6. Accumulation

Each share is recorded in:
- `damageData` / `healData` — contributes to overall DPS/HPS totals.
- `dotTickData` / `hotTickData` — feeds the per-skill breakdown with DoT sub-entries.
- Skill events — creates `SkillUseEvent` records with `IsDoTTick = true` / `IsHoTTick = true`.
- `GraphDataTracker` — per-source shares feed the real-time graph with skill markers.

---

## SnapToLowByte Algorithm

When the game applies a DoT/HoT status, the least significant byte (bits 0–7) of the expected non-crit tick damage is embedded in the status application effect. `SnapToLowByte()` uses this to constrain the estimated tick value.

### Algorithm

```
Input:  estimated (double) — the potency-weighted estimated tick
        lowByte   (byte)   — the LSB from the status application effect

1. estInt  = (int)estimated
2. baseVal = estInt & ~0xFF            // Clear bottom 8 bits
3. For offset in {-256, 0, +256}:
     candidate = (baseVal + offset) | lowByte
     if candidate > 0:
       record candidate if |candidate - estimated| < current best
4. Return closest candidate
```

### Why Three Candidates

The estimated tick and the true tick may differ by more than 255 (one byte's range). By testing `base - 256`, `base`, and `base + 256`, the algorithm covers the case where the estimate is off by up to one full byte-width in either direction. The closest candidate whose LSB matches the game's low byte is selected.

### Example

Suppose the estimated tick is 842.3 and `lowByte = 0x4B` (75 decimal):
- `baseVal = 842 & ~0xFF = 768`
- Candidate 1: `(768 - 256) | 75 = 512 + 75 = 587` → distance 255.3
- Candidate 2: `768 | 75 = 843` → distance 0.7
- Candidate 3: `(768 + 256) | 75 = 1024 + 75 = 1099` → distance 256.7
- **Winner**: 843 (distance 0.7)

The estimate was 842.3; the true tick was 843. Without snapping, it would have been rounded to 842.

---

## Potency Table

All known DoT/HoT status IDs, their per-tick potency, and initial hit potency (used for coefficient calibration). Sourced from per-job definition files in `DamageTerror/Jobs/`.

Default fallback for unknown statuses: `DefaultPotency = 50`.

### Damage over Time (DoTs)

| Job | Status ID | Status Name | Tick Potency | Initial Hit Potency | Ground Effect |
|-----|:---------:|-------------|:------------:|:-------------------:|:---:|
| BRD | 124 | Venomous Bite | 15 | — | |
| BRD | 129 | Windbite | 20 | — | |
| BRD | 1200 | Caustic Bite | 20 | 150 | |
| BRD | 1201 | Stormbite | 25 | 100 | |
| BLM | 163 | Thunder III | 35 | 120 | |
| BLM | 1210 | Thunder IV | 30 | 80 | |
| BLM | 3871 | High Thunder | 30 | 150 | |
| BLM | 3872 | High Thunder II | 30 | 80 | |
| WHM | 143 | Aero | 50 | — | |
| WHM | 144 | Aero II | 50 | — | |
| WHM | 798 | Aero III | 50 | — | |
| WHM | 1871 | Dia | 65 | 65 | |
| SCH | 189 | Bio II | 20 | — | |
| SCH | 1895 | Biolysis | 75 | 75 | |
| SCH | 3883 | Baneful Impaction | 50 | — | |
| AST | 838 | Combust | 40 | — | |
| AST | 843 | Combust II | 50 | — | |
| AST | 1881 | Combust III | 55 | — | |
| SGE | 2614 | Eukrasian Dosis | 40 | — | |
| SGE | 2615 | Eukrasian Dosis II | 60 | — | |
| SGE | 2616 | Eukrasian Dosis III | 75 | — | |
| SGE | 3897 | Eukrasian Dyskrasia | 40 | — | |
| DRG | 118 | Chaos Thrust | 40 | 100 | |
| DRG | 2719 | Chaotic Spring | 45 | 300 | |
| SAM | 1228 | Higanbana | 45 | 200 | |
| VPR | 3667 | Noxious Gnash | 35 | 200 | |
| GNB | 1837 | Sonic Break | 60 | 300 | |
| GNB | 1838 | Bow Shock | 60 | 150 | |
| PLD | 248 | Circle of Scorn | 30 | 120 | |
| MCH | 1866 | Bioblaster | 50 | 50 | |
| DRK | 749 | Salted Earth | 50 | — | Yes |
| NIN | 501 | Doton | 50 | — | Yes |
| SMN | 2706 | Slipstream | 30 | — | |
| BLU | 18 | Poison | 30 | — | |
| BLU | 1714 | Bleeding | 50 | — | |
| BLU | 1723 | Windburn | 20 | — | |
| BLU | 1736 | Dropsy | 50 | — | |
| BLU | 3643 | Mortal Flame | 50 | — | |
| BLU | 3712 | Breath of Magic | 80 | — | |

### PvP DoTs

| Job | Status ID | Status Name | Tick Potency | Ground Effect |
|-----|:---------:|-------------|:------------:|:---:|
| SCH | 2039 | Biolysis | 50 | |
| SGE | 3976 | Eukrasian Dosis III | 50 | |
| MCH | 2019 | Bioblaster | 65 | |
| DRK | 3036 | Salted Earth | 80 | Yes |
| NIN | 3184 | Goka Mekkyaku | 80 | |
| NIN | 4304 | Doton | 50 | Yes |
| DNC | 3162 | Honing Dance | 75 | Yes |
| SMN | 3231 | Scarlet Flame | 65 | |
| RDM | 4319 | Scorch | 65 | |

---

## Key Constants

All named constants referenced in the DoT calculation pipeline.

### SkillTracker Constants

| Constant | Value | Purpose |
|----------|:-----:|---------|
| `DefaultCritMulti` | 1.65 | Fallback crit damage multiplier when insufficient data for dynamic estimation |
| `CritFlag` | 0x20 | Bit flag in damage flags indicating a critical hit |
| `DirectHitFlag` | 0x40 | Bit flag in damage flags indicating a direct hit |
| `DotOutlierLowThreshold` | 0.3 | Hits below 30% of running average base damage are discarded |
| `DotOutlierHighThreshold` | 3.0 | Hits above 300% of running average base damage are discarded |
| `DotMinHitsForOutlierFilter` | 10 | Minimum accumulated hits before outlier filtering activates |

### StatusTracker Constants

| Constant | Value | Purpose |
|----------|:-----:|---------|
| `RecentlyRemovedGraceSec` | 6.0 | Seconds after a DoT/HoT expires that late-arriving ticks are still attributed (~2 server ticks) |
| `PendingGroundEffectTimeoutSec` | 5.0 | Window after a ground-effect skill use before the corresponding status gain is expected |

### DotPotencyTable Constants

| Constant | Value | Purpose |
|----------|:-----:|---------|
| `DefaultPotency` | 50 | Fallback tick potency for statuses not in the potency table |

---

## Data Flow

```
Type 21/22 (Ability Damage)
  │
  ├── AccumulateCombatantStats()
  │     Records base damage, crit/DH rates per combatant.
  │     Strips crit/DH multipliers. Applies outlier filter.
  │
  ├── [Refined] Scan 0x0E/0x0F Effects
  │     Extracts appliedStatusId, damageLowByte, critLowByte.
  │     Stores in pendingLowBytes[(source, target, statusId)].
  │
  └── [Refined] CalibrateFromDotHit()
        If action applies a known DoT with initial hit potency > 0:
        coefficient = baseDmg / initialHitPotency → running average.

          ↓

Type 26 (GainsEffect)
  │
  ├── Consume pendingLowBytes → attach to ActiveStatus
  │     ActiveStatus.DamageLowByte, CritLowByte, HasLowByteData
  │
  └── StatusTracker.OnStatusGained()
        Classifies DoT/HoT. Tracks active statuses per target.
        Registers ground effects.

          ↓

Type 24 (DoTHoT Aggregate Tick)
  │
  ├── Collect Sources
  │     StatusTracker.GetActiveStatuses(target) → active DoTs/HoTs
  │     StatusTracker.GetRecentlyRemovedDoTs(target) → grace period buffer
  │     StatusTracker.GetActiveGroundEffectDots(source) → ground DoTs
  │
  ├── CalculateTickWeight()  (per source-skill)
  │     baseWeight = coefficient × tickPotency
  │     [Refined] SnapToLowByte(baseWeight, damageLowByte)
  │     [Refined] Per-app crit rate from critLowByte (if ≤ 25.5%)
  │     × critFactor × dhFactor
  │
  ├── Proportional Distribution
  │     share[i] = totalAmount × (weight[i] / totalWeight)
  │     Last source gets remainder (avoids rounding loss)
  │
  └── Accumulate
        → damageData / healData      (DPS/HPS totals)
        → dotTickData / hotTickData   (skill breakdown sub-entries)
        → SkillUseEvent              (IsDoTTick / IsHoTTick)
        → GraphDataTracker           (skill markers on graph)

          ↓

Type 30 (LosesEffect)
  │
  └── StatusTracker.OnStatusLost()
        Moves DoT/HoT to recentlyRemovedDots (6s grace period).
        Ground-effect statuses also enter grace buffer.
```

---

## Deprecated: Plugin Mode

`DotCalcMode.Plugin = 2` was the original plugin-side DoT calculation mode before Refined was developed. It has been replaced entirely and is marked `[Obsolete]` in the enum.

On config load, the migration in `DamageTerrorPlugin` auto-converts any saved `Plugin` value to `Refined` and persists the change:

```csharp
if (cfg.DotCalcMode == DotCalcMode.Plugin)
{
    cfg.DotCalcMode = DotCalcMode.Refined;
    this.PluginInterface.SavePluginConfig(cfg);
}
```

The `Plugin` enum value is retained solely for backward-compatible JSON deserialization of old config files. It is excluded from the config UI (the combo only shows indices 0 and 1).
