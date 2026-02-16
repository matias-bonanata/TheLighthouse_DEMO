using UnityEngine;
using UnityEngine.UI;

public class MiniMapIcon : MonoBehaviour
{
    [SerializeField] public Transform target;
    [SerializeField] public RectTransform iconRect;
    [SerializeField] public Sprite iconSprite;
    [SerializeField] public Color iconColor = Color.white;
    [SerializeField] public float iconSize;
    [SerializeField] public bool rotateHeading = true;
    [SerializeField] public Transform headingSource;


}
