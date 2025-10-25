using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class VolumeSliderController : MonoBehaviour
{
    public enum VolumeType { Master, Music, SFX }
    [Header("What this slider controls")]
    public VolumeType volumeType = VolumeType.Master;

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetup = true;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        if (autoSetup) {
            // 슬라이더 기본 범위
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }
    }

    private void OnEnable()
    {
        // 현재 사운드 매니저 값을 슬라이더에 반영
        SyncFromSoundManager();
        // 값 변경시 호출 등록
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void SyncFromSoundManager()
    {
        if (SoundManager.Instance == null) return;

        switch (volumeType)
        {
            case VolumeType.Master:
                slider.value = SoundManager.Instance.GetMasterVolume();
                break;
            case VolumeType.Music:
                slider.value = SoundManager.Instance.GetMusicVolume();
                break;
            case VolumeType.SFX:
                slider.value = SoundManager.Instance.GetSFXVolume();
                break;
        }
    }

    private void OnSliderValueChanged(float value)
    {
        if (SoundManager.Instance == null) return;

        switch (volumeType)
        {
            case VolumeType.Master:
                SoundManager.Instance.SetMasterVolume(value);
                break;
            case VolumeType.Music:
                SoundManager.Instance.SetMusicVolume(value);
                break;
            case VolumeType.SFX:
                SoundManager.Instance.SetSFXVolume(value);
                break;
        }
    }

    // 외부에서 강제로 새로고침하고 싶을 때 호출 가능
    public void Refresh() => SyncFromSoundManager();
}
