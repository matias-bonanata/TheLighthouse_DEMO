using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FastStudios
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class ItemPositionAdjustment : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool AutoDeleteWhenUnselected = true;

        [HideInInspector] public GrabDeposit deposit;
        [HideInInspector] public int index = -1;
        Vector3 _lastOriginWorld;
        Quaternion _lastBaseRot;

        Vector3 _lastDataPos, _lastDataRot, _lastDataScale;

        const float kPosEpsSqr = 1e-8f;
        const float kScaleEpsSqr = 1e-8f;
        const float kAngleEps = 5e-4f;

        void OnEnable()
        {
            transform.hasChanged = false;
            CacheFromDeposit();
            CacheFromData();
        }

        void Update()
        {
            if (Application.isPlaying) return;
            if (deposit == null) return;
            if (deposit.SpecificObjects == null) return;
            if (index < 0 || index >= deposit.SpecificObjects.Count) return;

            var data = deposit.SpecificObjects[index];
            if (data == null) return;

            if (!TryGetDepositFrame(out var originWorld, out var baseRot))
                return;

            deposit.IsUsingAdjuster = true;

            if (Selection.activeGameObject != gameObject && AutoDeleteWhenUnselected)
            {
                End();
                return;
            }

            bool depositFrameChanged =
                (originWorld - _lastOriginWorld).sqrMagnitude > kPosEpsSqr ||
                Quaternion.Angle(baseRot, _lastBaseRot) > kAngleEps;

            bool dataChanged =
                (data.Position - _lastDataPos).sqrMagnitude > kPosEpsSqr ||
                (data.Scale - _lastDataScale).sqrMagnitude > kScaleEpsSqr ||
                (data.Rotation - _lastDataRot).sqrMagnitude > kPosEpsSqr;

            if (transform.hasChanged)
            {
                PushTransformToData(originWorld, baseRot, data);
                transform.hasChanged = false;

                CacheFromDeposit(originWorld, baseRot);
                CacheFromData(data);

                UnityEditor.SceneView.RepaintAll();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                return;
            }

            if (depositFrameChanged || dataChanged)
            {
                PullDataToTransform(originWorld, baseRot, data);

                CacheFromDeposit(originWorld, baseRot);
                CacheFromData(data);

                transform.hasChanged = false;
                UnityEditor.SceneView.RepaintAll();
            }
        }

        public void End()
        {
            Selection.activeGameObject = deposit.gameObject;
            EditorGUIUtility.PingObject(deposit.gameObject);

            deposit.IsUsingAdjuster = false;
            DestroyImmediate(gameObject);
        }

        public void Bind(GrabDeposit dep, int idx)
        {
            deposit = dep;
            index = idx;

            CacheFromDeposit();
            CacheFromData();

            if (deposit != null &&
                deposit.SpecificObjects != null &&
                index >= 0 && index < deposit.SpecificObjects.Count &&
                deposit.SpecificObjects[index] != null &&
                TryGetDepositFrame(out var originWorld, out var baseRot))
            {
                PullDataToTransform(originWorld, baseRot, deposit.SpecificObjects[index]);
                CacheFromDeposit(originWorld, baseRot);
                CacheFromData(deposit.SpecificObjects[index]);
                transform.hasChanged = false;
            }
        }

        bool TryGetDepositFrame(out Vector3 originWorld, out Quaternion baseRot)
        {
            originWorld = default;
            baseRot = default;

            if (deposit == null) return false;

            var box = deposit.GetComponent<BoxCollider>();
            if (!box) return false;

            originWorld = deposit.transform.TransformPoint(box.center);
            baseRot = deposit.transform.rotation;
            return true;
        }

        void PullDataToTransform(Vector3 originWorld, Quaternion baseRot, ObjectDepositData data)
        {
            Vector3 wantPos = originWorld + (baseRot * data.Position);
            Quaternion wantRot = baseRot * Quaternion.Euler(data.Rotation);
            Vector3 wantScale = data.Scale;

            transform.SetPositionAndRotation(wantPos, wantRot);
            transform.localScale = wantScale;
        }

        void PushTransformToData(Vector3 originWorld, Quaternion baseRot, ObjectDepositData data)
        {
            var inv = Quaternion.Inverse(baseRot);

            Vector3 newPos = inv * (transform.position - originWorld);
            Quaternion relRot = inv * transform.rotation;
            Vector3 newRot = relRot.eulerAngles;
            Vector3 newScale = transform.localScale;

            bool changed =
                (newPos - data.Position).sqrMagnitude > kPosEpsSqr ||
                (newScale - data.Scale).sqrMagnitude > kScaleEpsSqr ||
                (newRot - data.Rotation).sqrMagnitude > kPosEpsSqr;

            if (!changed) return;

            UnityEditor.Undo.RecordObject(deposit, "Adjust Deposit Item");
            data.Position = newPos;
            data.Rotation = newRot;
            data.Scale = newScale;

            UnityEditor.EditorUtility.SetDirty(deposit);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(deposit);

            deposit.ClampAllSpecificObjectsToBox();
            PullDataToTransform(originWorld, baseRot, data);
            transform.hasChanged = false;
        }

        void CacheFromDeposit()
        {
            if (TryGetDepositFrame(out var o, out var r))
                CacheFromDeposit(o, r);
        }

        void CacheFromDeposit(Vector3 originWorld, Quaternion baseRot)
        {
            _lastOriginWorld = originWorld;
            _lastBaseRot = baseRot;
        }

        void CacheFromData()
        {
            if (deposit == null) return;
            if (deposit.SpecificObjects == null) return;
            if (index < 0 || index >= deposit.SpecificObjects.Count) return;
            var d = deposit.SpecificObjects[index];
            if (d != null) CacheFromData(d);
        }

        void CacheFromData(ObjectDepositData d)
        {
            _lastDataPos = d.Position;
            _lastDataRot = d.Rotation;
            _lastDataScale = d.Scale;
        }
#else
        void Awake()
        {
            Destroy(this);
        }
#endif
    }
}
