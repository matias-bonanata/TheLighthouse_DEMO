using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
#endif

namespace FastStudios
{
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(BoxCollider))]
    public class GrabDeposit : MonoBehaviour
    {
        public int Count;
        public DepositMethod depositMethod = DepositMethod.Interact;

        // Trigger
        [Tooltip("If enabled, when player passes the helding object in the collider it will deposit. If disabled, player must drop the object on the collider.")] public bool AutoRelease = true;
        [Tooltip("If disabled, if a grab object roll at collider it will deposit it. If enabled, player must be helding to deposit")] public bool NeedsToBeHeldingToDeposit = true;

        // Interact
        public bool CanPlaceDownFreely = true;

        // Both
        public bool canPlayerPickupAgain = true;
        
        public Vector3 positionOffset;
        public bool isToOverrideRotation = false;
        public Vector3 NewAngles = Vector3.zero;
        public bool isToOverrideInput = false;
        public PressEInputBind NewInput = new PressEInputBind()
        {
            InputMethod = InputMethod.Keyboard,
            Key = KeyCode.E,
            MouseButton = MouseMethod.Left
        };

        public bool PreventOthers = false;
        public List<ObjectDepositData> SpecificObjects;

        public UnityEvent OnInteractableClamp;
        public UnityEvent OnTriggerEnterEvent;
        public UnityEvent OnTriggerExitEvent;

        [HideInInspector] public BoxCollider boxCollider;

        [HideInInspector] public int lastSelectedTab;
        private int previousLayer;

        public struct DepositInteractables
        {
            public Interactable depositedInteractable;


            public Vector3? FreePosition;
            public Quaternion? FreeRotation;
        }

        public List<DepositInteractables> DepositedObjects = new List<DepositInteractables>();
        private readonly HashSet<Interactable> _deposited = new HashSet<Interactable>();
        private int _ignoreLayer;

        private readonly HashSet<Interactable> _overlapping = new HashSet<Interactable>();

        void Awake()
        {
            SetupBoxCollider();

            _ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");

            _deposited.Clear();
            for (int i = 0; i < DepositedObjects.Count; i++)
            {
                var it = DepositedObjects[i].depositedInteractable;
                if (it != null) _deposited.Add(it);
            }
        }

        void Update()
        {
            InteractionManager singleton = InteractionManager.singleton;

            if (singleton != null)
            {
                if (singleton.heldingObject != null && gameObject.layer != _ignoreLayer)
                {
                    previousLayer = gameObject.layer;
                    gameObject.layer = _ignoreLayer;
                }
                else if (singleton.heldingObject == null && gameObject.layer != previousLayer)
                {
                    gameObject.layer = previousLayer;
                }
            }

            HandleManualTriggerDeposit(singleton);
        }

        void FixedUpdate()
        {
            foreach (var element in DepositedObjects)
            {
                Interactable obj = element.depositedInteractable;

                if (IsOnSpecificList(obj, out var data))
                {
                    if (data.CanStack)
                    {
                        if (data.HasVisualStackLimit)
                        {
                            int listIndex = data.StacksList.IndexOf(obj.gameObject);
                            Vector3 originWorld = transform.TransformPoint(boxCollider.center);
                            Quaternion baseRot = transform.rotation;
                            Vector3 stackLocal = data.Position + (data.DistanceBetween * listIndex);
                            Vector3 stackWorld = originWorld + (baseRot * stackLocal);

                            bool visible = true;

                            if (data.HasVisualStackLimit && listIndex >= data.VisualStackLimit)
                                visible = false;

                            if (visible && !IsInsideDepositBox(stackWorld))
                                visible = false;

                            if (obj.gameObject.activeSelf != visible)
                                obj.gameObject.SetActive(visible);
                        }

                    }
                    obj.SmoothSetObjectInDepositPosition(this, boxCollider, data);
                }
                else
                {
                    if (CanPlaceDownFreely)
                    {
                        obj.SetObjectInDepositPosition(
                            this,
                            boxCollider,
                            element.FreePosition != null ? element.FreePosition.Value : positionOffset,
                            element.FreeRotation != null ? element.FreeRotation.Value : Quaternion.Euler(isToOverrideRotation ? NewAngles : Vector3.zero),
                            obj.transform.localScale
                        );
                    }
                    else
                    {    
                        obj.SetObjectInDepositPosition(
                            this,
                            boxCollider,
                            positionOffset,
                            isToOverrideRotation ? NewAngles : Vector3.zero,
                            obj.transform.localScale
                        );
                    }
                }
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (depositMethod == DepositMethod.Interact) return;
            if (!TryGetInteractable(other, out var interactable)) return;
            if (interactable.interactMode != InteractMode.Grab) return;
            if (_deposited.Contains(interactable)) return;
            if (interactable.grabDeposit != null && interactable.grabDeposit != this) return;
            if (interactable.JustGotIt) return;

            OnTriggerEnterEvent.Invoke();

            if (depositMethod == DepositMethod.Interact) return;

            _overlapping.Add(interactable);

            InteractionManager singleton = InteractionManager.singleton;
            if (singleton == null) return;

            bool isHeld = singleton.heldingObject != null && singleton.heldingObject == interactable.gameObject;

            if (!NeedsToBeHeldingToDeposit && !isHeld)
            {
                DepositObject(interactable);
                return;
            }

            if (AutoRelease && isHeld)
            {
                DepositObject(interactable);
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (depositMethod == DepositMethod.Interact) return;

            if (!TryGetInteractable(other, out var interactable)) return;
            if (interactable.interactMode != InteractMode.Grab) return;
            if (_deposited.Contains(interactable)) return;
            if (interactable.grabDeposit != this) return;
            if (interactable.JustGotIt) return;

            _overlapping.Add(interactable);
        }

        void OnTriggerExit(Collider other)
        {
            if (TryGetInteractable(other, out var inter) && inter != null)
            {
                _overlapping.Remove(inter);

                StartCoroutine(NextFrameJutGotItReset(inter));
            }

            if (other.TryGetComponent<Interactable>(out var interactable)
                && interactable.interactMode == InteractMode.Grab
                && _deposited.Contains(interactable) == true)
            {
                OnTriggerExitEvent.Invoke();
            }
        }

        IEnumerator NextFrameJutGotItReset(Interactable interactable)
        {
            yield return null;
            interactable.JustGotIt = false;
        }

        void SetupBoxCollider()
        {
            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            previousLayer = gameObject.layer;
        }

        private void HandleManualTriggerDeposit(InteractionManager singleton)
        {
            if (depositMethod == DepositMethod.Interact) return;
            if (AutoRelease) return;
            if (singleton == null) return;

            bool dropDown = InputHandler.GeneralInputDown(singleton.GrabDrop);
            dropDown |= singleton.GrabDrop.UIButtonDown;

            if (!dropDown)
                return;

            if (singleton.heldingObject == null || singleton.heldingInteractable == null) return;

            Interactable heldInter = singleton.heldingInteractable;
            if (heldInter == null) return;
            if (heldInter.interactMode != InteractMode.Grab) return;
            if (heldInter.JustGotIt) return;
            if (_deposited.Contains(heldInter)) return;

            if (!_overlapping.Contains(heldInter)) return;

            DepositObject(heldInter);
        }

        public void DepositObject(Interactable interactable, Vector3 position, Quaternion rotation) => DepositObject(interactable, pos: position, rot: rotation);
        public void DepositObject(Interactable interactable, ObjectDepositData dataToAdd) => DepositObject(interactable, dataToBeAdded: dataToAdd);
        public void DepositObject(Interactable interactable, ObjectDepositData dataToBeAdded = null, Vector3? pos = null, Quaternion? rot = null)
        {
            InteractionManager singleton = InteractionManager.singleton;

            if (singleton == null)
            {
                Debug.LogWarning("[PressE] Trying to get manager, but it is null. Did you add a manager to the scene?");
                return;
            }

            if (interactable == null)
            {
                Debug.LogWarning("[PressE] Trying to attatch a null gameobject");
                return;
            }

            if (interactable.interactMode != InteractMode.Grab) return;

            SetupBoxCollider();

            if (boxCollider == null) boxCollider = GetComponent<BoxCollider>();

            bool CanAdd = false;
            DepositInteractables? tempDeposit = null;

            if (IsOnSpecificList(interactable, out ObjectDepositData data))
            {
                if (data.CanStack)
                {
                    if (data.HasStackLimit)
                    {
                        if (data.StacksList.Count >= data.ActualStackLimit) return;
                    }

                    data.StacksList.Add(interactable.gameObject);

                    int index = data.StacksList.IndexOf(interactable.gameObject);
                    if (data.HasVisualStackLimit)
                    {
                        if (data.HasVisualStackLimit && index >= data.VisualStackLimit)
                        {
                            if (interactable.gameObject.gameObject.activeInHierarchy) interactable.gameObject.SetActive(false);
                        }
                    }
                }
                else if (data.StacksList.Count == 0)
                {
                    data.StacksList.Add(interactable.gameObject);
                }

                singleton.ReleaseGrab();

                interactable.SetObjectInDepositPosition(this, boxCollider, data);
                data.gameObject = interactable.gameObject;
                data.IsAttatched = true;
                CanAdd = true;
            }
            else if (GetOnSpecificListNullObject(out ObjectDepositData nullObjData))
            {
                if (nullObjData.CanStack)
                {
                    if (nullObjData.HasStackLimit)
                    {
                        if (nullObjData.StacksList.Count >= nullObjData.ActualStackLimit) return;
                    }

                    nullObjData.StacksList.Add(interactable.gameObject);
                }

                singleton.ReleaseGrab();

                nullObjData.IsNullObject = true;

                interactable.SetObjectInDepositPosition(this, boxCollider, nullObjData);
                nullObjData.gameObject = interactable.gameObject;
                nullObjData.IsAttatched = true;
                CanAdd = true;
            }
            else if (PreventOthers == false)
            {
                singleton.ReleaseGrab();

                if (dataToBeAdded != null)
                {
                    if (CanPlaceDownFreely)
                    {
                        interactable.SetObjectInDepositPosition(
                            this,
                            boxCollider,
                            dataToBeAdded
                        );
                    }
                }
                else
                {
                    if (CanPlaceDownFreely)
                    {
                        Vector3 originWorld = transform.TransformPoint(boxCollider.center);
                        Quaternion baseRot = transform.rotation;

                        Vector3 localPos = positionOffset;
                        Quaternion localRot = Quaternion.Euler(isToOverrideRotation ? NewAngles : Vector3.zero);

                        if (pos.HasValue)
                            localPos = Quaternion.Inverse(baseRot) * (pos.Value - originWorld);

                        if (rot.HasValue)
                            localRot = Quaternion.Inverse(baseRot) * rot.Value;

                        interactable.SetObjectInDepositPosition(
                            this,
                            boxCollider,
                            localPos,
                            localRot,
                            interactable.transform.localScale
                        );

                        tempDeposit = new DepositInteractables()
                        {
                            depositedInteractable = interactable,
                            FreePosition = localPos,
                            FreeRotation = localRot,
                        };
                    }
                    else
                    {    
                        interactable.SetObjectInDepositPosition(
                            this,
                            boxCollider,
                            positionOffset,
                            isToOverrideRotation ? NewAngles : Vector3.zero,
                            interactable.transform.localScale
                        );
                    }
                }

                CanAdd = true;
            }

            if (CanAdd)
            {
                interactable.grabDeposit = this;
                interactable.wasKinematic = interactable.interactableRb.isKinematic;
                interactable.interactableRb.isKinematic = true;

                interactable.SetCanInteract(canPlayerPickupAgain);
                if (tempDeposit == null)
                {
                    DepositedObjects.Add(new DepositInteractables() { depositedInteractable = interactable });
                }
                else
                {
                    DepositedObjects.Add(tempDeposit.Value);
                }

                Count = DepositedObjects.Count;

                _deposited.Add(interactable);
                OnInteractableClamp.Invoke();
            }
        }

        public void RemoveFromDeposit(Interactable interactable)
        {
            InteractionManager singleton = InteractionManager.singleton;

            if (singleton == null)
            {
                Debug.LogWarning("[PressE] Trying to get manager, but it is null. Did you add a manager to the scene?");
                return;
            }

            if (interactable == null)
            {
                Debug.LogWarning("[PressE] Trying to attatch a null gameobject");
                return;
            }

            if (interactable.interactMode != InteractMode.Grab) return;
            if (!_deposited.Contains(interactable)) return;

            interactable.interactableRb.isKinematic = interactable.wasKinematic;

            if (IsOnSpecificList(interactable, out ObjectDepositData data))
            {
                data.IsAttatched = false;

                if (data.CanStack)
                {
                    data.StacksList.Remove(interactable.gameObject);
                }
                else if (data.StacksList.Contains(interactable.gameObject))
                {
                    data.StacksList.Remove(interactable.gameObject);
                }

                if (data.IsNullObject) data.gameObject = null;
            }

            interactable.grabDeposit = null;
            interactable.JustGotIt = true;

            for (int i = DepositedObjects.Count - 1; i >= 0; i--)
            {
                if (DepositedObjects[i].depositedInteractable == interactable)
                {
                    DepositedObjects.RemoveAt(i);
                    break;
                }
            }

            Count = DepositedObjects.Count;

            _deposited.Remove(interactable);
        }

        private bool TryGetInteractable(Collider other, out Interactable interactable)
        {
            interactable = null;
            if (other == null) return false;

            if (other.TryGetComponent(out interactable) && interactable != null)
                return true;

            interactable = other.GetComponentInParent<Interactable>();
            return interactable != null;
        }

        public bool IsOnSpecificList(Interactable interactable)
        {
            foreach (ObjectDepositData x in SpecificObjects)
            {
                GameObject g = x.gameObject;

                if (g.TryGetComponent<Interactable>(out var inter) && interactable.interactMode == InteractMode.Grab)
                {
                    if (inter.SameObject(interactable)) return true;
                }
            }

            return false;
        }

        public bool IsOnSpecificList(Interactable interactable, out ObjectDepositData element)
        {
            foreach (ObjectDepositData x in SpecificObjects)
            {
                GameObject dataMainGameobject = x.gameObject;

                if (dataMainGameobject != null && dataMainGameobject.TryGetComponent<Interactable>(out var dataInter) && interactable.interactMode == InteractMode.Grab)
                {
                    if (dataInter.SameObject(interactable))
                    {
                        if (x.CanStack == false)
                        {
                            if (x.StacksList.Count != 0 &&
                                x.StacksList.Contains(interactable.gameObject) == false) continue;
                        }
                        
                        element = x;
                        return true;
                    }
                }
            }

            element = null;
            return false;
        }

        public bool GetOnSpecificListNullObject(out ObjectDepositData element)
        {
            foreach (ObjectDepositData x in SpecificObjects)
            {
                GameObject g = x.gameObject;

                if (g == null)
                {
                    element = x;
                    return true;
                }
            }

            element = null;
            return false;
        }

        public static bool TryGetValidGrabInteractable(GameObject go, out Interactable inter)
        {
            inter = null;
            if (go == null) return false;

            if (!go.TryGetComponent(out inter) || inter == null)
                return false;

            if (inter.interactMode != InteractMode.Grab)
            {
                inter = null;
                return false;
            }

            return true;
        }

        bool IsInsideDepositBox(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            Vector3 delta = local - boxCollider.center;
            Vector3 half = boxCollider.size * 0.5f;

            return Mathf.Abs(delta.x) <= half.x
                && Mathf.Abs(delta.y) <= half.y
                && Mathf.Abs(delta.z) <= half.z;
        }


#if UNITY_EDITOR

        struct RenderItem
        {
            public Mesh mesh;
            public Material[] mats;
            public Matrix4x4 localToRoot;
            public int subMeshCount;
        }

        static readonly Dictionary<int, List<RenderItem>> s_RenderCache = new();

        static List<RenderItem> GetRenderItems(GameObject prefabRootGO)
        {
            if (prefabRootGO == null) return null;

            int id = prefabRootGO.GetInstanceID();
            if (s_RenderCache.TryGetValue(id, out var cached) && cached != null) return cached;

            var list = new List<RenderItem>(16);

            var rootT = prefabRootGO.transform;

            var mfs = prefabRootGO.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                var mr = mf.GetComponent<MeshRenderer>();
                var mats = mr ? mr.sharedMaterials : null;

                Matrix4x4 localToRoot = rootT.worldToLocalMatrix * mf.transform.localToWorldMatrix;

                list.Add(new RenderItem
                {
                    mesh = mf.sharedMesh,
                    mats = mats,
                    localToRoot = localToRoot,
                    subMeshCount = mf.sharedMesh.subMeshCount
                });
            }

            var skinned = prefabRootGO.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var sr in skinned)
            {
                if (sr == null || sr.sharedMesh == null) continue;

                Matrix4x4 localToRoot = rootT.worldToLocalMatrix * sr.transform.localToWorldMatrix;

                list.Add(new RenderItem
                {
                    mesh = sr.sharedMesh,
                    mats = sr.sharedMaterials,
                    localToRoot = localToRoot,
                    subMeshCount = sr.sharedMesh.subMeshCount
                });
            }

            s_RenderCache[id] = list;
            return list;
        }

        public bool IsUsingAdjuster = false;

        void OnDrawGizmos()
        {
            if (IsUsingAdjuster == false) return;

            Draw();
        }

        void OnDrawGizmosSelected()
        {
            if (IsUsingAdjuster == true) return;

            Draw();
        }

        void Draw()
        {
            if (SpecificObjects == null || SpecificObjects.Count == 0) return;

            var box = GetComponent<BoxCollider>();
            if (!box) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 1f, 1f, 0.06f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
            Gizmos.DrawWireCube(box.center, box.size);

            Vector3 originWorld = transform.TransformPoint(box.center);
            Quaternion baseRot = transform.rotation;

            Matrix4x4 depositNoScale = Matrix4x4.TRS(originWorld, baseRot, Vector3.one);

            for (int i = 0; i < SpecificObjects.Count; i++)
            {
                var data = SpecificObjects[i];
                if (data == null) continue;
                if (data.IsAttatched) continue;

                Vector3 slotWorldPos = originWorld + (baseRot * data.Position);

                if (data.gameObject == null)
                {
                    Quaternion slotRot = baseRot * Quaternion.Euler(data.Rotation);

                    Vector3 s = data.Scale;
                    s = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
                    s.x = Mathf.Max(0.001f, s.x);
                    s.y = Mathf.Max(0.001f, s.y);
                    s.z = Mathf.Max(0.001f, s.z);

                    const float baseRadius = 0.25f;

                    Gizmos.matrix = Matrix4x4.TRS(slotWorldPos, slotRot, s);

                    Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.12f);
                    Gizmos.DrawSphere(Vector3.zero, baseRadius);

                    Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.60f);
                    Gizmos.DrawWireSphere(Vector3.zero, baseRadius);

                    Gizmos.matrix = Matrix4x4.identity;
                    continue;
                }

                Matrix4x4 itemLocal = Matrix4x4.TRS(data.Position, Quaternion.Euler(data.Rotation), data.Scale);

                Matrix4x4 itemWorld = depositNoScale * itemLocal;

                var renderItems = GetRenderItems(data.gameObject);
                if (renderItems == null || renderItems.Count == 0) continue;

                foreach (var it in renderItems)
                {
                    if (it.mesh == null) continue;

                    Matrix4x4 m = itemWorld * it.localToRoot;

                    if (it.mats != null && it.mats.Length > 0)
                    {
                        for (int sm = 0; sm < it.subMeshCount; sm++)
                        {
                            Material mat = (sm < it.mats.Length) ? it.mats[sm] : it.mats[it.mats.Length - 1];
                            if (mat != null) mat.SetPass(0);
                            Graphics.DrawMeshNow(it.mesh, m, sm);
                        }
                    }
                    else
                    {
                        Graphics.DrawMeshNow(it.mesh, m);
                    }
                }
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        [MenuItem("CONTEXT/GrabDeposit/Open Documentation", false)]
        static void OpenDoc()
        {
            InteractionManager.OpenDoc();
        }

        void OnValidate()
        {
            SetupBoxCollider();
            ClampAllSpecificObjectsToBox();
            SanitizeSpecificObjects();
        }

        void SanitizeSpecificObjects()
        {
            if (SpecificObjects == null) return;

            bool changed = false;

            for (int i = 0; i < SpecificObjects.Count; i++)
            {
                var d = SpecificObjects[i];
                if (d == null) continue;

                var go = d.gameObject;
                if (go == null) continue;

                if (!TryGetValidGrabInteractable(go, out _))
                {
                    d.gameObject = null;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(this);
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            }
        }

        static readonly Dictionary<int, Bounds> s_RootBoundsCache = new();
        static readonly Vector3[] s_Corners = new Vector3[8];

        const float kPlaceholderBaseRadius = 0.25f;

        bool _isClampingOnValidate;

        public void ClampAllSpecificObjectsToBox()
        {
            if (_isClampingOnValidate) return;
            _isClampingOnValidate = true;

            try
            {
                if (SpecificObjects == null || SpecificObjects.Count == 0) return;
                var box = GetComponent<BoxCollider>();
                if (!box) return;

                bool changedAny = false;

                for (int i = 0; i < SpecificObjects.Count; i++)
                {
                    var data = SpecificObjects[i];
                    if (data == null) continue;

                    if (ClampOneToBox(box, data))
                        changedAny = true;
                }

                if (changedAny)
                {
                    EditorUtility.SetDirty(this);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                    SceneView.RepaintAll();
                }
            }
            finally
            {
                _isClampingOnValidate = false;
            }
        }
        

        bool ClampOneToBox(BoxCollider box, ObjectDepositData data)
        {
            Vector3 lossy = transform.lossyScale;
            lossy = new Vector3(Mathf.Abs(lossy.x), Mathf.Abs(lossy.y), Mathf.Abs(lossy.z));
            Vector3 halfBox = Vector3.Scale(box.size, lossy) * 0.5f;

            Bounds rootBounds;
            if (data.gameObject == null)
            {
                rootBounds = new Bounds(Vector3.zero, Vector3.one * (kPlaceholderBaseRadius * 2f));
            }
            else
            {
                if (!TryGetRootBoundsCached(data.gameObject, out rootBounds))
                {
                    rootBounds = new Bounds(Vector3.zero, Vector3.zero);
                }
            }

            Matrix4x4 rs = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(data.Rotation), data.Scale);
            Bounds rsBounds = TransformBounds(rs, rootBounds);

            Vector3 min0 = rsBounds.min;
            Vector3 max0 = rsBounds.max;

            Vector3 minAllowed = -halfBox - min0;
            Vector3 maxAllowed = halfBox - max0;

            Vector3 p = data.Position;
            Vector3 clamped = new Vector3(
                ClampAxis(p.x, minAllowed.x, maxAllowed.x),
                ClampAxis(p.y, minAllowed.y, maxAllowed.y),
                ClampAxis(p.z, minAllowed.z, maxAllowed.z)
            );

            if ((clamped - p).sqrMagnitude > 0)
            {
                data.Position = clamped;
                return true;
            }

            return false;
        }

        static float ClampAxis(float v, float min, float max)
        {
            if (min > max) return (min + max) * 0.5f;
            return Mathf.Clamp(v, min, max);
        }

        static Bounds TransformBounds(Matrix4x4 m, Bounds b)
        {
            GetCorners(b, s_Corners);

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < 8; i++)
            {
                Vector3 p = m.MultiplyPoint3x4(s_Corners[i]);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            Bounds outB = new Bounds();
            outB.SetMinMax(min, max);
            return outB;
        }

        static void GetCorners(Bounds b, Vector3[] corners)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;

            corners[0] = c + new Vector3(-e.x, -e.y, -e.z);
            corners[1] = c + new Vector3(-e.x, -e.y, e.z);
            corners[2] = c + new Vector3(-e.x, e.y, -e.z);
            corners[3] = c + new Vector3(-e.x, e.y, e.z);
            corners[4] = c + new Vector3(e.x, -e.y, -e.z);
            corners[5] = c + new Vector3(e.x, -e.y, e.z);
            corners[6] = c + new Vector3(e.x, e.y, -e.z);
            corners[7] = c + new Vector3(e.x, e.y, e.z);
        }

        bool TryGetRootBoundsCached(GameObject prefabRootGO, out Bounds bounds)
        {
            bounds = default;
            if (!prefabRootGO) return false;

            int id = prefabRootGO.GetInstanceID();
            if (s_RootBoundsCache.TryGetValue(id, out bounds))
                return bounds.size != Vector3.zero || bounds.center != Vector3.zero;

            var items = GetRenderItems(prefabRootGO);
            if (items == null || items.Count == 0)
            {
                s_RootBoundsCache[id] = new Bounds(Vector3.zero, Vector3.zero);
                bounds = s_RootBoundsCache[id];
                return false;
            }

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it.mesh == null) continue;

                Bounds mb = it.mesh.bounds;
                GetCorners(mb, s_Corners);

                for (int c = 0; c < 8; c++)
                {
                    Vector3 p = it.localToRoot.MultiplyPoint3x4(s_Corners[c]);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            bounds = new Bounds();
            bounds.SetMinMax(min, max);
            s_RootBoundsCache[id] = bounds;
            return true;
        }

        public struct FoldoutRecord { public string key; public bool open; }
        [SerializeField, HideInInspector] private List<FoldoutRecord> _foldouts = new();
        [NonSerialized] private Dictionary<string, bool> _foldMap;

        public bool GetFoldout(string key, bool @default = false)
        {
            if (_foldMap == null) RebuildFoldMap();
            if (_foldMap.TryGetValue(key, out var v)) return v;
            _foldMap[key] = @default;
            return @default;
        }

        public void SetFoldout(string key, bool open)
        {
            if (_foldMap == null) RebuildFoldMap();
            _foldMap[key] = open;

            int idx = _foldouts.FindIndex(r => r.key == key);
            var rec = new FoldoutRecord { key = key, open = open };
            if (idx >= 0) _foldouts[idx] = rec;
            else _foldouts.Add(rec);

            Undo.RecordObject(this, open ? "Expand Foldout" : "Collapse Foldout");
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }

        void RebuildFoldMap()
        {
            _foldMap = new Dictionary<string, bool>(_foldouts.Count);
            foreach (var r in _foldouts)
                if (!string.IsNullOrEmpty(r.key))
                    _foldMap[r.key] = r.open;
        }

        private void SetAllFoldoutsGeneric(bool open)
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in GetType().GetFields(flags))
                if (f.FieldType == typeof(bool) && f.Name.EndsWith("Foldout"))
                    f.SetValue(this, open);

            if (_foldMap != null && _foldMap.Count > 0)
                foreach (var k in new List<string>(_foldMap.Keys))
                    SetFoldout(k, open);

            Undo.RecordObject(this, open ? "Expand All Foldouts" : "Collapse All Foldouts");
            EditorUtility.SetDirty(this);
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public void OnBeforeSerialize()
        {
            if (_foldMap == null) return;
            _foldouts.Clear();
            foreach (var kv in _foldMap) _foldouts.Add(new FoldoutRecord { key = kv.Key, open = kv.Value });
        }
        public void OnAfterDeserialize() { }

#endif
    }
}
