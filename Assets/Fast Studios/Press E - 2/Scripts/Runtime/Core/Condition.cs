using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FastStudios
{
    public enum ConditionOperator
    {
        [InspectorName("=")] Equal,
        [InspectorName("!=")] NotEqual,
        [InspectorName(">")] Greater,
        [InspectorName(">=")] GreaterOrEqual,
        [InspectorName("<")] Less,
        [InspectorName("<=")] LessOrEqual,

        [InspectorName("X =")] XEqual,
        [InspectorName("Y =")] YEqual,
        [InspectorName("Z =")] ZEqual,

        Contains, [InspectorName("Starts With")] StartsWith, [InspectorName("Ends With")] EndsWith,
        [InspectorName("Is True")] IsTrue, [InspectorName("Is False")] IsFalse,
        [InspectorName("Is Null")] IsNull, [InspectorName("Not Null")] NotNull
    }

    public enum LogicalJoin { And, Or }


    [Serializable]
    public class ValueBinding
    {
        public UnityEngine.Object target;

        [SerializeField] private string componentTypeName;

        [SerializeField] private string member;

        [NonSerialized] private object _instance;
        [NonSerialized] private FieldInfo _field;
        [NonSerialized] private PropertyInfo _prop;

        public string MemberName
        {
            get => member;
            set { member = value; _field = null; _prop = null; }
        }

        object ResolveInstance()
        {
            if (target is GameObject go)
            {
                if (!string.IsNullOrEmpty(componentTypeName))
                {
                    var t = Type.GetType(componentTypeName);
                    if (t == null) return go.transform;
                    if (t == typeof(GameObject)) return go;
                    if (t == typeof(Transform)) return go.transform;
                    return go.GetComponent(t);
                }
                return go.transform;
            }

            return null;
        }

        public bool Resolve()
        {
            _instance = ResolveInstance();
            _field = null; _prop = null;

            if (_instance == null || string.IsNullOrEmpty(member)) return false;

            if (_instance is GameObject && member == "GameObject")
                return true;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var t = _instance.GetType();

            _field = t.GetField(member, flags);
            _prop = _field == null ? t.GetProperty(member, flags) : null;

            return _field != null || (_prop != null && _prop.CanRead);
        }

        public Type ValueType
        {
            get
            {
                if (_instance is GameObject && member == "GameObject") return typeof(GameObject);

                if (_field == null && _prop == null && !Resolve()) return null;
                return _field != null ? _field.FieldType : _prop.PropertyType;
            }
        }

        public object Get()
        {
            if (_instance == null && !Resolve()) return null;
            if (_instance is GameObject go && member == "GameObject") return go;

            if (_field == null && _prop == null && !Resolve()) return null;
            return _field != null ? _field.GetValue(_instance) : _prop.GetValue(_instance);
        }

        public void SetComponentType(Type t) => componentTypeName = t?.AssemblyQualifiedName;
        public Type GetComponentType() => string.IsNullOrEmpty(componentTypeName) ? null : Type.GetType(componentTypeName);
    }

    [Serializable]
    public class Condition
    {
        public ValueBinding left;
        public ConditionOperator op = ConditionOperator.Equal;

        public bool IsPlaying => Application.isPlaying;
        public bool RightSideDynamic = false;
        public ValueBinding right;

        public LogicalJoin joinToNext = LogicalJoin.And;

        public float number;
        public string str;
        public bool boolean;
        public GameObject GameObject;
        public UnityEngine.Object obj;
        public Vector2 vector2;
        public Vector3 vector3;
        public Quaternion quaternionConst = Quaternion.identity;

        public bool UseKey;
        public KeyAccept KeyCheckMethod = KeyAccept.KeyName;
        public Key SpecificKey;
        public string KeyName;

        public bool Evaluate()
        {
            object l = left?.Get();

            if (op == ConditionOperator.IsNull) return l == null;
            if (op == ConditionOperator.NotNull) return l != null;

            object r = null;
            if (RightSideDynamic)
            {
                r = right?.Get();
            }
            else
            {
                var lt = left?.ValueType;

                if (lt == typeof(string)) r = str;
                else if (lt == typeof(bool)) r = boolean;
                else if (lt == typeof(Vector2) || lt == typeof(Vector3))
                {
                    bool wantsVectorConst = op == ConditionOperator.Equal || op == ConditionOperator.NotEqual;
                    r = wantsVectorConst
                        ? (lt == typeof(Vector2) ? vector2 : vector3)
                        : number;
                }
                else if (lt == typeof(Quaternion)) r = quaternionConst;
                else if (typeof(UnityEngine.Object).IsAssignableFrom(lt)) r = obj;
                else if (lt != null)
                {
                    try { r = Convert.ChangeType(number, Nullable.GetUnderlyingType(lt) ?? lt); }
                    catch { r = number; }
                }
            }

            return ConditionRuntime.Compare(l, r, op);
        }
    }

    [Serializable]
    public class ConditionGroup
    {
        public List<Condition> items = new();
        public List<LogicalJoin> joins = new();
    }

    public static class ConditionRuntime
    {
        static bool EvaluateKeyItem(Condition item)
        {
            var im = InteractionManager.singleton;
            if (im == null) return false;

            var obtainedKeys = im.ObtainedKeys;
            if (obtainedKeys == null || obtainedKeys.Count == 0) return false;

            switch (item.KeyCheckMethod)
            {
                case KeyAccept.KeyName:
                    {
                        string keyName = item.KeyName;
                        for (int i = 0; i < obtainedKeys.Count; i++)
                        {
                            var k = obtainedKeys[i];
                            if (k != null && k.KeyName == keyName)
                                return true;
                        }
                        return false;
                    }

                case KeyAccept.SpecificKey:
                    return item.SpecificKey != null && obtainedKeys.Contains(item.SpecificKey);
            }

            return false;
        }

        public static bool Evaluate(ConditionGroup g)
        {
            if (g == null || g.items == null || g.items.Count == 0) return true;

            bool EvalItem(int index)
            {
                var it = g.items[index];
                if (it == null) return true;
                return it.UseKey ? EvaluateKeyItem(it) : (it.Evaluate());
            }

            bool acc = EvalItem(0);

            for (int i = 1; i < g.items.Count; i++)
            {
                bool next = EvalItem(i);
                var join = (g.joins != null && (i - 1) < g.joins.Count) ? g.joins[i - 1] : LogicalJoin.And;
                acc = (join == LogicalJoin.And) ? (acc && next) : (acc || next);
            }
            return acc;
        }

        public static bool Compare(object l, object r, ConditionOperator op)
        {
            if (l is Vector2 v2L)
            {
                if (op == ConditionOperator.Equal || op == ConditionOperator.NotEqual)
                {
                    var v2R = r is Vector2 v2 ? v2 : Vector2.zero;
                    bool eq = (v2L - v2R).sqrMagnitude < 1e-6f;
                    return (op == ConditionOperator.Equal) ? eq : !eq;
                }

                if (op == ConditionOperator.XEqual || op == ConditionOperator.YEqual)
                {
                    double rhs =
                        r is Vector2 rv2 ? (op == ConditionOperator.XEqual ? rv2.x : rv2.y) :
                        r is Vector3 rv3 ? (op == ConditionOperator.XEqual ? rv3.x : rv3.y) :
                        Convert.ToDouble(r ?? 0);

                    double lhs = (op == ConditionOperator.XEqual ? v2L.x : v2L.y);
                    return Math.Abs(lhs - rhs) < 1e-6;
                }

                return CompareNumbers(v2L.magnitude, Convert.ToDouble(r ?? 0), op);
            }

            if (l is Vector3 v3L)
            {
                if (op == ConditionOperator.Equal || op == ConditionOperator.NotEqual)
                {
                    var v3R = r is Vector3 v3 ? v3 : Vector3.zero;
                    bool eq = (v3L - v3R).sqrMagnitude < 1e-6f;
                    return (op == ConditionOperator.Equal) ? eq : !eq;
                }

                if (op == ConditionOperator.XEqual || op == ConditionOperator.YEqual || op == ConditionOperator.ZEqual)
                {
                    double rhs =
                        r is Vector3 rv3 ? (op == ConditionOperator.XEqual ? rv3.x : op == ConditionOperator.YEqual ? rv3.y : rv3.z) :
                        r is Vector2 rv2 ? (op == ConditionOperator.XEqual ? rv2.x : op == ConditionOperator.YEqual ? rv2.y : 0.0) :
                        Convert.ToDouble(r ?? 0);

                    double lhs = (op == ConditionOperator.XEqual ? v3L.x : op == ConditionOperator.YEqual ? v3L.y : v3L.z);
                    return Math.Abs(lhs - rhs) < 1e-6;
                }

                return CompareNumbers(v3L.magnitude, Convert.ToDouble(r ?? 0), op);
            }

            if (l is string ls)
            {
                string rs = r?.ToString() ?? string.Empty;
                switch (op)
                {
                    case ConditionOperator.Equal: return string.Equals(ls, rs, StringComparison.Ordinal);
                    case ConditionOperator.NotEqual: return !string.Equals(ls, rs, StringComparison.Ordinal);
                    case ConditionOperator.Contains: return ls.Contains(rs, StringComparison.Ordinal);
                    case ConditionOperator.StartsWith: return ls.StartsWith(rs, StringComparison.Ordinal);
                    case ConditionOperator.EndsWith: return ls.EndsWith(rs, StringComparison.Ordinal);
                    default: return false;
                }
            }

            if (l is bool lb)
            {
                switch (op)
                {
                    case ConditionOperator.IsTrue: return lb;
                    case ConditionOperator.IsFalse: return !lb;
                    case ConditionOperator.Equal: return Equals(lb, r is bool rb ? rb : false);
                    case ConditionOperator.NotEqual: return !Equals(lb, r is bool rb2 ? rb2 : false);
                    default: return false;
                }
            }

            if (l is UnityEngine.Object lo)
            {
                switch (op)
                {
                    case ConditionOperator.Equal: return ReferenceEquals(lo, r as UnityEngine.Object);
                    case ConditionOperator.NotEqual: return !ReferenceEquals(lo, r as UnityEngine.Object);
                    case ConditionOperator.IsNull: return lo == null;
                    case ConditionOperator.NotNull: return lo != null;
                    default: return false;
                }
            }

            var lt = l?.GetType();
            if (lt != null && lt.IsEnum)
            {
                long lv = Convert.ToInt64(l);
                long rv = Convert.ToInt64(r ?? 0);
                return CompareNumbers(lv, rv, op);
            }

            if (l is IConvertible)
            {
                double ld = Convert.ToDouble(l);
                double rd = Convert.ToDouble(r ?? 0);
                return CompareNumbers(ld, rd, op);
            }

            return op switch
            {
                ConditionOperator.Equal => Equals(l, r),
                ConditionOperator.NotEqual => !Equals(l, r),
                _ => false
            };
        }

        static bool CompareNumbers(double a, double b, ConditionOperator op) => op switch
        {
            ConditionOperator.Equal => a == b,
            ConditionOperator.NotEqual => a != b,
            ConditionOperator.Greater => a > b,
            ConditionOperator.GreaterOrEqual => a >= b,
            ConditionOperator.Less => a < b,
            ConditionOperator.LessOrEqual => a <= b,
            _ => false
        };
    }

    public static class InspectorLabels
    {
        public const string None = " None ";
    }
}