# Stats Domain

`Brain.Stats` resolves authored stats builds for one brain branch and supports
runtime replacement of the active build per stats type.

This is the surface that keeps state tuning explicit without forcing every state
to drag its own provider component around the hierarchy.

## Quick Mental Model

The stats domain works in two layers:

1. the branch default comes from `FSMStatsRegistry`
2. runtime overrides can replace the active asset for a given type

The active stats asset for a type is therefore:

- the override, if one exists
- otherwise the registry default
- otherwise nothing

This lets the branch behave like a configurable build of gameplay stats without
requiring value-by-value mutation.

## What The Stats Domain Owns

`Brain.Stats` owns:

- `Get<T>()`, `Get(Type)`
- `TryGet<T>(out T stats)`, `TryGet(Type, out FSMStatsAsset stats)`
- `SetOverride<T>(...)`, `SetOverride(Type, ...)`, `SetOverride(FSMStatsAsset)`
- `HasOverride<T>()`, `HasOverride(Type)`
- `ClearOverride<T>()`, `ClearOverride(Type)`
- `ClearAllOverrides()`
- `StatsChanged`

## Branch Registry Model

The domain resolves the first `FSMStatsRegistry` found under the brain branch.

That registry stores one default asset per concrete `FSMStatsAsset` type.

If the branch contains more than one registry, the domain warns and uses the
first one in the hierarchy. That warning means your composition is ambiguous and
needs cleanup.

## Why Whole-Build Overrides Matter

The intended design is to swap an entire stats asset reference.

That gives you stable authored builds such as:

- normal locomotion
- heavy armor locomotion
- underwater locomotion
- berserk dash tuning

Do not design your runtime around mutating every numeric field one by one in the
shared asset from inside state logic. That is how tuning turns into sludge.

## Example: Resolve Stats In A State

```csharp
private DashStats DashStats => Brain.Stats.Get<DashStats>();

private void OnInit()
{
    if (DashStats == null)
    {
        ThrowStateFailure(
            "DashState requires DashStats to be resolved by Brain.Stats.");
    }
}
```

This is the preferred state-side pattern: resolve the stats build through the
brain, validate it once, and then use it as authored configuration.

## Example: Runtime Override For A Power-Up

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class DashPowerUpInstaller : MonoBehaviour
    {
        [SerializeField]
        private FSMBrain _brain;

        [SerializeField]
        private DashStats _poweredDashStats;

        public void Apply()
        {
            if (_brain == null || _poweredDashStats == null)
            {
                return;
            }

            DashStats runtimeCopy = ScriptableObject.Instantiate(_poweredDashStats);
            _brain.Stats.SetOverride(runtimeCopy);
        }

        public void Remove()
        {
            _brain?.Stats.ClearOverride<DashStats>();
        }
    }
}
```

This pattern is clear because the branch either uses the powered dash build or
the default dash build. There is no mystery about which values are live.

## Example: Listen For Stats Changes

```csharp
using System;
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class StatsChangeLogger : MonoBehaviour
    {
        [SerializeField]
        private FSMBrain _brain;

        private void OnEnable()
        {
            if (_brain != null)
            {
                _brain.Stats.StatsChanged += OnStatsChanged;
            }
        }

        private void OnDisable()
        {
            if (_brain != null)
            {
                _brain.Stats.StatsChanged -= OnStatsChanged;
            }
        }

        private void OnStatsChanged(Type statsType, FSMStatsAsset activeStats)
        {
            Debug.Log(statsType.Name + " -> " + (activeStats != null
                ? activeStats.name
                : "null"));
        }
    }
}
```

This is useful when other branch systems need to react to a build swap.

## Registry Authoring Rules

- keep one `FSMStatsRegistry` per branch whenever possible
- keep one default asset per concrete stats type inside the registry
- use assets that derive from `FSMStatsAsset`
- keep the registry on the same branch as the owning `FSMBrain`

The sample `FSM CCPro Starter Kit` follows this pattern by placing the registry
on the `FSM` object and pointing it at the sample stats assets under
`Samples/FSM CCPro Starter Kit/Objects`.

## Guidance For Humans

- think of stats assets as authored builds, not as scratch pads
- validate missing required stats early in `OnInit`
- use overrides for branch-wide mode changes, power-ups, or loadout swaps
- reset overrides explicitly when the temporary mode ends

## Guidance For AI Agents

- when a state needs tuning, first check whether a matching `FSMStatsAsset`
  already exists before introducing new provider components
- prefer `SetOverride(...)` to whole-asset replacement instead of suggesting
  value-by-value mutation APIs that the module does not expose
- note the branch rule: `Brain.Stats` resolves the first registry found in
  children. If behavior looks inconsistent, check for duplicate registries
- if the task mentions "build", "loadout", "mode", or "power-up tuning", the
  stats domain is usually the correct starting point

## Common Mistakes

### Mutating shared asset values directly inside a state

That turns authored tuning into hidden global state.

### Forgetting to clear temporary overrides

If the powered build should be temporary, remove the override explicitly.

### Treating the registry like a random object bucket

The registry is keyed by concrete stats type. Keep it clean and one-per-type.
