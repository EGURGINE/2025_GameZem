using UnityEngine;
using UnityEngine.UI;

public class SpeechBubbleSpawner : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform canvas;       // Canvas의 RectTransform
    public Sprite bubbleSprite;        // 말풍선 스프라이트

    [Header("Spawn Area")]
    public RectTransform spawnArea;    // ✅ 이 영역 안에서만 랜덤 스폰(캔버스 자식 권장)
    public bool useCanvasWhenAreaMissing = true; // spawnArea가 없을 때 캔버스 전체 사용
    public Vector2 screenPadding = new Vector2(40, 80); // (캔버스 사용 시) 테두리 여백

    [Header("Bubble")]
    public Vector2 bubbleSize = new Vector2(320, 200);
    public float showSeconds = 1.5f;
    public float fadeSeconds = 0.35f;

    [Header("Auto Spawn (optional)")]
    public bool autoSpawn = false;
    public Vector2 intervalSeconds = new Vector2(1.5f, 3.5f);

    [Header("Debug")]
    public bool drawGizmos = true;     // 씬뷰에서 스폰 영역 표시

    void Reset()
    {
        if (canvas == null)
        {
            var c = FindObjectOfType<Canvas>();
            if (c) canvas = c.GetComponent<RectTransform>();
        }
    }

    void Start()
    {
        if (autoSpawn && canvas && bubbleSprite)
            Invoke(nameof(SpawnLoop), Random.Range(intervalSeconds.x, intervalSeconds.y));
    }

    void SpawnLoop()
    {
        ShowOnce();
        Invoke(nameof(SpawnLoop), Random.Range(intervalSeconds.x, intervalSeconds.y));
    }

    public void ShowOnce()
    {
        if (!canvas || !bubbleSprite)
        {
            Debug.LogWarning("[BubbleSpawner] Missing canvas or sprite.");
            return;
        }

        // 말풍선 GO
        var go = new GameObject("SpeechBubble",
            typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(SpeechBubbleController));

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(canvas, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        // 스폰 사각형 계산(캔버스 로컬 좌표계)
        Rect rect = GetSpawnRectInCanvasSpace();

        // 말풍선이 영역 밖으로 안나가게 half-size만큼 안전 여백
        float halfW = bubbleSize.x * 0.5f;
        float halfH = bubbleSize.y * 0.5f;
        float minX = rect.xMin + halfW;
        float maxX = rect.xMax - halfW;
        float minY = rect.yMin + halfH;
        float maxY = rect.yMax - halfH;

        // 영역이 너무 작으면 보정
        if (minX > maxX) { float c = (rect.xMin + rect.xMax) * 0.5f; minX = maxX = c; }
        if (minY > maxY) { float c = (rect.yMin + rect.yMax) * 0.5f; minY = maxY = c; }

        Vector2 pos = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        rt.anchoredPosition = pos;

        // 초기화
        var ctrl = go.GetComponent<SpeechBubbleController>();
        ctrl.showSeconds = showSeconds;
        ctrl.fadeOutSeconds = fadeSeconds;
        ctrl.Init(bubbleSprite, bubbleSize);
    }

    /// <summary>spawnArea가 있으면 그 영역, 없으면 캔버스(rect+padding) 반환</summary>
    Rect GetSpawnRectInCanvasSpace()
    {
        if (spawnArea)
        {
            // spawnArea의 월드 코너를 캔버스 로컬 좌표로 변환해서 Rect 생성
            Vector3[] wc = new Vector3[4];
            spawnArea.GetWorldCorners(wc);
            Vector3 bl = canvas.InverseTransformPoint(wc[0]); // bottom-left
            Vector3 tr = canvas.InverseTransformPoint(wc[2]); // top-right
            return Rect.MinMaxRect(bl.x, bl.y, tr.x, tr.y);
        }

        // fallback: 캔버스 전체(패딩 적용)
        if (useCanvasWhenAreaMissing)
        {
            var r = canvas.rect;
            r.xMin += screenPadding.x;
            r.xMax -= screenPadding.x;
            r.yMin += screenPadding.y;
            r.yMax -= screenPadding.y;
            return r;
        }

        // 마지막 안전장치: 0,0
        return new Rect(0, 0, 0, 0);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !canvas) return;
        Rect r = GetSpawnRectInCanvasSpace();

        // 씬뷰에 영역 테두리 그리기
        Vector3 p0 = canvas.TransformPoint(new Vector3(r.xMin, r.yMin));
        Vector3 p1 = canvas.TransformPoint(new Vector3(r.xMin, r.yMax));
        Vector3 p2 = canvas.TransformPoint(new Vector3(r.xMax, r.yMax));
        Vector3 p3 = canvas.TransformPoint(new Vector3(r.xMax, r.yMin));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p0, p1); Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3); Gizmos.DrawLine(p3, p0);
    }
#endif
}
