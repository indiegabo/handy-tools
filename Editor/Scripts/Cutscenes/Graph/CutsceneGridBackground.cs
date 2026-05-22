using IndieGabo.HandyTools.Editor.GraphCore;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    /// <summary>
    /// Draws one explicit major and minor grid for the cutscene graph so node
    /// placement reads as small units during authoring.
    /// </summary>
    internal sealed class CutsceneGridBackground : GraphCanvasGridBackground
    {
        private const float CutsceneMajorStep = 80f;
        private const int CutsceneMinorDivisions = 5;

        internal const float CutsceneMinorStep =
            CutsceneMajorStep / CutsceneMinorDivisions;

        private static readonly Color CutsceneBackgroundColor =
            new(0.105f, 0.105f, 0.105f, 1f);

        private static readonly Color CutsceneMinorGridColor =
            new(0.19f, 0.19f, 0.19f, 0.75f);

        private static readonly Color CutsceneMajorGridColor =
            new(0.27f, 0.27f, 0.27f, 0.95f);

        /// <summary>
        /// Creates one background drawer bound to the current graph view
        /// transform.
        /// </summary>
        /// <param name="graphView">
        /// Graph view that owns the background and its pan or zoom state.
        /// </param>
        public CutsceneGridBackground(GraphView graphView)
            : base(
                graphView,
                CutsceneMajorStep,
                CutsceneMinorDivisions,
                CutsceneBackgroundColor,
                CutsceneMinorGridColor,
                CutsceneMajorGridColor)
        {
        }
    }
}