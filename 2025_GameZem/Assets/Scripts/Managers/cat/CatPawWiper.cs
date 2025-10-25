// CatPawWiper.cs
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CatPawWiper : MonoBehaviour, IPointerDownHandler
{
    [Header("Target")]
    public RectTransform paw;          // 발 이미지 RectTransform (비워두면 자기 자신)
    public Image pawImage;             // 클릭(터치) 받는 Image (비워두면 자동)

    [Header("Swing (degrees)")]
    [Tooltip("왼쪽 끝 각도(음수 권장)")]
    public float leftAngle = -45f;
    [Tooltip("오른쪽 끝 각도(양수 권장)")]
    public float rightAngle = 45f;

    [Header("Motion")]
    [Tooltip("왕복 1회를 완료하는 데 걸리는 시간(초)")]
    public float periodSeconds = 2.0f;
    [Tooltip("끝단에서 잠깐 멈추는 시간(초)")]
    public float pauseAtEnds = 0.0f;

    [Header("Block Input on Touch")]
    [Tooltip("발을 터치했을 때 입력 차단 시간")]
    public float blockSecondsOnTouch = 2.0f;

    [Header("Visual")]
    [Tooltip("터치 시 살짝 커지게 하는 배율")]
    public float punchScale = 1.08f;
    public float punchDuration = 0.15f;

    float dir = 1f;  // 내부 방향
    float t;         // 0~1 보간 타이머
    bool pausing;

    void Reset()
    {
        paw = transform as RectTransform;
        pawImage = GetComponent<Image>();
    }

    void Awake()
    {
        if (paw == null) paw = transform as RectTransform;
        if (pawImage == null) pawImage = GetComponent<Image>();
        if (pawImage != null) pawImage.raycastTarget = true;

        // pivot을 아래쪽(발목)으로 맞추면 와이퍼 회전이 자연스러움
        paw.pivot = new Vector2(0.5f, 0f);
    }

    void Update()
    {
        if (periodSeconds <= 0.01f || pausing) return;

        // 0~1 왕복 타이머
        float speed = 2f / periodSeconds;               // 좌->우(0.5), 우->좌(0.5) 합이 1
        t += speed * Time.unscaledDeltaTime * dir;

        if (t >= 1f) { t = 1f; StartPauseThenFlip(); }
        else if (t <= 0f) { t = 0f; StartPauseThenFlip(); }

        float angle = Mathf.Lerp(leftAngle, rightAngle, t);
        paw.localRotation = Quaternion.Euler(0, 0, angle);
    }

    async void StartPauseThenFlip()
    {
        if (pauseAtEnds > 0f && !pausing)
        {
            pausing = true;
            await System.Threading.Tasks.Task.Delay((int)(pauseAtEnds * 1000f));
            pausing = false;
        }
        dir *= -1f;
    }

    // 발을 터치했을 때
    public void OnPointerDown(PointerEventData eventData)
    {
        // 전체 입력 N초 차단
        if (InputBlocker.Instance != null && blockSecondsOnTouch > 0f)
            InputBlocker.Instance.BlockForSeconds(blockSecondsOnTouch);

        // 작게 연출
        if (paw != null && punchScale > 1f)
            StartCoroutine(Punch());
    }

    System.Collections.IEnumerator Punch()
    {
        Vector3 baseScale = paw.localScale;
        Vector3 target = baseScale * punchScale;

        float t1 = 0f;
        while (t1 < punchDuration)
        {
            t1 += Time.unscaledDeltaTime;
            float k = t1 / punchDuration;
            paw.localScale = Vector3.Lerp(baseScale, target, k);
            yield return null;
        }

        float t2 = 0f;
        while (t2 < punchDuration)
        {
            t2 += Time.unscaledDeltaTime;
            float k = t2 / punchDuration;
            paw.localScale = Vector3.Lerp(target, baseScale, k);
            yield return null;
        }
        paw.localScale = baseScale;
    }
}
