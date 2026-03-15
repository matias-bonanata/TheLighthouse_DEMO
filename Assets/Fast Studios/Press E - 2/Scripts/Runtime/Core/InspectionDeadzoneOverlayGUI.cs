#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FastStudios
{
    public static class InspectionDeadzoneOverlayGUI
    {
        private static GUIStyle _deadzoneLabel;
        private static GUIStyle _deadzoneLabelLeft;

        private const float ARROW_LEN = 120f;
        private const float ARROW_THICKNESS = 3f;
        private const float FEATHER_BAR_W = 160f;
        private const float FEATHER_BAR_H = 10f;
        private const float LINE_THICKNESS = 2f;
        private const float CURSOR_SIZE = 7f;

        private static void EnsureStyles()
        {
            if (_deadzoneLabel != null) return;

            _deadzoneLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            _deadzoneLabel.normal.textColor = new Color(1f, 1f, 1f, 0.92f);

            _deadzoneLabelLeft = new GUIStyle(_deadzoneLabel)
            {
                alignment = TextAnchor.MiddleLeft
            };
        }

        private static void DrawSolidRect(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = prev;
        }

        private static void DrawRectOutline(Rect r, float thickness, Color c)
        {
            DrawSolidRect(new Rect(r.xMin, r.yMin, r.width, thickness), c);
            DrawSolidRect(new Rect(r.xMin, r.yMax - thickness, r.width, thickness), c);
            DrawSolidRect(new Rect(r.xMin, r.yMin, thickness, r.height), c);
            DrawSolidRect(new Rect(r.xMax - thickness, r.yMin, thickness, r.height), c);
        }

        private static void DrawArrowGUI(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Handles.BeginGUI();
            Color prev = Handles.color;
            Handles.color = color;

            Handles.DrawAAPolyLine(thickness, from, to);

            Vector2 dir = (to - from);
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                Vector2 perp = new Vector2(-dir.y, dir.x);

                const float headLen = 10f;
                const float headWidth = 6f;

                Vector2 p1 = to - dir * headLen + perp * headWidth;
                Vector2 p2 = to - dir * headLen - perp * headWidth;

                Handles.DrawAAPolyLine(thickness, to, p1);
                Handles.DrawAAPolyLine(thickness, to, p2);
            }

            Handles.color = prev;
            Handles.EndGUI();
        }

        public static void Draw(
            Interactable it,
            float w,
            float h,
            Vector2 mouseScreenPos,
            out Vector2 rawMouseN
        )
        {
            rawMouseN = Vector2.zero;
            if (it == null || w <= 2f || h <= 2f) return;

            EnsureStyles();

            float dead = Mathf.Clamp(it.InspectionMarginDeadZone, 0f, 0.95f);
            float feather = Mathf.Clamp01(it.InspectionMarginFeather);

            rawMouseN = InspectorRuntimeDeadzonePreview.ScreenPosToRawN(mouseScreenPos, w, h);

            float halfW = w * 0.5f;
            float halfH = h * 0.5f;

            float dx = dead * halfW;
            float dy = dead * halfH;

            float xL = halfW - dx;
            float xR = halfW + dx;
            float yT = halfH - dy;
            float yB = halfH + dy;

            Rect deadRect = new Rect(xL, yT, dx * 2f, dy * 2f);

            DrawSolidRect(deadRect, new Color(1f, 1f, 1f, 0.06f));
            DrawRectOutline(deadRect, LINE_THICKNESS, new Color(1f, 1f, 1f, 0.85f));

            DrawSolidRect(new Rect(xL - LINE_THICKNESS * 0.5f, 0f, LINE_THICKNESS, h), new Color(1f, 1f, 1f, 0.35f));
            DrawSolidRect(new Rect(xR - LINE_THICKNESS * 0.5f, 0f, LINE_THICKNESS, h), new Color(1f, 1f, 1f, 0.35f));
            DrawSolidRect(new Rect(0f, yT - LINE_THICKNESS * 0.5f, w, LINE_THICKNESS), new Color(1f, 1f, 1f, 0.35f));
            DrawSolidRect(new Rect(0f, yB - LINE_THICKNESS * 0.5f, w, LINE_THICKNESS), new Color(1f, 1f, 1f, 0.35f));

            float literalness = 1f - feather;
            if (literalness > 0.0001f)
            {
                float a = 0.10f * literalness;
                Color band = new Color(1f, 0.55f, 0.15f, a);

                DrawSolidRect(new Rect(xL, 0f, xR - xL, yT), band);
                DrawSolidRect(new Rect(xL, yB, xR - xL, h - yB), band);

                DrawSolidRect(new Rect(0f, yT, xL, yB - yT), band);
                DrawSolidRect(new Rect(xR, yT, w - xR, yB - yT), band);
            }

            Rect featherBar = new Rect(8f, 28f, FEATHER_BAR_W, FEATHER_BAR_H);
            DrawSolidRect(featherBar, new Color(1f, 1f, 1f, 0.10f));
            DrawSolidRect(new Rect(featherBar.x, featherBar.y, featherBar.width * feather, featherBar.height), new Color(1f, 1f, 1f, 0.70f));
            DrawRectOutline(featherBar, 1f, new Color(1f, 1f, 1f, 0.65f));
            GUI.Label(new Rect(featherBar.xMax + 8f, featherBar.y - 4f, 260f, 20f),
                $"Feather: {feather:0.00}  (literal -> free)", _deadzoneLabelLeft);

            GUI.Label(new Rect(halfW - 60f, Mathf.Max(2f, yT - 22f), 120f, 20f), "UP", _deadzoneLabel);
            GUI.Label(new Rect(halfW - 60f, Mathf.Min(h - 22f, yB + 2f), 120f, 20f), "DOWN", _deadzoneLabel);
            GUI.Label(new Rect(Mathf.Max(2f, xL - 62f), halfH - 10f, 60f, 20f), "LEFT", _deadzoneLabel);
            GUI.Label(new Rect(Mathf.Min(w - 62f, xR + 2f), halfH - 10f, 60f, 20f), "RIGHT", _deadzoneLabel);
            GUI.Label(new Rect(halfW - 70f, halfH - 10f, 140f, 20f), "DEADZONE", _deadzoneLabel);

            Vector2 guiPos = new Vector2(mouseScreenPos.x, h - mouseScreenPos.y);
            DrawSolidRect(
                new Rect(guiPos.x - CURSOR_SIZE * 0.5f, guiPos.y - CURSOR_SIZE * 0.5f, CURSOR_SIZE, CURSOR_SIZE),
                new Color(1f, 1f, 1f, 0.9f)
            );

            bool inDead = Mathf.Abs(rawMouseN.x) <= dead && Mathf.Abs(rawMouseN.y) <= dead;

            GUI.Label(new Rect(8f, 8f, w - 16f, 18f),
                $"MouseN: {rawMouseN.x:0.00}, {rawMouseN.y:0.00} | Dead: {dead:0.00} | {(inDead ? "IN" : "OUT")}",
                _deadzoneLabel
            );

            InspectorRuntimeDeadzonePreview.ComputeAxisFreeFinal(rawMouseN, dead, feather, out var axisN, out var freeN, out var finalN);

            Vector2 center = new Vector2(halfW, halfH);

            Vector2 axisDir = new Vector2(axisN.x, -axisN.y);
            Vector2 freeDir = new Vector2(freeN.x, -freeN.y);
            Vector2 finalDir = new Vector2(finalN.x, -finalN.y);

            if (axisDir.sqrMagnitude > 0.0001f) axisDir.Normalize();
            if (freeDir.sqrMagnitude > 0.0001f) freeDir.Normalize();
            if (finalDir.sqrMagnitude > 0.0001f) finalDir.Normalize();

            if (feather > 0.0001f && feather < 0.9999f)
            {
                DrawArrowGUI(center, center + axisDir * ARROW_LEN, 2.0f, new Color(1f, 1f, 1f, 0.20f * (1f - feather)));
                DrawArrowGUI(center, center + freeDir * ARROW_LEN, 2.0f, new Color(1f, 1f, 1f, 0.20f * feather));
            }

            DrawArrowGUI(center, center + finalDir * ARROW_LEN, ARROW_THICKNESS, new Color(1f, 1f, 1f, 0.90f));

            GUI.Label(new Rect(8f, 46f, w - 16f, 18f),
                $"AxisN: {axisN.x:0.00},{axisN.y:0.00} | FreeN: {freeN.x:0.00},{freeN.y:0.00} | FinalN: {finalN.x:0.00},{finalN.y:0.00}",
                _deadzoneLabelLeft
            );
        }
    }
}
#endif
