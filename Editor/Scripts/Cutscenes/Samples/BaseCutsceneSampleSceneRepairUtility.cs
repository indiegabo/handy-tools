using System;
using System.Reflection;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Flow;
using IndieGabo.HandyTools.CutscenesModule.Triggers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Samples
{
    /// <summary>
    /// Rebuilds the base cutscene sample scene graph through regular editor APIs
    /// so Unity can reserialize the scene with valid managed reference ids.
    /// </summary>
    public static class BaseCutsceneSampleSceneRepairUtility
    {
        private const string ScenePath =
            "Assets/HandyTools/Samples/Cutscenes Base Sample/Scenes/CutscenesBaseSample.unity";
        private const string RootObjectName = "Base Cutscene Sample";
        private const string PropObjectName = "Base Sample Prop";
        private const string MessageKey = "sample.message";
        private const string PropTargetKey = "sample.prop.target";
        private const string PropActiveKey = "sample.prop.active";

        private static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        /// <summary>
        /// Repairs the scene from the editor menu.
        /// </summary>
        [MenuItem("Tools/HandyTools/Cutscenes/Repair Base Sample Scene")]
        public static void RepairFromMenu()
        {
            RepairScene();
        }

        /// <summary>
        /// Opens the base sample scene, rebuilds the authored graph, and saves
        /// the scene back to disk.
        /// </summary>
        public static void RepairScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject rootObject = GetOrCreateRootObject(scene);
            CutsceneDirector director = GetOrCreateDirector(rootObject);
            CutsceneTrigger trigger = GetOrCreateTrigger(rootObject);
            GameObject propObject = GetOrCreatePropObject(rootObject.transform);

            CutsceneGraph graph = BuildGraph(propObject);
            director.ReplaceGraph(graph);
            director.SetBlackboardFoldoutState(MessageKey, false);
            director.SetBlackboardFoldoutState(PropTargetKey, false);
            director.SetBlackboardFoldoutState(PropActiveKey, false);
            ConfigureTrigger(trigger, director);

            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(propObject);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene))
            {
                throw new InvalidOperationException(
                    "Base cutscene sample scene could not be saved after repair.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Rebuilds the base sample graph using stable blackboard-bound and
        /// direct-value nodes expected by the smoke coverage.
        /// </summary>
        /// <param name="propObject">Scene prop toggled by the sample.</param>
        /// <returns>The rebuilt cutscene graph.</returns>
        private static CutsceneGraph BuildGraph(GameObject propObject)
        {
            CutsceneGraph graph = new();

            graph.Blackboard.SetValue(MessageKey, "Cutscene log");
            graph.Blackboard.SetValue(PropTargetKey, propObject);
            graph.Blackboard.SetValue(PropActiveKey, false);

            CutsceneEntryNode entryNode = graph.CreateNode<CutsceneEntryNode>();
            entryNode.Position = new Vector2(-640f, 32f);

            CutsceneSetBlackboardValuesNode setBlackboardNode =
                graph.CreateNode<CutsceneSetBlackboardValuesNode>();
            setBlackboardNode.Position = new Vector2(-320f, 32f);
            setBlackboardNode.SetAssignments(
                CutsceneSetBlackboardValuesNode.BlackboardValueAssignment
                    .CreateString(MessageKey, "Cutscene log"),
                CutsceneSetBlackboardValuesNode.BlackboardValueAssignment
                    .CreateObject(PropTargetKey, propObject),
                CutsceneSetBlackboardValuesNode.BlackboardValueAssignment
                    .CreateBool(PropActiveKey, false));

            CutsceneLogBlackboardValueNode logBlackboardNode =
                graph.CreateNode<CutsceneLogBlackboardValueNode>();
            logBlackboardNode.Configure(
                MessageKey,
                CutsceneBlackboardValueType.String,
                false,
                false);
            logBlackboardNode.Position = new Vector2(0f, 32f);

            CutsceneSetGameObjectActiveNode setInactiveNode =
                graph.CreateNode<CutsceneSetGameObjectActiveNode>();
            setInactiveNode.Position = new Vector2(320f, 32f);

            CutsceneWaitNode firstWaitNode = graph.CreateNode<CutsceneWaitNode>();
            firstWaitNode.Configure(1f, CutsceneTimeMode.Scaled);
            firstWaitNode.Position = new Vector2(640f, 32f);

            CutsceneWaitNode secondWaitNode = graph.CreateNode<CutsceneWaitNode>();
            secondWaitNode.Configure(1f, CutsceneTimeMode.Unscaled);
            secondWaitNode.Position = new Vector2(960f, 224f);

            CutsceneSetGameObjectActiveNode setActiveNode =
                graph.CreateNode<CutsceneSetGameObjectActiveNode>();
            setActiveNode.Configure(propObject, true);
            setActiveNode.Position = new Vector2(1280f, 224f);

            CutsceneFinishNode finishNode = graph.CreateNode<CutsceneFinishNode>();
            finishNode.Position = new Vector2(1600f, 224f);

            graph.Connect(entryNode.Id, CutsceneNodePorts.Next, setBlackboardNode.Id);
            graph.Connect(
                setBlackboardNode.Id,
                CutsceneNodePorts.Next,
                logBlackboardNode.Id);
            graph.Connect(
                logBlackboardNode.Id,
                CutsceneNodePorts.Next,
                setInactiveNode.Id);
            graph.Connect(setInactiveNode.Id, CutsceneNodePorts.Next, firstWaitNode.Id);
            graph.Connect(firstWaitNode.Id, CutsceneNodePorts.Next, secondWaitNode.Id);
            graph.Connect(secondWaitNode.Id, CutsceneNodePorts.Next, setActiveNode.Id);
            graph.Connect(setActiveNode.Id, CutsceneNodePorts.Next, finishNode.Id);

            NormalizeBlackboardReferences(graph, setBlackboardNode);
            BindLogBlackboardNode(graph, logBlackboardNode, MessageKey);
            BindSetActiveNode(graph, setInactiveNode, PropTargetKey, PropActiveKey);

            return graph;
        }

        /// <summary>
        /// Binds the authored assignment list to stable blackboard entry ids.
        /// </summary>
        /// <param name="graph">Graph that owns the blackboard entries.</param>
        /// <param name="node">Node whose assignments should be normalized.</param>
        private static void NormalizeBlackboardReferences(
            CutsceneGraph graph,
            CutsceneSetBlackboardValuesNode node)
        {
            if (graph == null || node == null)
            {
                return;
            }

            for (int index = 0; index < node.Assignments.Count; index++)
            {
                CutsceneSetBlackboardValuesNode.BlackboardValueAssignment assignment =
                    node.Assignments[index];

                if (assignment == null
                    || string.IsNullOrWhiteSpace(assignment.Key)
                    || !graph.Blackboard.TryGetEntry(
                        assignment.Key,
                        out CutsceneGraphBlackboardEntry entry))
                {
                    continue;
                }

                assignment.TargetVariable.Bind(entry);
            }
        }

        /// <summary>
        /// Rebinds the log node variable to one stable blackboard entry id.
        /// </summary>
        /// <param name="graph">Graph that owns the blackboard entries.</param>
        /// <param name="node">Node that reads from the blackboard.</param>
        /// <param name="entryKey">Entry key read by the node.</param>
        private static void BindLogBlackboardNode(
            CutsceneGraph graph,
            CutsceneLogBlackboardValueNode node,
            string entryKey)
        {
            if (graph == null
                || node == null
                || string.IsNullOrWhiteSpace(entryKey)
                || !graph.Blackboard.TryGetEntry(
                    entryKey,
                    out CutsceneGraphBlackboardEntry entry))
            {
                return;
            }

            CutsceneBlackboardVariableReference variable = GetFieldValue<
                CutsceneBlackboardVariableReference>(node, "_variable");
            variable.Bind(entry);
        }

        /// <summary>
        /// Switches the authored set-active node to blackboard mode using stable
        /// entry ids for both the target object and active-state sources.
        /// </summary>
        /// <param name="graph">Graph that owns the blackboard entries.</param>
        /// <param name="node">Node whose value sources should be rebound.</param>
        /// <param name="targetEntryKey">Blackboard key for the target object.</param>
        /// <param name="activeEntryKey">Blackboard key for the active state.</param>
        private static void BindSetActiveNode(
            CutsceneGraph graph,
            CutsceneSetGameObjectActiveNode node,
            string targetEntryKey,
            string activeEntryKey)
        {
            if (graph == null || node == null)
            {
                return;
            }

            if (graph.Blackboard.TryGetEntry(
                    targetEntryKey,
                    out CutsceneGraphBlackboardEntry targetEntry))
            {
                GetFieldValue<CutsceneValueSource>(node, "_targetSource")
                    .BindBlackboardVariable(targetEntry);
            }

            if (graph.Blackboard.TryGetEntry(
                    activeEntryKey,
                    out CutsceneGraphBlackboardEntry activeEntry))
            {
                GetFieldValue<CutsceneValueSource>(node, "_activeSource")
                    .BindBlackboardVariable(activeEntry);
            }
        }

        /// <summary>
        /// Resolves the root sample object or creates one when the scene is
        /// missing the authored sample hierarchy.
        /// </summary>
        /// <param name="scene">Scene being repaired.</param>
        /// <returns>The root sample object.</returns>
        private static GameObject GetOrCreateRootObject(Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();

            for (int index = 0; index < rootObjects.Length; index++)
            {
                if (string.Equals(
                        rootObjects[index].name,
                        RootObjectName,
                        StringComparison.Ordinal))
                {
                    return rootObjects[index];
                }
            }

            return new GameObject(RootObjectName);
        }

        /// <summary>
        /// Resolves the cutscene director component used by the sample scene.
        /// </summary>
        /// <param name="rootObject">Root sample object.</param>
        /// <returns>The ensured director component.</returns>
        private static CutsceneDirector GetOrCreateDirector(GameObject rootObject)
        {
            return rootObject.TryGetComponent(out CutsceneDirector director)
                ? director
                : rootObject.AddComponent<CutsceneDirector>();
        }

        /// <summary>
        /// Resolves the trigger component used to auto-play the sample graph.
        /// </summary>
        /// <param name="rootObject">Root sample object.</param>
        /// <returns>The ensured trigger component.</returns>
        private static CutsceneTrigger GetOrCreateTrigger(GameObject rootObject)
        {
            return rootObject.TryGetComponent(out CutsceneTrigger trigger)
                ? trigger
                : rootObject.AddComponent<CutsceneTrigger>();
        }

        /// <summary>
        /// Resolves the prop toggled by the sample graph or recreates it under
        /// the sample root when it is missing.
        /// </summary>
        /// <param name="rootTransform">Root sample transform.</param>
        /// <returns>The ensured sample prop object.</returns>
        private static GameObject GetOrCreatePropObject(Transform rootTransform)
        {
            Transform propTransform = rootTransform.Find(PropObjectName);

            if (propTransform == null)
            {
                GameObject sceneProp = GameObject.Find(PropObjectName);

                if (sceneProp != null)
                {
                    propTransform = sceneProp.transform;
                }
            }

            if (propTransform == null)
            {
                GameObject sceneProp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sceneProp.name = PropObjectName;
                sceneProp.transform.SetParent(rootTransform, false);
                sceneProp.transform.localPosition = Vector3.zero;
                sceneProp.transform.localScale = Vector3.one;
                sceneProp.SetActive(true);
                return sceneProp;
            }

            if (propTransform.parent != rootTransform)
            {
                propTransform.SetParent(rootTransform, true);
            }

            propTransform.gameObject.SetActive(true);
            return propTransform.gameObject;
        }

        /// <summary>
        /// Configures the scene trigger to start the repaired graph from the
        /// sample root object at startup.
        /// </summary>
        /// <param name="trigger">Trigger component to configure.</param>
        /// <param name="director">Director played by the trigger.</param>
        private static void ConfigureTrigger(
            CutsceneTrigger trigger,
            CutsceneDirector director)
        {
            SetFieldValue(trigger, "_director", director);
            SetFieldValue(trigger, "_triggerMode", CutsceneTriggerMode.Start);
            SetFieldValue(trigger, "_oneShot", false);
            SetFieldValue(trigger, "_gate", true);
        }

        /// <summary>
        /// Reads one private or public field from the provided object.
        /// </summary>
        /// <typeparam name="TField">Expected field value type.</typeparam>
        /// <param name="instance">Object that owns the field.</param>
        /// <param name="fieldName">Serialized field name.</param>
        /// <returns>The resolved field value.</returns>
        private static TField GetFieldValue<TField>(object instance, string fieldName)
        {
            FieldInfo field = GetRequiredField(instance, fieldName);
            return (TField)field.GetValue(instance);
        }

        /// <summary>
        /// Writes one private or public field on the provided object.
        /// </summary>
        /// <typeparam name="TValue">Written field value type.</typeparam>
        /// <param name="instance">Object that owns the field.</param>
        /// <param name="fieldName">Serialized field name.</param>
        /// <param name="value">Value written to the field.</param>
        private static void SetFieldValue<TValue>(
            object instance,
            string fieldName,
            TValue value)
        {
            FieldInfo field = GetRequiredField(instance, fieldName);
            field.SetValue(instance, value);
        }

        /// <summary>
        /// Resolves one required reflected field.
        /// </summary>
        /// <param name="instance">Object that owns the field.</param>
        /// <param name="fieldName">Serialized field name.</param>
        /// <returns>The resolved field metadata.</returns>
        private static FieldInfo GetRequiredField(object instance, string fieldName)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            FieldInfo field = instance.GetType().GetField(fieldName, FieldFlags);

            if (field != null)
            {
                return field;
            }

            throw new MissingFieldException(instance.GetType().FullName, fieldName);
        }
    }
}