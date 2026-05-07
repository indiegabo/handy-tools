# Best Practices and FAQ

This is the point where documentation stops being catechism and becomes engineering discipline.

## General Best Practices

### 1. Choose a dedicated base type for runtime states

If you use `GenericHandyFSMBrain<TBaseState>`, make `TBaseState` a base specific to your feature.

Good:

```csharp
public abstract class EnemyState : State
{
}
```

Bad:

```csharp
public abstract class EnemyBrain : GenericHandyFSMBrain<State>
{
}
```

## 2. Add transitions in `OnInit()` and sort them at the end

If priority matters, call `SortTransitions()` at the end of `OnInit()`.

## 3. Keep transition conditions cheap

Avoid doing this inside a condition:

- `FindObjectOfType`
- alocacao desnecessaria
- leitura cara repetida
- efeitos colaterais

## 4. Use `CompleteState` for normal completion and `FailState` for the error path

Do not mix the semantics. It helps the history view and it helps future humans.

## 5. Use `ThrowStateFailure` when state execution cannot continue

This speaks directly to the brain's recovery strategy.

## 6. Treat the blackboard as the canonical source of shared orchestration data

If multiple states depend on the same orchestration value, the blackboard is usually the most coherent place for that value to live.

## 7. In scriptable states, remember that the asset is cloned at runtime

Do not treat the asset in the Project window as if it were the instance running in the current frame.

## 8. Register temporary callbacks in `OnEnter` and remove them in `OnExit`

Especially with triggers. Otherwise the state turns into a haunted callback hub.

## 9. Use `CanEnter` for target-side restrictions

Do not overload the source condition with validations that belong to the target state itself.

## 10. Use the visualizer early

Do not wait until the FSM becomes a labyrinth before you start inspecting the active path and the transition history.

## FAQ

### What is the practical difference between `State` and `ScriptableState`?

`State` is a pure runtime class. `ScriptableState` is an asset authored in the project that gets cloned at runtime.

### Can I mix `State` and `ScriptableState` in the same machine?

Technically the core can handle both as `IState`, but the modeling is much clearer when a machine has one dominant workflow. Mixing them without a good reason usually creates loading and configuration confusion.

### How do I choose between a plain brain and a generic brain?

If the main workflow is based on `ScriptableState`, a plain brain is usually enough. If the main workflow is based on class-derived states discovered through inheritance, use `GenericHandyFSMBrain`.

### Can I resolve a state by string?

Yes. This works better with `ScriptableState`, because it has a serialized `_key`. In `State`, `Key` is empty by default, so you need to override it if you want string-based lookup.

Example:

```csharp
public override string Key => "enemy.chase";
```

### What happens if a state fails during initialization?

The brain intercepts `StateFailureException`, marks the state as faulted for the current session, and avoids using it as a normal candidate while the machine remains alive.

### What happens if the default state fails?

It may stop being eligible as a fallback, and the brain will try to recover using the first state that entered successfully, if one exists.

### Is the visualizer history kept between play sessions?

The window tracker clears the last session when play mode starts and stores what happened during that cycle. The goal is to help debug the current run, not to accumulate infinite archaeology.

### What about the old CCPro integration?

It is no longer a supported part of HandyFSM. If your project still depends on
that behavior, treat it as legacy project code and maintain it outside the
module.

### What should I do if the FSM becomes unpredictable?

Read these in order:

1. [03 - FSMBrain and Machine Flow](03-FSMBrain-and-Machine-Flow.md)
2. [05 - Transitions and Flow Control](05-Transitions-and-Flow-Control.md)
3. [09 - Debug, History, and Visualizer](09-Debug-History-and-Visualizer.md)

### What is the single most important authoring rule?

If the state needs to explain too much about what it is doing, it is probably doing too much.
