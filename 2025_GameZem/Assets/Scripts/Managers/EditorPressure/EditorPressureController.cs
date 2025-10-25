using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

/// <summary>
/// 편집자 원고 독촉 이벤트 컨트롤러
/// - Spine SkeletonGraphic 애니메이션 사용
/// - 시작: appear → idle (루프)
/// - 종료: disappear → SetActive(false)
/// - CutSpawner의 속도 상한선을 증가시킴
/// - 토글 방식으로 동작 (시작/종료)
/// </summary>
public class EditorPressureController : MonoBehaviour
{
    [Header("Spine Animation")]
    [Tooltip("Spine SkeletonGraphic 컴포넌트")]
    public SkeletonGraphic skeletonGraphic;
    
    [Header("Animation Names")]
    [Tooltip("등장 애니메이션 이름")]
    public string appearAnimationName = "appear";
    
    [Tooltip("대기 애니메이션 이름 (루프)")]
    public string idleAnimationName = "idle";
    
    [Tooltip("퇴장 애니메이션 이름")]
    public string disappearAnimationName = "disappear";
    
    [Header("Settings")]
    [Tooltip("표시 지속 시간 (0이면 무한정, 편집자 이벤트가 토글될 때까지)")]
    public float displayDuration = 0f;
    
    private CutSpawner cutSpawner;
    private bool isActive = false;
    private bool isDisappearing = false;
    private float timer = 0f;
    
    private void Awake()
    {
        // SkeletonGraphic 자동 찾기
        if (skeletonGraphic == null)
        {
            skeletonGraphic = GetComponent<SkeletonGraphic>();
        }
        
        if (skeletonGraphic == null)
        {
            Debug.LogError("EditorPressureController: SkeletonGraphic not found!");
            return;
        }
        
        // CutSpawner 찾기
        cutSpawner = FindObjectOfType<CutSpawner>();
        if (cutSpawner == null)
        {
            Debug.LogWarning("EditorPressureController: CutSpawner not found!");
        }
        
        // Spine 이벤트 리스너 등록
        skeletonGraphic.AnimationState.Complete += OnSpineAnimationComplete;
    }
    
    private void Start()
    {
        // 이벤트 시작
        StartPressure();
    }
    
    private void Update()
    {
        if (!isActive) return;
        
        // displayDuration이 설정되어 있고, 시간이 지나면 자동 종료
        if (displayDuration > 0f)
        {
            timer += Time.deltaTime;
            if (timer >= displayDuration)
            {
                EndPressure();
            }
        }
    }
    
    private void StartPressure()
    {
        if (isActive) return;
        
        isActive = true;
        isDisappearing = false;
        timer = 0f;
        
        // CutSpawner에 편집자 이벤트 시작 알림
        if (cutSpawner != null)
        {
            // 항상 시작 (ObstacleManager에서 이미 중복 체크 완료)
            cutSpawner.ToggleEditorPressure();
        }
        
        // Appear 애니메이션 재생
        PlayAppearAnimation();
        
        Debug.Log("Editor Pressure Event: Started with Appear animation");
    }
    
    public void EndPressure()
    {
        if (!isActive || isDisappearing) return;
        
        isActive = false;
        isDisappearing = true;
        
        // Disappear 애니메이션 재생
        PlayDisappearAnimation();
        
        Debug.Log("Editor Pressure Event: Ending with Disappear animation");
    }
    
    private void PlayAppearAnimation()
    {
        if (skeletonGraphic == null) return;
        
        // Appear 애니메이션 재생 (한 번만)
        skeletonGraphic.AnimationState.SetAnimation(0, appearAnimationName, false);
        
        Debug.Log($"Playing Appear animation: {appearAnimationName}");
    }
    
    private void PlayIdleAnimation()
    {
        if (skeletonGraphic == null) return;
        
        // Idle 애니메이션 재생 (루프)
        skeletonGraphic.AnimationState.SetAnimation(0, idleAnimationName, true);
        
        Debug.Log($"Playing Idle animation: {idleAnimationName} (Loop)");
    }
    
    private void PlayDisappearAnimation()
    {
        if (skeletonGraphic == null) return;
        
        // Disappear 애니메이션 재생 (한 번만)
        skeletonGraphic.AnimationState.SetAnimation(0, disappearAnimationName, false);
        
        Debug.Log($"Playing Disappear animation: {disappearAnimationName}");
    }
    
    private void OnSpineAnimationComplete(TrackEntry trackEntry)
    {
        // 애니메이션이 완료되었을 때 호출
        string completedAnimation = trackEntry.Animation.Name;
        
        if (completedAnimation == appearAnimationName)
        {
            // Appear 완료 -> Idle로 전환
            PlayIdleAnimation();
            Debug.Log("Appear animation completed, switching to Idle");
        }
        else if (completedAnimation == disappearAnimationName)
        {
            // Disappear 완료 -> 오브젝트 비활성화 및 제거
            gameObject.SetActive(false);
            Destroy(gameObject);
            Debug.Log("Disappear animation completed, destroying object");
        }
    }
    
    // 외부에서 강제 종료할 때 사용
    public void ForceEnd()
    {
        if (cutSpawner != null && cutSpawner.IsEditorPressureActive())
        {
            cutSpawner.ToggleEditorPressure();
        }
        
        StopAllCoroutines();
        Destroy(gameObject);
    }
    
    private void OnDestroy()
    {
        // Spine 이벤트 리스너 해제
        if (skeletonGraphic != null && skeletonGraphic.AnimationState != null)
        {
            skeletonGraphic.AnimationState.Complete -= OnSpineAnimationComplete;
        }
        
        // 오브젝트가 제거될 때 CutSpawner 상태 확인
        // (이미 종료되었을 수도 있으므로 확인 후 처리)
        if (cutSpawner != null && isActive && !isDisappearing)
        {
            // 아직 활성화 중이었다면 종료 처리
            if (cutSpawner.IsEditorPressureActive())
            {
                cutSpawner.ToggleEditorPressure();
            }
        }
    }
}

