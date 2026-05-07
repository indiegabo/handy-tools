# Transitions and Flow Control

If states are the body, transitions are the nervous system. This document explains how a state change actually happens in HandyFSM.

## How Evaluation Works

In the standard runtime flow, the brain calls `EvaluateTransition()` in the enabled loop.

The current state then walks its `_transitions` list in order.

For each item:

1. execute `ConditionMet()`
2. if the condition is false, continue
3. if the condition is true, fetch `TargetState`
4. call `target.CanEnter(this)`
5. if the target accepts, that transition wins and the search stops

In other words: the first valid transition wins.

## Priority

Every `StateTransition` has an integer `Priority`.

The higher the priority, the earlier that transition should be evaluated.

But there is an important nuance: if you add transitions in `OnInit`, you need to call `SortTransitions()` afterward. Otherwise the final order will remain insertion order.

## Example with Priorities

```csharp
private void OnInit()
{
    AddTransition(() => IsDead(), _deadState, 1000);
    AddTransition(() => IsStunned(), _stunnedState, 500);
    AddTransition(() => WantsToRun(), _runState, 100);
    AddTransition(() => true, _idleState, 0);

    SortTransitions();
}
```

In that case, evaluation will effectively be:

1. morte
2. stun
3. corrida
4. idle como fallback

## Practical Types of State Changes

### 1. Condition-Based Transition

This is the most common one.

It comes from the transition list of the current state and generates `StateTransitionReason.ConditionTransition`.

### 2. External State Request

Use this when another system needs to force the machine into a state.

```csharp
brain.Machine.RequestStateChange<MyState>();
```

This generates `StateTransitionReason.ExternalRequest`.

### 3. Natural Completion

Use this when the state itself knows its work is finished and should advance.

```csharp
Brain.Machine.CompleteState();
```

ou

```csharp
CompleteState(_nextState);
```

This generates `StateTransitionReason.NaturalTransition`.

### 4. Error Transition

Use this when the state detects a problem that requires a fallback path.

```csharp
FailState(null, "The required target was not found.");
```

ou

```csharp
ThrowStateFailure("The state entered an invalid runtime condition.");
```

This generates `StateTransitionReason.ErrorTransition`.

## Order of Events in a Successful Transition

When the brain changes state:

1. salva a referencia do estado anterior
2. executa `Exit()` no estado atual antigo
3. troca `_currentState`
4. grava o `StateTransitionReport`
5. executa `Enter()` no novo estado
6. atualiza `FirstEnteredState` se ainda estiver vazio
7. atualiza o nome atual exibido no inspector
8. dispara o evento `StateChanged`

## Common Recipes

### Recipe 1: Safe Fallback

```csharp
private void OnInit()
{
    AddTransition(() => Health <= 0, _deadState, 1000);
    AddTransition(() => IsTargetVisible(), _attackState, 100);
    AddTransition(() => true, _idleState, -1000);
    SortTransitions();
}
```

### Recipe 2: State Change by String Key

This works better with `ScriptableState`, because `State` uses an empty key by default.

```csharp
brain.Machine.CompleteState("combat.attack");
brain.Machine.FailState("fallback.idle", "Combat target became invalid.");
```

### Recipe 3: Target Entry Lock

```csharp
public override bool CanEnter(IState from)
{
    return _cooldownRemaining <= 0f;
}
```

### Recipe 4: A State That Completes Itself

```csharp
private float _timer;

private void OnEnter()
{
    _timer = 0f;
}

private void OnTick()
{
    _timer += Time.deltaTime;

    if (_timer >= 1.5f)
    {
        Brain.Machine.CompleteState();
    }
}
```

## When No Valid Transition Exists

Nothing changes. The brain stays in the current state and continues executing that state's tick normally.

## When No Transition Evaluation Loop Is Enabled

If all transition evaluation toggles are disabled:

- the brain will not change state automatically through transition conditions
- the machine can still change state through `Brain.Machine.CompleteState(...)`, `Brain.Machine.FailState(...)`, and related calls

## Transition Messages

`StateTransitionReport` also accepts an optional message.

That is especially useful for:

- error history
- visualizer diagnostics
- an audit trail explaining why a machine left a given state

Example:

```csharp
Brain.Machine.FailState(_fallbackState, "Navigation path became invalid.");
```

## Modeling Rule for Conditions

A good transition condition is:

- barata
- deterministica
- facil de ler
- sem alocacao desnecessaria

A bad transition condition is:

- resolves an expensive dependency every frame
- calls heavy search APIs repeatedly
- embeds side effects
- mutates global state just to ask whether it should transition

A transition should answer a question. It should not govern the entire state on its own.
