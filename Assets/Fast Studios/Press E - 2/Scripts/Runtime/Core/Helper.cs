using UnityEngine;
using System.Reflection;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FastStudios
{
    public static class Helper
    {
        public static T GetNoMatterWhat<T>(this GameObject original, bool includeInactive = true) where T : Component
        {
            if (original == null)
            {
                Debug.LogWarning($"[PressE] Trying to get {typeof(T)} from a null gameobject");
                return null;
            }

            if (original.TryGetComponent<T>(out var temp))
            {
                return temp;
            }

            temp = original.GetComponentInChildren<T>(includeInactive);
            if (temp != null)
            {
                return temp;
            }

            temp = original.GetComponentInParent<T>(includeInactive);

            return temp;
        }

        public static T GetNoMatterWhat<T>(this GameObject original, out T outVar, bool includeInactive = true) where T : Component
        {
            if (original == null)
            {
                Debug.LogWarning($"[PressE] Trying to get {typeof(T)} from a null gameobject");
                outVar = null;
                return null;
            }

            if (original.TryGetComponent<T>(out var temp) && temp != null)
            {
                outVar = temp;
                return temp;
            }

            temp = original.GetComponentInChildren<T>(includeInactive);
            if (temp != null)
            {
                outVar = temp;
                return temp;
            }

            temp = original.GetComponentInParent<T>(includeInactive);

            outVar = temp;
            return temp;
        }

        public static Quaternion SanitizeQuaternion(Quaternion q)
        {
            if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
                return Quaternion.identity;

            float magSq = (q.x * q.x) + (q.y * q.y) + (q.z * q.z) + (q.w * q.w);
            if (magSq < 1e-12f) return Quaternion.identity;

            float invMag = 1.0f / Mathf.Sqrt(magSq);
            q.x *= invMag; q.y *= invMag; q.z *= invMag; q.w *= invMag;
            return q;
        }

        public static T CopyComponent<T>(this T original, GameObject destination) where T : Component
        {
            if (original == null || destination == null) throw new ArgumentNullException();

            var type = original.GetType();
            var copy = destination.AddComponent(type);

            const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            for (var t = type; t != null && t != typeof(Component); t = t.BaseType)
            {
                var fields = t.GetFields(FLAGS | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (field.IsInitOnly || field.IsLiteral) continue;
                    try
                    {
                        field.SetValue(copy, field.GetValue(original));
                    }
                    catch { }
                }
            }

            for (var t = type; t != null && t != typeof(Component); t = t.BaseType)
            {
                var props = t.GetProperties(FLAGS | BindingFlags.DeclaredOnly);
                foreach (var prop in props)
                {
                    if (ShouldSkipProperty(prop)) continue;

                    try
                    {
                        var value = prop.GetValue(original, null);
                        prop.SetValue(copy, value, null);
                    }
                    catch { }
                }
            }

            PostCopyFixups(original, copy);

            return copy as T;
        }

        static bool ShouldSkipProperty(PropertyInfo prop)
        {
            if (!prop.CanRead || !prop.CanWrite) return true;
            if (prop.GetIndexParameters().Length != 0) return true;
            if (Attribute.IsDefined(prop, typeof(ObsoleteAttribute))) return true;

            if (prop.Name == "name" || prop.Name == "tag" || prop.Name == "hideFlags") return true;

            if (prop.Name == "time" || prop.Name == "timeSamples") return true;

            return false;
        }

        static void PostCopyFixups(Component original, Component copy)
        {
            if (original is AudioSource src && copy is AudioSource dst)
            {
                try
                {
                    dst.rolloffMode = src.rolloffMode;
                    dst.minDistance = src.minDistance;
                    dst.maxDistance = src.maxDistance;

                    CopyCurve(src, dst, AudioSourceCurveType.CustomRolloff);
                    CopyCurve(src, dst, AudioSourceCurveType.SpatialBlend);
                    CopyCurve(src, dst, AudioSourceCurveType.ReverbZoneMix);
                    CopyCurve(src, dst, AudioSourceCurveType.Spread);
                }
                catch { }
            }
        }

        static void CopyCurve(AudioSource src, AudioSource dst, AudioSourceCurveType type)
        {
            var c = src.GetCustomCurve(type);
            if (c == null || c.keys == null || c.keys.Length == 0)
            {
                dst.SetCustomCurve(type, null);
                return;
            }

            var clone = new AnimationCurve(c.keys)
            {
                preWrapMode = c.preWrapMode,
                postWrapMode = c.postWrapMode
            };

            dst.SetCustomCurve(type, clone);
        }

        public static float ExpLerp01(float sharpness, float dt)
        {
            if (sharpness <= 0f) return 1f;
            return 1f - Mathf.Exp(-sharpness * dt);
        }

        public static void SetMaterialForObjectAndAllChilds(this GameObject parent, Material toSet)
        {
            if (parent == null) return;

            MeshRenderer[] allRenderers = parent.GetComponentsInChildren<MeshRenderer>(includeInactive: true);
            for (int i = 0; i < allRenderers.Length; i++)
            {
                var r = allRenderers[i];
                if (r == null) continue;
                r.sharedMaterial = toSet;
            }
        }

        public static Material GetMaterial(this GameObject toGet)
        {
            if (toGet == null) return null;

            MeshRenderer renderer = toGet.GetNoMatterWhat<MeshRenderer>();
            if (renderer == null) return null;

            return renderer.sharedMaterial;
        }

        public static void SetObjectInDepositPosition(this Interactable interactable, GrabDeposit deposit, BoxCollider depositBox, ObjectDepositData data)
        {
            if (interactable == null || interactable.interactableRb == null) return;
            if (deposit == null || depositBox == null || data == null) return;
            Transform depositTransform = deposit.transform;

            Vector3 originWorld = depositTransform.TransformPoint(depositBox.center);
            Quaternion baseRot = depositTransform.rotation;

            Vector3 wantPos = originWorld + (baseRot * data.Position);

            if (data.CanStack)
            {
                int index = data.StacksList.IndexOf(interactable.gameObject);

                if (index < 0) index = 0;
                if (index > data.StacksList.Count) index = data.StacksList.Count;

                wantPos = originWorld + (baseRot * (data.Position + (data.DistanceBetween * index)));
            }

            Quaternion wantRot = baseRot * Quaternion.Euler(data.Rotation);

            Transform tr = interactable.transform;
            tr.SetPositionAndRotation(wantPos, wantRot);

            Rigidbody rb = interactable.interactableRb;

            rb.position = wantPos;
            rb.rotation = wantRot;

            Physics.SyncTransforms();
            InteractionManager singleton = InteractionManager.singleton;
            if (singleton != null)
                singleton.SetInterpolationNoneUntilNextFixed(rb);

            interactable.transform.localScale = data.Scale;
        }

        public static void SmoothSetObjectInDepositPosition(this Interactable interactable, GrabDeposit deposit, BoxCollider depositBox, ObjectDepositData data)
        {
            if (interactable == null || interactable.interactableRb == null) return;
            if (deposit == null || depositBox == null) return;

            Transform depositTransform = deposit.transform;

            Vector3 originWorld = depositTransform.TransformPoint(depositBox.center);
            Quaternion baseRot = depositTransform.rotation;

            Vector3 wantPos = originWorld + (baseRot * data.Position);

            if (data.CanStack)
            {
                int index = data.StacksList.IndexOf(interactable.gameObject);

                if (index < 0) index = 0;
                if (index > data.StacksList.Count) index = data.StacksList.Count;

                wantPos = originWorld + (baseRot * (data.Position + (data.DistanceBetween * index)));
            }

            Quaternion wantRot = baseRot * Quaternion.Euler(data.Rotation);

            var rb = interactable.interactableRb;

            rb.MovePosition(wantPos);
            rb.MoveRotation(wantRot);

            interactable.transform.localScale = data.Scale;
        }

        public static void SetObjectInDepositPosition(this Interactable interactable, GrabDeposit deposit, BoxCollider depositBox, Vector3 localPos, Vector3 localEuler, Vector3 scale)
        {
            if (interactable == null || interactable.interactableRb == null) return;
            if (deposit == null || depositBox == null) return;

            Transform depositTransform = deposit.transform;
            Vector3 originWorld = depositTransform.TransformPoint(depositBox.center);
            Quaternion baseRot = depositTransform.rotation;

            Vector3 wantPos = originWorld + (baseRot * localPos);
            Quaternion wantRot = baseRot * Quaternion.Euler(localEuler);

            var rb = interactable.interactableRb;

            rb.MovePosition(wantPos);
            rb.MoveRotation(wantRot);

            interactable.transform.localScale = scale;
        }

        public static void SetObjectInDepositPosition(this Interactable interactable, GrabDeposit deposit, BoxCollider depositBox, Vector3 localPos, Quaternion rotation, Vector3 scale)
        {
            if (interactable == null || interactable.interactableRb == null) return;
            if (deposit == null || depositBox == null) return;

            Transform depositTransform = deposit.transform;
            Vector3 originWorld = depositTransform.TransformPoint(depositBox.center);
            Quaternion baseRot = depositTransform.rotation;

            Vector3 wantPos = originWorld + (baseRot * localPos);
            Quaternion wantRot = baseRot * rotation;

            var rb = interactable.interactableRb;

            rb.MovePosition(wantPos);
            rb.MoveRotation(wantRot);

            interactable.transform.localScale = scale;
        }

        public static Vector3? GetPosition(this Interactable interactable, GrabDeposit deposit, BoxCollider depositBox, ObjectDepositData data)
        {
            if (interactable == null || interactable.interactableRb == null) return null;
            if (deposit == null || depositBox == null) return null;

            Transform depositTransform = deposit.transform;
            Vector3 originWorld = depositTransform.TransformPoint(depositBox.center);
            Quaternion baseRot = depositTransform.rotation;

            Vector3 wantPos = originWorld + (baseRot * data.Position);

            if (data.CanStack)
            {
                int index = data.StacksList.Count;

                if (index < 0) index = 0;
                if (index > data.StacksList.Count) index = data.StacksList.Count;

                wantPos = originWorld + (baseRot * (data.Position + (data.DistanceBetween * index)));
            }

            return wantPos;

        }

        public static bool SameObject(this Interactable original, Interactable clone)
        {
            if (original == null || clone == null) return false;
            if (original.interactMode != InteractMode.Grab || clone.interactMode != InteractMode.Grab) return false;

            return original.GrabId == clone.GrabId;
        }
    }
}