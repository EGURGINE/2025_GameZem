using UnityEngine;
using UnityEngine.EventSystems;
using Spine;
using Spine.Unity;

/// <summary>
/// - Start에서 idle 루프 시작
/// - autoDisappearDelay(기본 2초) 후 자동으로 disappear 1회 재생 → 제거
/// - 클릭(터치) 시 즉시 disappear 1회 재생 → 제거
/// - UI(SkeletonGraphic), 월드(SkeletonAnimation) 둘 다 지원
/// </summary>
public class SpineDisappearOnClickOrTime : MonoBehaviour, IPointerClickHandler
{
    [Header("Animation Names")]
    public string idleAnim = "idle";
    public string disappearAnim = "disappear";

    [Header("Timing")]
    [Tooltip("이 시간이 지나면 자동으로 disappear 실행")]
    public float autoDisappearDelay = 2f;

    [Header("After Disappear")]
    [Tooltip("disappear 끝나면 객체를 파괴할지 (끄면 SetActive(false))")]
    public bool destroyAfter = true;

    SkeletonGraphic sg;          // UI용
    SkeletonAnimation sa;        // 월드용
    bool disappeared;            // 중복 방지
    bool removed;                // 제거 1회 보장

    void Awake()
    {
        sg = GetComponent<SkeletonGraphic>();
        sa = GetComponent<SkeletonAnimation>();

        // UI는 클릭 받을 수 있게 보장
        if (sg) sg.raycastTarget = true;
    }

    void Start()
    {
        PlayIdle();
        // 자동 디스어피어 예약
        Invoke(nameof(DisappearNow), Mathf.Max(0.01f, autoDisappearDelay));
    }

    void PlayIdle()
    {
        if (sg != null)
        {
            sg.Initialize(true);
            sg.AnimationState.SetAnimation(0, idleAnim, true);
        }
        else if (sa != null)
        {
            sa.Initialize(true);
            sa.state.SetAnimation(0, idleAnim, true);
        }
    }

    /// <summary> 클릭/터치 시 즉시 disappear </summary>
    public void OnPointerClick(PointerEventData e) => DisappearNow();

    /// <summary> 월드 오브젝트(콜라이더 필요) 대응 </summary>
    void OnMouseDown() { if (sa != null) DisappearNow(); }

    /// <summary> 즉시 disappear 1회 재생 후 길이만큼 기다려 제거 </summary>
    public void DisappearNow()
    {
        if (disappeared) return;
        disappeared = true;

        float len = GetAnimDuration(disappearAnim);

        if (sg != null)
            sg.AnimationState.SetAnimation(0, disappearAnim, false);
        else if (sa != null)
            sa.state.SetAnimation(0, disappearAnim, false);

        // 제거 예약 (애니 길이 모르면 0.3s 사용)
        CancelInvoke(nameof(RemoveSelf));
        Invoke(nameof(RemoveSelf), Mathf.Max(0.1f, len));
    }

    void RemoveSelf()
    {
        if (removed) return;
        removed = true;

        if (destroyAfter) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    float GetAnimDuration(string animName)
    {
        SkeletonData data = null;
        if (sg != null && sg.Skeleton != null) data = sg.Skeleton.Data;
        else if (sa != null && sa.Skeleton != null) data = sa.Skeleton.Data;

        var anim = data?.FindAnimation(animName);
        return anim != null ? anim.Duration : 0.3f;
    }
}
