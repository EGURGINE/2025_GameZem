using UnityEngine;
using UnityEngine.EventSystems;
using Spine;
using Spine.Unity;

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

    [Header("After Disappear")]
    [Tooltip("disappear 종료 후 이 오브젝트를 제거할지")]
    public bool destroyAfterDisappear = true;

    [Tooltip("disappear 후 다시는 리스폰하지 않게 스포너를 멈출지(선택)")]
    public TimedUIRandomSpawner spawnerToStop;  // 인스펙터에 스포너를 드래그해서 연결하면 그 스포너의 리스폰을 막습니다.

    [Header("Random Position (UI)")]
    [Tooltip("랜덤 배치할 영역들(모두 RectTransform). 비어있으면 위치 변경 안 함")]
    public RectTransform[] spawnAreas;

    private SkeletonGraphic sg;       // UI용
    private SkeletonAnimation sa;     // 월드용
    private bool _disappeared;        // 중복 실행 방지
    private bool _removed;            // 제거 1회 보장

    void Awake()
    {
        sg = GetComponent<SkeletonGraphic>();
        sa = GetComponent<SkeletonAnimation>();
        if (sg) sg.raycastTarget = true; // UI 클릭 보장
    }

    void OnEnable()
    {
        _disappeared = false;
        _removed = false;

        if (sg && spawnAreas != null && spawnAreas.Length > 0)
            PlaceInRandomArea();
    }

    void Start()
    {
        if (playOnStart) PlaySequence();
    }

    /// idle → (idleTime 뒤) disappear 예약 + 자동 제거 스케줄
    public void PlaySequence()
    {
        float disappearLen = GetAnimDuration(disappearAnim);

        if (sg != null)
        {
            sg.Initialize(true);
            sg.AnimationState.SetAnimation(0, idleAnim, true);
            sg.AnimationState.AddAnimation(0, disappearAnim, false, idleTime);

            // idleTime + disappear 길이 후 자동 제거
            Invoke(nameof(RemoveSelfOnce), Mathf.Max(0.01f, idleTime + disappearLen));
            return;
        }

        if (sa != null)
        {
            sa.Initialize(true);
            sa.state.SetAnimation(0, idleAnim, true);
            sa.state.AddAnimation(0, disappearAnim, false, idleTime);

            Invoke(nameof(RemoveSelfOnce), Mathf.Max(0.01f, idleTime + disappearLen));
            return;
        }

        Debug.LogWarning("[ToyAnimator] SkeletonGraphic/Animation이 없습니다.", this);
    }

    /// 클릭/터치 시 즉시 disappear로 전환 + 끝나면 제거(리스폰 차단 포함)
    public void OnPointerDown(PointerEventData eventData) => PlayDisappearNow();

    public void PlayDisappearNow()
    {
        if (_disappeared) return;
        _disappeared = true;

        float disappearLen = GetAnimDuration(disappearAnim);

        if (sg != null)
            sg.AnimationState.SetAnimation(0, disappearAnim, false);
        else if (sa != null)
            sa.state.SetAnimation(0, disappearAnim, false);

        // 이미 예약되어 있던 자동 제거가 있더라도, 클릭 시점부터 길이만큼만 기다려 제거
        CancelInvoke(nameof(RemoveSelfOnce));
        Invoke(nameof(RemoveSelfOnce), Mathf.Max(0.01f, disappearLen));
    }

    void OnMouseDown()  // 월드 오브젝트용(콜라이더 필요)
    {
        if (sa != null) PlayDisappearNow();
    }

    // ===== Helpers =====
    private void RemoveSelfOnce()
    {
        if (_removed) return;
        _removed = true;

        // 이후 리스폰 막기
        if (spawnerToStop != null)
        {
            spawnerToStop.loop = false;        // 다시는 스폰하지 않도록
            spawnerToStop.enabled = false;     // 안전하게 비활성화
        }

        if (destroyAfterDisappear) Destroy(gameObject);
        else gameObject.SetActive(false);
    }

    private float GetAnimDuration(string animName)
    {
        SkeletonData data = null;
        if (sg != null && sg.Skeleton != null) data = sg.Skeleton.Data;
        else if (sa != null && sa.Skeleton != null) data = sa.Skeleton.Data;

        var anim = data?.FindAnimation(animName);
        return anim != null ? anim.Duration : 0.3f; // 못 찾으면 기본값
    }

    private void PlaceInRandomArea()
    {
        var rt = transform as RectTransform;
        if (!rt) return;
        int idx = Random.Range(0, spawnAreas.Length);
        var area = spawnAreas[idx];
        if (!area) return;

        Rect r = area.rect;
        Vector3 local = new Vector3(Random.Range(r.xMin, r.xMax),
                                    Random.Range(r.yMin, r.yMax), 0f);
        Vector3 world = area.TransformPoint(local);
        rt.position = world;
    }
}
