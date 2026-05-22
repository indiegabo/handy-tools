using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.Scenes.Testing
{
    /// <summary>
    /// Executes the HandyScene edit-mode suite from the Unity CLI through one
    /// explicit executeMethod entry point.
    /// </summary>
    public static class HandySceneCliEditModeTestRunner
    {
        #region Constants

        private const string ScenesEditModeAssemblyName =
            "IndieGabo.HandyTools.Scenes.EditMode.Tests";

        private const string ScenesTestNamespaceRegex =
            "^IndieGabo\\.HandyTools\\.Scenes\\.Tests\\.";

        #endregion

        #region Public API

        /// <summary>
        /// Runs the HandyScene edit-mode suite synchronously and exits the
        /// editor with a non-zero code when the suite fails.
        /// </summary>
        public static void Run()
        {
            ScenesCliTestCallbacks callbacks = new();
            TestRunnerApi testRunnerApi = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunnerApi.RegisterCallbacks(callbacks);

            try
            {
                ExecutionSettings executionSettings = new(
                    new Filter
                    {
                        testMode = TestMode.EditMode,
                        assemblyNames = new[] { ScenesEditModeAssemblyName },
                        groupNames = new[] { ScenesTestNamespaceRegex },
                    })
                {
                    runSynchronously = true,
                };

                Debug.Log("Running HandyScene edit-mode tests from the CLI runner.");
                testRunnerApi.Execute(executionSettings);

                if (callbacks.RunResult == null)
                {
                    Debug.LogError(
                        "HandyScene edit-mode CLI runner finished without producing one run result.");
                    EditorApplication.Exit(1);
                    return;
                }

                LogSummary(callbacks.RunResult, callbacks.Failures);
                EditorApplication.Exit(callbacks.RunResult.FailCount > 0 ? 2 : 0);
            }
            finally
            {
                testRunnerApi.UnregisterCallbacks(callbacks);
                Object.DestroyImmediate(testRunnerApi);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Logs the final suite summary and any individual failures captured
        /// during the run.
        /// </summary>
        /// <param name="runResult">Final run result.</param>
        /// <param name="failures">Captured failing test results.</param>
        private static void LogSummary(
            ITestResultAdaptor runResult,
            IReadOnlyList<ITestResultAdaptor> failures)
        {
            Debug.Log(
                "HandyScene edit-mode tests finished. "
                + $"Passed: {runResult.PassCount}, Failed: {runResult.FailCount}, "
                + $"Skipped: {runResult.SkipCount}, Inconclusive: {runResult.InconclusiveCount}."
            );

            for (int index = 0; index < failures.Count; index++)
            {
                ITestResultAdaptor failure = failures[index];
                Debug.LogError(
                    $"[Scenes Test Failure] {failure.FullName}\n"
                    + $"{failure.Message}\n{failure.StackTrace}");
            }
        }

        /// <summary>
        /// Captures the HandyScene edit-mode run result and any leaf failures.
        /// </summary>
        private sealed class ScenesCliTestCallbacks : ICallbacks
        {
            private readonly List<ITestResultAdaptor> _failures = new();

            /// <summary>
            /// Gets the final run result.
            /// </summary>
            public ITestResultAdaptor RunResult { get; private set; }

            /// <summary>
            /// Gets the captured failing leaf tests.
            /// </summary>
            public IReadOnlyList<ITestResultAdaptor> Failures => _failures;

            /// <inheritdoc />
            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log(
                    $"HandyScene edit-mode CLI runner started with {testsToRun.TestCaseCount} test cases.");
            }

            /// <inheritdoc />
            public void RunFinished(ITestResultAdaptor result)
            {
                RunResult = result;
            }

            /// <inheritdoc />
            public void TestStarted(ITestAdaptor test)
            {
            }

            /// <inheritdoc />
            public void TestFinished(ITestResultAdaptor result)
            {
                if (result == null
                    || result.Test == null
                    || result.Test.IsSuite
                    || result.TestStatus != UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed)
                {
                    return;
                }

                _failures.Add(result);
            }
        }

        #endregion
    }
}