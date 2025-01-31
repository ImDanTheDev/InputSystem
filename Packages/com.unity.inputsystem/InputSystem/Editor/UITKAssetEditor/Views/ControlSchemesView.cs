#if UNITY_EDITOR && UNITY_2022_1_OR_NEWER
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace UnityEngine.InputSystem.Editor
{
    internal class ControlSchemesView : ViewBase<InputControlScheme>
    {
        public ControlSchemesView(VisualElement root, StateContainer stateContainer, bool updateExisting = false)
            : base(stateContainer)
        {
            m_Root = root;
            m_UpdateExisting = updateExisting;

            var controlSchemeEditor = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                InputActionsEditorConstants.PackagePath +
                InputActionsEditorConstants.ResourcesPath +
                InputActionsEditorConstants.ControlSchemeEditorViewUxml);

            var controlSchemeVisualElement = controlSchemeEditor.CloneTree();
            controlSchemeVisualElement.Q<Button>(kCancelButton).clicked += Close;
            controlSchemeVisualElement.Q<Button>(kSaveButton).clicked += SaveAndClose;
            controlSchemeVisualElement.Q<TextField>(kControlSchemeNameTextField).RegisterCallback<FocusOutEvent>(evt =>
            {
                Dispatch(ControlSchemeCommands.ChangeSelectedControlSchemeName(((TextField)evt.currentTarget).value));
            });

            m_ModalWindow = new VisualElement
            {
                style = { position = new StyleEnum<Position>(Position.Absolute) }
            };
            var popupWindow = new PopupWindow
            {
                text = "Add Control Scheme",
                style = { position = new StyleEnum<Position>(Position.Absolute) }
            };
            popupWindow.contentContainer.Add(controlSchemeVisualElement);
            m_ModalWindow.Add(popupWindow);
            m_Root.Add(m_ModalWindow);
            m_ModalWindow.StretchToParentSize();
            m_ModalWindow.RegisterCallback<ClickEvent>(evt => Close());
            popupWindow.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());

            m_ListView = controlSchemeVisualElement.Q<MultiColumnListView>(kControlSchemesListView);
            m_ListView.columns[kDeviceTypeColumnName].makeCell = () => new Label();
            m_ListView.columns[kDeviceTypeColumnName].bindCell = BindDeviceTypeCell;

            m_ListView.columns[kRequiredColumnName].makeCell = MakeRequiredCell;
            m_ListView.columns[kRequiredColumnName].bindCell = BindDeviceRequiredCell;
            m_ListView.columns[kRequiredColumnName].unbindCell = UnbindDeviceRequiredCell;

            m_ListView.Q<Button>(kUnityListViewAddButton).clickable = new Clickable(AddDeviceRequirement);
            m_ListView.Q<Button>(kUnityListViewRemoveButton).clickable = new Clickable(RemoveDeviceRequirement);

            m_ListView.itemIndexChanged += (oldPosition, newPosition) =>
            {
                Dispatch(ControlSchemeCommands.ReorderDeviceRequirements(oldPosition, newPosition));
            };

            m_ListView.itemsSource = new List<string>();

            CreateSelector(s => s.selectedControlScheme,
                (_, s) => s.selectedControlScheme);
        }

        private void AddDeviceRequirement()
        {
            var dropdown = new InputControlPickerDropdown(new InputControlPickerState(), path =>
            {
                var requirement = new InputControlScheme.DeviceRequirement { controlPath = path, isOptional = false };
                Dispatch(ControlSchemeCommands.AddDeviceRequirement(requirement));
            }, mode: InputControlPicker.Mode.PickDevice);
            dropdown.Show(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private void RemoveDeviceRequirement()
        {
            if (m_ListView.selectedIndex == -1)
                return;

            Dispatch(ControlSchemeCommands.RemoveDeviceRequirement(m_ListView.selectedIndex));
        }

        public override void RedrawUI(InputControlScheme viewState)
        {
            m_Root.Q<TextField>(kControlSchemeNameTextField).value = viewState.name;

            m_ListView.itemsSource?.Clear();
            m_ListView.itemsSource = viewState.deviceRequirements.Count > 0 ?
                viewState.deviceRequirements.Select(r => (r.controlPath, r.isOptional)).ToList() :
                new List<(string, bool)>();
            m_ListView.Rebuild();
        }

        public override void DestroyView()
        {
            m_ModalWindow.RemoveFromHierarchy();
        }

        private void SaveAndClose()
        {
            Dispatch(ControlSchemeCommands.SaveControlScheme(m_UpdateExisting));
            Close();
        }

        private VisualElement MakeRequiredCell()
        {
            var ve = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    alignContent = new StyleEnum<Align>(Align.Center)
                }
            };
            ve.Add(new Toggle());
            return ve;
        }

        private void BindDeviceRequiredCell(VisualElement visualElement, int rowIndex)
        {
            var toggle = visualElement.Q<Toggle>();
            var rowItem = ((string path, bool optional))m_ListView.itemsSource[rowIndex];

            toggle.value = !rowItem.optional;
            var eventCallback = (EventCallback<ChangeEvent<bool>>)(evt =>
                Dispatch(ControlSchemeCommands.ChangeDeviceRequirement(rowIndex, evt.newValue)));
            toggle.userData = eventCallback;

            toggle.RegisterValueChangedCallback(eventCallback);
        }

        private void UnbindDeviceRequiredCell(VisualElement visualElement, int rowIndex)
        {
            var toggle = visualElement.Q<Toggle>();
            toggle.UnregisterValueChangedCallback((EventCallback<ChangeEvent<bool>>)toggle.userData);
        }

        private void BindDeviceTypeCell(VisualElement visualElement, int rowIndex)
        {
            ((Label)visualElement).text = (((string, bool))m_ListView.itemsSource[rowIndex]).Item1;
        }

        private readonly VisualElement m_Root;
        private readonly bool m_UpdateExisting;
        private MultiColumnListView m_ListView;
        private VisualElement m_ModalWindow;

        private const string kControlSchemeNameTextField = "control-scheme-name";
        private const string kCancelButton = "cancel-button";
        private const string kSaveButton = "save-button";
        private const string kControlSchemesListView = "control-schemes-list-view";
        private const string kDeviceTypeColumnName = "device-type";
        private const string kRequiredColumnName = "required";
        private const string kUnityListViewAddButton = "unity-list-view__add-button";
        private const string kUnityListViewRemoveButton = "unity-list-view__remove-button";
    }
}

#endif
