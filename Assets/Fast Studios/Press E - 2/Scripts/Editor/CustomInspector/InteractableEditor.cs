using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;
using FastStudios.EditorTools;

namespace FastStudios
{
    [CustomEditor(typeof(Interactable)), CanEditMultipleObjects]
    public class InteractableEditor : Editor, IHasCustomMenu
    {
        private VisualElement _root;
        public VisualTreeAsset visualTree;
        public StyleSheet USS;
        private int interactionTab = 1;

        private const int MainTabIndex = 100;
        private const int UITabIndex = 101;
        private const int EventsTabIndex = 102;
        private const int SettingsTabIndex = 103;

        private int selectedTab = MainTabIndex;

        private const string FoldoutContainerClassName = "FoldoutContainer";
        private const string HiddenClass = "Hide";
        Interactable localTarget;

        public Color normalTabColor = new Color(0.345098f, 0.345098f, 0.345098f);
        public Color selectedTabColor = new Color(0.4117647f, 0.4117647f, 0.4117647f);

        private readonly List<(string key, VisualElement header, VisualElement container, SerializedProperty sp)> _autoFoldouts = new();

        void LoadLastSession(Interactable lTarget)
        {
            if (lTarget.lastSelectedTab >= 5 && lTarget.lastSelectedTab <= 8)
                lTarget.lastSelectedTab = MainTabIndex + (lTarget.lastSelectedTab - 5);

            if (lTarget.lastSelectedTab < MainTabIndex)
                lTarget.lastSelectedTab = MainTabIndex;

            selectedTab = lTarget.lastSelectedTab;
        }

        public override VisualElement CreateInspectorGUI()
        {
            localTarget = target as Interactable;

            VisualElement root = new VisualElement();
            visualTree.CloneTree(root);
            _root = root;

            LoadLastSession(localTarget);

            if (USS != null) root.styleSheets.Add(USS);

            VisualElement MainVars = root.Q<VisualElement>(name = "MainVars");
            VisualElement Upper = MainVars.Q<VisualElement>(name = "Upper");
            VisualElement Lower = MainVars.Q<VisualElement>(name = "Lower");
            VisualElement TabsParent = Upper.Q<VisualElement>(name = "Tabs");
            PropertyField InteractionParent = Upper.Q<PropertyField>(name = "interactMode");
            VisualElement[] allVisuals = { root, MainVars, Upper, Lower, TabsParent, InteractionParent };

            root.Bind(serializedObject);

            var interactProp = serializedObject.FindProperty("interactMode");
            root.TrackPropertyValue(interactProp, _ =>
            {
                UpdateInteractionTab(localTarget, root);
            });

            Button mainTabButton = TabsParent.Q<Button>(name = "MainButton");
            Button UIButton = TabsParent.Q<Button>(name = "UIButton");
            Button eventsButton = TabsParent.Q<Button>(name = "EventsButton");
            Button settingsButton = TabsParent.Q<Button>(name = "SettingsButton");
            Button[] allButtons = { mainTabButton, UIButton, eventsButton, settingsButton };

            SetupPageChange(mainTabButton, MainTabIndex, root, allButtons, allVisuals);
            SetupPageChange(UIButton, UITabIndex, root, allButtons, allVisuals);
            SetupPageChange(eventsButton, EventsTabIndex, root, allButtons, allVisuals);
            SetupPageChange(settingsButton, SettingsTabIndex, root, allButtons, allVisuals);

            VisualElement HoldTab = Lower.Q<VisualElement>(name = "HoldTab");
            VisualElement GrabTab = Lower.Q<VisualElement>(name = "GrabTab");
            VisualElement DragTab = Lower.Q<VisualElement>(name = "DragTab");
            VisualElement InspectionTab = Lower.Q<VisualElement>(name = "InspectionTab");
            VisualElement UITabs = Lower.Q<VisualElement>(name = "UITabs");
            VisualElement EventsTabs = Lower.Q<VisualElement>(name = "EventsTabs");
            VisualElement SettingsTabs = Lower.Q<VisualElement>(name = "SettingsTabs");
            VisualElement KeySection = Lower.Q<VisualElement>(name = "KeysSectionForMain");
            VisualElement ConditionSection = Lower.Q<VisualElement>(name = "ConditionsSectionForMain");

            FSEditorUI.AutoFoldouts(
                root,
                serializedObject,
                (key, deflt) => localTarget.GetFoldout(key, deflt),
                (key, open) => localTarget.SetFoldout(key, open),
                FSEditorUI.HiddenClass
            );

            UpdateInteractionTab(localTarget, root);
            SetTabIndex(selectedTab, root, allButtons, allVisuals);

            // Show ifs
            {
                // Conditions
                VisualElement ConditionsContaienr = ConditionSection.Q<VisualElement>(name = "ConditionsContainer");
                VisualElement ConditionsList = ConditionsContaienr.Q<VisualElement>(name = "Conditions");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.UseConditions), new[] { ConditionsList });

                //Hold
                VisualElement hasMaxHoldInteractions = HoldTab.Q<VisualElement>(name = "maxHoldInteractions");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasMaxHoldInteractions), hasMaxHoldInteractions);

                // Grab
                VisualElement GrabDistanceMinMax = GrabTab.Q<VisualElement>(name = "GrabDistanceMinMax");
                VisualElement ScrollMultiplier = GrabTab.Q<VisualElement>(name = "ScrollMultiplier");
                VisualElement newGrabRotation = GrabTab.Q<VisualElement>(name = "newGrabRotation");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.ScrolledBasedDistance), new[] { GrabDistanceMinMax, ScrollMultiplier });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideGrabRotation), new[] { newGrabRotation });

                VisualElement PlayerHelperForce = GrabTab.Q<VisualElement>(name = "PlayerHelperForce");
                VisualElement OverrideAnchorPosition = GrabTab.Q<VisualElement>(name = "OverrideAnchorPosition");
                VisualElement linearDamping = GrabTab.Q<VisualElement>(name = "linearDamping");
                VisualElement angularDamping = GrabTab.Q<VisualElement>(name = "angularDamping");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.PhysicsGrabMode), new[] { PlayerHelperForce, OverrideAnchorPosition, linearDamping, angularDamping });

                VisualElement VisualizeObject = GrabTab.Q<VisualElement>(name = "VisualizeObject");
                VisualElement UseManagerMaterials = GrabTab.Q<VisualElement>(name = "UseManagerMaterials");
                VisualElement CheckCollisionBeforePlacing = GrabTab.Q<VisualElement>(name = "CheckCollisionBeforePlacing");
                VisualElement PreventDropping = GrabTab.Q<VisualElement>(name = "PreventDropping");
                VisualElement AlignNormals = GrabTab.Q<VisualElement>(name = "AlignNormals");
                VisualElement CanRotatePlaceObject = GrabTab.Q<VisualElement>(name = "CanRotatePlaceObject");
                VisualElement RotationAxis = GrabTab.Q<VisualElement>(name = "RotationAxis");
                VisualElement CalculateOnLocal = GrabTab.Q<VisualElement>(name = "CalculateOnLocal");
                VisualElement OverrideTurnKeys = GrabTab.Q<VisualElement>(name = "OverrideTurnKeys");
                VisualElement NewTurnLeft = GrabTab.Q<VisualElement>(name = "NewTurnLeft");
                VisualElement NewTurnRight = GrabTab.Q<VisualElement>(name = "NewTurnRight");
                VisualElement PressDown = GrabTab.Q<VisualElement>(name = "PressDown");
                VisualElement RotationIncrement = GrabTab.Q<VisualElement>(name = "RotationIncrement");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.CanPlaceGrabDown), new[] { CheckCollisionBeforePlacing, VisualizeObject, PreventDropping, UseManagerMaterials, AlignNormals, CanRotatePlaceObject, RotationAxis, CalculateOnLocal, OverrideTurnKeys, NewTurnLeft, NewTurnRight, PressDown, RotationIncrement });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.CanRotatePlaceObject), "Hide2", new[] { RotationAxis, CalculateOnLocal, OverrideTurnKeys, NewTurnLeft, NewTurnRight, PressDown, RotationIncrement });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.VisualizeObject), "Hide2", new[] { UseManagerMaterials });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.PressDown), "Hide3", new[] { RotationIncrement });

                VisualElement OverrideGrabRotateKeys = GrabTab.Q<VisualElement>(name = "OverrideGrabRotateKeys");
                VisualElement NewGrabRotation = GrabTab.Q<VisualElement>(name = "NewGrabRotation");
                VisualElement RotationSensitivity = GrabTab.Q<VisualElement>(name = "RotationSensitivity");
                VisualElement GrabShowCursorWhenRotating = GrabTab.Q<VisualElement>(name = "GrabShowCursorWhenRotating");
                VisualElement GrabDontHideCursorOnStop = GrabTab.Q<VisualElement>(name = "GrabDontHideCursorOnStop");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.CanRotateGrab), new[] { OverrideGrabRotateKeys, NewGrabRotation, RotationSensitivity, GrabShowCursorWhenRotating, GrabDontHideCursorOnStop });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideGrabRotateKeys), "Hide3", new[] { NewGrabRotation });

                VisualElement OverrideManagerTransform = GrabTab.Q<VisualElement>(name = "OverrideManagerTransform");
                VisualElement GrabTransform = GrabTab.Q<VisualElement>(name = "GrabTransform");
                VisualElement GrabSpace = GrabTab.Q<VisualElement>(name = "GrabSpace");
                VisualElement TransformGrabInstantFollow = GrabTab.Q<VisualElement>(name = "TransformGrabInstantFollow");
                VisualElement TransformGrabFollowSharpness = GrabTab.Q<VisualElement>(name = "TransformGrabFollowSharpness");
                VisualElement TransformGrabRotationSharpness = GrabTab.Q<VisualElement>(name = "TransformGrabRotationSharpness");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.UseTransformToPosition), new[] { OverrideManagerTransform, GrabTransform, GrabSpace, TransformGrabInstantFollow, TransformGrabFollowSharpness, TransformGrabRotationSharpness });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideManagerTransform), "Hide2", new[] { GrabTransform });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.TransformGrabInstantFollow == false; }, new[] { TransformGrabFollowSharpness, TransformGrabRotationSharpness }, serializedObject, new[] { "TransformGrabInstantFollow" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.PhysicsGrabMode == false; }, new[] { TransformGrabInstantFollow }, serializedObject, new[] { "PhysicsGrabMode" }, "Hide2");

                VisualElement ThrowContainer = GrabTab.Q<VisualElement>(name = "ThrowContainer");
                List<VisualElement> ThrowSpaces = ThrowContainer.Query<VisualElement>(name = "Space").ToList();

                VisualElement throwForce = GrabTab.Q<VisualElement>(name = "throwForce");
                VisualElement ThrowTowardsAim = GrabTab.Q<VisualElement>(name = "ThrowTowardsAim");
                VisualElement ThrowUseRaycastAim = GrabTab.Q<VisualElement>(name = "ThrowUseRaycastAim");
                VisualElement ThrowAimDistance = GrabTab.Q<VisualElement>(name = "ThrowAimDistance");
                VisualElement TimePressedIncreaseForce = GrabTab.Q<VisualElement>(name = "TimePressedIncreaseForce");
                VisualElement ForceClamp = GrabTab.Q<VisualElement>(name = "ForceClamp");
                VisualElement TimeToReachMaxForce = GrabTab.Q<VisualElement>(name = "TimeToReachMaxForce");
                VisualElement hasThrowAnimationCurve = GrabTab.Q<VisualElement>(name = "hasThrowAnimationCurve");
                VisualElement ThrowAnimationCurve = GrabTab.Q<VisualElement>(name = "ThrowAnimationCurve");
                VisualElement canSeeThrowTrajectory = GrabTab.Q<VisualElement>(name = "canSeeThrowTrajectory");
                VisualElement OnlySeeTrajectoryOnForceIncrease = GrabTab.Q<VisualElement>(name = "OnlySeeTrajectoryOnForceIncrease");
                VisualElement LinePoints = GrabTab.Q<VisualElement>(name = "LinePoints");
                VisualElement TrajectoryPrecision = GrabTab.Q<VisualElement>(name = "TrajectoryPrecision");
                VisualElement LineMaterial = GrabTab.Q<VisualElement>(name = "LineMaterial");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.isThrowable), new[] { throwForce, ThrowTowardsAim,        ThrowUseRaycastAim,     ThrowAimDistance,    TimePressedIncreaseForce,
                                                                      ForceClamp, TimeToReachMaxForce,    hasThrowAnimationCurve, ThrowAnimationCurve, canSeeThrowTrajectory,
                                                                      OnlySeeTrajectoryOnForceIncrease,   LinePoints,             TrajectoryPrecision, LineMaterial});
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.TimePressedIncreaseForce), "Hide2", new[] { ForceClamp, TimeToReachMaxForce, hasThrowAnimationCurve, ThrowAnimationCurve });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.canSeeThrowTrajectory), "Hide2", new[] { OnlySeeTrajectoryOnForceIncrease, LinePoints, TrajectoryPrecision, LineMaterial });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasThrowAnimationCurve), "Hide3", new[] { ThrowAnimationCurve });

                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.ThrowTowardsAim && !localTarget.ThrowUseRaycastAim; }, new[] { ThrowAimDistance }, serializedObject, new[] { "ThrowTowardsAim", "ThrowUseRaycastAim" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.TimePressedIncreaseForce && !localTarget.canSeeThrowTrajectory; }, new[] { OnlySeeTrajectoryOnForceIncrease }, serializedObject, new[] { "TimePressedIncreaseForce", "canSeeThrowTrajectory" }, "Hide3");

                foreach (VisualElement space in ThrowSpaces)
                {
                    FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.isThrowable), space);
                }

                // Drag
                VisualElement DragPositionContainer = DragTab.Q<VisualElement>(name = "DragPositionContainer");
                VisualElement DragRotationContainer = DragTab.Q<VisualElement>(name = "DragRotationContainer");
                VisualElement DragClampMinMax = DragTab.Q<VisualElement>(name = "DragClampMinMax");
                VisualElement DragAngleClampMinMax = DragTab.Q<VisualElement>(name = "DragAngleClampMinMax");
                VisualElement DragMaxDistance = DragTab.Q<VisualElement>(name = "DragMaxDistance");
                VisualElement DragRailGizmosColor = DragTab.Q<VisualElement>(name = "DragRailGizmosColor");
                VisualElement DragLimitGizmosColor = DragTab.Q<VisualElement>(name = "DragLimitGizmosColor");
                VisualElement DragCurrentGizmosColor = DragTab.Q<VisualElement>(name = "DragCurrentGizmosColor");
                VisualElement DragGizmoSphereRadius = DragTab.Q<VisualElement>(name = "DragGizmoSphereRadius");
                VisualElement SeeDragDistanceGizmo = DragTab.Q<VisualElement>(name = "SeeDragDistanceGizmo");
                VisualElement DragDistanceEdgeColor = DragTab.Q<VisualElement>(name = "DragDistanceEdgeColor");
                VisualElement UseRotStartPosition = DragTab.Q<VisualElement>(name = "UseRotStartPosition");
                VisualElement rotClampStartType = DragTab.Q<VisualElement>(name = "rotClampStartType");
                VisualElement startPercentage = DragTab.Q<VisualElement>(name = "startPercentage");
                VisualElement startDegrees = DragTab.Q<VisualElement>(name = "startDegrees");
                VisualElement DragStepCount = DragTab.Q<VisualElement>(name = "DragStepCount");
                VisualElement AlwaysReturnToStartPos = DragTab.Q<VisualElement>(name = "AlwaysReturnToStartPos");
                VisualElement ReturnByTheSameDirection = DragTab.Q<VisualElement>(name = "ReturnByTheSameDirection");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Position; }, new[] { DragPositionContainer }, serializedObject, new[] { "DragType", "interactMode" });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Rotation; }, new[] { DragRotationContainer }, serializedObject, new[] { "DragType", "interactMode" });
                FSEditorUI.ShowIfPredicate(root, () =>
                {
                    return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Rotation && localTarget.UseRotStartPosition && localTarget.rotClampStartType == RotClampStartType.Percentage;
                }, new[] { startPercentage }, serializedObject, new[] { "UseRotStartPosition", "rotClampStartType", "interactMode", "DragType" }, "HideField");
                FSEditorUI.ShowIfPredicate(root, () =>
                {
                    return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Rotation && localTarget.UseRotStartPosition && localTarget.rotClampStartType == RotClampStartType.Degree;
                }, new[] { startDegrees }, serializedObject, new[] { "UseRotStartPosition", "rotClampStartType", "interactMode", "DragType" }, "HideField");
                FSEditorUI.ShowIfPredicate(root, () =>
                {
                    return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Rotation && localTarget.UseRotStartPosition;
                }, new[] { AlwaysReturnToStartPos, ReturnByTheSameDirection }, serializedObject, new[] { "UseRotStartPosition", "interactMode", "DragType" }, "HideField");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragIsToClamp), DragClampMinMax);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragRotIsToClamp), new[] { DragAngleClampMinMax, UseRotStartPosition, rotClampStartType, startPercentage, startDegrees });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.UseRotStartPosition), "Hide2", new[] { rotClampStartType, startPercentage, startDegrees });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragHasMaxDistance), DragMaxDistance);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.SeeDragClampGizmos), new[] { DragRailGizmosColor, DragLimitGizmosColor, DragCurrentGizmosColor, DragGizmoSphereRadius });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragHasMaxDistance), new[] { SeeDragDistanceGizmo, DragDistanceEdgeColor });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.SeeDragDistanceGizmo), "Hide3", new[] { DragDistanceEdgeColor });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUseSteps), new[] { DragStepCount });
                
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.AlwaysReturnToStartPos), new[] { ReturnByTheSameDirection });

                VisualElement DragSliderMax = DragTab.Q<VisualElement>(name = "DragSliderMax");
                VisualElement DragPosSliderMinMax = DragTab.Q<VisualElement>(name = "DragPosSliderMinMax");
                VisualElement DragRotSliderMaxAngle = DragTab.Q<VisualElement>(name = "DragRotSliderMaxAngle");
                VisualElement lastSliderHolder = DragTab.Q<VisualElement>(name = "lastSliderValue");
                VisualElement RotationRadius = DragTab.Q<VisualElement>(name = "RotationRadius");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUseSlider), new[] { DragSliderMax, DragPosSliderMinMax, DragRotSliderMaxAngle, lastSliderHolder });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Position; }, new[] { DragPosSliderMinMax }, serializedObject, new[] { "DragType" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Rotation; }, new[] { DragSliderMax, DragRotSliderMaxAngle }, serializedObject, new[] { "DragType" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.DragRotIsToClamp && localTarget.DragType == DragType.Rotation; }, new[] { DragRotSliderMaxAngle }, serializedObject, new[] { "DragRotIsToClamp" }, "HideAlt");
                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.AutomaticRotationRadius; }, new[] { RotationRadius }, serializedObject, new[] { "AutomaticRotationRadius" });

                lastSliderHolder?.Clear();

                var rotLiveLabel = new Label();
                rotLiveLabel.name = "rotSliderLiveLabel";
                lastSliderHolder?.Add(rotLiveLabel);

                FSEditorUI.ShowIfPredicate(root, () => { return Application.isPlaying && localTarget.DragUseSlider; }, new[] { lastSliderHolder }, serializedObject, new[] { "playing" });

                _root.schedule.Execute(() =>
                {
                    if (!Application.isPlaying || localTarget == null) return;

                    rotLiveLabel.style.marginTop = 15;
                    rotLiveLabel.style.unityFontDefinition = UnityEngine.UIElements.FontDefinition.FromFont(FSEditorUI.mainFont);
                    rotLiveLabel.style.fontSize = 14;
                    rotLiveLabel.text = $"Slider value: {localTarget.DragSliderValue:0.00}";
                })
                .Every(100);

                // Inspection
                VisualElement InspectionDistance = InspectionTab.Q<VisualElement>(name = "InspectionDistance");
                VisualElement CanRotateFoldout = InspectionTab.Q<VisualElement>(name = "CanRotate");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.InspectionViewMode == InspectionViewMode.MoveObjectToCamera; }, new[] { InspectionDistance, CanRotateFoldout }, serializedObject, new[] { nameof(localTarget.InspectionViewMode) }, "HideAlt");

                VisualElement InspectionTargetTransform = InspectionTab.Q<VisualElement>(name = "InspectionTargetTransform");
                VisualElement InspectionTargetPosition = InspectionTab.Q<VisualElement>(name = "InspectionTargetPosition");
                VisualElement InspectionTargetRotation = InspectionTab.Q<VisualElement>(name = "InspectionTargetRotation");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.InspectionTargetType == InspectionNavigationTargetType.Transform; }, new[] { InspectionTargetTransform }, serializedObject, new[] { nameof(localTarget.InspectionTargetType) }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.InspectionTargetType == InspectionNavigationTargetType.Position; }, new[] { InspectionTargetPosition, InspectionTargetRotation }, serializedObject, new[] { nameof(localTarget.InspectionTargetType) }, "Hide2");
                
                VisualElement InspectionLeftMargin = InspectionTab.Q<VisualElement>(name = "InspectionLeftMargin");
                VisualElement InspectionRightMargin = InspectionTab.Q<VisualElement>(name = "InspectionRightMargin");
                VisualElement InspectionTopMargin = InspectionTab.Q<VisualElement>(name = "InspectionTopMargin");
                VisualElement InspectionBottomMargin = InspectionTab.Q<VisualElement>(name = "InspectionBottomMargin");
                VisualElement InspectionRotationOffsetOnEdge = InspectionTab.Q<VisualElement>(name = "InspectionRotationOffsetOnEdge");
                VisualElement InspectionLeftOffset = InspectionTab.Q<VisualElement>(name = "InspectionLeftOffset");
                VisualElement InspectionRightOffset = InspectionTab.Q<VisualElement>(name = "InspectionRightOffset");
                VisualElement InspectionTopOffset = InspectionTab.Q<VisualElement>(name = "InspectionTopOffset");
                VisualElement InspectionBottomOffset = InspectionTab.Q<VisualElement>(name = "InspectionBottomOffset");
                VisualElement InspectionMarginDeadZone = InspectionTab.Q<VisualElement>(name = "InspectionMarginDeadZone");
                VisualElement InspectionMarginFeather = InspectionTab.Q<VisualElement>(name = "InspectionMarginFeather");
                VisualElement InspectionPreviewDeadZoneOnGame = InspectionTab.Q<VisualElement>(name = "InspectionPreviewDeadZoneOnGame");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.InspectionHasMargin), new[] { InspectionLeftMargin, InspectionRightMargin, InspectionTopMargin, InspectionBottomMargin,
                                                                                                               InspectionRotationOffsetOnEdge, InspectionLeftOffset, InspectionRightOffset, InspectionTopOffset, InspectionBottomOffset,
                                                                                                               InspectionMarginDeadZone, InspectionMarginFeather, InspectionPreviewDeadZoneOnGame });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.InspectionRotationOffsetOnEdge), "Hide2", new[] {InspectionLeftOffset, InspectionRightOffset, InspectionTopOffset, InspectionBottomOffset });
                
            
                VisualElement DetailsColParent = InspectionTab.Q<VisualElement>(name = "DetailsColParent");

                VisualElement InspectionRotationSens = InspectionTab.Q<VisualElement>(name = "InspectionRotationSens");
                VisualElement OverrideRotationKey = InspectionTab.Q<VisualElement>(name = "OverrideRotationKey");
                VisualElement NewInspectionRotation = InspectionTab.Q<VisualElement>(name = "NewInspectionRotation");
                VisualElement ShowCursorWhenRotating = InspectionTab.Q<VisualElement>(name = "ShowCursorWhenRotating");
                VisualElement DontHideCursorOnStop = InspectionTab.Q<VisualElement>(name = "DontHideCursorOnStop");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.InspectionCanRotate), new[] { InspectionRotationSens, OverrideRotationKey, NewInspectionRotation });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideRotationKey), "Hide3", new[] { NewInspectionRotation });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.ShowCursorWhenRotating), new[] { DontHideCursorOnStop });

                VisualElement DetailBackgroundColor = InspectionTab.Q<VisualElement>(name = "DetailBackgroundColor");
                VisualElement DetailBackground = InspectionTab.Q<VisualElement>(name = "DetailBackground");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasDetailBackground), new[] { DetailsColParent, DetailBackground });

                VisualElement DetailText = InspectionTab.Q<VisualElement>(name = "DetailText");
                VisualElement OverrideDetailTextKey = InspectionTab.Q<VisualElement>(name = "OverrideDetailTextKey");
                VisualElement NewDetailText = InspectionTab.Q<VisualElement>(name = "NewDetailText");
                VisualElement DetailTextFirst = InspectionTab.Q<VisualElement>(name = "DetailTextFirst");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasDetailText), new[] { DetailText, OverrideDetailTextKey, NewDetailText, DetailTextFirst });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideDetailTextKey), "Hide2", new[] { NewDetailText });


                VisualElement DetailImageColor = InspectionTab.Q<VisualElement>(name = "DetailImageColor");
                VisualElement DetailImage = InspectionTab.Q<VisualElement>(name = "DetailImage");
                VisualElement OverrideDetailImageKey = InspectionTab.Q<VisualElement>(name = "OverrideDetailImageKey");
                VisualElement NewDetailImage = InspectionTab.Q<VisualElement>(name = "NewDetailImage");
                VisualElement DetailImageFirst = InspectionTab.Q<VisualElement>(name = "DetailImageFirst");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasDetailImage), new[] { DetailImageColor, DetailImage, OverrideDetailImageKey, NewDetailImage, DetailImageFirst });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideDetailImageKey), "Hide2", new[] { NewDetailImage });

                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.DetailTextFirst && localTarget.hasDetailImage && localTarget.hasDetailText; }, new[] { DetailImageFirst }, serializedObject, new[] { "DetailTextFirst", "hasDetailImage", "hasDetailText" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.DetailImageFirst && localTarget.hasDetailImage && localTarget.hasDetailText; }, new[] { DetailTextFirst }, serializedObject, new[] { "DetailImageFirst", "hasDetailImage", "hasDetailText" }, "Hide2");

                VisualElement overrideInspectionQuaternionPropHide = InspectionTab.Q<VisualElement>(name = "inspectionQuaternionOverride");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideInspectionQuaternion), overrideInspectionQuaternionPropHide);

                VisualElement InspectionPrefab = InspectionTab.Q<VisualElement>(name = "InspectionPrefab");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideInspectionPrefab), InspectionPrefab);

                // UI
                VisualElement ScreenSpaceOffset = UITabs.Q<VisualElement>(name = "ScreenSpaceOffset");
                VisualElement overrideScreenSpacePrefab = UITabs.Q<VisualElement>(name = "overrideScreenSpacePrefab");
                VisualElement ScreenSpacePrefab = UITabs.Q<VisualElement>(name = "ScreenSpacePrefab");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.ScreenSpacePrompt), new[] { ScreenSpaceOffset, overrideScreenSpacePrefab, ScreenSpacePrefab });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideScreenSpacePrefab), "Hide2", ScreenSpacePrefab);

                VisualElement AlignWorldSpaceToAnchor = UITabs.Q<VisualElement>(name = "AlignWorldSpaceToAnchor");
                VisualElement OverrideWorldPrefabContainer = UITabs.Q<VisualElement>(name = "OverrideWorldPrefabContainer");
                VisualElement WorldSpacePrefab = UITabs.Q<VisualElement>(name = "WorldSpacePrefab");
                VisualElement WorldSpaceOffset = UITabs.Q<VisualElement>(name = "WorldSpaceOffset");
                VisualElement AdditionalWorldPrompt = UITabs.Q<VisualElement>(name = "AdditionalWorldPrompt");
                VisualElement WorldPromptLayer = UITabs.Q<VisualElement>(name = "WorldPromptLayer");
                VisualElement WorldSizeContainer = UITabs.Q<VisualElement>(name = "WorldSizeContainer");
                VisualElement WorldSize = UITabs.Q<VisualElement>(name = "WorldSize");
                VisualElement ReferencePromptDistance = UITabs.Q<VisualElement>(name = "ReferencePromptDistance");
                VisualElement ReferencePrompHasScaleMinMax = UITabs.Q<VisualElement>(name = "ReferencePrompHasScaleMinMax");
                VisualElement PromptScaleMinMax = UITabs.Q<VisualElement>(name = "PromptScaleMinMax");
                VisualElement OverrideWorldPromptMessage = UITabs.Q<VisualElement>(name = "OverrideWorldPromptMessage");
                VisualElement WorldUIHelpBox = UITabs.Q<VisualElement>(name = "WorldUIHelpBox");
                VisualElement WorldPromptMessage = UITabs.Q<VisualElement>(name = "WorldPromptMessage");
                VisualElement ConditionPromptMessageContainer = UITabs.Q<VisualElement>(name = "ConditionPromptMessageContainer");
                VisualElement DeclinedWorldPromptMessage = UITabs.Q<VisualElement>(name = "DeclinedWorldPromptMessage");
                VisualElement WorldUIOverrideAnchor = UITabs.Q<VisualElement>(name = "WorldUIOverrideAnchor");
                VisualElement WorldUIOverrideAnchorContainer = UITabs.Q<VisualElement>(name = "WorldUIOverrideAnchorContainer");
                VisualElement LocalPositionWorldUIAnchor = UITabs.Q<VisualElement>(name = "LocalPositionWorldUIAnchor");
                Button LocalPositionWorldUIAnchorButton = UITabs.Q<Button>(name = "LocalPositionWorldUIAnchorButton");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.WorldSpacePrompt),
                new[] { AlignWorldSpaceToAnchor, WorldSpaceOffset, AdditionalWorldPrompt, WorldPromptLayer, WorldSizeContainer, ReferencePrompHasScaleMinMax, PromptScaleMinMax, OverrideWorldPromptMessage, WorldUIHelpBox, WorldPromptMessage, ConditionPromptMessageContainer, DeclinedWorldPromptMessage, WorldUIOverrideAnchor, WorldUIOverrideAnchorContainer, OverrideWorldPrefabContainer });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.ReferencePrompHasScaleMinMax), "Hide2", PromptScaleMinMax);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.WorldSize), new[] { ReferencePromptDistance });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.UseConditions), "Hide2", new[] { ConditionPromptMessageContainer });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideWorldSpacePrefab), "Hide2", new[] { WorldSpacePrefab });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.HasDeclinedConditionMessage), "Hide2", new[] { DeclinedWorldPromptMessage });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.WillOverrideAnchor; }, new[] { AlignWorldSpaceToAnchor }, serializedObject, new[] { "overrideAnchorPosition" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.HasSensor; }, new[] { AdditionalWorldPrompt }, serializedObject, new[] { "HasSensor" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.OverrideWorldPromptMessage; }, new[] { WorldUIHelpBox, WorldPromptMessage, ConditionPromptMessageContainer }, serializedObject, new[] { "OverrideWorldPromptMessage" }, "Hide3");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.WorldUIOverrideAnchor), "Hide2", new[] { LocalPositionWorldUIAnchor, LocalPositionWorldUIAnchorButton });
                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.AlignWorldSpaceToAnchor; }, new[] { WorldUIOverrideAnchor, LocalPositionWorldUIAnchor, LocalPositionWorldUIAnchorButton }, serializedObject, new[] { "AlignWorldSpaceToAnchor" }, "Hide3");

                FSEditorUI.ShowIfPredicate(root, () =>
                {
                    return localTarget.overrideWorldSpacePrefab ?
                    (localTarget.WorldSpacePrefab != null && localTarget.WorldSpacePrefab.TryGetComponent<UIPrefab>(out var uiprefab) && (!uiprefab.hasInteractionText || uiprefab.InteractionTmpText == null)) :
                    (GameObject.FindFirstObjectByType<InteractionManager>() != null && GameObject.FindFirstObjectByType<InteractionManager>().WorldPromptPrefab != null && GameObject.FindFirstObjectByType<InteractionManager>().WorldPromptPrefab.gameObject.TryGetComponent<UIPrefab>(out var uiprefab2) && (uiprefab2.hasInteractionText == false || uiprefab2.InteractionTmpText == null));

                }, new[] { WorldUIHelpBox }, serializedObject, new[] { "overrideWorldSpacePrefab", "WorldSpacePrefab", "WorldPromptPrefab", "hasInteractionText", "InteractionTmpText" }, "HideAlt");
                FSEditorUI.ShowIfPredicate(root, () =>
                {
                    return !(localTarget.overrideWorldSpacePrefab ?
                    (localTarget.WorldSpacePrefab != null && localTarget.WorldSpacePrefab.TryGetComponent<UIPrefab>(out var uiprefab) && (!uiprefab.hasInteractionText || uiprefab.InteractionTmpText == null)) :
                    (GameObject.FindFirstObjectByType<InteractionManager>() != null && GameObject.FindFirstObjectByType<InteractionManager>().WorldPromptPrefab != null && GameObject.FindFirstObjectByType<InteractionManager>().WorldPromptPrefab.gameObject.TryGetComponent<UIPrefab>(out var uiprefab2) && (uiprefab2.hasInteractionText == false || uiprefab2.InteractionTmpText == null)));

                }, new[] { WorldPromptMessage, ConditionPromptMessageContainer }, serializedObject, new[] { "overrideWorldSpacePrefab", "WorldSpacePrefab", "WorldPromptPrefab", "hasInteractionText", "InteractionTmpText" }, "HideAlt");

                VisualElement GrabUI = UITabs.Q<VisualElement>(name = "GrabUI");
                VisualElement GrabUISpriteRow = UITabs.Q<VisualElement>(name = "GrabUISpriteRow");
                VisualElement GrabUIColorRow = UITabs.Q<VisualElement>(name = "GrabUIColorRow");
                VisualElement GrabUISizeRow = UITabs.Q<VisualElement>(name = "GrabUISizeRow");
                VisualElement GrabUIPrefabRow = UITabs.Q<VisualElement>(name = "GrabUIPrefabRow");
                VisualElement GrabUISprite = UITabs.Q<VisualElement>(name = "GrabUISprite");
                VisualElement GrabUIColor = UITabs.Q<VisualElement>(name = "GrabUIColor");
                VisualElement GrabUISize = UITabs.Q<VisualElement>(name = "GrabUISize");
                VisualElement GrabUIPrefab = UITabs.Q<VisualElement>(name = "GrabUIPrefab");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.GrabUIEnabled), new[] { GrabUISpriteRow, GrabUIColorRow, GrabUISizeRow, GrabUIPrefabRow });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.GrabUIControlSprite), new[] { GrabUISprite });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.GrabUIControlColor), new[] { GrabUIColor });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.GrabUIControlSize), new[] { GrabUISize });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideGrabUIPrefab), new[] { GrabUIPrefab });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Grab; }, new[] { GrabUI }, serializedObject, new[] { "interactMode" }, "Hide2");

                VisualElement DragUI = UITabs.Q<VisualElement>(name = "DragUI");
                VisualElement DrabUISizeRow = UITabs.Q<VisualElement>(name = "DrabUISizeRow");
                VisualElement DrabUISpriteRow = UITabs.Q<VisualElement>(name = "DrabUISpriteRow");
                VisualElement DrabUIColorRow = UITabs.Q<VisualElement>(name = "DrabUIColorRow");
                VisualElement DragUIPrefabRow = UITabs.Q<VisualElement>(name = "DragUIPrefabRow");
                VisualElement DragUISize = UITabs.Q<VisualElement>(name = "DragUISize");
                VisualElement DragUISprite = UITabs.Q<VisualElement>(name = "DragUISprite");
                VisualElement DragUIColor = UITabs.Q<VisualElement>(name = "DragUIColor");
                VisualElement DragUIPrefab = UITabs.Q<VisualElement>(name = "DragUIPrefab");
                VisualElement DragUIOverrideScreenOffset = UITabs.Q<VisualElement>(name = "DragUIOverrideScreenOffset");
                VisualElement DragUIOverrideOnArc = UITabs.Q<VisualElement>(name = "DragUIOverrideOnArc");
                VisualElement DragUIOverrideAnchor = UITabs.Q<VisualElement>(name = "DragUIOverrideAnchor");
                VisualElement NewUIAnchorPositionContainer = UITabs.Q<VisualElement>(name = "NewUIAnchorPositionContainer");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUIEnabled), new[] { DrabUISizeRow, DrabUISpriteRow, DrabUIColorRow, DragUIPrefabRow, DragUIOverrideScreenOffset, DragUIOverrideOnArc, DragUIOverrideAnchor, NewUIAnchorPositionContainer });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUIControlSize), new[] { DragUISize });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUIControlSprite), new[] { DragUISprite });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUIControlColor), new[] { DragUIColor });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideDragUIPrefab), new[] { DragUIPrefab });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Drag; }, new[] { DragUI }, serializedObject, new[] { "interactMode" }, "Hide2");

                Button LocalPositionDragUIAnchorButton = UITabs.Q<Button>(name = "LocalPositionDragUIAnchorButton");
                LocalPositionDragUIAnchorButton.clicked += localTarget.SetNewDragUIAnchorButton;

                LocalPositionWorldUIAnchorButton.clicked += localTarget.SetNewWorldUIAnchorButton;

                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragUIOverrideAnchor), "Hide3", new[] { NewUIAnchorPositionContainer });

                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.DragUIOverrideOnArc; }, new[] { DragUIOverrideAnchor }, serializedObject, new[] { "DragUIOverrideOnArc" }, "Hide3");
                FSEditorUI.ShowIfPredicate(root, () => { return !localTarget.DragUIOverrideAnchor; }, new[] { DragUIOverrideOnArc }, serializedObject, new[] { "DragUIOverrideAnchor" }, "Hide3");

                VisualElement HoldUI = UITabs.Q<VisualElement>(name = "HoldUI");
                VisualElement ActAsWorldPrompt = UITabs.Q<VisualElement>(name = "ActAsWorldPrompt");
                VisualElement HoldUISliderRow = UITabs.Q<VisualElement>(name = "HoldUISliderRow");
                VisualElement HoldUIPrefabRow = UITabs.Q<VisualElement>(name = "HoldUIPrefabRow");
                VisualElement HoldUIOverrideAnchor = UITabs.Q<VisualElement>(name = "HoldUIOverrideAnchor");
                VisualElement NewHoldUIAnchorPositionContainer = UITabs.Q<VisualElement>(name = "NewHoldUIAnchorPositionContainer");
                Button LocalPositionHoldUIAnchorButton = UITabs.Q<Button>(name = "LocalPositionHoldUIAnchorButton");
                VisualElement applySliderValue = UITabs.Q<VisualElement>(name = "applySliderValue");
                VisualElement HoldUIPrefab = UITabs.Q<VisualElement>(name = "HoldUIPrefab");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.HoldUIEnabled), new[] { ActAsWorldPrompt, HoldUISliderRow, HoldUIPrefabRow, HoldUIOverrideAnchor, NewHoldUIAnchorPositionContainer });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.DragHasSlider), new[] { applySliderValue });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.overrideHoldPrefab), new[] { HoldUIPrefab });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.HoldUIOverrideAnchor), "Hide2", new[] { NewHoldUIAnchorPositionContainer });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.ActAsWorldPrompt), "Hide3", new[] { HoldUIOverrideAnchor, NewHoldUIAnchorPositionContainer });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode == InteractMode.Hold; }, new[] { HoldUI }, serializedObject, new[] { "interactMode" }, "Hide2");

                LocalPositionHoldUIAnchorButton.clicked += localTarget.SetNewHoldUIAnchorButton;

                // Events
                VisualElement OnInteractEvent = EventsTabs.Q<VisualElement>(name = "OnInteractEvent");
                VisualElement EndEvent = EventsTabs.Q<VisualElement>(name = "EndEvent");
                VisualElement OnRayCastEnterAndExit = EventsTabs.Q<VisualElement>(name = "OnRayCastEnterAndExit");
                VisualElement onInteract = EventsTabs.Q<VisualElement>(name = "onInteract");
                VisualElement onInteractEnd = EventsTabs.Q<VisualElement>(name = "onInteractEnd");
                VisualElement OnRayCastEnterAndExitLower = EventsTabs.Q<VisualElement>(name = "OnRayCastEnterAndExitLower");

                VisualElement ConditionEvents = EventsTabs.Q<VisualElement>(name = "ConditionEvents");
                VisualElement onConditionAttended = ConditionEvents.Q<VisualElement>(name = "onConditionAttended");

                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.UseConditions), ConditionEvents);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.AddOnInteractEvent), onInteract);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.AddEndEvent), onInteractEnd);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OnRayCastEnterAndExit), OnRayCastEnterAndExitLower);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasOnConditionAttended), onConditionAttended);
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode != InteractMode.UnityEvent; }, new[] { OnInteractEvent, EndEvent }, serializedObject, new[] { "interactMode" });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.interactMode != InteractMode.UnityEvent && localTarget.interactMode != InteractMode.Hold; }, new[] { EndEvent }, serializedObject, new[] { "interactMode" });

                VisualElement OnSensorExitEvent = EventsTabs.Q<VisualElement>(name = "OnSensorExitEvent");
                VisualElement onSensorExit = EventsTabs.Q<VisualElement>(name = "onSensorExit");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.HasSensor), OnSensorExitEvent);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasOnSensorExit), onSensorExit);
                
                VisualElement GrabReturnToPosEvent = EventsTabs.Q<VisualElement>(name = "GrabReturnToPosEvent");
                VisualElement BeforeAndAfter = EventsTabs.Q<VisualElement>(name = "BeforeAndAfter");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.AlwaysReturnToStartPos && localTarget.interactMode == InteractMode.Drag && localTarget.DragType == DragType.Rotation && localTarget.UseRotStartPosition; }, new[] { GrabReturnToPosEvent }, serializedObject, new[] { "AlwaysReturnToStartPos", "interactMode", "DragType", "UseRotStartPosition" });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasGrabReturnToPosEvent), BeforeAndAfter);

                // Settings
                PropertyField field = SettingsTabs.Q<PropertyField>("OverrideInteractionKey");

                field.RegisterCallback<AttachToPanelEvent>(_ =>
                {
                    field.schedule.Execute(() =>
                    {
                        var label =
                            field.Q<Label>(className: "unity-base-field__label") ??
                            field.Q<Label>(className: "unity-label");

                        if (label == null) return;

                        label.style.minWidth = 50;
                        label.style.width = 50;
                        label.style.maxWidth = 50;
                    })
                    .Until(() =>
                        field.Q<Label>(className: "unity-base-field__label") != null ||
                        field.Q<Label>(className: "unity-label") != null
                    );
                });


                VisualElement NewInteraction = SettingsTabs.Q<VisualElement>(name = "NewInteraction");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.OverrideInteractionKey), new[] { NewInteraction });

                VisualElement MaxInteractionsField = SettingsTabs.Q<VisualElement>(name = "maxInteractions");
                VisualElement SensorOffset = SettingsTabs.Q<VisualElement>(name = "SensorOffset");
                VisualElement SensorType = SettingsTabs.Q<VisualElement>(name = "SensorType");
                VisualElement SensorRadius = SettingsTabs.Q<VisualElement>(name = "SensorRadius");
                VisualElement SensorRadiusCirclePrefab = SettingsTabs.Q<VisualElement>(name = "SensorRadiusCirclePrefab");
                VisualElement DrawRadiusInEditor = SettingsTabs.Q<VisualElement>(name = "DrawRadiusInEditor");
                VisualElement SensorDistance = SettingsTabs.Q<VisualElement>(name = "SensorDistance");
                VisualElement OnlyShowUI = SettingsTabs.Q<VisualElement>(name = "OnlyShowUI");

                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.hasMaxInteractions), MaxInteractionsField);
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.HasSensor), new[] { SensorOffset, SensorType, SensorRadius, OnlyShowUI, SensorRadiusCirclePrefab, DrawRadiusInEditor, SensorDistance });
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.SensorType == FastStudios.SensorType.Radius; }, new[] { SensorRadius, SensorRadiusCirclePrefab, DrawRadiusInEditor }, serializedObject, new[] { "SensorType" }, "Hide2");
                FSEditorUI.ShowIfPredicate(root, () => { return localTarget.SensorType == FastStudios.SensorType.Distance; }, new[] { SensorDistance }, serializedObject, new[] { "SensorType" }, "Hide2");

                Button localPositionNewAnchorButton = SettingsTabs.Q<Button>(name = "LocalPositionNewAnchorButton");
                localPositionNewAnchorButton.clicked += localTarget.SetNewAnchorButton;

                VisualElement WillOverrideAnchor = SettingsTabs.Q<VisualElement>(name = "WillOverrideAnchor");
                VisualElement NewAnchorPositionContainer = SettingsTabs.Q<VisualElement>(name = "NewAnchorPositionContainer");
                VisualElement SeeAnchorPointGizmosContainer = SettingsTabs.Q<VisualElement>(name = "SeeAnchorPointGizmosContainer");
                VisualElement SeeCenterOfMassGizmosContainer = SettingsTabs.Q<VisualElement>(name = "SeeCenterOfMassGizmosContainer");
                VisualElement AnchorPointGizmosColor = SettingsTabs.Q<VisualElement>(name = "AnchorPointGizmosColor");
                VisualElement SeeCenterOfMassGizmosColor = SettingsTabs.Q<VisualElement>(name = "SeeCenterOfMassGizmosColor");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.SeeAnchorPointGizmos), "Hide2", new[] { AnchorPointGizmosColor });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.SeeCenterOfMassGizmos), "Hide2", new[] { SeeCenterOfMassGizmosColor });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.WillOverrideAnchor), "Hide2", new[] { NewAnchorPositionContainer });

                VisualElement AllowJustSpecificInteractions = SettingsTabs.Q<VisualElement>(name = "AllowJustSpecificInteractions");
                VisualElement SpecificOthersInteractions = SettingsTabs.Q<VisualElement>(name = "SpecificOthersInteractions");
                VisualElement AllowJustSpecificUIToShow = SettingsTabs.Q<VisualElement>(name = "AllowJustSpecificUIToShow");
                VisualElement SpecificOthersUI = SettingsTabs.Q<VisualElement>(name = "SpecificOthersUI");
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.CanInteractWithOthers), new[] { AllowJustSpecificInteractions, SpecificOthersInteractions });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.AllowJustSpecificInteractions), "Hide2", new[] { SpecificOthersInteractions });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.CanShowOthersUI), new[] { AllowJustSpecificUIToShow, SpecificOthersUI });
                FSEditorUI.ShowIfBool(root, serializedObject, nameof(localTarget.AllowJustSpecificUIToShow), "Hide2", new[] { SpecificOthersUI });
            }

            // Others
            {
                bool applied = false;
                IVisualElementScheduledItem poller = null;

                void ApplyConditionListClass()
                {
                    if (applied) return;

                    if (_root == null || _root.panel == null)
                    {
                        poller?.Pause();
                        poller = null;
                        return;
                    }

                    var list = ConditionSection?.Q<VisualElement>("unity-property-field-Conditions.items");
                    var joinList = ConditionSection?.Q<VisualElement>("unity-property-field-Conditions.joins");
                    var unityContent = list?.Q<VisualElement>("unity-content");
                    VisualElement scrollView = null;

                    if (unityContent == null) return;

                    unityContent.style.display = DisplayStyle.Flex;

                    if (!unityContent.ClassListContains("ConditionUnityContent")) unityContent.AddToClassList("ConditionUnityContent");

                    if (unityContent.childCount > 0) scrollView = unityContent.Children().ToList()[0];

                    if (scrollView == null) return;

                    if (!scrollView.ClassListContains("ConditionListBorder")) scrollView.AddToClassList("ConditionListBorder");

                    if (list == null) return;

                    if (!list.ClassListContains("ConditionListContainer"))
                        list.AddToClassList("ConditionListContainer");

                    if (!joinList.ClassListContains("JoinListContainer"))
                        joinList.AddToClassList("JoinListContainer");

                    applied = true;
                    poller?.Pause();
                    poller = null;
                }

                void StartPolling()
                {
                    if (poller != null) return;

                    poller = _root.schedule
                        .Execute(ApplyConditionListClass)
                        .Every(100)
                        .Until(() => applied || _root == null || _root.panel == null);
                }

                _root.RegisterCallback<AttachToPanelEvent>(_ => StartPolling());
                _root.RegisterCallback<DetachFromPanelEvent>(_ =>
                {
                    poller?.Pause();
                    poller = null;
                });

                StartPolling();
            }

            // Font Apply
            {
                root.style.unityFontDefinition = new StyleFontDefinition(FSEditorUI.mainFont);
            }
            
            if (localTarget.interactableRb == null) localTarget.AssignRB();

            AddUniversalMenu();

            InteractableUniversalsSO.OnChanged -= OnUniversalsChanged;
            InteractableUniversalsSO.OnChanged += OnUniversalsChanged;

            return root;
        }

        void OnEnable()
        {
            InteractableUniversalsSO.OnEntryValueChanged -= OnUniversalEntryValueChanged;
            InteractableUniversalsSO.OnEntryValueChanged += OnUniversalEntryValueChanged;
        }

        void OnDisable()
        {
            InteractableUniversalsSO.OnEntryValueChanged -= OnUniversalEntryValueChanged;
        }

        #region Universals
        private readonly Dictionary<string, PropertyField> _fieldsByPath = new();
        private readonly Dictionary<string, VisualElement> _bindablesByPath = new();
        static bool _applying;
        void OnUniversalEntryValueChanged(string propertyPath)
        {
            if (_applying) return;
            try
            {
                _applying = true;

                foreach (var t in targets)
                {
                    var it = t as Interactable;
                    if (it == null) continue;

                    if (!it.IsUniversalBound(propertyPath)) continue;

                    InteractableUniversalsSO.ApplySingle(it, propertyPath);
                }

                Repaint();
            }
            finally { _applying = false; }
        }

        void OnUniversalsChanged()
        {
            if (localTarget == null) return;
            var so = serializedObject;
            bool changed = false;

            foreach (var path in _fieldsByPath.Keys.ToList())
            {
                if (!localTarget.IsUniversalBound(path)) continue;
                if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var ue))
                {
                    var sp = so.FindProperty(path);
                    if (InteractableUniversalsSO.ApplyToProperty(ue, sp))
                        changed = true;
                }
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(localTarget);
            }
        }

        void AddUniversalMenu()
        {
            foreach (var pf in _root.Query<PropertyField>().ToList())
            {
                var path = pf.bindingPath;
                if (string.IsNullOrEmpty(path)) continue;
                _fieldsByPath[path] = pf;

                AttachUniversalContext(pf, path);

                UpdateReadonlyBadge(path, pf);
            }

            foreach (var be in _root.Query<BindableElement>().ToList())
            {
                if (be.GetFirstAncestorOfType<PropertyField>() != null) continue;

                var path = be.bindingPath;
                if (string.IsNullOrEmpty(path)) continue;

                _bindablesByPath[path] = be;

                AttachUniversalContext(be, path);

                UpdateReadonlyBadge(path, be);
            }

            _root.schedule.Execute(() =>
            {
                if (this == null || target == null) return;

                var so = new SerializedObject(target);
                foreach (var kv in _fieldsByPath.ToArray())
                {
                    if (kv.Value == null) continue;
                    var sp = so.FindProperty(kv.Key);
                    if (sp != null)
                        UpdateReadonlyBadge(kv.Key, kv.Value);
                }
            }).ExecuteLater(0);
        }

        void AttachUniversalContext(VisualElement target, string path)
        {
            void AttachOne(VisualElement ve)
            {
                var m = new ContextualMenuManipulator(evt =>
                {
                    if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var _))
                    {
                        if (!localTarget.IsUniversalBound(path))
                            evt.menu.AppendAction("Universals/Load Universal", _ => ApplyAndBindUniversal(path), DropdownMenuAction.Status.Normal);
                        else
                            evt.menu.AppendAction("Universals/Unbind Universal", _ => UnbindUniversal(path), DropdownMenuAction.Status.Normal);
                    }
                    evt.menu.AppendAction("Universals/Open Window", _ => InteractableUniversalsWindow.Open(), DropdownMenuAction.Status.Normal);
                    evt.StopPropagation();
                });

                ve.AddManipulator(m);
                ve.pickingMode = PickingMode.Position;
            }

            AttachOne(target);

            void AttachToAllIMGUI()
            {
                foreach (var imgui in target.Query<IMGUIContainer>().ToList())
                    AttachOne(imgui);
            }
            EditorApplication.delayCall += AttachToAllIMGUI;
            target.RegisterCallback<GeometryChangedEvent>(_ => AttachToAllIMGUI());

            if (target is PropertyField pf)
            {
                var sp = serializedObject.FindProperty(path);
                if (sp != null && sp.propertyType == SerializedPropertyType.Quaternion)
                {
                    var wrapper = EnsureWrapper(pf);
                    var overlay = EnsureOverlay(wrapper, path);
                }
            }
        }

        const string UniversalWrapperName = "__UniversalWrapper__";
        const string UniversalOverlayName = "__UniversalOverlay__";

        VisualElement EnsureWrapper(PropertyField pf)
        {
            if (pf.parent != null && pf.parent.name == UniversalWrapperName)
                return pf.parent;

            var parent = pf.parent;
            if (parent == null) return null;

            int index = parent.IndexOf(pf);
            var wrapper = new VisualElement { name = UniversalWrapperName };
            wrapper.style.position = Position.Relative;
            wrapper.style.flexGrow = 1;

            parent.Insert(index, wrapper);
            parent.Remove(pf);
            wrapper.Add(pf);

            return wrapper;
        }

        VisualElement EnsureOverlay(VisualElement wrapper, string path)
        {
            if (wrapper == null) return null;

            var overlay = wrapper.Q<VisualElement>(UniversalOverlayName);
            if (overlay == null)
            {
                overlay = new VisualElement { name = UniversalOverlayName };
                overlay.style.position = Position.Absolute;
                overlay.style.left = 0; overlay.style.right = 0;
                overlay.style.top = 0; overlay.style.bottom = 0;
                overlay.pickingMode = PickingMode.Position;

                var m = new ContextualMenuManipulator(evt =>
                {
                    if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var _))
                    {
                        if (!localTarget.IsUniversalBound(path))
                            evt.menu.AppendAction("Universals/Load Universal", _ => ApplyAndBindUniversal(path), DropdownMenuAction.Status.Normal);
                        else
                            evt.menu.AppendAction("Universals/Unbind Universal", _ => UnbindUniversal(path), DropdownMenuAction.Status.Normal);
                    }
                    evt.menu.AppendAction("Universals/Open Window", _ => InteractableUniversalsWindow.Open(), DropdownMenuAction.Status.Normal);

                    evt.StopPropagation();
                });

                overlay.AddManipulator(m);
                wrapper.Add(overlay);
            }

            var pf = wrapper.Q<PropertyField>();
            overlay.style.display = (pf != null && pf.resolvedStyle.display != DisplayStyle.None)
                                    ? DisplayStyle.Flex : DisplayStyle.None;

            wrapper.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var _pf = wrapper.Q<PropertyField>();
                overlay.style.display = (_pf != null && _pf.resolvedStyle.display != DisplayStyle.None)
                                        ? DisplayStyle.Flex : DisplayStyle.None;
            });

            return overlay;
        }

        void HideOverlay(PropertyField pf)
        {
            var wrapper = (pf.parent != null && pf.parent.name == UniversalWrapperName) ? pf.parent : null;
            var overlay = wrapper?.Q<VisualElement>(UniversalOverlayName);
            if (overlay != null) overlay.style.display = DisplayStyle.None;
        }

        void ApplyAndBindUniversal(string path)
        {
            var so = serializedObject;

            var sp = so.FindProperty(path);
            if (sp == null) return;

            Undo.RecordObject(localTarget, "Bind Universal");

            localTarget.__Editor_SaveUniversalBackup(so, path);

            so.UpdateIfRequiredOrScript();

            sp = so.FindProperty(path);

            if (InteractableUniversalsSO.Instance.TryGet(localTarget.GetType().FullName, path, out var ue) &&
                InteractableUniversalsSO.ApplyToProperty(ue, sp))
            {
                so.ApplyModifiedProperties();
                localTarget.BindUniversal(path);
                if (_fieldsByPath.TryGetValue(path, out var pf)) UpdateReadonlyBadge(path, pf);
                else if (_bindablesByPath.TryGetValue(path, out var be)) UpdateReadonlyBadge(path, be);
                EditorUtility.SetDirty(localTarget);
                PrefabUtility.RecordPrefabInstancePropertyModifications(localTarget);
            }

        }

        void UnbindUniversal(string path)
        {
            var so = serializedObject;

            Undo.RecordObject(localTarget, "Unbind Universal");
            bool restored = localTarget.__Editor_RestoreUniversalBackup(so, path);

            localTarget.UnbindUniversal(path);
            if (_fieldsByPath.TryGetValue(path, out var pf)) UpdateReadonlyBadge(path, pf);

            if (_bindablesByPath.TryGetValue(path, out var be))
                UpdateReadonlyBadge(path, be);

            if (restored)
                EditorUtility.SetDirty(localTarget);

            _root.MarkDirtyRepaint();
            PrefabUtility.RecordPrefabInstancePropertyModifications(localTarget);
        }

        void UpdateReadonlyBadge(string path, PropertyField pf)
        {
            bool bound = localTarget.IsUniversalBound(path);
            pf.SetEnabled(!bound);

            const string kBadgeName = "UniversalBadge";
            var old = pf.Q<Label>(kBadgeName);
            if (old != null) old.RemoveFromHierarchy();

            if (bound)
            {
                var badge = new Label("Universal");
                badge.name = kBadgeName;
                badge.style.unityFontStyleAndWeight = FontStyle.Italic;
                badge.style.marginLeft = 6;
                badge.style.opacity = 0.75f;
                pf.Add(badge);
            }
        }

        void UpdateReadonlyBadge(string path, VisualElement ve)
        {
            bool bound = localTarget.IsUniversalBound(path);
            ve.SetEnabled(!bound);

            const string kBadgeName = "UniversalBadge";
            var oldHere = ve.Q<Label>(kBadgeName);
            if (oldHere != null) oldHere.RemoveFromHierarchy();
            var oldParent = ve.parent?.Q<Label>(kBadgeName);
            if (oldParent != null) oldParent.RemoveFromHierarchy();

            if (bound)
            {
                var badge = new Label("Universal") { name = kBadgeName };
                badge.style.unityFontStyleAndWeight = FontStyle.Italic;
                badge.style.marginLeft = 6;
                badge.style.opacity = 0.75f;

                (ve.parent ?? ve).Add(badge);
            }
        }

        #endregion

        void UpdateInteractionTab(Interactable target, VisualElement root)
        {
            interactionTab = (int)target.interactMode;

            HideInteractModeElements(root);
        }

        void SetupPageChange(Button button, int index, VisualElement root, Button[] allButtons, VisualElement[] allVisuals)
        {
            Action action = () => SetTabIndex(index, root, allButtons, allVisuals);
            button.clicked += action;
        }

        void SetTabIndex(int i, VisualElement root, Button[] allButtons, VisualElement[] allVisuals)
        {
            selectedTab = i;
            localTarget.lastSelectedTab = selectedTab;

            int index = i == MainTabIndex ? 0 :
                        i == UITabIndex ? 1 :
                        i == EventsTabIndex ? 2 : 3;

            Button button = allButtons[index];

            button.style.backgroundColor = selectedTabColor;

            foreach (Button btn in allButtons.Where(x => x != button))
            {
                btn.style.backgroundColor = normalTabColor;
            }

            HideNonTabElements(root, allButtons, allVisuals);
        }

        void HideInteractModeElements(VisualElement root)
        {
            foreach (VisualElement visualElement in root.Query<VisualElement>().ToList())
            {
                if (visualElement is PropertyField)
                {
                    int parentIndex = visualElement.tabIndex;
                    foreach (VisualElement child in visualElement.Query<VisualElement>().ToList())
                        child.tabIndex = parentIndex;
                }

                if (visualElement.ClassListContains(FoldoutContainerClassName))
                    continue;

                bool mask =
                    visualElement.tabIndex == 0 ||
                    visualElement.tabIndex == -1 ||
                    visualElement.tabIndex == interactionTab ||
                    visualElement.tabIndex == selectedTab;

                if (mask)
                    FSEditorUI.SetVisible(true, HiddenClass, visualElement);
                else
                    FSEditorUI.SetVisible(false, HiddenClass, visualElement);
            }
        }

        void HideNonTabElements(VisualElement root, Button[] allButtons, VisualElement[] allVisuals)
        {
            foreach (VisualElement visualElement in root.Query<VisualElement>().ToList())
            {
                if (visualElement is PropertyField)
                {
                    int parentIndex = visualElement.tabIndex;
                    foreach (VisualElement child in visualElement.Query<VisualElement>().ToList())
                        child.tabIndex = parentIndex;
                }

                if (visualElement.ClassListContains(FoldoutContainerClassName))
                    continue;

                bool mask =
                    (visualElement is Button && allButtons.Contains(visualElement as Button)) ||
                    allVisuals.Contains(visualElement) ||
                    visualElement.tabIndex == 0 ||
                    visualElement.tabIndex == -1 ||
                    (selectedTab == MainTabIndex && visualElement.tabIndex == interactionTab);

                bool hide = visualElement.tabIndex != selectedTab &&
                            visualElement.parent != null &&
                            visualElement.parent.tabIndex != selectedTab;

                if (mask)
                {
                    FSEditorUI.SetVisible(true, HiddenClass, visualElement);
                    continue;
                }

                if (hide)
                    FSEditorUI.SetVisible(false, HiddenClass, visualElement);
                else
                    FSEditorUI.SetVisible(true, HiddenClass, visualElement);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Foldouts/Expand All"), false, () => ToggleAllFoldouts(true));
            menu.AddItem(new GUIContent("Foldouts/Collapse All"), false, () => ToggleAllFoldouts(false));
        }

        void ToggleAllFoldouts(bool show)
        {
            serializedObject.Update();

            foreach (var (key, header, container, sp) in _autoFoldouts)
            {
                if (sp != null) sp.boolValue = show;
                else localTarget.SetFoldout(key, show);

                FSEditorUI.SetVisible(show, HiddenClass, container);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

}