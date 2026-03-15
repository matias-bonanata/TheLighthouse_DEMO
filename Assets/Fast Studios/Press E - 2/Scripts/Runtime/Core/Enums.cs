namespace FastStudios
{
    public enum PlayerDetection
    {
        GameObject = 0,
        Tag = 1,
        Layer = 2,
        ObjectName = 3,
        MonoBehaviour = 4,
        Reference,
    }

    public enum InteractMode
    {
        UnityEvent = 1,
        Hold = 2,
        Grab = 3,
        Drag = 4,
        Inspection = 5
    }

    public enum Axis
    {
        Forward,
        Right,
        Up,
    }

    public enum MoveAxis
    {
        ForwardBack,
        LeftRight,
        UpAndDown,
    }

    public enum DepositMethod
    {
        Interact = 0,
        TriggerCollider = 1,
        Both = 2,
    }

    public enum DragType
    {
        Position,
        Rotation
    }

    public enum DragRotDirection
    {
        CounterClockwise = 0,
        ClockWise = 1,
    }

    public enum KeyAccept
    {
        KeyName,
        SpecificKey,
    }

    public enum RotClampStartType
    {
        Percentage,
        Degree,
    }

    public enum InputSystemEnum
    {
        Old = 0,
        New = 1,
        Both = 2,
    }

    public enum InputMethod
    {
        Keyboard,
        Mouse
    }

    public enum MouseMethod
    {
        Left = 0,
        Right = 1,
        Middle = 2,
    }

    public enum SpecifInteractable
    {
        Specific,
        Specifics,
    }

    public enum SensorType
    {
        Radius,
        Distance,
    }

    public enum TransformApplyType
    {
        World,
        Local,
    }

    public enum LerpType
    {
        Set,
        Add,
    }

    public enum AnchorChangerType
    {
        AnchorPosition,
        WorldUIAnchor,
        DragUIAnchor,
        HoldUIAnchor,
    }

    public enum InspectionViewMode
    {
        MoveObjectToCamera = 0,
        MoveCameraToObject = 1,
    }

    public enum InspectionNavigationTargetType
    {
        Transform,
        Position,
    }
}
