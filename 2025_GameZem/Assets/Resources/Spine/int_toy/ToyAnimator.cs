using UnityEngine;
using UnityEngine.EventSystems;
using Spine.Unity;

/// <summary>
/// - 시작 시 idle 재생
/// - idleTime 초 뒤 자동으로 disappear 1회 재생
/// - 그 전에 사용자가 이 오브젝트를 클릭/터치하면 즉시 disappear 전환
/// - 지정한 여러 SpawnArea(RectTransform) 중 하나를 골라 그 내부 랜덤 위치에 배치
///   (UI Canvas 기준. 월드 오브젝트일 경우 영역 배치는 생략됨)
/// </summary>
public class ToyAnimator : MonoBehaviour, IPointerDownHandler
{
    [Header("Animation Settings")]
    [Tooltip("idle을 유지할 시간(초). 이 시간이 지나면 자동으로 disappear 실행")]
    public float idleTime = 2.0f;

    [Tooltip("시작 시 자동 실행할지 여부")]
    public bool playOnStart = true;

    [Tooltip("Spine 애니메이션 이름(정확히 일치해야 함)")]
    public string idleAnim = "idle";
    public string disappearAnim = "disappear";

    [Header("Random Position (UI)")]
    [Tooltip("랜덤 배치할 영역들(모두 RectTransform). 비어있으면 위치 변경 안 함")]
    public RectTransform[] spawnAreas;

    private SkeletonGraphic sg;       // UI용
    private SkeletonAnimation sa;     // 월드용
    private bool _disappeared;        // 중복 실행 방지

    void Awake()
    {
        sg = GetComponent<SkeletonGraphic>();
        sa = GetComponent<SkeletonAnimation>();

        // UI라면 클릭 받게끔 보장
        if (sg) sg.raycastTarget = true;
    }

    void OnEnable()
    {
        _disappeared = false;

        // UI인 경우 지정 영역 중 하나로 랜덤 배치
        if (sg && spawnAreas != null && spawnAreas.Length > 0)
            PlaceInRandomArea();
    }

    void Start()
    {
        if (playOnStart) PlaySequence();
    }

    /// <summary>
    /// idle → (idleTime 뒤) disappear
    /// 클릭으로 조기 전환되면 queued 애니메이션은 자동 취소(Replace)됨.
    /// </summary>
    public void PlaySequence()
    {
        if (sg != null)
        {
            sg.Initialize(true);
            sg.AnimationState.SetAnimation(0, idleAnim, true);
            // idleTime 뒤에 disappear 예약. 클릭 시 SetAnimation으로 대체됨.
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
    /// 사용자가 클릭/터치했을 때 즉시 disappear로 전환 (UI)
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        PlayDisappearNow();
    }

    /// <summary>
    /// 외부에서 즉시 disappear로 바꾸고 싶을 때 호출 (버튼/이벤트)
    /// </summary>
    public void PlayDisappearNow()
    {
        if (_disappeared) return;
        _disappeared = true;

        if (sg != null)
        {
            // 현재 트랙을 즉시 교체(큐 제거)
            sg.AnimationState.SetAnimation(0, disappearAnim, false);
        }
        else if (sa != null)
        {
            sa.state.SetAnimation(0, disappearAnim, false);
        }
    }

    /// <summary>
    /// 월드 오브젝트(스크린 스페이스 UI가 아닌 경우)에서 마우스 클릭으로도 동작하게.
    /// (Collider 필요)
    /// </summary>
    void OnMouseDown()
    {
        if (sa != null) PlayDisappearNow();
    }

    /// <summary>
    /// spawnAreas 중 하나를 골라 그 안의 랜덤 위치에 배치 (UI 전용).
    /// 서로 다른 Canvas라도 화면상의 절대 위치로 맞춰줌.
    /// </summary>
    private void PlaceInRandomArea()
    {
        var rt = transform as RectTransform;
        if (!rt) return;

        int idx = Random.Range(0, spawnAreas.Length);
        RectTransform area = spawnAreas[idx];
        if (!area) return;

        // 영역의 로컬 좌표계에서 임의 점
        Rect r = area.rect;
        Vector3 local = new Vector3(
            Random.Range(r.xMin, r.xMax),
            Random.Range(r.yMin, r.yMax),
            0f
        );

        // 영역 로컬 → 월드 → 내 RectTransform 위치로 반영
        Vector3 world = area.TransformPoint(local);
        rt.position = world;
    }
}
