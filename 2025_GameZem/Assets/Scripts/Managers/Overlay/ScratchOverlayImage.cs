using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class ScratchOverlayImage : MonoBehaviour
{
    [Header("Gameplay")]
    [Range(0.2f, 1f)] public float requiredErasedRatio = 0.8f;
    [Min(0.5f)] public float timeLimit = 3f;
    public bool pauseGameWhileActive = true;
    public bool hideWhenCleared = true;

    [Header("Brush")]
    [Range(5, 256)] public int brushRadius = 48;
    [Range(0f, 1f)] public float brushHardness = 1f;

    [Header("UI (Optional)")]
    public Text debugText;
    public Text countdownText;

    [Header("Events")]
    public UnityEvent onCleared;
    public UnityEvent onFailed;
    public System.Action OnCleared;
    public System.Action OnFailed;

    [Header("Clear Behavior")]
    [Tooltip("성공 시 페이드아웃 시간(0이면 바로 숨김)")]
    [SerializeField] float hideFadeOut = 0.15f;
    [Tooltip("성공 후 오브젝트를 아예 파괴할지 여부")]
    [SerializeField] bool destroyOnClear = false;

    [Header("Post Clear Freeze")]
    [Tooltip("성공 후 게임을 멈출(터치 불가) 시간(초), 0이면 바로 진행")]
    [SerializeField] float postClearFreezeSeconds = 1.0f;

    // ---- internals ----
    Image _img;
    Canvas _canvas;
    Camera _eventCam;

    Texture2D _workTex;
    Color32[] _pixels, _original;
    int _w, _h, _initialOpaque, _erasedCount;
    bool _active, _mouseHold;

    // 타이머/일시정지 상태
    float _elapsedUnscaled = 0f;
    int _lastShownCountdown = -1;
    bool _paused = false;
    Coroutine _timerCo;

    // 원래 타임스케일 저장 (중요!)
    float _savedTimeScale = 1f;

    // 실패시 유지용 플래그(Disable 시 타임스케일 복원 금지)
    bool _keepPausedOnDisable = false;

    // 한 번이라도 유저가 터치/클릭했는지
    bool _touchedOnce = false;

    void Awake()
    {
        _img = GetComponent<Image>();
        _canvas = GetComponentInParent<Canvas>();
        _eventCam =
            (_canvas && _canvas.renderMode == RenderMode.ScreenSpaceCamera) ? _canvas.worldCamera :
            (_canvas && _canvas.renderMode == RenderMode.WorldSpace) ? _canvas.worldCamera : null;

        if (_img.sprite == null || _img.sprite.texture == null)
        {
            Debug.LogError("[ScratchOverlayImage] Image에 Sprite가 없습니다.");
            enabled = false; return;
        }

        // 오버레이 활성 중엔 배경 클릭을 막기 위해 Raycast Target ON
        _img.raycastTarget = true;

        _workTex = MakeReadableCopy(_img.sprite.texture);
        _w = _workTex.width; _h = _workTex.height;

        float ppu = _img.sprite.pixelsPerUnit > 0 ? _img.sprite.pixelsPerUnit : 100f;
        _img.sprite = Sprite.Create(_workTex, new Rect(0, 0, _w, _h), new Vector2(0.5f, 0.5f), ppu);

        _pixels = _workTex.GetPixels32();
        _original = (Color32[])_pixels.Clone();

        _initialOpaque = 0;
        for (int i = 0; i < _pixels.Length; i++)
            if (_pixels[i].a > 25) _initialOpaque++;

        _erasedCount = 0;

        if (debugText) debugText.text = "Erased: 0%  Time: 0.0s";
        if (countdownText) countdownText.text = "";
    }

    void OnEnable()
    {
        // 현재 스케일을 먼저 저장하고, 필요하면 0으로 멈춤
        _savedTimeScale = Time.timeScale;

        if (pauseGameWhileActive) Time.timeScale = 0f;
        _active = true;
        _paused = false;
        _elapsedUnscaled = 0f;
        _lastShownCountdown = -1;
        _keepPausedOnDisable = false;
        _touchedOnce = false;

        // 페이드/입력차단 제어용 CanvasGroup 초기화(있으면)
        var cg = GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }

        if (_timerCo != null) StopCoroutine(_timerCo);
        _timerCo = StartCoroutine(Timer());
    }

    void OnDisable()
    {
        // 실패로 인해 '멈춘 상태 유지'가 필요한 경우 복원하지 않음
        if (!_keepPausedOnDisable)
            Time.timeScale = _savedTimeScale;

        _active = false;
        _paused = false;
        if (_timerCo != null) StopCoroutine(_timerCo);
        if (countdownText) countdownText.text = "";
    }

    IEnumerator Timer()
    {
        UpdateCountdownUI(Mathf.CeilToInt(timeLimit));

        while (_active && _elapsedUnscaled < timeLimit)
        {
            if (!_paused)
            {
                _elapsedUnscaled += Time.unscaledDeltaTime;

                int remain = Mathf.CeilToInt(timeLimit - _elapsedUnscaled);
                UpdateCountdownUI(Mathf.Max(remain, 0));

                CollectInput();

                if (debugText)
                    debugText.text = $"Erased: {(GetErasedRatio() * 100f):F0}%  Time: {_elapsedUnscaled:F1}s";

                if (GetErasedRatio() >= requiredErasedRatio)
                {
                    Success();
                    yield break;
                }
            }
            yield return null;
        }

        if (_active)
        {
            // 시간 종료 시: (1) 전혀 터치가 없었거나 (2) 지운 비율 미달 → 실패 처리
            UpdateCountdownUI(0);

            if (!_touchedOnce || GetErasedRatio() < requiredErasedRatio)
                Fail();
            else
                Success(); // (안전망) 거의 없겠지만 임계치 경계에서 성공 판정
        }
    }

    void UpdateCountdownUI(int remain)
    {
        if (!countdownText) return;
        if (_paused) return;
        if (remain == _lastShownCountdown) return;

        _lastShownCountdown = remain;
        countdownText.text = (remain >= 1) ? remain.ToString() : "";
    }

    void CollectInput()
    {
        if (_paused || !_active) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            _mouseHold = true;
            _touchedOnce = true; // 첫 입력 기록
        }
        if (Input.GetMouseButtonUp(0))   _mouseHold = false;
        if (_mouseHold) EraseAt(Input.mousePosition);
#endif
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began) _touchedOnce = true; // 첫 터치 기록
            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                EraseAt(t.position);
        }
    }

    void EraseAt(Vector2 screenPos)
    {
        RectTransform rt = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, _eventCam, out var local))
            return;

        Rect r = rt.rect;
        float u = Mathf.InverseLerp(r.xMin, r.xMax, local.x);
        float v = Mathf.InverseLerp(r.yMin, r.yMax, local.y);
        if (u < 0 || u > 1 || v < 0 || v > 1) return;

        int cx = Mathf.RoundToInt(u * (_w - 1));
        int cy = Mathf.RoundToInt(v * (_h - 1));

        PaintCircle(cx, cy);
        _workTex.SetPixels32(_pixels);
        _workTex.Apply(false);
    }

    void PaintCircle(int cx, int cy)
    {
        int rad = brushRadius, r2 = rad * rad;
        int x0 = Mathf.Max(cx - rad, 0), x1 = Mathf.Min(cx + rad, _w - 1);
        int y0 = Mathf.Max(cy - rad, 0), y1 = Mathf.Min(cy + rad, _h - 1);

        for (int y = y0; y <= y1; y++)
        {
            int dy = y - cy;
            for (int x = x0; x <= x1; x++)
            {
                int dx = x - cx; int d2 = dx * dx + dy * dy;
                if (d2 > r2) continue;

                int i = y * _w + x;
                byte a0 = _pixels[i].a;
                if (a0 == 0) continue;

                float norm = Mathf.Sqrt(d2) / rad;
                float strength = Mathf.Clamp01(1f - norm);
                strength = Mathf.Lerp(strength, 1f, brushHardness);

                byte a1 = (byte)Mathf.Max(0, a0 - Mathf.RoundToInt(255f * strength));
                _pixels[i].a = a1;
                if (a1 == 0 && a0 > 0) _erasedCount++;
            }
        }
    }

    float GetErasedRatio()
    {
        if (_initialOpaque <= 0) return 1f;
        return Mathf.Clamp01((float)_erasedCount / _initialOpaque);
    }

    // ---- 결과 처리 ----
    void Success()
    {
        if (!_active) return;
        _active = false;

        // 성공 이벤트 즉시 알림
        onCleared?.Invoke();
        OnCleared?.Invoke();

        // 타이머 중지 후 성공 플로우(정지 유지 → 페이드 → 대기 → 재개)
        if (_timerCo != null) StopCoroutine(_timerCo);
        StartCoroutine(SuccessFlow());
    }

    void Fail()
    {
        if (!_active) return;
        _active = false;

        // 요구사항: (1) 오버레이 제거 (2) 게임은 멈춘 상태 유지
        // 타임스케일 0으로 고정하고 Disable에서 복원 못하게 플래그 설정
        Time.timeScale = 0f;
        _keepPausedOnDisable = true;

        onFailed?.Invoke();
        OnFailed?.Invoke();

        if (_timerCo != null) StopCoroutine(_timerCo);

        // 오버레이 즉시 제거(낙서 사진 제거)
        Destroy(gameObject);
    }

    // 성공 후: 게임 완전 정지 & 터치 차단 유지 → (선택)페이드 → unscaled 대기 → 재개 + 숨김/파괴
    IEnumerator SuccessFlow()
    {
        // 성공 후 멈춤 시간이 있을 때 0으로 멈춤
        if (postClearFreezeSeconds > 0f)
            Time.timeScale = 0f;

        // 입력(터치) 완전 차단 유지
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = true;       // 투명해도 터치 막기
        if (_img) _img.raycastTarget = true;

        // (옵션) 페이드아웃: 알파만 0으로. 차단은 유지.
        if (hideWhenCleared)
            yield return StartCoroutine(FadeOutOnly());
        else if (_img)
            _img.enabled = false;

        // 성공 후 정지 시간 대기 (unscaled)
        if (postClearFreezeSeconds > 0f)
        {
            float t = 0f;
            while (t < postClearFreezeSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 게임 재개(원래 스케일로 복원)
        Time.timeScale = _savedTimeScale;

        // 최종 정리: 입력 차단 해제 및 숨김/파괴
        cg.blocksRaycasts = false;
        if (_img) _img.raycastTarget = false;

        if (hideWhenCleared)
        {
            if (destroyOnClear) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    // 알파만 0으로 낮추는 전용 페이드(차단 상태는 유지)
    IEnumerator FadeOutOnly()
    {
        var cg = GetComponent<CanvasGroup>();
        if (!cg) cg = gameObject.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = true; // 여기서는 계속 차단 유지
        float from = cg.alpha;

        if (hideFadeOut <= 0f)
        {
            cg.alpha = 0f;
            yield break;
        }

        float t = 0f;
        while (t < hideFadeOut)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, 0f, t / hideFadeOut);
            yield return null;
        }
        cg.alpha = 0f;
    }

    // ===== Stop / Pause / Resume =====
    public void PauseOverlay()
    {
        _paused = true;
        if (countdownText)
        {
            int remain = Mathf.CeilToInt(timeLimit - _elapsedUnscaled);
            countdownText.text = (remain >= 1) ? remain.ToString() : "";
        }
    }

    public void ResumeOverlay()
    {
        _paused = false;
        _lastShownCountdown = -1;
    }

    public void StopOverlay(bool asFail = false)
    {
        if (asFail) { Fail(); return; }
        _active = false;
        // 정상 종료는 원래 스케일로 복원
        Time.timeScale = _savedTimeScale;
        gameObject.SetActive(false);
    }

    public void Show(bool reset = true)
    {
        if (reset && _original != null && _original.Length == (_pixels?.Length ?? 0))
        {
            System.Array.Copy(_original, _pixels, _pixels.Length);
            _workTex.SetPixels32(_pixels);
            _workTex.Apply(false);

            _initialOpaque = 0; _erasedCount = 0;
            for (int i = 0; i < _pixels.Length; i++)
                if (_pixels[i].a > 25) _initialOpaque++;
        }

        _lastShownCountdown = -1;
        _paused = false;
        _elapsedUnscaled = 0f;
        _touchedOnce = false;
        if (countdownText) countdownText.text = "";

        // 다시 켤 때 완전히 보이도록
        var cg = GetComponent<CanvasGroup>();
        if (cg) { cg.alpha = 1f; cg.blocksRaycasts = true; cg.interactable = true; }
        if (_img) _img.enabled = true;
        _img.raycastTarget = true;

        gameObject.SetActive(true);
    }

    // ---- helpers ----
    static Texture2D MakeReadableCopy(Texture src)
    {
        int w = src.width, h = src.height;
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
        tex.Apply(false, false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }
}
