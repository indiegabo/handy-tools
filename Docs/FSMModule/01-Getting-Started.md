# Getting Started

This guide exists to get a working machine up and running as quickly as possible.

The simplest path for a beginner is:

1. add an `FSMBrain` to a GameObject
2. create two `ScriptableState` assets
3. place those states in the brain state list
4. choose a default state
5. let the brain evaluate transitions in `Update`

Once that works, you can move on to class-based states, triggers, and the core runtime workflow.

## Before Anything Else: What Happens at Runtime?

By default, `FSMBrain` does this:

1. in `Awake`, it prepares the provider, loads configured states, and initializes each state
2. in `Start`, if the mode is `Automatic`, it turns the machine on using the default state
3. in each enabled loop, it evaluates whether the current state wants to transition
4. then it executes the corresponding tick method on the current state

If you understand that flow, half of the FSM fight is already over.

## Minimal Example with ScriptableState

Create an empty GameObject named `Player`.

Add the `FSMBrain` component.

In the `FSMBrain` inspector:

- leave `Initialization Mode` as `Automatic`
- enable `Transitions On Update`
- leave `Transitions On Late Update` and `Transitions On Fixed Update` disabled for now

Now create two scripts.

The snippets below use `ExampleInput.Horizontal` as a stand-in for any
New Input System-backed adapter owned by your project.

### Idle state

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "ExampleIdleState",
        menuName = "HandyTools/FSM/Examples/Example Idle State")]
    public sealed class ExampleIdleState : ScriptableState
    {
        private ExampleMoveState _moveState;

        private void OnInit()
        {
            _moveState = Brain.States.Get<ExampleMoveState>();

            AddTransition(
                () => ExampleInput.Horizontal != 0f,
                _moveState,
                10);

            SortTransitions();
        }

        private void OnEnter()
        {
            Debug.Log("Entered Idle");
        }

        private void OnTick()
        {
        }
    }
}
```

### Move state

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    [CreateAssetMenu(
        fileName = "ExampleMoveState",
        menuName = "HandyTools/FSM/Examples/Example Move State")]
    public sealed class ExampleMoveState : ScriptableState
    {
        private ExampleIdleState _idleState;

        private void OnInit()
        {
            _idleState = Brain.States.Get<ExampleIdleState>();

            AddTransition(
                () => ExampleInput.Horizontal == 0f,
                _idleState,
                10);

            SortTransitions();
        }

        private void OnEnter()
        {
            Debug.Log("Entered Move");
        }

        private void OnTick()
        {
            Debug.Log("Moving...");
        }
    }
}
```

## Creating the Assets

After the scripts compile:

1. right-click in the Project window
2. use `Create > HandyFSM > Examples > Example Idle State`
3. use `Create > HandyFSM > Examples > Example Move State`

Select the GameObject with `FSMBrain` and:

1. drag both assets into `Scriptable States`
2. assign `ExampleIdleState` as `Default Scriptable State`

Enter play mode.

When you press horizontal input, the machine should switch between Idle and Move.

## Why Does `SortTransitions()` Appear in the Examples?

Important detail: the examples add transitions inside `OnInit()`.

Because state initialization calls `SortTransitions()` before invoking `OnInit()`, any transition added inside `OnInit()` must be sorted manually at the end if you depend on priority ordering.

If you forget that:

- the FSM will still run
- but evaluation order will follow insertion order
- numeric priority may not have the effect you expect

## What to Check in the Inspector

During play mode:

- `Status` should change to `On`
- the current state text should change when transitions happen
- the `Open State Visualizer` button should open the visualization window

## When to Use `Manual` Instead of `Automatic`

Use `Manual` when the brain cannot turn on immediately in `Start`.

Classic examples:

- dependencies have not been configured yet
- you need to load data before choosing the initial state
- another system owns the decision of when the FSM should start

Example of manual start-up:

```csharp
using UnityEngine;

namespace IndieGabo.HandyTools.FSMModule.Examples
{
    public sealed class ExampleBootstrapper : MonoBehaviour
    {
        [SerializeField]
        private FSMBrain _brain;

        private void Start()
        {
            if (_brain == null)
            {
                return;
            }

            _brain.TurnOn(_brain.DefaultState);
        }
    }
}
```

## First Sanity Checklist

If nothing happens, check this before blaming the revolution:

- is the brain set to `Automatic`, or did you start it manually?
- is `Default Scriptable State` assigned?
- is the correct transition loop enabled?
- did the scripts compile without errors?
- were the state assets added to the list?
- did you call `SortTransitions()` after adding transitions in `OnInit()`?

## Recommended Next Step

Once this minimal example is alive, read [02 - Core Concepts](02-Core-Concepts.md) and [04 - Creating States](04-Creating-States.md).
