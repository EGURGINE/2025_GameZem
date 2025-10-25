using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

/// UI Image 위에 올린 스프라이트를 손가락/마우스로 지우는 오버레이
/// - 원본 텍스처 Read/Write 꺼져 있어도 RenderTexture 경유 복사로 안전 동작
/// - Canvas 모드(Overlay/Camera/World) 모두 좌표 정확
[RequireComponent(typeof(Image))]
public class ScratchOverlayImage : MonoBehaviour
{
    [Header("Gameplay")]
    [Range(0.2f, 1f)] public float requiredErasedRatio = 0.8f; // 성공 기준 (80%)
    [Min(0.5f)] public float timeLimit = 3f;                   // 제한 시간(초, 실시간)
    public bool pauseGameWhileActive = true;                   // 활성화 중 Time.timeScale=0
    public bool hideWhenCleared = true;                        // 성공 시 오브젝트 숨김

    [Header("Brush")]
    [Range(5, 256)] public int brushRadius = 48;               // 브러시 반경(px)
    [Range(0f, 1f)] public float brushHardness = 1f;           // 1=딱딱, 0=부드럽게

    [Header("Debug (Optional)")]
    public Text debugText;

    [Header("Events (Inspector에서 연결 가능)")]
    public UnityEvent onCleared;
    public UnityEvent onFailed;

    // 코드에서 구독하고 싶을 때
    public System.Action OnCleared;
    public System.Action OnFailed;

    Image _img;
    Canvas _canvas;
    Camera _eventCam;

    Texture2D _workTex;            // 작업용 복사 텍스처(RGBA32)
    Color32[] _pixels, _original;
    int _w, _h;
    int _initialOpaque;            // 초기 불투명 픽셀 수(알파>10%)
    int _erasedCount;              // 완전 0이 된 픽셀 누계
    bool _active, _mouseHold;

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

        // Read/Write 여부 무관하게 읽기 가능한 복사본 생성
        _workTex = MakeReadableCopy(_img.sprite.texture);
        _w = _workTex.width; _h = _workTex.height;

        // 작업 텍스처로 새 스프라이트 교체 (원본 보호)
        float ppu = _img.sprite.pixelsPerUnit > 0 ? _img.sprite.pixelsPerUnit : 100f;
        _img.sprite = Sprite.Create(_workTex, new Rect(0, 0, _w, _h), new Vector2(0.5f, 0.5f), ppu);

        _pixels = _workTex.GetPixels32();
        _original = (Color32[])_pixels.Clone();

        _initialOpaque = 0;
        for (int i = 0; i < _pixels.Length; i++)
            if (_pixels[i].a > 25) _initialOpaque++;

        _erasedCount = 0;
        if (debugText) debugText.text = "Erased: 0%  Time: 0.0s";
    }

    void OnEnable()
    {
        if (pauseGameWhileActive) Time.timeScale = 0f;
        _active = true;
        StartCoroutine(Timer());
    }

    void OnDisable()
    {
        if (pauseGameWhileActive) Time.timeScale = 1f;
        _active = false;
    }

    IEnumerator Timer()
    {
        float t = 0f;
        while (_active && t < timeLimit)
        {
            t += Time.unscaledDeltaTime;       // timeScale=0에서도 흐르게
            CollectInput();
            if (debugText) debugText.text = $"Erased: {(GetErasedRatio()*100f):F0}%  Time: {t:F1}s";

            if (GetErasedRatio() >= requiredErasedRatio) { Success(); yield break; }
            yield return null;
        }
        if (_active)
        {
            if (GetErasedRatio() >= requiredErasedRatio) Success();
            else Fail();
        }
    }

    void CollectInput()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0)) _mouseHold = true;
        if (Input.GetMouseButtonUp(0)) _mouseHold = false;
        if (_mouseHold) EraseAt(Input.mousePosition);
#endif
        for (int i = 0; i < Input.touchCount; i++)
        {
            var t = Input.GetTouch(i);
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
                int dx = x - cx; int d2 = dx*dx + dy*dy;
                if (d2 > r2) continue;

                int i = y * _w + x;
                byte a0 = _pixels[i].a;
                if (a0 == 0) continue;

                float norm = Mathf.Sqrt(d2) / rad;     // 0~1
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
        Debug.Log($"[ScratchOverlay] Cleared ({GetErasedRatio():P0})");
    }

    void Fail()
    {
        _active = false;
        if (pauseGameWhileActive) Time.timeScale = 1f;
        onFailed?.Invoke();
        OnFailed?.Invoke();
        Debug.Log($"[ScratchOverlay] Failed ({GetErasedRatio():P0})");
    }

    /// 외부에서 다시 띄우고 싶을 때. reset=true면 원본으로 복원
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
        gameObject.SetActive(true);
    }

    // Read/Write 꺼진 텍스처도 안전하게 복사(RGBA32)
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
