using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class UIPositionSync : MonoBehaviour
{
    [Header("Minimap Setup")]
    [SerializeField] private RectTransform minimapRect;
    [SerializeField] private Vector2 worldMin = new Vector2(-50, -50);
    [SerializeField] private Vector2 worldMax = new Vector2(50, 50);

    [Header("Player Follow")]
    [SerializeField] private Transform player;
    [SerializeField] private bool followPlayer = true;
    [SerializeField, Range(0.1f, 20f)] private float zoom = 1f;

    private List<MiniMapIcon> trackedIcons = new List<MiniMapIcon>();

    private void Start()
    {
        if (minimapRect == null)
            minimapRect = GetComponent<RectTransform>();

        RefreshTrackedIcons();
        CreateIcons();
    }

    private void RefreshTrackedIcons()
    {
        trackedIcons.Clear();
        trackedIcons.AddRange(Object.FindObjectsByType<MiniMapIcon>(FindObjectsSortMode.None));
    }

    private void CreateIcons()
    {
        foreach (var icon in trackedIcons)
        {
            if (icon.iconRect == null)
            {
                GameObject iconGO = new GameObject($"Icon_{icon.target?.name ?? "Unknown"}");

                // CRITICAL: Parent to SAME LEVEL as minimapRect
                iconGO.transform.SetParent(minimapRect, false);  //  Keep this!

                RectTransform rt = iconGO.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.one * 0.5f;  // Center pivot
                rt.anchorMax = Vector2.one * 0.5f;
                rt.sizeDelta = Vector2.one * 80f;
                rt.anchoredPosition = Vector2.zero;

                Image img = iconGO.AddComponent<Image>();
                img.sprite = icon.iconSprite;
                img.color = icon.iconColor;
                img.raycastTarget = false;  // Click-through

                icon.iconRect = rt;
            }
        }
    }

    private void LateUpdate()
    {
        if (player == null) return;

        Vector2 mapSize = minimapRect.rect.size * zoom;

        foreach (var icon in trackedIcons)
        {
            UpdateIcon(icon, mapSize);
        }
    }

    private void UpdateIcon(MiniMapIcon icon, Vector2 mapSize)
    {
        if (icon.target == null) return;

        Vector2 worldPos = new Vector2(icon.target.position.x, icon.target.position.z);
        Vector2 minimapPos = GetMinimapPosition(worldPos, mapSize);

        icon.iconRect.anchoredPosition = minimapPos;

        if (icon.rotateHeading && icon.headingSource != null)
        {
            float heading = icon.headingSource.eulerAngles.y;
            icon.iconRect.localRotation = Quaternion.Euler(0, 0, -heading);
        }
    }

    private Vector2 GetMinimapPosition(Vector2 worldPos, Vector2 mapSize)
    {
        if (followPlayer)
        {
            Vector2 playerPos = new Vector2(player.position.x, player.position.z);
            Vector2 offset = worldPos - playerPos;
            float mapWidth = (worldMax.x - worldMin.x) / mapSize.x;
            return new Vector2(offset.x / mapWidth, offset.y / mapWidth);
        }
        else
        {
            float tX = Mathf.InverseLerp(worldMin.x, worldMax.x, worldPos.x);
            float tZ = Mathf.InverseLerp(worldMin.y, worldMax.y, worldPos.y);
            return new Vector2((tX - 0.5f) * mapSize.x, (tZ - 0.5f) * mapSize.y);
        }
    }

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireCube(new Vector3((worldMin.x + worldMax.x) / 2, 0, (worldMin.y + worldMax.y) / 2),
    //                       new Vector3(worldMax.x - worldMin.x, 1, worldMax.y - worldMin.y));
    //}
}
