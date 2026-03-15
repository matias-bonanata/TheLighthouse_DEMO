#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FastStudios.EditorTools
{
    [Serializable]
    public enum UniversalValueType
    {
        Bool, Int, Float, String, Color, Vector2, Vector3, Vector2Int, Vector3Int,
        Enum, AnimationCurve, Quaternion, LayerMask
    }

    [Serializable]
    public class UniversalEntry
    {
        public string componentTypeName = typeof(Interactable).FullName;
        public string propertyPath;
        public bool loadOnCreation;
        public UniversalValueType valueType;

        public bool boolValue;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public Color colorValue = Color.white;
        public Vector2 v2;
        public Vector3 v3;
        public Vector2Int v2i;
        public Vector3Int v3i;
        public int enumValueIndex;
        public UnityEngine.Object objectRef;
        public AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);
        public Quaternion quaternion = Quaternion.identity;
        public LayerMask layerMask;
    }

    public class InteractableUniversalsSO : ScriptableObject
    {
        public List<UniversalEntry> entries = new();

        const string kResourceLoadPath = "FastStudios/ForEditor/Data/InteractableUniversals";

        static InteractableUniversalsSO _instance;
        public static InteractableUniversalsSO Instance
        {
            get
            {
                if (_instance != null) return _instance;

                _instance = Resources.Load<InteractableUniversalsSO>(kResourceLoadPath);

                if (_instance == null)
                {
                    var all = Resources.LoadAll<InteractableUniversalsSO>(string.Empty);
                    if (all != null && all.Length > 0)
                        _instance = all[0];
                }

                if (_instance == null)
                {
                    _instance = CreateInstance<InteractableUniversalsSO>();
                    GetAllVariablesData();
                }

                return _instance;
            }
        }

        public static void GetAllVariablesData()
        {
            _instance.TryBootstrapFromDefaults<Interactable>();
            _instance.TryBootstrapFromDefaults<UIPrefab>();
            _instance.TryBootstrapFromDefaults<Key>();

            _instance.TryBootstrapFromDefaults<PositionLerp>();
            _instance.TryBootstrapFromDefaults<RotationLerp>();
            _instance.TryBootstrapFromDefaults<ScaleLerp>();
            _instance.TryBootstrapFromDefaults<TransformLerp>();
        }

        public bool TryGet(string componentTypeFullName, string propertyPath, out UniversalEntry entry)
        {
            entry = null;

            foreach (var e in entries)
            {
                if (e.componentTypeName == componentTypeFullName &&
                    e.propertyPath == propertyPath)
                    entry = e;
            }

            return entry != null;
        }

        public void TryBootstrapFromDefaults<T>() where T : Component
        {
            var go = new GameObject($"~{typeof(T).Name}Defaults~");
            var it = go.AddComponent<T>();
            try
            {
                var so = new SerializedObject(it);
                var iter = so.GetIterator();
                if (iter.NextVisible(true))
                {
                    do
                    {
                        if (iter.propertyPath == "m_Script") continue;

                        UniversalValueType vt;
                        if (!TryMapType(iter.propertyType, out vt)) continue;

                        var ue = new UniversalEntry
                        {
                            componentTypeName = typeof(T).FullName,
                            propertyPath = iter.propertyPath,
                            valueType = vt
                        };

                        CopyFromProperty(ue, iter);
                        entries.Add(ue);
                    }
                    while (iter.NextVisible(false));
                }
                EditorUtility.SetDirty(this);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        public static bool TryMapType(SerializedPropertyType spt, out UniversalValueType vt)
        {
            switch (spt)
            {
                case SerializedPropertyType.Boolean: vt = UniversalValueType.Bool; return true;
                case SerializedPropertyType.Integer: vt = UniversalValueType.Int; return true;
                case SerializedPropertyType.Float: vt = UniversalValueType.Float; return true;
                case SerializedPropertyType.String: vt = UniversalValueType.String; return true;
                case SerializedPropertyType.Color: vt = UniversalValueType.Color; return true;
                case SerializedPropertyType.Vector2: vt = UniversalValueType.Vector2; return true;
                case SerializedPropertyType.Vector3: vt = UniversalValueType.Vector3; return true;
                case SerializedPropertyType.Vector2Int: vt = UniversalValueType.Vector2Int; return true;
                case SerializedPropertyType.Vector3Int: vt = UniversalValueType.Vector3Int; return true;
                case SerializedPropertyType.Enum: vt = UniversalValueType.Enum; return true;
                case SerializedPropertyType.AnimationCurve: vt = UniversalValueType.AnimationCurve; return true;
                case SerializedPropertyType.Quaternion: vt = UniversalValueType.Quaternion; return true;
                case SerializedPropertyType.LayerMask: vt = UniversalValueType.LayerMask; return true;
                default: vt = default; return false;
            }
        }

        public static void CopyFromProperty(UniversalEntry ue, SerializedProperty sp)
        {
            switch (ue.valueType)
            {
                case UniversalValueType.Bool: ue.boolValue = sp.boolValue; break;
                case UniversalValueType.Int: ue.intValue = sp.intValue; break;
                case UniversalValueType.Float: ue.floatValue = sp.floatValue; break;
                case UniversalValueType.String: ue.stringValue = sp.stringValue; break;
                case UniversalValueType.Color: ue.colorValue = sp.colorValue; break;
                case UniversalValueType.Vector2: ue.v2 = sp.vector2Value; break;
                case UniversalValueType.Vector3: ue.v3 = sp.vector3Value; break;
                case UniversalValueType.Vector2Int: ue.v2i = sp.vector2IntValue; break;
                case UniversalValueType.Vector3Int: ue.v3i = sp.vector3IntValue; break;
                case UniversalValueType.Enum: ue.enumValueIndex = sp.enumValueIndex; break;
                case UniversalValueType.AnimationCurve: ue.curve = sp.animationCurveValue; break;
                case UniversalValueType.Quaternion: ue.quaternion = sp.quaternionValue; break;
                case UniversalValueType.LayerMask: ue.layerMask = sp.intValue; break;
            }
        }

        public static bool ApplyToProperty(UniversalEntry ue, SerializedProperty sp)
        {
            if (sp == null) return false;
            switch (ue.valueType)
            {
                case UniversalValueType.Bool: sp.boolValue = ue.boolValue; break;
                case UniversalValueType.Int: sp.intValue = ue.intValue; break;
                case UniversalValueType.Float: sp.floatValue = ue.floatValue; break;
                case UniversalValueType.String: sp.stringValue = ue.stringValue; break;
                case UniversalValueType.Color: sp.colorValue = ue.colorValue; break;
                case UniversalValueType.Vector2: sp.vector2Value = ue.v2; break;
                case UniversalValueType.Vector3: sp.vector3Value = ue.v3; break;
                case UniversalValueType.Vector2Int: sp.vector2IntValue = ue.v2i; break;
                case UniversalValueType.Vector3Int: sp.vector3IntValue = ue.v3i; break;
                case UniversalValueType.Enum: sp.enumValueIndex = ue.enumValueIndex; break;
                case UniversalValueType.AnimationCurve: sp.animationCurveValue = ue.curve; break;
                case UniversalValueType.Quaternion: sp.quaternionValue = ue.quaternion; break;
                case UniversalValueType.LayerMask: sp.intValue = ue.layerMask; break;
                default: return false;
            }
            return true;
        }

        public static void ApplyLoadOnCreation(Component comp)
        {
            var so = new SerializedObject(comp);
            List<UniversalEntry> data = new List<UniversalEntry>();

            foreach (var e in Instance.entries)
            {
                if (e.loadOnCreation && e.componentTypeName == comp.GetType().FullName)
                    data.Add(e);
            }
            
            bool changed = false;

            var saveBackupMI = comp.GetType().GetMethod(
                "__Editor_SaveUniversalBackup",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic
            );

            foreach (var ue in data)
            {
                var sp = so.FindProperty(ue.propertyPath);
                if (sp == null) continue;

                if (saveBackupMI != null)
                {
                    try
                    {
                        saveBackupMI.Invoke(comp, new object[] { so, ue.propertyPath });
                    }
                    catch { }
                }
                
                if (ApplySingle(comp, ue.propertyPath)) changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
            }
        }

        public static event Action OnChanged;
        public static void NotifyChanged() => OnChanged?.Invoke();

        public static event Action<string> OnEntryValueChanged;

        public static void NotifyEntryValueChanged(string propertyPath)
        {
            OnEntryValueChanged?.Invoke(propertyPath);
        }

        public static bool ApplySingle(Component comp, string propertyPath)
        {
            var so = new SerializedObject(comp);
            var sp = so.FindProperty(propertyPath);
            if (sp == null) return false;

            if (Application.isPlaying &&
                UniversalsRuntime.TryGetCurrent(comp.GetType(), propertyPath, out var rtType, out var boxed))
            {
                switch (rtType)
                {
                    case UniversalsRuntime.RTValueType.Bool: sp.boolValue = (bool)boxed; break;
                    case UniversalsRuntime.RTValueType.Int: sp.intValue = (int)boxed; break;
                    case UniversalsRuntime.RTValueType.Float: sp.floatValue = (float)boxed; break;
                    case UniversalsRuntime.RTValueType.String: sp.stringValue = (string)boxed; break;
                    case UniversalsRuntime.RTValueType.Color: sp.colorValue = (Color)boxed; break;
                    case UniversalsRuntime.RTValueType.Vector2: sp.vector2Value = (Vector2)boxed; break;
                    case UniversalsRuntime.RTValueType.Vector3: sp.vector3Value = (Vector3)boxed; break;
                    case UniversalsRuntime.RTValueType.Vector2Int: sp.vector2IntValue = (Vector2Int)boxed; break;
                    case UniversalsRuntime.RTValueType.Vector3Int: sp.vector3IntValue = (Vector3Int)boxed; break;
                    case UniversalsRuntime.RTValueType.Enum: sp.enumValueIndex = (int)boxed; break;
                    case UniversalsRuntime.RTValueType.Quaternion: sp.quaternionValue = (Quaternion)boxed; break;
                    case UniversalsRuntime.RTValueType.LayerMask: sp.intValue = (int)boxed; break;
                    default: return false;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(comp);
                return true;
            }

            var ue = Instance.entries.Find(x => x.propertyPath == propertyPath &&
                                                x.componentTypeName == comp.GetType().FullName);
            if (ue == null) return false;
            bool changed = ApplyToProperty(ue, sp);
            if (changed) { so.ApplyModifiedProperties(); EditorUtility.SetDirty(comp); }
            return changed;
        }
    }
}
#endif
