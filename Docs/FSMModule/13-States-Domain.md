# States Domain

`Brain.States` is the public loader and registry for the states owned by one
brain branch.

If `Machine` is the authority that changes control, `States` is the authority
that answers a simpler question first: "what states are available to control?"

## Quick Mental Model

The states domain owns two things:

- loading states into the branch runtime
- retrieving loaded states in a stable way

It is the branch-local catalog, not a global state database.

## What The States Domain Owns

`Brain.States` owns:

- `LoadStatesFromBaseType(...)`
- `LoadStatesFromScriptablesList(...)`
- `LoadState(Type)`
- `LoadState(IState)`
- `InitializeAllStates()`
- `IsLoaded(...)`
- `Get(...)` and `TryGet(...)` by type, key, or generic argument
- `GetAllStates()`

## What It Does Not Own

`Brain.States` does not own:

- transition decisions
- machine lifecycle
- state history or transition reasons
- editor asset creation
- state serialization policy outside the current branch

## Loading Strategies

### Strategy 1: Scriptable states from the inspector

This is the most common authored workflow.

The brain serializes a list of `ScriptableState` assets. During `Awake`, it
loads them into the provider and initializes them as part of the branch startup.

This is ideal when designers or technical designers own the state assets.

### Strategy 2: Runtime states from a shared base type

Use `LoadStatesFromBaseType(...)` when your project uses class-based runtime
states discovered by inheritance.

```csharp
Brain.States.LoadStatesFromBaseType(typeof(PlayerState));
```

This is typical when a generic brain or a feature-specific base state owns the
main runtime family.

### Strategy 3: Programmatic loading of one state

Use `LoadState(Type)` or `LoadState(IState)` when a composition step wants to
register one extra runtime state explicitly.

```csharp
Brain.States.LoadState(typeof(PauseMenuState));
Brain.States.LoadState(new DebugOnlyFallbackState());
```

Use this sparingly. If every state is being loaded ad hoc from scattered code,
your orchestration is already decaying.

## Initialization Timing

The loading methods that accept `initializeAfterCommit` let you choose whether
newly committed states should initialize immediately.

That matters when you want to batch several state loads and initialize them in a
single later step.

```csharp
Brain.States.LoadStatesFromBaseType(typeof(PlayerState), false);
Brain.States.LoadStatesFromScriptablesList(extraStates, false);
Brain.States.InitializeAllStates();
```

For most projects, the default `true` is the correct choice. Reach for `false`
only when you explicitly control initialization timing.

## Retrieval Patterns

### By generic type

```csharp
IdleState idleState = Brain.States.Get<IdleState>();
```

Use this when you know the state type at compile time and want the cleanest call
site.

### By runtime type

```csharp
IState state = Brain.States.Get(typeof(IdleState));
```

Use this when the state type is being chosen dynamically.

### By key

```csharp
IState state = Brain.States.Get("combat.attack");
```

Use this when authored state keys are already part of your branch contract.

### Safe retrieval

```csharp
if (!Brain.States.TryGet("combat.attack", out IState state))
{
    Brain.Machine.FailState(null, "combat.attack is not loaded.");
    return;
}
```

Use `TryGet` when missing states are a normal runtime possibility and should not
explode into error spam.

## Example: Resolve dependencies in `OnInit`

```csharp
private IdleState _idleState;
private AttackState _attackState;

private void OnInit()
{
    _idleState = Brain.States.Get<IdleState>();
    _attackState = Brain.States.Get<AttackState>();

    if (_idleState == null || _attackState == null)
    {
        ThrowStateFailure("Combat flow requires IdleState and AttackState.");
    }
}
```

This is the normal pattern. Resolve the states you depend on once, early, and
fail loudly if the branch contract is broken.

## Example: Enumerate the loaded catalog

```csharp
List<IState> loadedStates = Brain.States.GetAllStates();

for (int index = 0; index < loadedStates.Count; index++)
{
    Debug.Log(loadedStates[index].GetType().Name);
}
```

`GetAllStates()` returns a copy, which is appropriate for debugging, tooling,
and inspection.

## Guidance For Humans

- load states at one clear composition boundary instead of from random gameplay code
- resolve other states once in `OnInit` when possible
- use typed `Get<T>()` when the dependency is static and intentional
- treat missing critical states as a branch contract failure, not as something
  to quietly ignore

## Guidance For AI Agents

- before suggesting `Brain.Machine.RequestStateChange(...)`, ensure the target
  state is known to `Brain.States`
- if the task involves adding a new runtime state family, inspect the current
  loading strategy first: inspector scriptables, base-type discovery, or manual load
- note the API nuance: `TryGet<T>(out IState state)` returns `IState`, not `T`
  directly. Use `Get<T>()` when a typed result matters more than boolean safety
- do not propose loading the same state every frame or on every transition check

## Common Mistakes

### Assuming authored assets are the active runtime instances

`ScriptableState` assets are loaded into the provider and used through the
runtime branch. Write your gameplay code against the loaded state instance, not
against a naked asset reference you happened to drag somewhere else.

### Hiding loading strategy across many files

If nobody can answer where the states are loaded from, debugging the machine is
already slower than it needs to be.

### Treating keys as optional decoration

If your workflow depends on string keys, document them and keep them stable.
Magic strings with no contract rot fast.
