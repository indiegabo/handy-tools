using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IndieGabo.HandyTools.Utils
{

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    /// <summary>
    /// Draws a styled separator row inside the Unity hierarchy window.
    /// </summary>
    public class HierarchySeparator : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        private Color m_OutlineColor = Color.black;

        /// <summary>
        /// Gets or sets the outline color used by the separator.
        /// </summary>
        public Color OutlineColor
        {
            get => m_OutlineColor;
            set
            {
                value.a = 1f;
                m_OutlineColor = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        private Color m_BarColor = Color.black;

        /// <summary>
        /// Gets or sets the fill color used by the separator bar.
        /// </summary>
        public Color BarColor
        {
            get => m_BarColor;
            set
            {
                value.a = 1f;
                m_BarColor = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        private Color m_TextColor = Color.white;

        /// <summary>
        /// Gets or sets the text color used by the separator label.
        /// </summary>
        public Color TextColor
        {
            get => m_TextColor;
            set
            {
                value.a = 1f;
                m_TextColor = value;
            }
        }

        [HideInInspector]
        [SerializeField]
        private int m_OutlineSize = 0;

        /// <summary>
        /// Gets or sets the outline thickness used by the separator.
        /// </summary>
        public int OutlineSize
        {
            get => m_OutlineSize;
            set
            {
                m_OutlineSize = value;
            }
        }

        /// <summary>
        /// Destroys the runtime separator object so it remains editor-only.
        /// </summary>
        public void Awake()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// Marks the separator object as editor-only.
        /// </summary>
        public void Reset()
        {
            gameObject.tag = "EditorOnly";
        }

#if UNITY_EDITOR
        static HierarchySeparator()
        {
            EditorApplication.hierarchyWindowItemByEntityIdOnGUI += HierarchyWindowItemOnGUI;
        }

        /// <summary>
        /// Creates a new hierarchy separator GameObject.
        /// </summary>
        /// <param name="menuCommand">Menu command context used for parenting.</param>
        [MenuItem("GameObject/Separator", false, 30)]
        public static void CreateSeparator(MenuCommand menuCommand)
        {
            GameObject separator = new GameObject("Separator");
            separator.AddComponent<HierarchySeparator>();
            GameObjectUtility.SetParentAndAlign(separator, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(separator, "Create " + separator.name);
            Selection.activeObject = separator;
        }

        static void HierarchyWindowItemOnGUI(EntityId entityId, Rect selectionRect)
        {
            GameObject gameObject = EditorUtility.EntityIdToObject(entityId) as GameObject;

            if (gameObject == null) return;
            if (!gameObject.TryGetComponent(out HierarchySeparator hierarchy)) return;

            GUIStyle guiStyle = new GUIStyle();
            guiStyle.fontStyle = FontStyle.Bold;
            guiStyle.normal.textColor = hierarchy.TextColor;
            guiStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUI.DrawRect(selectionRect, hierarchy.OutlineColor);
            EditorGUI.DrawRect(new Rect(selectionRect.x + hierarchy.OutlineSize, selectionRect.y + hierarchy.OutlineSize, selectionRect.width - (hierarchy.OutlineSize * 2), selectionRect.height - (hierarchy.OutlineSize * 2)), hierarchy.BarColor);
            EditorGUI.DropShadowLabel(selectionRect, $"{gameObject.name.ToUpperInvariant()}", guiStyle);
        }

        void OnValidate()
        {
            EditorApplication.RepaintHierarchyWindow();
        }
#endif
    }
}