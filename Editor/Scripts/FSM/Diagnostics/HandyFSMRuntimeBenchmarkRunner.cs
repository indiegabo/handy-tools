using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using IndieGabo.HandyTools.FSMModule;
using IndieGabo.HandyTools.FSMModule.Implementations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace IndieGabo.HandyTools.Editor.FSMModule
{
    /// <summary>
    /// Generates repeatable benchmark reports for HandyTools FSM runtime hotspots directly from the editor.
    /// </summary>
    internal static class HandyFSMRuntimeBenchmarkRunner
    {
        #region Constants

        private const int WarmupSampleCount = 2;
        private const int MeasurementSampleCount = 5;
        private const int SmallInitializationBatchSize = 32;
        private const int LargeInitializationBatchSize = 128;
        private const int TransitionMachineBatchSize = 64;
        private const int TransitionEvaluationCount = 2048;
        private const string ReportRelativePath = "docs/handyfsm-runtime-benchmark-report.md";

        #endregion

        #region Menu

        /// <summary>
        /// Runs the built-in HandyTools FSM runtime benchmarks and writes a Markdown report under the docs folder.
        /// </summary>
        [MenuItem("HandyTools/FSM/Generate Runtime Benchmark Report")]
        public static void GenerateRuntimeBenchmarkReport()
        {
            BenchmarkResult[] results =
            {
                MeasureInitializationBatch(SmallInitializationBatchSize),
                MeasureInitializationBatch(LargeInitializationBatchSize),
                MeasureTransitionBatch(
                    "Transition evaluation (steady state)",
                    TransitionMachineBatchSize,
                    TransitionEvaluationCount,
                    false),
                MeasureTransitionBatch(
                    "Transition evaluation (forced transitions)",
                    TransitionMachineBatchSize,
                    TransitionEvaluationCount,
                    true)
            };

            string reportPath = GetReportPath();
            string reportDirectory = Path.GetDirectoryName(reportPath);

            if (!string.IsNullOrEmpty(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }

            File.WriteAllText(reportPath, BuildReport(results));
            AssetDatabase.Refresh();

            Debug.Log($"HandyTools FSM runtime benchmark report written to '{reportPath}'.");
        }

        #endregion

        #region Benchmarking

        /// <summary>
        /// Measures the initialization cost of a batch of machines.
        /// </summary>
        /// <param name="machineCount">The number of machines to initialize per sample.</param>
        /// <returns>The aggregated benchmark result.</returns>
        private static BenchmarkResult MeasureInitializationBatch(int machineCount)
        {
            const string scenarioName = "State initialization and delegate setup";
            string batchLabel = $"{machineCount.ToString(CultureInfo.InvariantCulture)} machines";

            Warmup(
                WarmupSampleCount,
                () =>
                {
                    BenchmarkBrain[] warmupMachines = CreateMachines(machineCount, false);
                    DestroyMachines(warmupMachines);
                });

            double totalMilliseconds = 0d;
            double minimumMilliseconds = double.MaxValue;
            double maximumMilliseconds = double.MinValue;
            long totalAllocatedBytes = 0L;

            for (int sampleIndex = 0; sampleIndex < MeasurementSampleCount; sampleIndex++)
            {
                ForceGarbageCollection();

                long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
                long startTimestamp = Stopwatch.GetTimestamp();
                BenchmarkBrain[] machines = CreateMachines(machineCount, false);
                double elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;

                DestroyMachines(machines);

                totalMilliseconds += elapsedMilliseconds;
                minimumMilliseconds = Math.Min(minimumMilliseconds, elapsedMilliseconds);
                maximumMilliseconds = Math.Max(maximumMilliseconds, elapsedMilliseconds);
                totalAllocatedBytes += allocatedBytes;
            }

            return new BenchmarkResult(
                scenarioName,
                batchLabel,
                machineCount,
                MeasurementSampleCount,
                totalMilliseconds / MeasurementSampleCount,
                minimumMilliseconds,
                maximumMilliseconds,
                totalAllocatedBytes / MeasurementSampleCount);
        }

        /// <summary>
        /// Measures the transition-evaluation cost of a batch of already initialized machines.
        /// </summary>
        /// <param name="scenarioName">The scenario display name.</param>
        /// <param name="machineCount">The number of machines to evaluate.</param>
        /// <param name="evaluationsPerMachine">The number of transition evaluations to perform per machine.</param>
        /// <param name="allowTransitions">Whether each evaluation should result in an actual state transition.</param>
        /// <returns>The aggregated benchmark result.</returns>
        private static BenchmarkResult MeasureTransitionBatch(
            string scenarioName,
            int machineCount,
            int evaluationsPerMachine,
            bool allowTransitions)
        {
            string batchLabel = string.Format(
                CultureInfo.InvariantCulture,
                "{0} machines x {1} evaluations",
                machineCount,
                evaluationsPerMachine);

            Warmup(
                WarmupSampleCount,
                () =>
                {
                    BenchmarkBrain[] warmupMachines = CreateMachines(machineCount, true);
                    RunTransitionEvaluations(
                        warmupMachines,
                        evaluationsPerMachine,
                        allowTransitions);
                    DestroyMachines(warmupMachines);
                });

            double totalMilliseconds = 0d;
            double minimumMilliseconds = double.MaxValue;
            double maximumMilliseconds = double.MinValue;
            long totalAllocatedBytes = 0L;

            for (int sampleIndex = 0; sampleIndex < MeasurementSampleCount; sampleIndex++)
            {
                BenchmarkBrain[] machines = CreateMachines(machineCount, true);
                ForceGarbageCollection();

                long allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
                long startTimestamp = Stopwatch.GetTimestamp();

                RunTransitionEvaluations(
                    machines,
                    evaluationsPerMachine,
                    allowTransitions);

                double elapsedMilliseconds = GetElapsedMilliseconds(startTimestamp);
                long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesBefore;

                DestroyMachines(machines);

                totalMilliseconds += elapsedMilliseconds;
                minimumMilliseconds = Math.Min(minimumMilliseconds, elapsedMilliseconds);
                maximumMilliseconds = Math.Max(maximumMilliseconds, elapsedMilliseconds);
                totalAllocatedBytes += allocatedBytes;
            }

            int totalOperations = machineCount * evaluationsPerMachine;

            return new BenchmarkResult(
                scenarioName,
                batchLabel,
                totalOperations,
                MeasurementSampleCount,
                totalMilliseconds / MeasurementSampleCount,
                minimumMilliseconds,
                maximumMilliseconds,
                totalAllocatedBytes / MeasurementSampleCount);
        }

        /// <summary>
        /// Creates and initializes a hidden batch of benchmark machines.
        /// </summary>
        /// <param name="machineCount">The number of machines to create.</param>
        /// <param name="turnOnDefaultState">Whether the default state should be entered after initialization.</param>
        /// <returns>The created machine batch.</returns>
        private static BenchmarkBrain[] CreateMachines(int machineCount, bool turnOnDefaultState)
        {
            BenchmarkBrain[] machines = new BenchmarkBrain[machineCount];

            for (int machineIndex = 0; machineIndex < machineCount; machineIndex++)
            {
                GameObject gameObject = new(
                    $"HandyTools FSM Benchmark Machine {machineIndex.ToString(CultureInfo.InvariantCulture)}")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                BenchmarkBrain machine = gameObject.AddComponent<BenchmarkBrain>();
                machine.hideFlags = HideFlags.HideAndDontSave;
                machine.RunBenchmarkInitialize(turnOnDefaultState);
                machines[machineIndex] = machine;
            }

            return machines;
        }

        /// <summary>
        /// Destroys a batch of temporary benchmark machines.
        /// </summary>
        /// <param name="machines">The machines to destroy.</param>
        private static void DestroyMachines(BenchmarkBrain[] machines)
        {
            if (machines == null)
            {
                return;
            }

            for (int machineIndex = 0; machineIndex < machines.Length; machineIndex++)
            {
                BenchmarkBrain machine = machines[machineIndex];

                if (machine == null)
                {
                    continue;
                }

                Object.DestroyImmediate(machine.gameObject);
            }
        }

        /// <summary>
        /// Executes repeated transition evaluations over a machine batch.
        /// </summary>
        /// <param name="machines">The machines to evaluate.</param>
        /// <param name="evaluationsPerMachine">The number of evaluations to perform per machine.</param>
        /// <param name="allowTransitions">Whether the states should actually transition.</param>
        private static void RunTransitionEvaluations(
            BenchmarkBrain[] machines,
            int evaluationsPerMachine,
            bool allowTransitions)
        {
            for (int machineIndex = 0; machineIndex < machines.Length; machineIndex++)
            {
                machines[machineIndex].SetAllowTransitions(allowTransitions);
            }

            for (int evaluationIndex = 0; evaluationIndex < evaluationsPerMachine; evaluationIndex++)
            {
                for (int machineIndex = 0; machineIndex < machines.Length; machineIndex++)
                {
                    machines[machineIndex].RunBenchmarkTransitionEvaluation();
                }
            }
        }

        /// <summary>
        /// Runs a scenario a fixed number of times outside the measured samples to stabilize caches.
        /// </summary>
        /// <param name="warmupCount">The number of warmup runs.</param>
        /// <param name="action">The warmup action.</param>
        private static void Warmup(int warmupCount, Action action)
        {
            for (int warmupIndex = 0; warmupIndex < warmupCount; warmupIndex++)
            {
                action();
            }
        }

        /// <summary>
        /// Forces a full garbage collection before a measured sample starts.
        /// </summary>
        private static void ForceGarbageCollection()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        /// <summary>
        /// Converts a stopwatch timestamp into milliseconds.
        /// </summary>
        /// <param name="startTimestamp">The timestamp captured before the measured work began.</param>
        /// <returns>The elapsed time in milliseconds.</returns>
        private static double GetElapsedMilliseconds(long startTimestamp)
        {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;
            return elapsedTicks * 1000d / Stopwatch.Frequency;
        }

        #endregion

        #region Reporting

        /// <summary>
        /// Builds the Markdown report for the collected benchmark results.
        /// </summary>
        /// <param name="results">The measured benchmark results.</param>
        /// <returns>The Markdown report contents.</returns>
        private static string BuildReport(IReadOnlyList<BenchmarkResult> results)
        {
            StringBuilder builder = new();

            builder.AppendLine("# HandyTools FSM Runtime Benchmark Report");
            builder.AppendLine();
            builder.AppendLine($"Generated on {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}.");
            builder.AppendLine();
            builder.AppendLine("This report is produced by `HandyTools/FSM/Generate Runtime Benchmark Report`.");
            builder.AppendLine();
            builder.AppendLine($"Warmup samples per scenario: {WarmupSampleCount.ToString(CultureInfo.InvariantCulture)}.");
            builder.AppendLine($"Measured samples per scenario: {MeasurementSampleCount.ToString(CultureInfo.InvariantCulture)}.");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Batch | Avg ms | Min ms | Max ms | Avg alloc (B) | Avg us/op | Avg B/op |");
            builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |");

            for (int resultIndex = 0; resultIndex < results.Count; resultIndex++)
            {
                BenchmarkResult result = results[resultIndex];

                builder.Append("| ")
                    .Append(result.ScenarioName)
                    .Append(" | ")
                    .Append(result.BatchLabel)
                    .Append(" | ")
                    .Append(result.AverageMilliseconds.ToString("F4", CultureInfo.InvariantCulture))
                    .Append(" | ")
                    .Append(result.MinimumMilliseconds.ToString("F4", CultureInfo.InvariantCulture))
                    .Append(" | ")
                    .Append(result.MaximumMilliseconds.ToString("F4", CultureInfo.InvariantCulture))
                    .Append(" | ")
                    .Append(result.AverageAllocatedBytes.ToString(CultureInfo.InvariantCulture))
                    .Append(" | ")
                    .Append(result.AverageMicrosecondsPerOperation.ToString("F4", CultureInfo.InvariantCulture))
                    .Append(" | ")
                    .Append(result.AverageAllocatedBytesPerOperation.ToString("F4", CultureInfo.InvariantCulture))
                    .AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Scenarios");
            builder.AppendLine();
            builder.AppendLine("- `State initialization and delegate setup`: creates hidden benchmark brains and runs the same discovery, state initialization, reflection, and delegate binding path used by the runtime generic machine flow.");
            builder.AppendLine("- `Transition evaluation (steady state)`: turns on a batch of machines and repeatedly evaluates transitions while all conditions stay false.");
            builder.AppendLine("- `Transition evaluation (forced transitions)`: turns on a batch of machines and repeatedly evaluates transitions while every evaluation advances to the next state.");
            builder.AppendLine();
            builder.AppendLine("## Notes");
            builder.AppendLine();
            builder.AppendLine("- Allocation values come from `GC.GetAllocatedBytesForCurrentThread()` and represent the benchmark runner thread only.");
            builder.AppendLine("- Re-run this report after significant runtime changes, domain reload changes, or Unity upgrades.");
            builder.AppendLine("- Close unrelated editor tooling and let the editor settle before running the benchmark for cleaner results.");

            return builder.ToString();
        }

        /// <summary>
        /// Resolves the absolute path to the generated benchmark report.
        /// </summary>
        /// <returns>The absolute report path.</returns>
        private static string GetReportPath()
        {
            return Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", ReportRelativePath));
        }

        #endregion

        #region Supporting Types

        /// <summary>
        /// Holds aggregate timing and allocation statistics for one benchmark scenario.
        /// </summary>
        private readonly struct BenchmarkResult
        {
            public BenchmarkResult(
                string scenarioName,
                string batchLabel,
                int operationCount,
                int sampleCount,
                double averageMilliseconds,
                double minimumMilliseconds,
                double maximumMilliseconds,
                long averageAllocatedBytes)
            {
                ScenarioName = scenarioName;
                BatchLabel = batchLabel;
                OperationCount = operationCount;
                SampleCount = sampleCount;
                AverageMilliseconds = averageMilliseconds;
                MinimumMilliseconds = minimumMilliseconds;
                MaximumMilliseconds = maximumMilliseconds;
                AverageAllocatedBytes = averageAllocatedBytes;
            }

            public string ScenarioName { get; }
            public string BatchLabel { get; }
            public int OperationCount { get; }
            public int SampleCount { get; }
            public double AverageMilliseconds { get; }
            public double MinimumMilliseconds { get; }
            public double MaximumMilliseconds { get; }
            public long AverageAllocatedBytes { get; }

            public double AverageMicrosecondsPerOperation =>
                OperationCount <= 0
                    ? 0d
                    : AverageMilliseconds * 1000d / OperationCount;

            public double AverageAllocatedBytesPerOperation =>
                OperationCount <= 0
                    ? 0d
                    : (double)AverageAllocatedBytes / OperationCount;
        }

        /// <summary>
        /// Hidden benchmark brain used to exercise the generic runtime initialization and transition pipeline.
        /// </summary>
        private sealed class BenchmarkBrain : GenericHandyFSMBrain<BenchmarkStateBase, BenchmarkState01>
        {
            private bool _allowTransitions;

            /// <summary>
            /// Gets whether benchmark states should transition when evaluated.
            /// </summary>
            public bool AllowTransitions => _allowTransitions;

            /// <summary>
            /// Suppresses Unity-driven initialization so the benchmark runner controls exactly when it happens.
            /// </summary>
            protected override void Awake()
            {
            }

            /// <summary>
            /// Suppresses automatic startup so the benchmark runner controls state entry explicitly.
            /// </summary>
            protected override void Start()
            {
            }

            /// <summary>
            /// Runs the underlying FSM brain initialization and optionally turns on the default state.
            /// </summary>
            /// <param name="turnOnDefaultState">Whether to enter the default state after initialization.</param>
            public void RunBenchmarkInitialize(bool turnOnDefaultState)
            {
                base.Awake();

                if (turnOnDefaultState && DefaultState != null)
                {
                    Machine.TurnOn(DefaultState);
                }
            }

            /// <summary>
            /// Sets whether transition conditions should advance to the next state.
            /// </summary>
            /// <param name="allowTransitions">Whether benchmark transitions should succeed.</param>
            public void SetAllowTransitions(bool allowTransitions)
            {
                _allowTransitions = allowTransitions;
            }

            /// <summary>
            /// Executes one runtime transition evaluation pass.
            /// </summary>
            public void RunBenchmarkTransitionEvaluation()
            {
                base.EvaluateTransition();
            }
        }

        /// <summary>
        /// Shared benchmark runtime-state base used to configure identical lifecycle hooks and transitions.
        /// </summary>
        private abstract class BenchmarkStateBase : State
        {
            private bool _transitionsConfigured;

            /// <summary>
            /// Gets the runtime type of the next state in the benchmark cycle.
            /// </summary>
            protected abstract Type NextStateType { get; }

            /// <summary>
            /// Configures the benchmark transitions once the owning brain has loaded all states.
            /// </summary>
            /// <param name="brain">The owning FSM brain.</param>
            public override void Initialize(FSMBrain brain)
            {
                base.Initialize(brain);

                if (_transitionsConfigured)
                {
                    return;
                }

                IState nextState = brain.States.Get(NextStateType);

                AddTransition(ShouldAdvance, nextState, 10);
                AddTransition(AlwaysFalse, nextState, 5);
                AddTransition(AlwaysFalse, this, 0);
                SortTransitions();

                _transitionsConfigured = true;
            }

            /// <summary>
            /// Declares a benchmark OnInit hook so lifecycle delegate binding exercises a populated state type.
            /// </summary>
            protected void OnInit()
            {
            }

            /// <summary>
            /// Declares a benchmark OnEnter hook so lifecycle delegate binding exercises a populated state type.
            /// </summary>
            protected void OnEnter()
            {
            }

            /// <summary>
            /// Declares a benchmark OnExit hook so lifecycle delegate binding exercises a populated state type.
            /// </summary>
            protected void OnExit()
            {
            }

            /// <summary>
            /// Declares a benchmark OnTick hook so lifecycle delegate binding exercises a populated state type.
            /// </summary>
            protected void OnTick()
            {
            }

            /// <summary>
            /// Declares a benchmark OnLateTick hook so lifecycle delegate binding exercises a populated state type.
            /// </summary>
            protected void OnLateTick()
            {
            }

            /// <summary>
            /// Declares a benchmark OnFixedTick hook so lifecycle delegate binding exercises a populated state type.
            /// </summary>
            protected void OnFixedTick()
            {
            }

            /// <summary>
            /// Determines whether the benchmark should advance to the next state.
            /// </summary>
            /// <returns>True when the owning benchmark brain enables transitions.</returns>
            private bool ShouldAdvance()
            {
                return Brain is BenchmarkBrain benchmarkBrain && benchmarkBrain.AllowTransitions;
            }

            /// <summary>
            /// Provides a permanently false condition to force additional transition checks.
            /// </summary>
            /// <returns>Always false.</returns>
            private static bool AlwaysFalse()
            {
                return false;
            }
        }

        /// <summary>
        /// Benchmark state 01.
        /// </summary>
        private sealed class BenchmarkState01 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState02);
        }

        /// <summary>
        /// Benchmark state 02.
        /// </summary>
        private sealed class BenchmarkState02 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState03);
        }

        /// <summary>
        /// Benchmark state 03.
        /// </summary>
        private sealed class BenchmarkState03 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState04);
        }

        /// <summary>
        /// Benchmark state 04.
        /// </summary>
        private sealed class BenchmarkState04 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState05);
        }

        /// <summary>
        /// Benchmark state 05.
        /// </summary>
        private sealed class BenchmarkState05 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState06);
        }

        /// <summary>
        /// Benchmark state 06.
        /// </summary>
        private sealed class BenchmarkState06 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState07);
        }

        /// <summary>
        /// Benchmark state 07.
        /// </summary>
        private sealed class BenchmarkState07 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState08);
        }

        /// <summary>
        /// Benchmark state 08.
        /// </summary>
        private sealed class BenchmarkState08 : BenchmarkStateBase
        {
            protected override Type NextStateType => typeof(BenchmarkState01);
        }

        #endregion
    }
}