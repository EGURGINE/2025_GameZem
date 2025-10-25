using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 전역 사운드 매니저(싱글톤, 최소 기능 버전)
/// - BGM 재생/정지
/// - SFX 재생
/// - 마스터/뮤직/SFX 볼륨 적용
/// - 인스펙터에서 등록한 클립을 이름으로 재생
/// </summary>
public class SoundManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource; // BGM용 (loop)
    [SerializeField] private AudioSource sfxSource;   // SFX용 (OneShot)

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] musicClips;  // BGM 후보 클립
    [SerializeField] private AudioClip[] sfxClips;    // SFX 후보 클립

    [Header("Volume Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume  = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume    = 0.8f;

    // 이름으로 빠르게 찾기
    private Dictionary<string, AudioClip> musicDict;
    private Dictionary<string, AudioClip> sfxDict;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            BuildDictionaries();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadVolumeSettings(); // 저장된 볼륨 설정 로드
        ApplyVolumeSettings();
    }

    private void EnsureAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f; // 2D 사운드
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f; // 2D 사운드
        }
    }

    private void BuildDictionaries()
    {
        musicDict = new Dictionary<string, AudioClip>();
        sfxDict   = new Dictionary<string, AudioClip>();

        if (musicClips != null)
        {
            foreach (var c in musicClips) 
            {
                if (c != null && !string.IsNullOrEmpty(c.name))
                    musicDict[c.name] = c;
            }
        }
        
        if (sfxClips != null)
        {
            foreach (var c in sfxClips) 
            {
                if (c != null && !string.IsNullOrEmpty(c.name))
                    sfxDict[c.name] = c;
            }
        }
    }

    // ===== Music =====
    /// <summary> 이름으로 BGM 재생(같은 트랙이면 무시) </summary>
    public void PlayMusic(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("[SoundManager] Music clip name is null or empty");
            return;
        }
        
        if (musicDict == null || !musicDict.TryGetValue(clipName, out var clip))
        {
            Debug.LogWarning($"[SoundManager] Music '{clipName}' not found");
            return;
        }
        
        if (musicSource == null)
        {
            Debug.LogError("[SoundManager] Music source is null");
            return;
        }
        
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.Play();
    }

    /// <summary> BGM 즉시 정지 </summary>
    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    // ===== SFX =====
    /// <summary>
    /// 이름으로 효과음 1회 재생.
    /// 내부 sfxSource의 볼륨 설정과는 별개로, 전역 볼륨(마스터*SFX)을 곱해 OneShot 볼륨에 반영.
    /// </summary>
    public void PlaySFX(string clipName, float localVolume = 1f)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            Debug.LogWarning("[SoundManager] SFX clip name is null or empty");
            return;
        }
        
        if (sfxDict == null || !sfxDict.TryGetValue(clipName, out var clip))
        {
            Debug.LogWarning($"[SoundManager] SFX '{clipName}' not found");
            return;
        }
        
        if (sfxSource == null)
        {
            Debug.LogError("[SoundManager] SFX source is null");
            return;
        }
        
        // 전역 볼륨은 ApplyVolumeSettings()로 sfxSource.volume에 이미 반영됨
        // 여기서는 버튼/이벤트 등 개별 강도만 조절
        float v = Mathf.Clamp01(localVolume);
        sfxSource.PlayOneShot(clip, v);
    }


    // ===== Volume =====
    public void SetMasterVolume(float v) { masterVolume = Mathf.Clamp01(v); ApplyVolumeSettings(); SaveVolumeSettings(); }
    public void SetMusicVolume (float v) { musicVolume  = Mathf.Clamp01(v); ApplyVolumeSettings(); SaveVolumeSettings(); }
    public void SetSFXVolume   (float v) { sfxVolume    = Mathf.Clamp01(v); ApplyVolumeSettings(); SaveVolumeSettings(); }

    private void ApplyVolumeSettings()
    {
        if (musicSource != null) 
            musicSource.volume = Mathf.Clamp01(musicVolume * masterVolume);
        if (sfxSource != null)   
            sfxSource.volume   = Mathf.Clamp01(sfxVolume * masterVolume);
    }

    // 게터(슬라이더 저장 등 필요 시 사용)
    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume () => musicVolume;
    public float GetSFXVolume   () => sfxVolume;

    // ===== Volume Persistence =====
    /// <summary> 볼륨 설정을 PlayerPrefs에 저장 </summary>
    public void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    /// <summary> PlayerPrefs에서 볼륨 설정을 로드 </summary>
    public void LoadVolumeSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        ApplyVolumeSettings();
    }
}

/// <summary>
/// 버튼 클릭 사운드(최소 기능)
/// - 클릭 AudioClip만 설정하여 재생
/// - 전역 볼륨(마스터*SFX)을 함께 반영
/// </summary>
public class ButtonSoundController : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private AudioClip clickSound;
    [Range(0f,1f)] [SerializeField] private float volume = 1f; // 버튼 개별 볼륨

    [Header("Auto Setup")]
    [SerializeField] private bool autoSetup = true;

    private Button button;
    private AudioSource audioSource;

    private void Start()
    {
        if (autoSetup) Setup();
    }

    public void Setup()
    {
        button = GetComponent<Button>();
        if (!button)
        {
            Debug.LogError($"[ButtonSoundController] Button not found on {name}");
            return;
        }

        EnsureAudioSource();
        button.onClick.AddListener(PlayClickSound);
    }

    private void EnsureAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // UI는 2D 권장
    }

    /// <summary> 버튼 클릭 시 재생(전역 SFX/마스터 볼륨 반영) </summary>
    public void PlayClickSound()
    {
        if (clickSound == null) 
        {
            Debug.LogWarning("[ButtonSoundController] Click sound is not assigned");
            return;
        }
        
        if (audioSource == null)
        {
            Debug.LogError("[ButtonSoundController] Audio source is null");
            return;
        }

        float global = SoundManager.Instance != null
            ? SoundManager.Instance.GetSFXVolume() * SoundManager.Instance.GetMasterVolume()
            : 1f;

        audioSource.PlayOneShot(clickSound, Mathf.Clamp01(volume) * Mathf.Clamp01(global));
    }
}

