using UnityEngine;
using Spine.Unity;

public class SpineToScratchSequence : MonoBehaviour
{
    [Header("Spine")]
    public SkeletonGraphic spine;            // ScribbleSpine의 SkeletonGraphic
    public string appearAnim = "appear";     // 먼저 한 번 재생(1회)
    public string afterAnim  = "idle";       // (선택) 이후 루프
    public bool   afterLoop  = true;

    [Header("Scratch Overlay")]
    public GameObject overlayGO;             // DoodleOverlay 루트
    public ScratchOverlayImage overlay;      // ScratchOverlayImage 참조
    public float delayAfterAppear = 0f;      // appear 끝난 뒤 오버레이 켜기까지 딜레이

    [Header("Hide Spine After Starting Overlay")]
    public bool hideSpine = true;            // 오버레이 시작 후 Spine 숨길지
    public enum HideMode { DisableObject, DestroyObject, FadeOut }
    public HideMode hideMode = HideMode.FadeOut;
    public float fadeDuration = 0.25f;       // FadeOut일 때만 사용
    public float hideDelay = 0f;             // 오버레이 켜고 난 직후 대기(초)

    void Reset()
    {
        if (!spine) spine = GetComponent<SkeletonGraphic>();
        if (overlayGO && !overlay) overlay = overlayGO.GetComponent<ScratchOverlayImage>();
    }

    void Start()
    {
        // 처음엔 오버레이 꺼두기
        if (overlayGO) overlayGO.SetActive(false);

        if (spine && !string.IsNullOrEmpty(appearAnim))
        {
            var entry = spine.AnimationState.SetAnimation(0, appearAnim, false);
            entry.Complete += _ =>
            {
                // (선택) 이후 루프 애니
                if (!string.IsNullOrEmpty(afterAnim))
                    spine.AnimationState.AddAnimation(0, afterAnim, afterLoop, 0f);

                // 오버레이 시작
                if (delayAfterAppear > 0f)
                    Invoke(nameof(ShowOverlay), delayAfterAppear);
                else
                    ShowOverlay();
            };
        }
        else
        {
            // spine이 없거나 start anim 미지정 → 즉시 오버레이
            ShowOverlay();
        }
    }

    void ShowOverlay()
    {
        if (overlay != null) overlay.BeginNow();
        else if (overlayGO) overlayGO.SetActive(true);

        if (hideSpine) Invoke(nameof(HideSpineNow), hideDelay);
    }

    void HideSpineNow()
    {
        if (!spine) return;

        switch (hideMode)
        {
            case HideMode.DisableObject:
                spine.gameObject.SetActive(false);
                break;

            case HideMode.DestroyObject:
                Destroy(spine.gameObject);
                break;

            case HideMode.FadeOut:
                StartCoroutine(FadeOutSpine());
                break;
        }
    }

    System.Collections.IEnumerator FadeOutSpine()
    {
        // SkeletonGraphic의 색상 알파를 unscaled 시간으로 페이드
        float t = 0f;
        var c0 = spine.color;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // Time.timeScale=0이어도 진행
            float a = Mathf.Lerp(c0.a, 0f, fadeDuration > 0f ? t / fadeDuration : 1f);
            var c = spine.color; c.a = a; spine.color = c;
            yield return null;
        }
        // 완전히 투명 처리 후, 필요하면 비활성까지
        var cf = spine.color; cf.a = 0f; spine.color = cf;
        // 선택: 투명만 두고 싶으면 아래 두 줄 주석
        spine.gameObject.SetActive(false);
        // Destroy 원하면 위의 hideMode를 DestroyObject로 사용
    }
}
