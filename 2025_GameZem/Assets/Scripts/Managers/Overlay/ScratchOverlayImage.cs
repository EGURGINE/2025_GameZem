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

    // ---- internals ----
    Image _img;
    Canvas _canvas;
    Camera _eventCam;

    Texture2D _workTex;
    Color32[] _pixels, _original;
    int _w, _h, _initialOpaque, _erasedCount;
    bool _active, _mouseHold;

    // 타이머/일시정지 상태(★ 핵심: 경과시간을 필드로 유지)
    float _elapsedUnscaled = 0f;
    int _lastShownCountdown = -1;
    bool _paused = false;
    Coroutine _timerCo;

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

        // 버튼 눌리도록 오버레이의 레이캐스트 차단
        _img.raycastTarget = false;

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
        if (pauseGameWhileActive) Time.timeScale = 0f;
        _active = true;
        _paused = false;
        _elapsedUnscaled = 0f;     // ★ 타이머 리셋
        _lastShownCountdown = -1;
        if (_timerCo != null) StopCoroutine(_timerCo);
        _timerCo = StartCoroutine(Timer());
    }

    void OnDisable()
    {
        if (pauseGameWhileActive) Time.timeScale = 1f;
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
                    debugText.text = $"Erased: {(GetErasedRatio()*100f):F0}%  Time: {_elapsedUnscaled:F1}s";

                if (GetErasedRatio() >= requiredErasedRatio) { Success(); yield break; }
            }
            yield return null;
        }

        if (_active)
        {
            UpdateCountdownUI(0);
            if (GetErasedRatio() >= requiredErasedRatio) Success();
            else Fail();
        }
    }

    void UpdateCountdownUI(int remain)
    {
        if (!countdownText) return;
        if (_paused) return;                 // ★ 멈춘 동안 숫자 고정
        if (remain == _lastShownCountdown) return;

        _lastShownCountdown = remain;
        countdownText.text = (remain >= 1) ? remain.ToString() : "";
    }

    void CollectInput()
    {

        if (_paused || !_active) return;

        if (IsPointerOverUI()) return;       // 버튼 위 터치면 지우기 무시

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) _mouseHold = true;
        if (Input.GetMouseButtonUp(0)) _mouseHold = false;
        if (_mouseHold) EraseAt(Input.mousePosition);
#endif


        Debug.Log("_mouseHold: " + _mouseHold);

        Debug.Log("Input.touchCount: " + Input.touchCount);


        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
            if (t.phase == TouchPhase.Began || t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
                EraseAt(t.position);
        }
    }

    bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        if (EventSystem.current.IsPointerOverGameObject()) return true;
#endif
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        return false;
    }

    void EraseAt(Vector2 screenPos)
    {

        Debug.Log("EraseAt: " + screenPos);
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
                int dx = x - cx; int d2 = dx*dx + dy*dy;
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

    void Success()
    {
        _active = false;
        if (hideWhenCleared) gameObject.SetActive(false);
        if (pauseGameWhileActive) Time.timeScale = 1f;
        onCleared?.Invoke();
        OnCleared?.Invoke();
    }

    void Fail()
    {
        _active = false;
        if (pauseGameWhileActive) Time.timeScale = 1f;
        onFailed?.Invoke();
        OnFailed?.Invoke();
    }

    // ===== Stop / Pause / Resume =====
    public void PauseOverlay()
    {
        _paused = true;
        // 표시도 즉시 고정
        if (countdownText)
        {
            int remain = Mathf.CeilToInt(timeLimit - _elapsedUnscaled);
            countdownText.text = (remain >= 1) ? remain.ToString() : "";
        }
    }

    public void ResumeOverlay()
    {
        _paused = false;
        _lastShownCountdown = -1; // 재개 시 다음 프레임에 숫자 갱신
    }

    public void StopOverlay(bool asFail = false)
    {
        if (asFail) { Fail(); return; }
        _active = false;
        if (pauseGameWhileActive) Time.timeScale = 1f;
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
        if (countdownText) countdownText.text = "";
        gameObject.SetActive(true);
    }

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
