using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace UIAnimation
{
    public class LoadScene : MonoBehaviour
    {
        [SerializeField] Button loadSceneButton;
        [SerializeField] string SceneName;
        void Start()
        {
            loadSceneButton.onClick.AddListener(() => LoadSceneMethod(SceneName));
        }
        private void LoadSceneMethod(string name)
        {
            SceneManager.LoadScene(name);
        }
    }
}