using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSoundController : MonoBehaviour
{
    [Header("클릭 시 재생할 사운드")]
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)] [SerializeField] private float volume = 1f;

    private Button button;
    private AudioSource audioSource;

    void Awake()
    {
        // 버튼 컴포넌트 자동 연결
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[ButtonSoundController] 버튼이 없습니다: {name}");
            return;
        }

        // AudioSource 자동 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // UI용 2D 사운드
    }

    void Start()
    {
        button.onClick.AddListener(PlayClickSound);
    }

    void PlayClickSound()
    {
        if (clickSound == null)
        {
            Debug.LogWarning($"[ButtonSoundController] {name}에 클릭 사운드가 없습니다.");
            return;
        }

        float globalVolume = 1f;
        if (SoundManager.Instance != null)
            globalVolume = SoundManager.Instance.GetMasterVolume() * SoundManager.Instance.GetSFXVolume();

        audioSource.PlayOneShot(clickSound, volume * globalVolume);
    }
}
