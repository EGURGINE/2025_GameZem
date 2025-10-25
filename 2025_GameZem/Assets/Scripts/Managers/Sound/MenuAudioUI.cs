using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuAudioUI : MonoBehaviour
{
    [Header("Master volume only")]
    public Slider masterSlider;          // 0..1
    public TMP_Text masterLabel;         // "70%" 같은 표시(선택)
    public Toggle muteToggle;            // 전체 음소거(선택)

    bool _wired = false;
    float _prevMaster = 1f;              // mute 전 값 저장

    void OnEnable()  { TryInit(); }
    void Start()     { TryInit(); }      // SoundManager가 늦게 뜨는 경우 대비
    void OnDisable()
    {
        if (!_wired) return;
        masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
        if (muteToggle) muteToggle.onValueChanged.RemoveListener(OnMuteChanged);
        _wired = false;
    }

    void TryInit()
    {
        if (!masterSlider)
        {
            Debug.LogError("[MenuAudioUI] masterSlider를 인스펙터에 지정하세요.");
            return;
        }
        if (SoundManager.Instance == null) return; // 아직 준비 전

        // 초기값 동기화
        float v = SoundManager.Instance.GetMasterVolume();
        masterSlider.SetValueWithoutNotify(v);
        if (masterLabel) masterLabel.text = Mathf.RoundToInt(v * 100) + "%";
        if (muteToggle)
        {
            bool muted = v <= 0f;
            muteToggle.SetIsOnWithoutNotify(muted);
        }

        if (!_wired)
        {
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            if (muteToggle) muteToggle.onValueChanged.AddListener(OnMuteChanged);
            _wired = true;
        }
    }

    void OnMasterChanged(float v)
    {
        SoundManager.Instance?.SetMasterVolume(v);
        if (masterLabel) masterLabel.text = Mathf.RoundToInt(v * 100) + "%";
        if (muteToggle)  muteToggle.SetIsOnWithoutNotify(v <= 0f);
    }

    void OnMuteChanged(bool on)
    {
        if (SoundManager.Instance == null) return;
        if (on)
        {
            _prevMaster = SoundManager.Instance.GetMasterVolume();
            SoundManager.Instance.SetMasterVolume(0f);
            masterSlider.SetValueWithoutNotify(0f);
        }
        else
        {
            float restore = (_prevMaster > 0f) ? _prevMaster : 1f;
            SoundManager.Instance.SetMasterVolume(restore);
            masterSlider.SetValueWithoutNotify(restore);
        }
        if (masterLabel) masterLabel.text = Mathf.RoundToInt(masterSlider.value * 100) + "%";
    }
}
