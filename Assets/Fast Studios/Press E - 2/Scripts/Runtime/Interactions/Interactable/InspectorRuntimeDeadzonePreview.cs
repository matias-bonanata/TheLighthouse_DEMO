using UnityEngine;

namespace FastStudios
{
    public static class InspectorRuntimeDeadzonePreview
    {
        public static float ApplyDeadZone(float v, float deadZone)
        {
            float a = Mathf.Abs(v);
            if (a <= deadZone) return 0f;

            float sign = Mathf.Sign(v);
            float t = (a - deadZone) / Mathf.Max(0.0001f, 1f - deadZone);
            return sign * Mathf.Clamp01(t);
        }

        public static float ApplySoftCurve(float v)
        {
            float a = Mathf.Abs(v);
            a = a * a * (3f - 2f * a);
            return Mathf.Sign(v) * a;
        }

        public static void ComputeAxisFreeFinal(
            Vector2 rawMouseN,
            float deadZone,
            float feather,
            out Vector2 axisN,
            out Vector2 freeN,
            out Vector2 finalN
        )
        {
            float xRaw = Mathf.Clamp(rawMouseN.x, -1f, 1f);
            float yRaw = Mathf.Clamp(rawMouseN.y, -1f, 1f);

            float xAxis = ApplySoftCurve(ApplyDeadZone(xRaw, deadZone));
            float yAxis = ApplySoftCurve(ApplyDeadZone(yRaw, deadZone));
            axisN = new Vector2(xAxis, yAxis);

            freeN = Vector2.zero;

            float f = Mathf.Clamp01(feather);
            if (f > 0.0001f)
            {
                float maxA = Mathf.Max(Mathf.Abs(xRaw), Mathf.Abs(yRaw));
                if (maxA > 0.0001f)
                {
                    float tFree = ApplySoftCurve(ApplyDeadZone(maxA, deadZone));
                    freeN = new Vector2(xRaw / maxA, yRaw / maxA) * tFree;
                }
            }

            finalN = Vector2.Lerp(axisN, freeN, Mathf.Clamp01(feather));
        }

        public static Vector2 SmoothFinalN(
            ref Vector2 smoothN,
            ref Vector2 smoothVel,
            Vector2 targetFinalN,
            float dt,
            float smoothTime = 0.06f
        )
        {
            smoothN = Vector2.SmoothDamp(
                smoothN,
                targetFinalN,
                ref smoothVel,
                smoothTime,
                Mathf.Infinity,
                dt
            );
            return smoothN;
        }

        public static void StepPanLocal_NoRecenter(
            ref Vector2 panLocal,
            ref Vector2 panVel,
            Vector2 n,
            float leftM,
            float rightM,
            float topM,
            float bottomM,
            float dt,
            float timeToEdge = 0.35f,
            float velSmooth = 22f
        )
        {
            float ax = Mathf.Abs(n.x);
            float ay = Mathf.Abs(n.y);

            if (ax <= 0.0001f && ay <= 0.0001f)
            {
                panVel = Vector2.zero;
                return;
            }

            float vx = 0f;
            if (ax > 0f)
            {
                float maxX = (n.x < 0f) ? leftM : rightM;
                if (maxX > 0f)
                    vx = Mathf.Sign(n.x) * (maxX / Mathf.Max(0.0001f, timeToEdge)) * ax;
            }

            float vy = 0f;
            if (ay > 0f)
            {
                float maxY = (n.y < 0f) ? bottomM : topM;
                if (maxY > 0f)
                    vy = Mathf.Sign(n.y) * (maxY / Mathf.Max(0.0001f, timeToEdge)) * ay;
            }

            Vector2 desiredVel = new Vector2(vx, vy);

            float vt = 1f - Mathf.Exp(-velSmooth * dt);
            panVel = Vector2.Lerp(panVel, desiredVel, vt);

            panLocal += panVel * dt;

            panLocal.x = Mathf.Clamp(panLocal.x, -leftM, rightM);
            panLocal.y = Mathf.Clamp(panLocal.y, -bottomM, topM);
        }

        public static void ComputeTargetPose(
            Vector3 centerPos,
            Quaternion centerRot,
            Vector3 baseRight,
            Vector3 baseUp,
            Vector2 panLocal,
            float leftM,
            float rightM,
            float topM,
            float bottomM,
            bool rotationOffsetOnEdge,
            float leftOffset,
            float rightOffset,
            float topOffset,
            float bottomOffset,
            out Vector3 targetPos,
            out Quaternion targetRot
        )
        {
            targetPos = centerPos
                        + baseRight * panLocal.x
                        + baseUp * panLocal.y;

            targetRot = centerRot;

            if (!rotationOffsetOnEdge) return;

            float leftT = (panLocal.x < 0f && leftM > 0f) ? Mathf.Clamp01(-panLocal.x / leftM) : 0f;
            float rightT = (panLocal.x > 0f && rightM > 0f) ? Mathf.Clamp01(panLocal.x / rightM) : 0f;
            float topT = (panLocal.y > 0f && topM > 0f) ? Mathf.Clamp01(panLocal.y / topM) : 0f;
            float bottomT = (panLocal.y < 0f && bottomM > 0f) ? Mathf.Clamp01(-panLocal.y / bottomM) : 0f;

            float yaw = -leftOffset * leftT + rightOffset * rightT;
            float pitch = -topOffset * topT + bottomOffset * bottomT;

            targetRot = targetRot * Quaternion.Euler(pitch, yaw, 0f);
        }

        static Quaternion Sanitize(Quaternion q)
        {
            if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w)) return Quaternion.identity;
            if (float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w)) return Quaternion.identity;
            return q;
        }

        public static void SmoothCamera(
            Transform cam,
            Vector3 targetPos,
            Quaternion targetRot,
            float dt,
            float camSmooth = 18f
        )
        {
            if (!cam) return;

            float t = 1f - Mathf.Exp(-camSmooth * dt);

            cam.position = Vector3.Lerp(cam.position, targetPos, t);
            cam.rotation = Quaternion.Slerp(cam.rotation, Sanitize(targetRot), t);
        }

        public static void StepMarginCamera(
            Transform cam,
            Vector2 rawMouseN,
            float deadZone,
            float feather,
            ref Vector2 smoothN,
            ref Vector2 smoothNVel,
            ref Vector2 panLocal,
            ref Vector2 panVel,
            Vector3 centerPos,
            Quaternion centerRot,
            Vector3 baseRight,
            Vector3 baseUp,
            float leftM,
            float rightM,
            float topM,
            float bottomM,
            bool rotationOffsetOnEdge,
            float leftOffset,
            float rightOffset,
            float topOffset,
            float bottomOffset,
            float dt,
            float inputSmoothTime = 0.06f,
            float timeToEdge = 0.35f,
            float velSmooth = 22f,
            float camSmooth = 18f
        )
        {
            if (dt <= 0f) return;

            ComputeAxisFreeFinal(rawMouseN, deadZone, feather, out _, out _, out Vector2 finalN);

            SmoothFinalN(ref smoothN, ref smoothNVel, finalN, dt, inputSmoothTime);

            StepPanLocal_NoRecenter(ref panLocal, ref panVel, smoothN, leftM, rightM, topM, bottomM, dt, timeToEdge, velSmooth);

            ComputeTargetPose(
                centerPos, centerRot, baseRight, baseUp,
                panLocal,
                leftM, rightM, topM, bottomM,
                rotationOffsetOnEdge, leftOffset, rightOffset, topOffset, bottomOffset,
                out Vector3 targetPos,
                out Quaternion targetRot
            );

            SmoothCamera(cam, targetPos, targetRot, dt, camSmooth);
        }

        public static Vector2 ScreenPosToRawN(Vector2 mp, float w, float h)
        {
            if (w <= 0f || h <= 0f) return Vector2.zero;

            float x = ((mp.x / w) - 0.5f) * 2f;
            float y = ((mp.y / h) - 0.5f) * 2f;

            x = Mathf.Clamp(x, -1f, 1f);
            y = Mathf.Clamp(y, -1f, 1f);

            return new Vector2(x, y);
        }

        public static Vector3 GetInspectionFocusWorld(Interactable it, Transform inspectedObj)
        {
            if (it == null)
                return inspectedObj ? inspectedObj.position : Vector3.zero;

            if (it.InspectionTargetType == InspectionNavigationTargetType.Transform &&
                it.InspectionTargetTransform != null)
                return it.InspectionTargetTransform.position;

            Transform basis = inspectedObj != null ? inspectedObj : it.transform;
            return basis.TransformPoint(it.InspectionTargetPosition);
        }

        public static void ComputeInspectionCenterPose(
            Interactable it,
            Transform inspectedObj,
            Vector3 originalCamPos,
            Quaternion originalCamRot,
            out Vector3 focusWorld,
            out float dist,
            out Vector3 centerPos,
            out Quaternion centerRot,
            out Vector3 baseForward,
            out Vector3 baseUp,
            out Vector3 baseRight
        )
        {
            focusWorld = GetInspectionFocusWorld(it, inspectedObj);
            dist = Mathf.Max(0.01f, it != null ? it.InspectionDistance : 0.01f);

            bool useTargetTf =
                it != null &&
                it.InspectionTargetType == InspectionNavigationTargetType.Transform &&
                it.InspectionTargetTransform != null;

            bool useTargetPosRot =
                it != null &&
                it.InspectionTargetType == InspectionNavigationTargetType.Position;

            Transform basis = inspectedObj != null ? inspectedObj : it.transform;

            if (useTargetTf)
            {
                centerRot = it.InspectionTargetTransform.rotation;
            }
            else if (useTargetPosRot)
            {
                centerRot = basis.rotation * Quaternion.Euler(it.InspectionTargetRotation);
            }
            else
            {
                Vector3 fwd = focusWorld - originalCamPos;
                if (fwd.sqrMagnitude < 1e-6f)
                    fwd = originalCamRot * Vector3.forward;

                fwd.Normalize();

                Vector3 up = originalCamRot * Vector3.up;
                up = Vector3.ProjectOnPlane(up, fwd);
                if (up.sqrMagnitude < 1e-6f) up = Vector3.up;
                up.Normalize();

                centerRot = Quaternion.LookRotation(fwd, up);
            }

            baseForward = centerRot * Vector3.forward;
            baseUp = centerRot * Vector3.up;
            baseRight = centerRot * Vector3.right;

            centerPos = focusWorld - baseForward * dist;
        }

    }
}
