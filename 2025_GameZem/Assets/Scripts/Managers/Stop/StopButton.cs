using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class StopButton : MonoBehaviour
{
    public Text label; // 버튼 글자를 바꾸고 싶으면 연결(옵션)

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OnStopButtonClick);
        UpdateLabel();
    }

    public void OnStopButtonClick()
    {
        PauseManager.Instance?.TogglePause();
        UpdateLabel();
    }

    void UpdateLabel()
    {
        if (!label) return;
        label.text = PauseManager.IsPaused ? "Resume" : "Stop";
    }
}
