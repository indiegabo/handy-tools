using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Actions;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Flow;
using UnityEngine;

namespace IndieGabo.HandyTools.Samples.Cutscenes.DialogueSystem
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class DialogueSystemCutsceneSampleInstaller : MonoBehaviour
    {
        private const float NodeHorizontalSpacing = 288f;

        [SerializeField] private string _conversationTitle = "Sample Dialogue Conversation";
        [SerializeField] private Transform _speaker;
        [SerializeField] private Transform _listener;

        private CutsceneDirector _director;

        private void Reset()
        {
            EnsureSetup(true);
        }

        private void OnValidate()
        {
            EnsureSetup();
        }

        private void Awake()
        {
            EnsureSetup();
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                _director?.Play();
            }
        }

        private void EnsureSetup(bool forceRebuildGraph = false)
        {
            if (!TryGetComponent(out _director))
            {
                _director = gameObject.AddComponent<CutsceneDirector>();
            }

            EnsureCamera();
            EnsureLight();
            EnsureActors();

            if (forceRebuildGraph || _director.Graph.Nodes.Count == 0)
            {
                BuildGraph();
                MarkDirectorDirtyInEditor();
                return;
            }

            if (TryUpgradeLegacyGraphLayout())
            {
                MarkDirectorDirtyInEditor();
            }
        }

        private void EnsureCamera()
        {
            if (Camera.main != null || FindAnyObjectByType<Camera>() != null)
            {
                return;
            }

            GameObject cameraObject = new("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 2f, -10f), Quaternion.identity);
        }

        private void EnsureLight()
        {
            if (FindAnyObjectByType<Light>() != null)
            {
                return;
            }

            GameObject lightObject = new("Directional Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private void EnsureActors()
        {
            if (_speaker == null)
            {
                GameObject speakerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                speakerObject.name = "Dialogue Speaker";
                speakerObject.transform.SetParent(transform, false);
                speakerObject.transform.localPosition = new Vector3(-1.5f, 0f, 0f);
                _speaker = speakerObject.transform;
            }

            if (_listener == null)
            {
                GameObject listenerObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                listenerObject.name = "Dialogue Listener";
                listenerObject.transform.SetParent(transform, false);
                listenerObject.transform.localPosition = new Vector3(1.5f, 0f, 0f);
                _listener = listenerObject.transform;
            }
        }

        private void BuildGraph()
        {
            CutsceneGraph graph = new();

            CutsceneEntryNode entryNode = graph.CreateNode<CutsceneEntryNode>();
            entryNode.Position = new Vector2(0f, 0f);

            CutsceneLogNode logNode = graph.CreateNode<CutsceneLogNode>();
            logNode.Configure("Dialogue System cutscene sample started.");
            logNode.Position = new Vector2(NodeHorizontalSpacing, 0f);

            CutsceneDialogueConversationNode dialogueNode = graph.CreateNode<CutsceneDialogueConversationNode>();
            dialogueNode.Configure(_conversationTitle, string.Empty, _speaker, _listener, true, false);
            dialogueNode.Position = new Vector2(NodeHorizontalSpacing * 2f, 0f);

            CutsceneWaitNode waitNode = graph.CreateNode<CutsceneWaitNode>();
            waitNode.Configure(0.5f, CutsceneTimeMode.Unscaled);
            waitNode.Position = new Vector2(NodeHorizontalSpacing * 3f, 0f);

            CutsceneFinishNode finishNode = graph.CreateNode<CutsceneFinishNode>();
            finishNode.Position = new Vector2(NodeHorizontalSpacing * 4f, 0f);

            graph.Connect(entryNode.Id, CutsceneNodePorts.Next, logNode.Id);
            graph.Connect(logNode.Id, CutsceneNodePorts.Next, dialogueNode.Id);
            graph.Connect(dialogueNode.Id, CutsceneNodePorts.Next, waitNode.Id);
            graph.Connect(waitNode.Id, CutsceneNodePorts.Next, finishNode.Id);

            _director.ReplaceGraph(graph);
        }

        private bool TryUpgradeLegacyGraphLayout()
        {
            if (!HasLegacyDefaultLayout())
            {
                return false;
            }

            _director.Graph.Nodes[0].Position = new Vector2(0f, 0f);
            _director.Graph.Nodes[1].Position = new Vector2(NodeHorizontalSpacing, 0f);
            _director.Graph.Nodes[2].Position = new Vector2(NodeHorizontalSpacing * 2f, 0f);
            _director.Graph.Nodes[3].Position = new Vector2(NodeHorizontalSpacing * 3f, 0f);
            _director.Graph.Nodes[4].Position = new Vector2(NodeHorizontalSpacing * 4f, 0f);
            return true;
        }

        private bool HasLegacyDefaultLayout()
        {
            if (_director == null || _director.Graph.Nodes.Count != 5)
            {
                return false;
            }

            return _director.Graph.Nodes[0] is CutsceneEntryNode
                && _director.Graph.Nodes[1] is CutsceneLogNode
                && _director.Graph.Nodes[2] is CutsceneDialogueConversationNode
                && _director.Graph.Nodes[3] is CutsceneWaitNode
                && _director.Graph.Nodes[4] is CutsceneFinishNode
                && _director.Graph.Nodes[0].Position == new Vector2(0f, 0f)
                && _director.Graph.Nodes[1].Position == new Vector2(220f, 0f)
                && _director.Graph.Nodes[2].Position == new Vector2(440f, 0f)
                && _director.Graph.Nodes[3].Position == new Vector2(660f, 0f)
                && _director.Graph.Nodes[4].Position == new Vector2(880f, 0f);
        }

        private void MarkDirectorDirtyInEditor()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && _director != null)
            {
                UnityEditor.EditorUtility.SetDirty(_director);

                if (_director.gameObject.scene.IsValid())
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                        _director.gameObject.scene);
                }
            }
#endif
        }
    }
}