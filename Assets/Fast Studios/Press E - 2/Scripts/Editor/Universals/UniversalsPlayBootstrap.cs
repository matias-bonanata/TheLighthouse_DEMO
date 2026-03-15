#if UNITY_EDITOR
using UnityEditor;
using FastStudios.EditorTools;

namespace FastStudios
{
    static class UniversalsPlayBootstrap
    {
        [InitializeOnEnterPlayMode]
        static void OnEnterPlayMode(EnterPlayModeOptions _)
        {
            var so = InteractableUniversalsSO.Instance;
            UniversalsRuntime.SeedFromEditorSO(so);
            UniversalsRuntime.ApplyAllFor<Interactable>();
            UniversalsRuntime.ApplyAllFor<UIPrefab>();
            UniversalsRuntime.ApplyAllFor<Key>();

            UniversalsRuntime.ApplyAllFor<PositionLerp>();
            UniversalsRuntime.ApplyAllFor<RotationLerp>();
            UniversalsRuntime.ApplyAllFor<ScaleLerp>();
            UniversalsRuntime.ApplyAllFor<TransformLerp>();
        }
    }
}
#endif
