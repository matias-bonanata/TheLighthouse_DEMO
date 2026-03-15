using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace FastStudios
{
    public static class UniversalsRuntime
    {
        public enum RTValueType
        {
            Bool, Int, Float, String,
            Color, Vector2, Vector3, Vector2Int, Vector3Int,
            Enum, Quaternion, LayerMask
        }

        [Serializable]
        class RTEntry
        {
            public Type componentType;
            public string propertyPath;
            public RTValueType type;
            public object boxed;
        }

        static readonly Dictionary<(Type t, string path), RTEntry> _store =
            new Dictionary<(Type, string), RTEntry>();

        static readonly Dictionary<Type, HashSet<string>> _allowedPathsByType =
                new Dictionary<Type, HashSet<string>>();

        static readonly Dictionary<Type, HashSet<string>> _loadOnCreationByType = new();
        static readonly HashSet<(int id, Type t, string path)> _locApplied = new();

        public static event System.Action<string> OnUniversalNotFound;

        static int _lastWarnFrame = -1;
        static readonly HashSet<string> _warnedThisFrame
            = new HashSet<string>();

        static readonly Dictionary<Type, Dictionary<string, RTValueType>> _expectedTypeByType
            = new Dictionary<Type, Dictionary<string, RTValueType>>();

        static RTEntry GetOrCreate(Type compType, string propertyPath, RTValueType vt)
        {
            var key = (compType, propertyPath);
            if (!_store.TryGetValue(key, out var e))
            {
                e = new RTEntry { componentType = compType, propertyPath = propertyPath, type = vt };
                _store[key] = e;
            }
            else
            {
                e.type = vt;
            }
            return e;
        }

        public static bool SetBool<TComp>(string propertyPath, bool value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Bool, value, applyNow);

        public static bool SetInt<TComp>(string propertyPath, int value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Int, value, applyNow);

        public static bool SetFloat<TComp>(string propertyPath, float value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Float, value, applyNow);

        public static bool SetString<TComp>(string propertyPath, string value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.String, value, applyNow);

        public static bool SetEnumIndex<TComp>(string propertyPath, int enumIndex, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Enum, enumIndex, applyNow);

        public static bool SetColor<TComp>(string propertyPath, Color value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Color, value, applyNow);

        public static bool SetVector2<TComp>(string propertyPath, Vector2 value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Vector2, value, applyNow);

        public static bool SetVector3<TComp>(string propertyPath, Vector3 value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Vector3, value, applyNow);

        public static bool SetVector2Int<TComp>(string propertyPath, Vector2Int value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Vector2Int, value, applyNow);

        public static bool SetVector3Int<TComp>(string propertyPath, Vector3Int value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Vector3Int, value, applyNow);

        public static bool SetQuaternion<TComp>(string propertyPath, Quaternion value, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.Quaternion, value, applyNow);

        public static bool SetLayerMask<TComp>(string propertyPath, int layerMask, bool applyNow = true) where TComp : Component
            => SetInternal<TComp>(propertyPath, RTValueType.LayerMask, layerMask, applyNow);

        public static bool SetKeyCode<TComp>(string propertyPath, KeyCode key, bool applyNow = true) where TComp : Component
            => SetEnumIndex<TComp>(propertyPath, (int)key, applyNow);

        static bool SetInternal<TComp>(string propertyPath, RTValueType vt, object boxed, bool applyNow) where TComp : Component
        {
            if (!EnsurePathExistsOrWarn<TComp>(propertyPath, out var normalized))
                return false;

            var t = typeof(TComp);
            RTValueType expected = default;
            bool hasExpected = _expectedTypeByType.TryGetValue(t, out var map) && map.TryGetValue(normalized, out expected);

            if (!hasExpected)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var fi = t.GetField(normalized, flags);
                var pi = t.GetProperty(normalized, flags);
                Type target = fi != null ? fi.FieldType : (pi != null && pi.CanWrite ? pi.PropertyType : null);
                if (target != null && TryMapSystemTypeToVT(target, out var vt2))
                {
                    expected = vt2;
                    hasExpected = true;
                }
            }

            if (hasExpected && expected != vt)
            {
                string suggestion = $"Use Set{VTName(expected)}<{t.Name}>(nameof({t.Name}.{normalized}), ...)";
                string msg = $"[Press E] Type mismatch for '{normalized}' on {t.Name}: expected {VTName(expected)}, but got {VTName(vt)}. {suggestion}";
                WarnOncePerFrame(msg);
                return false;
            }

            var e = GetOrCreate(typeof(TComp), normalized, vt);
            e.boxed = boxed;
            if (applyNow) ApplyToScene<TComp>(normalized, onlyIfBound: true);
            return true;
        }

        public static int ApplyToScene<TComp>(string propertyPath, bool onlyIfBound = true) where TComp : Component
        {
            var key = (typeof(TComp), propertyPath);
            if (!_store.TryGetValue(key, out var e)) return 0;

            var comps = UnityEngine.Object.FindObjectsByType<TComp>(FindObjectsSortMode.None);

            int applied = 0;
            foreach (var comp in comps)
            {
                if (comp == null) continue;
                if (onlyIfBound && !IsBound(comp, propertyPath)) continue;
                if (TrySetViaReflection(comp, propertyPath, e)) applied++;
            }
            return applied;
        }

        public static int ApplyAllFor<TComp>(bool onlyIfBound = true) where TComp : Component
        {
            int total = 0;
            foreach (var kv in _store)
            {
                if (kv.Key.t == typeof(TComp))
                    total += ApplyToScene<TComp>(kv.Key.path, onlyIfBound);
            }
            return total;
        }

        static bool IsBound(Component comp, string path)
        {
            var mi = comp.GetType().GetMethod("IsUniversalBound",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (mi == null) return false;
            try
            {
                var r = mi.Invoke(comp, new object[] { path });
                return r is bool b && b;
            }
            catch { return false; }
        }

        static bool TrySetViaReflection(Component comp, string propertyPath, RTEntry e)
        {
            string name = propertyPath;
            int dot = name.IndexOf('.');
            if (dot >= 0) name = name.Substring(0, dot);

            var t = comp.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var fi = t.GetField(name, flags);
            if (fi != null)
            {
                object val = ConvertToTarget(e, fi.FieldType);
                try { fi.SetValue(comp, val); return true; } catch { return false; }
            }

            var pi = t.GetProperty(name, flags);
            if (pi != null && pi.CanWrite)
            {
                object val = ConvertToTarget(e, pi.PropertyType);
                try { pi.SetValue(comp, val, null); return true; } catch { return false; }
            }

            return false;
        }

        static object ConvertToTarget(RTEntry e, Type targetType)
        {
            switch (e.type)
            {
                case RTValueType.Bool: return e.boxed is bool b ? b : default(bool);
                case RTValueType.Int: return e.boxed is int i ? ConvertForIntTarget(i, targetType) : 0;
                case RTValueType.Float: return e.boxed is float f ? f : default(float);
                case RTValueType.String: return e.boxed as string ?? string.Empty;
                case RTValueType.Color: return e.boxed is Color col ? col : default(Color);
                case RTValueType.Vector2: return e.boxed is Vector2 v2 ? v2 : default(Vector2);
                case RTValueType.Vector3: return e.boxed is Vector3 v3 ? v3 : default(Vector3);
                case RTValueType.Vector2Int: return e.boxed is Vector2Int v2i ? v2i : default(Vector2Int);
                case RTValueType.Vector3Int: return e.boxed is Vector3Int v3i ? v3i : default(Vector3Int);
                case RTValueType.Quaternion: return e.boxed is Quaternion q ? q : default(Quaternion);
                case RTValueType.LayerMask:
                    {
                        int mask = e.boxed is int li ? li : 0;
                        if (targetType == typeof(LayerMask))
                        {
                            var lm = new LayerMask();
                            typeof(LayerMask).GetProperty("value").SetValue(lm, mask, null);
                            return lm;
                        }
                        return mask;
                    }
                case RTValueType.Enum:
                    {
                        int idx = e.boxed is int ei ? ei : 0;
                        if (targetType.IsEnum) return Enum.ToObject(targetType, idx);
                        return idx;
                    }
                default:
                    return null;
            }
        }

        static object ConvertForIntTarget(int i, Type targetType)
        {
            if (targetType.IsEnum) return Enum.ToObject(targetType, i);
            if (targetType == typeof(LayerMask))
            {
                var lm = new LayerMask();
                typeof(LayerMask).GetProperty("value").SetValue(lm, i, null);
                return lm;
            }
            return i;
        }

        static bool EnsurePathExistsOrWarn<TComp>(string propertyPath, out string normalizedName) where TComp : Component
        {
            var t = typeof(TComp);
            string name = propertyPath;
            int dot = name.IndexOf('.');
            if (dot >= 0) name = name.Substring(0, dot);
            normalizedName = name;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (_allowedPathsByType.TryGetValue(t, out var allowed) && allowed != null && allowed.Count > 0)
            {
                if (allowed.Contains(name)) return true;

                string SuggestFromAllowed()
                {
                    string ci = null;
                    string sw = null;
                    foreach (string s in allowed)
                    {
                        if (string.Equals(s, name, StringComparison.OrdinalIgnoreCase))
                        {
                            ci = s;
                        }
                        
                        if (s.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                        {
                            sw = s;
                        }
                    }
                    if (!string.IsNullOrEmpty(ci)) return ci;
                    if (!string.IsNullOrEmpty(sw)) return sw;

                    int Dist(string a, string b)
                    {
                        var dp = new int[a.Length + 1, b.Length + 1];
                        for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
                        for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
                        for (int i = 1; i <= a.Length; i++)
                            for (int j = 1; j <= b.Length; j++)
                            {
                                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                                dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                            }
                        return dp[a.Length, b.Length];
                    }

                    return allowed.OrderBy(c => Dist(c, name)).FirstOrDefault();
                }

                var sug = SuggestFromAllowed();
                string msg = (sug != null)
                    ? $"[Press E] '{name}' doesn't exist on {t.Name}. Did you mean '{sug}'?"
                    : $"[Press E] '{name}' doesn't exist on {t.Name}.";
                WarnOncePerFrame(msg);
                return false;
            }

            var fi = t.GetField(name, flags);
            var pi = t.GetProperty(name, flags);

            bool ok =
                (fi != null && (fi.IsPublic || Attribute.IsDefined(fi, typeof(SerializeField))) && IsRuntimeSupportedType(fi.FieldType)) ||
                (pi != null && pi.CanWrite && IsRuntimeSupportedType(pi.PropertyType));

            if (ok) return true;

            string SuggestMemberName()
            {
                var candidates = t.GetMembers(flags)
                    .Where(m =>
                    {
                        if (m.MemberType == MemberTypes.Field)
                        {
                            var f = (FieldInfo)m;
                            return (f.IsPublic || Attribute.IsDefined(f, typeof(SerializeField))) && IsRuntimeSupportedType(f.FieldType);
                        }
                        if (m.MemberType == MemberTypes.Property)
                        {
                            var p = (PropertyInfo)m;
                            return p.CanWrite && IsRuntimeSupportedType(p.PropertyType);
                        }
                        return false;
                    })
                    .Select(m => m.Name)
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                var ci = candidates.FirstOrDefault(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(ci)) return ci;

                var sw = candidates.FirstOrDefault(c => c.StartsWith(name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(sw)) return sw;

                int Dist(string a, string b)
                {
                    var dp = new int[a.Length + 1, b.Length + 1];
                    for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
                    for (int j = 0; j <= b.Length; j++) dp[0, j] = j;
                    for (int i = 1; i <= a.Length; i++)
                        for (int j = 1; j <= b.Length; j++)
                        {
                            int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                            dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1), dp[i - 1, j - 1] + cost);
                        }
                    return dp[a.Length, b.Length];
                }

                return candidates.OrderBy(c => Dist(c, name)).FirstOrDefault();
            }

            var suggestion = SuggestMemberName();
            string warn = (suggestion != null)
                ? $"[Press E] '{name}' doesn't exist on {t.Name}. Did you mean '{suggestion}'?"
                : $"[Press E] '{name}' doesn't exist on {t.Name}.";
            WarnOncePerFrame(warn);
            return false;
        }


        static void WarnOncePerFrame(string msg)
        {
            int f = Time.frameCount;
            if (f != _lastWarnFrame)
            {
                _lastWarnFrame = f;
                _warnedThisFrame.Clear();
            }
            if (_warnedThisFrame.Add(msg))
            {
                Debug.LogWarning(msg);
                OnUniversalNotFound?.Invoke(msg);
            }
        }

        static bool IsRuntimeSupportedType(Type t)
        {
            if (t == typeof(bool) || t == typeof(int) || t == typeof(float) || t == typeof(string) ||
                t == typeof(Color) || t == typeof(Vector2) || t == typeof(Vector3) ||
                t == typeof(Vector2Int) || t == typeof(Vector3Int) ||
                t == typeof(Quaternion) || t == typeof(LayerMask) || t.IsEnum)
                return true;

            if (typeof(UnityEngine.Events.UnityEventBase).IsAssignableFrom(t))
                return false;

            return false;
        }

        static bool TryMapSystemTypeToVT(Type t, out RTValueType vt)
        {
            if (t == typeof(bool)) { vt = RTValueType.Bool; return true; }
            if (t == typeof(int)) { vt = RTValueType.Int; return true; }
            if (t == typeof(float)) { vt = RTValueType.Float; return true; }
            if (t == typeof(string)) { vt = RTValueType.String; return true; }
            if (t == typeof(Color)) { vt = RTValueType.Color; return true; }
            if (t == typeof(Vector2)) { vt = RTValueType.Vector2; return true; }
            if (t == typeof(Vector3)) { vt = RTValueType.Vector3; return true; }
            if (t == typeof(Vector2Int)) { vt = RTValueType.Vector2Int; return true; }
            if (t == typeof(Vector3Int)) { vt = RTValueType.Vector3Int; return true; }
            if (t == typeof(Quaternion)) { vt = RTValueType.Quaternion; return true; }
            if (t == typeof(LayerMask)) { vt = RTValueType.LayerMask; return true; }
            if (t.IsEnum) { vt = RTValueType.Enum; return true; }
            vt = default; return false;
        }

        public static bool ApplyToInstance(Component comp, string propertyPath, bool onlyIfBound = true)
        {
            var key = (comp.GetType(), propertyPath);
            if (!_store.TryGetValue(key, out var e)) return false;
            if (onlyIfBound && !IsBound(comp, propertyPath)) return false;
            return TrySetViaReflection(comp, propertyPath, e);
        }

        public static int ApplyAllForInstance(Component comp, bool onlyIfBound = true)
        {
            int applied = 0;
            var t = comp.GetType();
            foreach (var kv in _store)
            {
                if (kv.Key.t == t)
                {
                    if (!onlyIfBound || IsBound(comp, kv.Key.path))
                        if (TrySetViaReflection(comp, kv.Key.path, kv.Value)) applied++;
                }
            }
            return applied;
        }

        public static bool TryGetCurrent(Type compType, string propertyPath,
                                 out RTValueType vt, out object boxed)
        {
            if (_store.TryGetValue((compType, propertyPath), out var e))
            { vt = e.type; boxed = e.boxed; return true; }
            vt = default; boxed = null; return false;
        }

        public static void ApplyLoadOnCreationForInstance(Component comp)
        {
            var t = comp.GetType();
            if (!_loadOnCreationByType.TryGetValue(t, out var paths)) return;
            foreach (var p in paths)
            {
                var key = (comp.GetInstanceID(), t, p);
                if (_locApplied.Contains(key)) continue;
                if (_store.TryGetValue((t, p), out var e))
                {
                    TrySetViaReflection(comp, p, e);
                    _locApplied.Add(key);
                }
            }
        }

        static string VTName(RTValueType vt) => vt.ToString();

#if UNITY_EDITOR
        public static void SeedFromEditorSO(EditorTools.InteractableUniversalsSO so)
        {
            if (so == null || so.entries == null) return;
            foreach (var ue in so.entries)
            {
                var compType = Type.GetType(ue.componentTypeName);
                if (compType == null || !typeof(Component).IsAssignableFrom(compType)) continue;
                
                if (!_allowedPathsByType.TryGetValue(compType, out var allowed))
                    _allowedPathsByType[compType] = allowed = new HashSet<string>(StringComparer.Ordinal);
                if (!_expectedTypeByType.TryGetValue(compType, out var exp))
                    _expectedTypeByType[compType] = exp = new Dictionary<string, RTValueType>(StringComparer.Ordinal);

                var root = ue.propertyPath;
                int dot = root.IndexOf('.');

                if (dot >= 0) root = root[..dot];
                
                if (ue.loadOnCreation)
                {
                    if (!_loadOnCreationByType.TryGetValue(compType, out var set))
                        _loadOnCreationByType[compType] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(root);
                }
                allowed.Add(root);

                RTValueType rtv =
                    ue.valueType == EditorTools.UniversalValueType.Bool ? RTValueType.Bool :
                    ue.valueType == EditorTools.UniversalValueType.Int ? RTValueType.Int :
                    ue.valueType == EditorTools.UniversalValueType.Float ? RTValueType.Float :
                    ue.valueType == EditorTools.UniversalValueType.String ? RTValueType.String :
                    ue.valueType == EditorTools.UniversalValueType.Color ? RTValueType.Color :
                    ue.valueType == EditorTools.UniversalValueType.Vector2 ? RTValueType.Vector2 :
                    ue.valueType == EditorTools.UniversalValueType.Vector3 ? RTValueType.Vector3 :
                    ue.valueType == EditorTools.UniversalValueType.Vector2Int ? RTValueType.Vector2Int :
                    ue.valueType == EditorTools.UniversalValueType.Vector3Int ? RTValueType.Vector3Int :
                    ue.valueType == EditorTools.UniversalValueType.Enum ? RTValueType.Enum :
                    ue.valueType == EditorTools.UniversalValueType.Quaternion ? RTValueType.Quaternion :
                    ue.valueType == EditorTools.UniversalValueType.LayerMask ? RTValueType.LayerMask :
                    default;

                exp[root] = rtv;

                switch (ue.valueType)
                {
                    case EditorTools.UniversalValueType.Bool:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Bool).boxed = ue.boolValue; break;
                    case EditorTools.UniversalValueType.Int:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Int).boxed = ue.intValue; break;
                    case EditorTools.UniversalValueType.Float:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Float).boxed = ue.floatValue; break;
                    case EditorTools.UniversalValueType.String:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.String).boxed = ue.stringValue; break;
                    case EditorTools.UniversalValueType.Color:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Color).boxed = ue.colorValue; break;
                    case EditorTools.UniversalValueType.Vector2:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Vector2).boxed = ue.v2; break;
                    case EditorTools.UniversalValueType.Vector3:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Vector3).boxed = ue.v3; break;
                    case EditorTools.UniversalValueType.Vector2Int:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Vector2Int).boxed = ue.v2i; break;
                    case EditorTools.UniversalValueType.Vector3Int:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Vector3Int).boxed = ue.v3i; break;
                    case EditorTools.UniversalValueType.Enum:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Enum).boxed = ue.enumValueIndex; break;
                    case EditorTools.UniversalValueType.AnimationCurve:
                        break;
                    case EditorTools.UniversalValueType.Quaternion:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.Quaternion).boxed = ue.quaternion; break;
                    case EditorTools.UniversalValueType.LayerMask:
                        GetOrCreate(compType, ue.propertyPath, RTValueType.LayerMask).boxed = ue.layerMask.value; break;
                }
            }
        }
#endif
    }
}
