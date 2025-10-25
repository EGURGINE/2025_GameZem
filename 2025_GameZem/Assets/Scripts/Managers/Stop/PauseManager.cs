using System;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    // 전역 신호(원하면 다른 스크립트에서 구독)
    public static event Action Paused;
    public static event Action Resumed;

    // Animator/ParticleSystem 같은 엔진 객체도 멈추기 위해 저장
    private readonly List<Animator> _animators = new();
    private readonly List<float>   _animatorSpeeds = new();
    private readonly List<ParticleSystem> _particles = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeAll();
        else PauseAll();
    }

    public void PauseAll()
    {
        if (IsPaused) return;
        IsPaused = true;

        // 1) 일반 게임 루프 정지
        Time.timeScale = 0f;
        AudioListener.pause = true;

        // 2) Animator/Particles 멈추기
        CacheSceneAnimatorsAndParticles();
        for (int i = 0; i < _animators.Count; i++)
            _animators[i].speed = 0f;
        foreach (var ps in _particles)
            ps.Pause();

        // 3) IPausable 전파
        BroadcastToPausables(true);

        // 4) (선택) DOTween 사용한다면
        // DG.Tweening.DOTween.PauseAll();

        Paused?.Invoke();
        Debug.Log("[PauseManager] Paused All");
    }

    public void ResumeAll()
    {
        if (!IsPaused) return;
        IsPaused = false;

        // 1) 일반 게임 루프 재개
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 2) Animator/Particles 재개
        for (int i = 0; i < _animators.Count; i++)
            _animators[i].speed = _animatorSpeeds[i];
        foreach (var ps in _particles)
            ps.Play(true);
        _animators.Clear(); _animatorSpeeds.Clear(); _particles.Clear();

        // 3) IPausable 전파
        BroadcastToPausables(false);

        // 4) (선택) DOTween 사용한다면
        // DG.Tweening.DOTween.PlayAll();

        Resumed?.Invoke();
        Debug.Log("[PauseManager] Resumed All");
    }

    private void CacheSceneAnimatorsAndParticles()
    {
        _animators.Clear(); _animatorSpeeds.Clear(); _particles.Clear();

        // 비활성 오브젝트까지 전부 포함
        var anims = Resources.FindObjectsOfTypeAll<Animator>();
        foreach (var a in anims)
        {
            if (!a.gameObject.scene.IsValid()) continue; // 프로젝트 asset 제외
            _animators.Add(a);
            _animatorSpeeds.Add(a.speed);
        }

        var pss = Resources.FindObjectsOfTypeAll<ParticleSystem>();
        foreach (var p in pss)
        {
            if (!p.gameObject.scene.IsValid()) continue;
            _particles.Add(p);
        }
    }

    private void BroadcastToPausables(bool pause)
    {
        // 씬 안의 모든 IPausable에게 전달
        var list = new List<IPausable>(128);
        // 활성/비활성 모두 검색
        foreach (var mb in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            if (!mb) continue;
            if (!mb.gameObject.scene.IsValid()) continue;
            if (mb is IPausable p)
                list.Add(p);
        }

        foreach (var p in list)
        {
            if (pause) p.OnPaused();
            else p.OnResumed();
        }
    }
}
