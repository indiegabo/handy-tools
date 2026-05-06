# Pooling Guide

This guide explains how to use the Pooling module as a runtime tool, not just
how it is internally structured.

The module is split into three layers:

- `HandyPool<TBehaviour>` is a `ScriptableObject` definition asset.
- `HandyPoolRuntime<TBehaviour>` is one independent runtime owner created from
  that definition.
- `PoolRegistry` is the optional global lookup surface for identified active
  subpools.

Treat the asset as configuration, not as shared mutable runtime state.

## The Short Version

If a human only remembers four things, they should remember these:

- The asset defines what can be pooled.
- The runtime owns the actual live `ObjectPool<TBehaviour>` instances.
- One owner should create one runtime.
- Use `PoolRegistry` only when other systems truly need identifier-based lookup.

## Mental Model

Think about the module in this order:

1. A `HandyPool<TBehaviour>` asset describes one or more possible subpools.
2. A runtime owner calls `CreateRuntime()` or uses `HandyPoolInitializer<T>`.
3. That runtime creates the live subpools and tracks spawned subjects.
4. Subjects return themselves through the `IHandyPool<TBehaviour>` handle they
   received.
5. When the runtime is dismissed, it clears inactive subjects and destroys any
   remaining tracked active subjects that still belong to that runtime.

That means the runtime is the real owner. The asset is only the recipe.

## Core Model

Each `HandyPool<TBehaviour>` definition can describe multiple subpools.

Each subpool is still anchored by a prefab, but it can now also expose an
optional `PoolIdentifier` for named lookup.

That gives you two resolution paths:

- Prefab-keyed access when the caller already owns the prefab reference.
- Identifier-keyed access when the caller should not depend on the prefab.

`HandyPoolRuntime<TBehaviour>` owns the actual `ObjectPool<TBehaviour>`
instances, tracked subjects, registry registrations, and prewarm lifecycle.

## Runtime Ownership

Use one runtime per owner.

- `HandyPoolInitializer<TBehaviour>` creates and owns its own runtime.
- `HandyPool<TBehaviour>.CreateRuntime()` returns a fully independent runtime.
- The asset still exposes a default runtime facade through `Initialize`,
  `Dismiss`, `Get`, `TryGet`, and `RequestPoolCreation`, but that facade is for
  convenience rather than shared multi-owner orchestration.

Do not expect two different scene owners to safely share one mutable runtime
through the same asset instance.

If two systems need independent lifecycle, warmup, or dismissal timing, they
should not share a runtime.

## Subject Contract

Pooled behaviours implement `IPoolSubject<TBehaviour>`.

The subject receives an `IHandyPool<TBehaviour>` handle instead of the concrete
`ObjectPool<TBehaviour>` type. That keeps the subject focused on the operations
it actually needs.

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.PoolingModule
{
  public sealed class ProjectileView
    : MonoBehaviour, IPoolSubject<ProjectileView>
  {
    private IHandyPool<ProjectileView> _pool;

    public void SetPool(IHandyPool<ProjectileView> pool)
    {
      _pool = pool;
    }

    public void OnTakenFromPool()
    {
      gameObject.SetActive(true);
    }

    public void ReleaseToPool()
    {
      _pool.Release(this);
    }

    public void OnReturnedToPool()
    {
      gameObject.SetActive(false);
    }
  }
}
```

The important point is simple: subjects do not need to know who owns the whole
runtime. They only need the handle for the subpool that created them.

## Example 1: Private Prefab-Keyed Pool

This is the simplest and most common usage.

Use it when one gameplay system owns the prefab reference and no other system
needs global access.

```csharp
using IndieGabo.HandyTools.PoolingModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
  public sealed class ProjectileSpawner : MonoBehaviour
  {
    [SerializeField] private ProjectilePool _poolDefinition;
    [SerializeField] private ProjectileView _projectilePrefab;
    [SerializeField] private Transform _poolContainer;

    private HandyPoolRuntime<ProjectileView> _runtime;

    private void OnEnable()
    {
      _runtime = _poolDefinition.CreateRuntime();
      _runtime.Initialize(_poolContainer);
      _runtime.RequestPoolCreation(_projectilePrefab, 8);
    }

    private void OnDisable()
    {
      _runtime?.Dismiss();
    }

    public void Spawn(Vector3 position, Vector3 direction)
    {
      ProjectileView projectile = _runtime.Get(_projectilePrefab);
      projectile.transform.position = position;
      projectile.transform.forward = direction;
    }
  }

  public sealed class ProjectilePool : HandyPool<ProjectileView>
  {
  }
}
```

Why this is the right shape:

- the spawner owns the runtime,
- the spawner controls warmup,
- the spawner can dismiss everything when it is disabled,
- and no registry lookup is needed.

## Example 2: Scene-Owned Runtime Through HandyPoolInitializer

Use `HandyPoolInitializer<TBehaviour>` when one component should own the pool
lifecycle and the setup can stay mostly declarative.

```csharp
using IndieGabo.HandyTools.PoolingModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
  public sealed class EnemyVfxPoolInitializer
    : HandyPoolInitializer<EnemyVfxView>
  {
  }
}
```

In this pattern, the inspector drives the lifecycle:

- assign the pool asset,
- assign the optional container,
- set the initial amount,
- let `OnEnable()` initialize,
- let `OnDisable()` dismiss.

This is the lowest-friction option when a scene object already exists just to
own pooled content.

## Example 3: Identified Pool And Global Lookup

Use identifiers when the caller should not depend on the prefab asset, or when
multiple systems need to resolve the same active subpool.

First, centralize the keys.

```csharp
using IndieGabo.HandyTools.PoolingModule;

namespace IndieGabo.HandyTools.GameplayModule
{
  public static class ProjectilePoolKeys
  {
    public static readonly PoolIdentifier Bullet =
      PoolIdentifier.Create("projectiles/bullet");

    public static readonly PoolIdentifier Rocket =
      PoolIdentifier.Create("projectiles/rocket");
  }
}
```

Then create the identified subpool in the runtime owner.

```csharp
using IndieGabo.HandyTools.PoolingModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
  public sealed class ProjectilePoolHost : MonoBehaviour
  {
    [SerializeField] private ProjectilePool _poolDefinition;
    [SerializeField] private ProjectileView _bulletPrefab;

    private HandyPoolRuntime<ProjectileView> _runtime;

    private void OnEnable()
    {
      _runtime = _poolDefinition.CreateRuntime();
      _runtime.Initialize(transform);
      _runtime.RequestPoolCreation(
        ProjectilePoolKeys.Bullet,
        _bulletPrefab,
        12
      );
    }

    private void OnDisable()
    {
      _runtime?.Dismiss();
    }
  }
}
```

Now a different system can resolve the active pool without holding the prefab.

```csharp
using IndieGabo.HandyTools.PoolingModule;
using UnityEngine;

namespace IndieGabo.HandyTools.GameplayModule
{
  public sealed class WeaponSystem : MonoBehaviour
  {
    public void Fire(Vector3 position, Vector3 direction)
    {
      ProjectileView projectile = PoolRegistry.GetRequired<ProjectileView>(
        ProjectilePoolKeys.Bullet
      );

      projectile.transform.position = position;
      projectile.transform.forward = direction;
    }
  }
}
```

Registry rules:

- One active pool per subject type and identifier.
- Two runtimes cannot hold the same identifier for the same subject type at the
  same time.
- `Dismiss()` unregisters identified pools owned by that runtime.

## When To Use PoolRegistry

Use `PoolRegistry` only when the lookup problem is real.

Good reasons:

- the caller is data-driven,
- the caller should not reference the prefab asset,
- multiple systems must spawn from the same live pool,
- or the pool identity is part of a shared runtime contract.

Bad reasons:

- the prefab is already available locally,
- the pool is private to one owner,
- or the registry only saves one direct serialized field.

If direct reference is clearer, use direct reference.

## What Prewarm Actually Does

Prewarm uses the pool release path directly.

- The runtime creates instances through `Get()`.
- It immediately returns them through the owning pool handle.
- The objects end the process inactive and ready for later reuse.

This means prewarm is not a special hidden state. It is just an early roundtrip
through the same runtime contract used later during real gameplay.

## What Dismiss Actually Does

`Dismiss()` is the hard shutdown for that runtime.

It does three important things:

1. it clears inactive pooled instances,
2. it unregisters any identified pools owned by that runtime,
3. it destroys tracked active subjects that were created by that runtime and
   never returned.

That last point matters. If a subject is still checked out when the runtime is
dismissed, the runtime does not leak it. It destroys it.

## Collection Checks

Collection checks are configuration-driven.

- Entry-level checks stay enabled in the editor and development builds.
- Player builds can opt in through the definition asset when runtime safety is
  worth the extra overhead.

Use checks during feature development. Turn them on in shipped builds only when
you explicitly want the safety net.

## Choosing Between The Main APIs

Use `HandyPoolInitializer<TBehaviour>` when:

- one component owns the lifecycle,
- inspector-driven setup is enough,
- and you want the smallest amount of runtime code.

Use `CreateRuntime()` when:

- the owner is a manager or service,
- lifecycle is not tied to a simple MonoBehaviour wrapper,
- or warmup and teardown need explicit orchestration in code.

Use prefab-keyed `Get(prefab)` when:

- the caller already owns the prefab,
- the pool is private,
- and there is no reason to expose an identifier.

Use identifier-keyed `Get(identifier)` or `PoolRegistry` when:

- the caller should not know the prefab,
- the identity is part of a module contract,
- or several systems need the same live subpool.

## Common Mistakes

These are the mistakes most likely to confuse a human reader or future maintainer.

### Mistake 1: Treating The Asset As The Live Pool

Wrong mental model:

- one asset equals one runtime.

Correct mental model:

- one asset can spawn many independent runtimes.

### Mistake 2: Sharing One Runtime Between Unrelated Owners

If two systems want different initialization timing, different dismissal timing,
or different ownership boundaries, they should not share one runtime.

### Mistake 3: Using PoolRegistry For Everything

The registry is useful, but it is not the default answer.

If a system already owns the prefab reference, adding identifiers and registry
lookup usually makes the flow harder to read, not easier.

### Mistake 4: Forgetting That Dismiss Destroys Checked-Out Subjects

Do not dismiss a runtime while another system still assumes its active subjects
will survive.

If a subject must outlive the pool owner, then that subject should not be owned
by that runtime in the first place.

## Recommended Usage Patterns

Prefer prefab-keyed access when:

- the caller already owns the prefab reference,
- the pool is private to one system,
- and you do not need cross-system lookup.

Prefer identifier-keyed access when:

- the caller should not depend on the prefab asset,
- the spawn decision is data-driven,
- or multiple systems need to resolve the same active subpool.

Prefer `HandyPoolInitializer<TBehaviour>` when one scene object should own the
pool lifecycle.

Prefer `CreateRuntime()` when the owning system is not a simple component
lifecycle wrapper.
