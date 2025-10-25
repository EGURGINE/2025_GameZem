using UnityEngine;
using Spine.Unity;

/// <summary>
/// Spine 애니메이션을: idle을 먼저 재생하고, idle을 idleTime초 보여준 뒤 disappear 1회 재생.
/// UI(Canvas) 아래라면 SkeletonGraphic을, 월드 오브젝트라면 SkeletonAnimation을 자동으로 잡습니다.
/// </summary>
public class ToyAnimator : MonoBehaviour
{
    [Tooltip("idle을 유지할 시간(초)")]
    public float idleTime = 2.0f;

    [Tooltip("시작 시 자동 실행할지 여부")]
    public bool playOnStart = true;

    [Header("애니메이션 이름(Spine에서 실제 존재하는 이름)")]
    public string idleAnim = "idle";
    public string disappearAnim = "disappear";

    private SkeletonGraphic sg;       // UI용
    private SkeletonAnimation sa;     // 월드용

    void Awake()
    {
        // 둘 중 붙어있는 컴포넌트를 자동으로 캐싱
        sg = GetComponent<SkeletonGraphic>();
        sa = GetComponent<SkeletonAnimation>();
    }

    void Start()
    {
        if (playOnStart) PlaySequence();
    }

    /// <summary>
    /// idle -> (idleTime 후) disappear
    /// </summary>
    public void PlaySequence()
    {
        if (sg != null)
        {
            sg.Initialize(true);
            // idle 루프
            sg.AnimationState.SetAnimation(0, idleAnim, true);
            // idleTime 지연 후 disappear 1회
            sg.AnimationState.AddAnimation(0, disappearAnim, false, idleTime);
            return;
        }

        if (sa != null)
        {
            sa.Initialize(true);
            sa.state.SetAnimation(0, idleAnim, true);
            sa.state.AddAnimation(0, disappearAnim, false, idleTime);
            return;
        }

        Debug.LogWarning("[ToyAnimator] SkeletonGraphic/Animation이 없습니다.", this);
    }

    /// <summary>
    /// 외부에서 즉시 disappear로 바꾸고 싶을 때 호출
    /// </summary>
    public void PlayDisappearNow()
    {
        if (sg != null)
        {
            sg.AnimationState.SetAnimation(0, disappearAnim, false);
        }
        else if (sa != null)
        {
            sa.state.SetAnimation(0, disappearAnim, false);
        }
    }
}
