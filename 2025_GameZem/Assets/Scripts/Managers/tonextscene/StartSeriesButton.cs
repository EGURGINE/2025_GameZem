// StartSeriesButton.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSeriesButton : MonoBehaviour
{
    [SerializeField] private string sceneName; // 이동할 씬 이름 (예: "Episode01")

    // Button.onClick 에 이 함수를 연결
    public void Go()
    {
        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
