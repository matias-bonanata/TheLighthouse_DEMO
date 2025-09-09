using System.Security.Cryptography;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class PlayerInteractUI : MonoBehaviour
{
    [SerializeField] private GameObject containerGameObject;
    [SerializeField] private ConversationStarter converstaionStarter;
    [SerializeField] private TextMeshProUGUI uiText;

    void Start()
    {
        if (containerGameObject != null)
            containerGameObject.SetActive(false);
    }

    public void ShowContainer()
    {
        containerGameObject.SetActive(true);
    }

    public void HideContainer()
    {
        containerGameObject.SetActive(false);
    }
}
