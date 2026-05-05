using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace IndieGabo.HandyTools.Editor.Utils
{
    /// <summary>
    /// Inspects and repairs the vendor Easy Save slots prefab after GUID
    /// rotation breaks binary script references.
    /// </summary>
    public static class EasySaveSlotsPrefabRepair
    {
        private const string PrefabPath
            = "Assets/Plugins/Easy Save 3/Scripts/Save Slots/Easy Save Slots Canvas.prefab";

        private const string ManagerTypeName = "ES3SlotManager, EasySave3";
        private const string CreateSlotTypeName = "ES3CreateSlot, EasySave3";
        private const string SlotTypeName = "ES3Slot, EasySave3";
        private const string SlotDialogTypeName = "ES3SlotDialog, EasySave3";
        private const string TmpTextTypeName = "TMPro.TMP_Text, Unity.TextMeshPro";
        private const string TmpInputFieldTypeName
            = "TMPro.TMP_InputField, Unity.TextMeshPro";

        /// <summary>
        /// Loads the prefab and prints its hierarchy plus missing-script state.
        /// </summary>
        public static void InspectPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                Debug.Log($"[EasySaveSlotsPrefabRepair] Missing scripts: {CountMissingScripts(root)}");
                LogHierarchy(root.transform, 0);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Repairs the Easy Save slots prefab by removing missing scripts,
        /// adding the current component types, and reconstructing references.
        /// </summary>
        public static void RepairPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

            try
            {
                int missingBefore = CountMissingScripts(root);
                int removed = RemoveMissingScripts(root);

                RepairContext context = BuildContext(root);
                RepairManager(context);
                RepairCreateSlot(context);
                RepairSlotTemplate(context);
                RepairDialog(context.CreateSlotDialog, context.CreateSlotDialogComponent, true);
                RepairDialog(
                    context.ConfirmOverwriteDialog,
                    context.ConfirmOverwriteDialogComponent,
                    false
                );

                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                int missingAfter = CountMissingScripts(root);
                Debug.Log(
                    "[EasySaveSlotsPrefabRepair] "
                    + $"missingBefore={missingBefore} removed={removed} missingAfter={missingAfter}"
                );
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Logs the current prefab hierarchy and component names.
        /// </summary>
        /// <param name="transform">Hierarchy node being logged.</param>
        /// <param name="depth">Current indentation depth.</param>
        private static void LogHierarchy(Transform transform, int depth)
        {
            List<string> componentNames = new();
            Component[] components = transform.GetComponents<Component>();
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                componentNames.Add(component == null ? "<Missing Script>" : component.GetType().Name);
            }

            string indent = new string(' ', depth * 2);
            Debug.Log($"[EasySaveSlotsPrefabRepair] {indent}- {transform.name}: {string.Join(", ", componentNames)}");

            for (int index = 0; index < transform.childCount; index++)
            {
                LogHierarchy(transform.GetChild(index), depth + 1);
            }
        }

        /// <summary>
        /// Counts missing MonoBehaviour references across the prefab hierarchy.
        /// </summary>
        /// <param name="root">Prefab root object.</param>
        /// <returns>Total number of missing scripts.</returns>
        private static int CountMissingScripts(GameObject root)
        {
            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                count += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                    transforms[index].gameObject
                );
            }

            return count;
        }

        /// <summary>
        /// Removes missing MonoBehaviour references across the prefab hierarchy.
        /// </summary>
        /// <param name="root">Prefab root object.</param>
        /// <returns>Total number of removed missing scripts.</returns>
        private static int RemoveMissingScripts(GameObject root)
        {
            int removed = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(
                    transforms[index].gameObject
                );
            }

            return removed;
        }

        /// <summary>
        /// Resolves the set of prefab objects and components used by the repair.
        /// </summary>
        /// <param name="root">Prefab root object.</param>
        /// <returns>The resolved repair context.</returns>
        private static RepairContext BuildContext(GameObject root)
        {
            Type managerType = RequireType(ManagerTypeName);
            Type createSlotType = RequireType(CreateSlotTypeName);
            Type slotType = RequireType(SlotTypeName);
            Type slotDialogType = RequireType(SlotDialogTypeName);

            GameObject managerHost = FindRequiredGameObject(root, "Easy Save Slots");
            GameObject createSlotButton = FindRequiredGameObject(root, "Create Slot Button");
            GameObject slotTemplate = FindRequiredGameObject(root, "Slot Template");
            GameObject createSlotDialog = FindRequiredGameObject(root, "Create Slot Dialog");
            GameObject confirmOverwriteDialog = FindRequiredGameObject(root, "Confirm Overwrite Dialog");
            GameObject errorDialog = FindRequiredGameObject(root, "Error Dialog");

            Component managerComponent = EnsureComponent(managerHost, managerType);
            Component createSlotComponent = EnsureComponent(createSlotButton, createSlotType);
            Component slotComponent = EnsureComponent(slotTemplate, slotType);
            Component createSlotDialogComponent = EnsureComponent(createSlotDialog, slotDialogType);
            Component confirmOverwriteDialogComponent = EnsureComponent(confirmOverwriteDialog, slotDialogType);

            return new RepairContext(
                root,
                managerHost,
                managerComponent,
                createSlotButton,
                createSlotComponent,
                slotTemplate,
                slotComponent,
                createSlotDialog,
                createSlotDialogComponent,
                confirmOverwriteDialog,
                confirmOverwriteDialogComponent,
                errorDialog
            );
        }

        /// <summary>
        /// Rebuilds the slot-manager component bindings.
        /// </summary>
        /// <param name="context">Resolved repair context.</param>
        private static void RepairManager(RepairContext context)
        {
            SetField(context.ManagerComponent, "showConfirmationIfExists", true);
            SetField(context.ManagerComponent, "showCreateSlotButton", true);
            SetField(context.ManagerComponent, "autoCreateSaveFile", false);
            SetField(context.ManagerComponent, "selectSlotAfterCreation", false);
            SetField(context.ManagerComponent, "loadSceneAfterSelectSlot", string.Empty);
            SetField(context.ManagerComponent, "slotDirectory", "slots/");
            SetField(context.ManagerComponent, "slotExtension", ".es3");
            SetField(context.ManagerComponent, "slotTemplate", context.SlotTemplate);
            SetField(context.ManagerComponent, "createDialog", context.CreateSlotDialog);
            SetField(context.ManagerComponent, "errorDialog", context.ErrorDialog);
            SetField(context.ManagerComponent, "slots", new List<GameObject>());
        }

        /// <summary>
        /// Rebuilds the create-slot button component bindings.
        /// </summary>
        /// <param name="context">Resolved repair context.</param>
        private static void RepairCreateSlot(RepairContext context)
        {
            Type tmpInputFieldType = RequireType(TmpInputFieldTypeName);

            Button createButton = RequireComponent<Button>(context.CreateSlotButton);
            Component inputField = RequireDescendantComponent(
                context.CreateSlotDialog,
                tmpInputFieldType
            );

            SetField(context.CreateSlotComponent, "createButton", createButton);
            SetField(
                context.CreateSlotComponent,
                "createDialog",
                context.CreateSlotDialogComponent
            );
            SetField(context.CreateSlotComponent, "inputField", inputField);
            SetField(context.CreateSlotComponent, "mgr", context.ManagerComponent);
        }

        /// <summary>
        /// Rebuilds the slot template component bindings.
        /// </summary>
        /// <param name="context">Resolved repair context.</param>
        private static void RepairSlotTemplate(RepairContext context)
        {
            Type tmpTextType = RequireType(TmpTextTypeName);

            Component nameLabel = FindRequiredDescendantComponentByNameContains(
                context.SlotTemplate,
                tmpTextType,
                "name"
            );
            Component timestampLabel = FindAnotherRequiredDescendantComponent(
                context.SlotTemplate,
                tmpTextType,
                nameLabel
            );

            List<Button> buttons = new(
                context.SlotTemplate.GetComponentsInChildren<Button>(true)
            );

            Button selectButton = FindButtonByNameContains(buttons, "choose")
                ?? FindButtonByNameContains(buttons, "select")
                ?? buttons[0];
            Button deleteButton = FindButtonByNameContains(buttons, "delete");
            Button undoButton = FindButtonByNameContains(buttons, "undo");

            if (deleteButton == null || undoButton == null)
            {
                List<Button> remainingButtons = new();
                for (int index = 0; index < buttons.Count; index++)
                {
                    Button button = buttons[index];
                    if (button != selectButton && button != deleteButton && button != undoButton)
                    {
                        remainingButtons.Add(button);
                    }
                }

                if (deleteButton == null && remainingButtons.Count > 0)
                {
                    deleteButton = remainingButtons[0];
                }

                if (undoButton == null)
                {
                    for (int index = remainingButtons.Count - 1; index >= 0; index--)
                    {
                        if (remainingButtons[index] != deleteButton)
                        {
                            undoButton = remainingButtons[index];
                            break;
                        }
                    }
                }
            }

            if (deleteButton == null || undoButton == null)
            {
                throw new InvalidOperationException(
                    "Could not resolve the Slot Template delete and undo buttons."
                );
            }

            SetField(context.SlotComponent, "nameLabel", nameLabel);
            SetField(context.SlotComponent, "timestampLabel", timestampLabel);
            SetField(
                context.SlotComponent,
                "confirmationDialog",
                context.ConfirmOverwriteDialog
            );
            SetField(context.SlotComponent, "mgr", context.ManagerComponent);
            SetField(context.SlotComponent, "selectButton", selectButton);
            SetField(context.SlotComponent, "deleteButton", deleteButton);
            SetField(context.SlotComponent, "undoButton", undoButton);
            SetField(context.SlotComponent, "markedForDeletion", false);
        }

        /// <summary>
        /// Rebuilds one dialog component binding set.
        /// </summary>
        /// <param name="dialogObject">Dialog root object.</param>
        /// <param name="dialogComponent">Dialog component instance.</param>
        /// <param name="preferCreateButton">
        /// True when the dialog uses a create button instead of a confirm one.
        /// </param>
        private static void RepairDialog(
            GameObject dialogObject,
            Component dialogComponent,
            bool preferCreateButton
        )
        {
            List<Button> buttons = new(dialogObject.GetComponentsInChildren<Button>(true));
            Button confirmButton = FindButtonByNameContains(buttons, "confirm");
            if (confirmButton == null && preferCreateButton)
            {
                confirmButton = FindButtonByNameContains(buttons, "create");
            }

            Button cancelButton = FindButtonByNameContains(buttons, "cancel");

            if (confirmButton == null && buttons.Count > 0)
            {
                confirmButton = buttons[0];
            }

            if (cancelButton == null)
            {
                for (int index = buttons.Count - 1; index >= 0; index--)
                {
                    if (buttons[index] != confirmButton)
                    {
                        cancelButton = buttons[index];
                        break;
                    }
                }
            }

            if (confirmButton == null || cancelButton == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve dialog buttons for '{dialogObject.name}'."
                );
            }

            SetField(dialogComponent, "confirmButton", confirmButton);
            SetField(dialogComponent, "cancelButton", cancelButton);
        }

        /// <summary>
        /// Finds the first optional child object with the provided name.
        /// </summary>
        /// <param name="root">Prefab root object.</param>
        /// <param name="name">Exact object name.</param>
        /// <returns>The matching object, or null when absent.</returns>
        private static GameObject FindOptionalGameObject(GameObject root, string name)
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (transforms[index].name == name)
                {
                    return transforms[index].gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds one required child object by exact name.
        /// </summary>
        /// <param name="root">Prefab root object.</param>
        /// <param name="name">Exact object name.</param>
        /// <returns>The matching object.</returns>
        private static GameObject FindRequiredGameObject(GameObject root, string name)
        {
            GameObject match = FindOptionalGameObject(root, name);
            if (match == null)
            {
                throw new InvalidOperationException(
                    $"Could not find '{name}' inside the Easy Save slots prefab."
                );
            }

            return match;
        }

        /// <summary>
        /// Finds one required descendant component by name fragment.
        /// </summary>
        /// <param name="root">Root object searched for descendants.</param>
        /// <param name="componentType">Component type to locate.</param>
        /// <param name="nameFragment">Case-insensitive name fragment.</param>
        /// <returns>The matching component.</returns>
        private static Component FindRequiredDescendantComponentByNameContains(
            GameObject root,
            Type componentType,
            string nameFragment
        )
        {
            Component[] components = root.GetComponentsInChildren(componentType, true);
            for (int index = 0; index < components.Length; index++)
            {
                Component component = components[index];
                if (component.gameObject.name.IndexOf(
                        nameFragment,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    return component;
                }
            }

            throw new InvalidOperationException(
                $"Could not find a {componentType.Name} containing '{nameFragment}' under '{root.name}'."
            );
        }

        /// <summary>
        /// Finds one descendant component of the specified type that is not the
        /// excluded instance.
        /// </summary>
        /// <param name="root">Root object searched for descendants.</param>
        /// <param name="componentType">Component type to locate.</param>
        /// <param name="excluded">Component instance to skip.</param>
        /// <returns>The first non-excluded matching component.</returns>
        private static Component FindAnotherRequiredDescendantComponent(
            GameObject root,
            Type componentType,
            Component excluded
        )
        {
            Component[] components = root.GetComponentsInChildren(componentType, true);
            for (int index = 0; index < components.Length; index++)
            {
                if (components[index] != excluded)
                {
                    return components[index];
                }
            }

            throw new InvalidOperationException(
                $"Could not find a second {componentType.Name} under '{root.name}'."
            );
        }

        /// <summary>
        /// Finds the first descendant component of the provided type.
        /// </summary>
        /// <param name="root">Root object searched for descendants.</param>
        /// <param name="componentType">Component type to locate.</param>
        /// <returns>The matching component.</returns>
        private static Component RequireDescendantComponent(
            GameObject root,
            Type componentType
        )
        {
            Component[] components = root.GetComponentsInChildren(componentType, true);
            if (components.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Could not find a {componentType.Name} under '{root.name}'."
                );
            }

            return components[0];
        }

        /// <summary>
        /// Finds the first button whose name contains the provided fragment.
        /// </summary>
        /// <param name="buttons">Buttons searched in order.</param>
        /// <param name="nameFragment">Case-insensitive name fragment.</param>
        /// <returns>The matching button, or null when absent.</returns>
        private static Button FindButtonByNameContains(
            IList<Button> buttons,
            string nameFragment
        )
        {
            for (int index = 0; index < buttons.Count; index++)
            {
                if (buttons[index].gameObject.name.IndexOf(
                        nameFragment,
                        StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    return buttons[index];
                }
            }

            return null;
        }

        /// <summary>
        /// Ensures a component of the specified type exists on the host object.
        /// </summary>
        /// <param name="host">GameObject receiving the component.</param>
        /// <param name="componentType">Component type to ensure.</param>
        /// <returns>The existing or newly added component.</returns>
        private static Component EnsureComponent(GameObject host, Type componentType)
        {
            Component component = host.GetComponent(componentType);
            return component ?? host.AddComponent(componentType);
        }

        /// <summary>
        /// Resolves one required component type by assembly-qualified name.
        /// </summary>
        /// <param name="assemblyQualifiedTypeName">Assembly-qualified type name.</param>
        /// <returns>The resolved runtime type.</returns>
        private static Type RequireType(string assemblyQualifiedTypeName)
        {
            Type type = Type.GetType(assemblyQualifiedTypeName);
            if (type == null)
            {
                throw new InvalidOperationException(
                    $"Could not resolve type '{assemblyQualifiedTypeName}'."
                );
            }

            return type;
        }

        /// <summary>
        /// Resolves one required component on a specific object.
        /// </summary>
        /// <typeparam name="T">Component type to resolve.</typeparam>
        /// <param name="gameObject">Host object.</param>
        /// <returns>The existing component instance.</returns>
        private static T RequireComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"Could not find required component '{typeof(T).Name}' on '{gameObject.name}'."
                );
            }

            return component;
        }

        /// <summary>
        /// Assigns one serialized field through reflection.
        /// </summary>
        /// <param name="target">Component instance whose field is assigned.</param>
        /// <param name="fieldName">Field name to assign.</param>
        /// <param name="value">Assigned value.</param>
        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field == null)
            {
                throw new InvalidOperationException(
                    $"Could not find field '{fieldName}' on '{target.GetType().FullName}'."
                );
            }

            field.SetValue(target, value);
            if (target is UnityEngine.Object unityObject)
            {
                EditorUtility.SetDirty(unityObject);
            }
        }

        /// <summary>
        /// Stores the prefab objects and components used during repair.
        /// </summary>
        private sealed class RepairContext
        {
            /// <summary>
            /// Initializes the repair context.
            /// </summary>
            /// <param name="prefabRoot">Prefab root object.</param>
            /// <param name="managerHost">Host object for ES3SlotManager.</param>
            /// <param name="managerComponent">ES3SlotManager instance.</param>
            /// <param name="createSlotButton">Create Slot Button object.</param>
            /// <param name="createSlotComponent">ES3CreateSlot instance.</param>
            /// <param name="slotTemplate">Slot Template object.</param>
            /// <param name="slotComponent">ES3Slot instance.</param>
            /// <param name="createSlotDialog">Create Slot Dialog object.</param>
            /// <param name="createSlotDialogComponent">Create dialog component.</param>
            /// <param name="confirmOverwriteDialog">Confirm Overwrite Dialog object.</param>
            /// <param name="confirmOverwriteDialogComponent">Confirm dialog component.</param>
            /// <param name="errorDialog">Error Dialog object.</param>
            public RepairContext(
                GameObject prefabRoot,
                GameObject managerHost,
                Component managerComponent,
                GameObject createSlotButton,
                Component createSlotComponent,
                GameObject slotTemplate,
                Component slotComponent,
                GameObject createSlotDialog,
                Component createSlotDialogComponent,
                GameObject confirmOverwriteDialog,
                Component confirmOverwriteDialogComponent,
                GameObject errorDialog
            )
            {
                PrefabRoot = prefabRoot;
                ManagerHost = managerHost;
                ManagerComponent = managerComponent;
                CreateSlotButton = createSlotButton;
                CreateSlotComponent = createSlotComponent;
                SlotTemplate = slotTemplate;
                SlotComponent = slotComponent;
                CreateSlotDialog = createSlotDialog;
                CreateSlotDialogComponent = createSlotDialogComponent;
                ConfirmOverwriteDialog = confirmOverwriteDialog;
                ConfirmOverwriteDialogComponent = confirmOverwriteDialogComponent;
                ErrorDialog = errorDialog;
            }

            /// <summary>
            /// Gets the prefab root object.
            /// </summary>
            public GameObject PrefabRoot { get; }

            /// <summary>
            /// Gets the manager host object.
            /// </summary>
            public GameObject ManagerHost { get; }

            /// <summary>
            /// Gets the slot manager component instance.
            /// </summary>
            public Component ManagerComponent { get; }

            /// <summary>
            /// Gets the Create Slot Button object.
            /// </summary>
            public GameObject CreateSlotButton { get; }

            /// <summary>
            /// Gets the create-slot component instance.
            /// </summary>
            public Component CreateSlotComponent { get; }

            /// <summary>
            /// Gets the Slot Template object.
            /// </summary>
            public GameObject SlotTemplate { get; }

            /// <summary>
            /// Gets the slot component instance.
            /// </summary>
            public Component SlotComponent { get; }

            /// <summary>
            /// Gets the Create Slot Dialog object.
            /// </summary>
            public GameObject CreateSlotDialog { get; }

            /// <summary>
            /// Gets the Create Slot Dialog component instance.
            /// </summary>
            public Component CreateSlotDialogComponent { get; }

            /// <summary>
            /// Gets the Confirm Overwrite Dialog object.
            /// </summary>
            public GameObject ConfirmOverwriteDialog { get; }

            /// <summary>
            /// Gets the Confirm Overwrite Dialog component instance.
            /// </summary>
            public Component ConfirmOverwriteDialogComponent { get; }

            /// <summary>
            /// Gets the Error Dialog object.
            /// </summary>
            public GameObject ErrorDialog { get; }
        }
    }
}