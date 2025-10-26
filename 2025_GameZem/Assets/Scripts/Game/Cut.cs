using UnityEngine;
using UnityEngine.UI;
using System;
using Spine.Unity;
public class Cut : MonoBehaviour
{
    [Header("Cut Settings")]
    public float moveSpeed = 2f;
    public float successRange = 0.5f;
    public Sprite[] cutSprites; // 다양한 웹툰 컷 이미지들
    
    [Header("Visual Effects")]
    public GameObject successEffect;
    public GameObject missEffect;

    [SerializeField] private GameObject cutLine;
    
    private RectTransform rectTransform;
    private Image cutImage;
    // private bool hasReachedCutLine = false; // 사용되지 않음 - 제거
    private bool hasPassedCutLine = false;
    private bool isWaitingForTouch = false;
    
    // Events
    private Action onSuccess;
    private Action onMiss;
    
    // Cut Line Reference
    private Transform cutLineTransform;
    private RectTransform cutLineRectTransform; // 컷라인 RectTransform 캐싱
    private CutSpawner spawnerReference; // 스폰어 참조

    [Header("Cut Line Tape")]
    [SerializeField] private GameObject cutLineTapePrefab;
    private bool isTape = false;
    
    // Double Click Detection
    private float lastClickTime = 0f;
    private float doubleClickThreshold = 0.3f; // 더블클릭으로 인정하는 시간 간격 (초) - 연타에 관대하게 수정

    [Header("Spine")]
    private Spine.Unity.SkeletonAnimation[] skeletonAnimations; // 여러 개의 SkeletonAnimation 지원
    private Spine.Unity.SkeletonGraphic[] skeletonGraphics; // 여러 개의 SkeletonGraphic 지원
    [SerializeField] private string idleAnimationName = "default"; // 정지 애니메이션 이름
    [SerializeField] private string successAnimationName = "idle"; // 성공 시 재생할 애니메이션 이름
    [SerializeField] private bool loopSuccessAnimation = true; // 성공 애니메이션 루프 여부
    
    private Spine.Unity.SkeletonGraphic tapeGraphic;
    private Spine.Unity.SkeletonAnimation tapeAnimation;

    [SerializeField] private string tapeAnimationName = "idle";
    [SerializeField] private string tapeSuccessAnimationName = "disappear";

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        cutImage = GetComponent<Image>();
        
        // Spine 애니메이션 컴포넌트들 찾기 (여러 개 지원) - null safety 추가
        try
        {
            skeletonAnimations = GetComponentsInChildren<Spine.Unity.SkeletonAnimation>();
            skeletonGraphics = GetComponentsInChildren<Spine.Unity.SkeletonGraphic>();
        }
        catch (System.Exception e)
        {
            skeletonAnimations = new Spine.Unity.SkeletonAnimation[0];
            skeletonGraphics = new Spine.Unity.SkeletonGraphic[0];
        }
        
        if (skeletonAnimations != null && skeletonAnimations.Length > 0)
        {
            for (int i = 0; i < skeletonAnimations.Length; i++)
            {
                if (skeletonAnimations[i] != null)
                {
                    try
                    {
                        var data = skeletonAnimations[i].Skeleton?.Data;
                    }
                    catch (System.Exception e)
                    {
                        // 에러 처리
                    }
                }
            }
            SetIdleAnimation();
        }
        else if (skeletonGraphics != null && skeletonGraphics.Length > 0)
        {
            for (int i = 0; i < skeletonGraphics.Length; i++)
            {
                if (skeletonGraphics[i] != null)
                {
                    try
                    {
                        var data = skeletonGraphics[i].Skeleton?.Data;
                    }
                    catch (System.Exception e)
                    {
                        // 에러 처리
                    }
                }
            }
            SetIdleAnimation();
        }
    }
    
    private void SetIdleAnimation()
    {
        bool hasValidComponents = (skeletonAnimations != null && skeletonAnimations.Length > 0) || 
                                 (skeletonGraphics != null && skeletonGraphics.Length > 0);
        
        if (!hasValidComponents)
        {
            return;
        }
        
        // idle 애니메이션이 있으면 설정 (첫 프레임)
        if (!string.IsNullOrEmpty(idleAnimationName))
        {
            try
            {
                // SkeletonAnimation들 처리
                if (skeletonAnimations != null && skeletonAnimations.Length > 0)
                {
                    for (int i = 0; i < skeletonAnimations.Length; i++)
                    {
                        if (skeletonAnimations[i] != null)
                        {
                            // SkeletonAnimation 사용
                            skeletonAnimations[i].timeScale = 0f;
                            
                            // 애니메이션 존재 여부 확인
                            string animToPlay = idleAnimationName;
                            try
                            {
                                if (skeletonAnimations[i].Skeleton?.Data?.FindAnimation(idleAnimationName) == null)
                                {
                                    animToPlay = "idle";
                                }
                            }
                            catch (System.Exception e)
                            {
                                animToPlay = "idle";
                            }
                            
                            var trackEntry = skeletonAnimations[i].AnimationState.SetAnimation(0, animToPlay, false);
                        }
                    }
                }
                
                // SkeletonGraphic들 처리
                if (skeletonGraphics != null && skeletonGraphics.Length > 0)
                {
                    for (int i = 0; i < skeletonGraphics.Length; i++)
                    {
                        if (skeletonGraphics[i] != null)
                        {
                            // SkeletonGraphic 사용
                            skeletonGraphics[i].timeScale = 0f;
                            
                            // 애니메이션 존재 여부 확인
                            string animToPlay = idleAnimationName;
                            try
                            {
                                if (skeletonGraphics[i].Skeleton?.Data?.FindAnimation(idleAnimationName) == null)
                                {
                                    // 사용 가능한 애니메이션 목록에서 첫 번째 애니메이션 사용
                                    var animations = skeletonGraphics[i].Skeleton?.Data?.Animations;
                                    if (animations != null && animations.Count > 0)
                                    {
                                        animToPlay = animations.Items[0].Name;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                            catch (System.Exception e)
                            {
                                animToPlay = "idle";
                            }
                            
                            var trackEntry = skeletonGraphics[i].AnimationState.SetAnimation(0, animToPlay, false);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                // 에러 처리
            }
        }
    }
    
    private void InitializeTapeAnimation()
    {
        // isTape 상태가 아니면 테이프 애니메이션 초기화하지 않음
        if (!isTape)
        {
            return;
        }
        
        // 테이프 스파인 애니메이션 컴포넌트 찾기
        try
        {
            if (cutLineTapePrefab == null)
            {
                Debug.LogWarning("[Cut] cutLineTapePrefab이 null입니다.");
                return;
            }
            
            // 테이프 프리팹이 활성화되어 있는지 확인
            Debug.Log($"[Cut] 테이프 프리팹 상태 확인 - activeInHierarchy: {cutLineTapePrefab.activeInHierarchy}, activeSelf: {cutLineTapePrefab.activeSelf}");
            if (!cutLineTapePrefab.activeInHierarchy)
            {
                Debug.LogWarning("[Cut] cutLineTapePrefab이 비활성화 상태입니다.");
                return;
            }
            
            // SkeletonGraphic 우선 검색
            tapeGraphic = cutLineTapePrefab.GetComponentInChildren<Spine.Unity.SkeletonGraphic>();
            if (tapeGraphic == null)
            {
                // SkeletonAnimation 검색
                tapeAnimation = cutLineTapePrefab.GetComponentInChildren<Spine.Unity.SkeletonAnimation>();
            }
            
            Debug.Log($"[Cut] 테이프 애니메이션 초기화 - tapeGraphic: {tapeGraphic != null}, tapeAnimation: {tapeAnimation != null}");
            
            if (tapeGraphic != null)
            {
                // 테이프 idle 애니메이션 설정
                SetTapeIdleAnimation();
            }
            else if (tapeAnimation != null)
            {
                // 테이프 idle 애니메이션 설정
                SetTapeIdleAnimation();
            }
            else
            {
                Debug.LogWarning("[Cut] 테이프 프리팹에서 Spine 애니메이션 컴포넌트를 찾을 수 없습니다.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Cut] 테이프 애니메이션 초기화 중 오류: {e.Message}");
        }
    }
    
    private void SetTapeIdleAnimation()
    {
        if (!string.IsNullOrEmpty(tapeAnimationName))
        {
            try
            {
                if (tapeGraphic != null)
                {
                    tapeGraphic.timeScale = 0f; // 정지 상태
                    var trackEntry = tapeGraphic.AnimationState.SetAnimation(0, tapeAnimationName, false);
                    Debug.Log($"[Cut] 테이프 idle 애니메이션 설정 (Graphic): {tapeAnimationName}");
                }
                else if (tapeAnimation != null)
                {
                    tapeAnimation.timeScale = 0f; // 정지 상태
                    var trackEntry = tapeAnimation.AnimationState.SetAnimation(0, tapeAnimationName, false);
                    Debug.Log($"[Cut] 테이프 idle 애니메이션 설정 (Animation): {tapeAnimationName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Cut] 테이프 idle 애니메이션 설정 중 오류: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[Cut] tapeAnimationName이 설정되지 않았습니다.");
        }
    }
    
    private void PlayTapeSuccessAnimation()
    {
        if (!isTape)
        {
            return;
        }
        
        // 테이프 프리팹 활성화 상태 확인
        if (cutLineTapePrefab != null)
        {
            Debug.Log($"[Cut] 테이프 success 애니메이션 재생 전 상태 - activeInHierarchy: {cutLineTapePrefab.activeInHierarchy}, activeSelf: {cutLineTapePrefab.activeSelf}");
        }
        
        if (!string.IsNullOrEmpty(tapeSuccessAnimationName))
        {
            try
            {
                if (tapeGraphic != null)
                {
                    tapeGraphic.timeScale = 1f; // 정상 속도로 재생
                    tapeGraphic.AnimationState.ClearTracks();
                    var trackEntry = tapeGraphic.AnimationState.SetAnimation(0, tapeSuccessAnimationName, false);
                    Debug.Log($"[Cut] 테이프 success 애니메이션 재생 (Graphic): {tapeSuccessAnimationName}");
                }
                else if (tapeAnimation != null)
                {
                    tapeAnimation.timeScale = 1f; // 정상 속도로 재생
                    tapeAnimation.AnimationState.ClearTracks();
                    var trackEntry = tapeAnimation.AnimationState.SetAnimation(0, tapeSuccessAnimationName, false);
                    Debug.Log($"[Cut] 테이프 success 애니메이션 재생 (Animation): {tapeSuccessAnimationName}");
                }
                else
                {
                    Debug.LogWarning("[Cut] 테이프 애니메이션 컴포넌트가 null입니다.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Cut] 테이프 success 애니메이션 재생 중 오류: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[Cut] tapeSuccessAnimationName이 설정되지 않았습니다.");
        }
    }
    
    private void PlaySuccessAnimation()
    {
        bool hasValidComponents = (skeletonAnimations != null && skeletonAnimations.Length > 0) || 
                                 (skeletonGraphics != null && skeletonGraphics.Length > 0);
        
        if (!hasValidComponents)
        {
            return;
        }
        
        // 성공 애니메이션 재생
        if (!string.IsNullOrEmpty(successAnimationName))
        {
            try
            {
                // SkeletonAnimation들 처리
                if (skeletonAnimations != null && skeletonAnimations.Length > 0)
                {
                    for (int i = 0; i < skeletonAnimations.Length; i++)
                    {
                        if (skeletonAnimations[i] != null)
                        {
                            // SkeletonAnimation 사용
                            skeletonAnimations[i].timeScale = 1f;
                            
                            // AnimationState 초기화 (Clear)
                            skeletonAnimations[i].AnimationState.ClearTracks();
                            
                            // 애니메이션 존재 여부 확인 후 설정
                            string animToPlay = successAnimationName;
                            if (skeletonAnimations[i].Skeleton.Data.FindAnimation(successAnimationName) == null)
                            {
                                animToPlay = "idle";
                            }
                            
                            var trackEntry = skeletonAnimations[i].AnimationState.SetAnimation(0, animToPlay, loopSuccessAnimation);
                            if (trackEntry != null)
                            {
                                trackEntry.Loop = loopSuccessAnimation; // 명시적으로 루프 설정
                                trackEntry.TimeScale = 1f; // TrackEntry의 timeScale도 설정
                            }
                        }
                    }
                }
                
                // SkeletonGraphic들 처리
                if (skeletonGraphics != null && skeletonGraphics.Length > 0)
                {
                    for (int i = 0; i < skeletonGraphics.Length; i++)
                    {
                        if (skeletonGraphics[i] != null)
                        {
                            // SkeletonGraphic 사용
                            skeletonGraphics[i].timeScale = 1f;
                            
                            // AnimationState 초기화 (Clear)
                            skeletonGraphics[i].AnimationState.ClearTracks();
                            
                            // 애니메이션 존재 여부 확인 후 설정
                            string animToPlay = successAnimationName;
                            if (skeletonGraphics[i].Skeleton.Data.FindAnimation(successAnimationName) == null)
                            {
                                // 사용 가능한 애니메이션 목록에서 첫 번째 애니메이션으로 대체
                                var animations = skeletonGraphics[i].Skeleton.Data.Animations;
                                if (animations.Count > 0)
                                {
                                    animToPlay = animations.Items[0].Name;
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            
                            var trackEntry = skeletonGraphics[i].AnimationState.SetAnimation(0, animToPlay, loopSuccessAnimation);
                            if (trackEntry != null)
                            {
                                trackEntry.Loop = loopSuccessAnimation; // 명시적으로 루프 설정
                                trackEntry.TimeScale = 1f; // TrackEntry의 timeScale도 설정
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                // 에러 처리
            }
        }
    }
    
    private void Start()
    {
        // Cut Line 찾기
        GameObject cutLineObj = GameObject.FindGameObjectWithTag("CutLine");
        if (cutLineObj != null)
        {
            cutLineTransform = cutLineObj.transform;
            cutLineRectTransform = cutLineObj.GetComponent<RectTransform>(); // RectTransform 캐싱
        }
        
        // 랜덤 이미지 설정
        SetRandomImage();
    }
    
    private void Update()
    {
        // GameObject가 비활성화되면 Update 중단
        if (!gameObject.activeInHierarchy) return;
        
        CheckCutLine();
        // 터치 입력은 CutSpawner에서 중앙 관리
    }
    
    private void LateUpdate()
    {
        // GameObject가 비활성화되면 LateUpdate 중단
        if (!gameObject.activeInHierarchy) return;
        
        // UI는 LateUpdate에서 움직이는 것이 더 부드러움
        MoveCut();
    }
    
    public void Initialize(bool isTape,float speed, float xPosition, Action successCallback, Action missCallback)
    {
        moveSpeed = speed;
        onSuccess = successCallback;
        onMiss = missCallback;

        this.isTape = isTape;
        
        // cutLineTapePrefab이 있을 때만 활성화/비활성화
        if (cutLineTapePrefab != null)
        {
            cutLineTapePrefab.SetActive(isTape);
        }

        if (cutLine != null)
        {
            cutLine.SetActive(!isTape);
        }
        
        if(this.isTape)
        {
            // 테이프 애니메이션 초기화 (isTape 상태 설정 후)
            InitializeTapeAnimation();
            if (cutLineTapePrefab != null)
            {
                cutLineTapePrefab.SetActive(true);
                Debug.Log($"[Cut] 테이프 프리팹 활성화 - isActive: {cutLineTapePrefab.activeInHierarchy}, isActiveSelf: {cutLineTapePrefab.activeSelf}");
            }
        }
        
        // 화면 하단에서 시작 (X축은 랜덤, 화면 밖으로 나가지 않도록 제한)
        if (rectTransform != null)
        {
            // 화면 중앙 기준 좌우 절반 너비 (헤더 100px 고려)
            float halfWidth = (Screen.width * 0.5f) - 50f;
            
            // 컷 이미지의 절반 너비를 고려하여 범위 계산
            float cutHalfWidth = rectTransform.sizeDelta.x * 0.5f;
            
            // X 위치를 화면 안에 머물도록 클램프
            float clampedX = Mathf.Clamp(xPosition, -halfWidth + cutHalfWidth, halfWidth - cutHalfWidth);
            
            rectTransform.anchoredPosition = new Vector2(clampedX, -Screen.height * 0.5f);
        }
    }
    
    public void SetSpawnerReference(CutSpawner spawner)
    {
        spawnerReference = spawner;
    }
    
    public void ResetCutState()
    {
        // 컷 상태 초기화 (재사용을 위해)
        // hasReachedCutLine = false; // 제거됨
        hasPassedCutLine = false;
        isWaitingForTouch = true; // 스폰 즉시 터치 대기 시작
        lastClickTime = 0f; // 더블클릭 타이머 리셋
        
        // 트랜스폼 초기화
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = new Vector2(0, -Screen.height * 0.5f); // Initialize와 동일한 위치
        }
        
        // Cut 내부의 cutLine 게임오브젝트 다시 활성화
        if (cutLine != null)
        {
            cutLine.SetActive(true);
        }
        
        // 테이프 프리팹 상태 초기화 (재사용을 위해)
        if (cutLineTapePrefab != null)
        {
            cutLineTapePrefab.SetActive(false); // 초기에는 비활성화
        }
        
        // 컷라인 RectTransform 다시 캐싱 (오브젝트 풀링 대응)
        if (cutLineTransform != null && cutLineRectTransform == null)
        {
            cutLineRectTransform = cutLineTransform.GetComponent<RectTransform>();
        }
        
        // 컷라인 대기 상태로 설정
        if (cutLineTransform != null)
        {
            CutLine cutLineComponent = cutLineTransform.GetComponent<CutLine>();
            if (cutLineComponent != null)
            {
                cutLineComponent.SetWaitingState(true);
            }
        }
        
        // Spine 참조가 없으면 다시 찾기 (오브젝트 풀링 대응)
        bool hasValidComponents = (skeletonAnimations != null && skeletonAnimations.Length > 0) || 
                                 (skeletonGraphics != null && skeletonGraphics.Length > 0);
        
        if (!hasValidComponents)
        {
            skeletonAnimations = GetComponentsInChildren<Spine.Unity.SkeletonAnimation>();
            skeletonGraphics = GetComponentsInChildren<Spine.Unity.SkeletonGraphic>();
            Debug.Log($"[Cut] ResetCutState - Spine 재탐색: SkeletonAnimation={skeletonAnimations?.Length ?? 0}개, SkeletonGraphic={skeletonGraphics?.Length ?? 0}개");
        }
        
        // Spine 애니메이션 다시 정지 (idle 상태로)
        SetIdleAnimation();
        
        // 테이프 애니메이션 컴포넌트 리셋
        tapeGraphic = null;
        tapeAnimation = null;
        
        // 랜덤 이미지 다시 설정
        SetRandomImage();
    }
    
    private void SetRandomImage()
    {
        if (cutImage != null && cutSprites != null && cutSprites.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, cutSprites.Length);
            cutImage.sprite = cutSprites[randomIndex];
        }
    }
    
    private void MoveCut()
    {
        if (rectTransform == null) return;
        
        // 부드러운 움직임을 위한 최적화된 위치 업데이트
        Vector2 currentPos = rectTransform.anchoredPosition;
        float deltaMove = moveSpeed * Time.deltaTime * 100f;
        currentPos.y += deltaMove;
        rectTransform.anchoredPosition = currentPos;
        
        // 화면 상단을 벗어나면 제거 (더 큰 여유를 둠)
        if (currentPos.y > Screen.height * 0.6f + 200f)
        {
            if (!hasPassedCutLine)
            {
                onMiss?.Invoke();
            }
            DestroyCut();
        }
    }
    
    private void CheckCutLine()
    {
        if (cutLineRectTransform == null || hasPassedCutLine) return;
        
        float cutLineY = cutLineRectTransform.anchoredPosition.y;
        float cutY = rectTransform.anchoredPosition.y;
        
        // 이미지 상단 기준으로 계산 (이미지 높이의 절반을 더함)
        float cutTopY = cutY + (rectTransform.sizeDelta.y / 2f);
        
        // 컷라인을 완전히 지나쳤는지만 확인 (실패 판정)
        if (cutTopY > cutLineY + successRange * 100f && isWaitingForTouch)
        {
            // 컷 라인을 지나쳤고 터치하지 않았으면 실패
            onMiss?.Invoke();
            ShowMissEffect();
            hasPassedCutLine = true;
            isWaitingForTouch = false; // 판정 후 터치 입력 비활성화
            
            // Cut 내부의 cutLine 게임오브젝트 비활성화
            if (cutLine != null)
            {
                cutLine.SetActive(false);
            }
            
            // 컷라인 색상 변경 (실패) 및 대기 상태 해제
            CutLine cutLineComponent = cutLineTransform.GetComponent<CutLine>();
            if (cutLineComponent != null)
            {
                cutLineComponent.ShowMissFeedback();
                cutLineComponent.SetWaitingState(false);
            }
        }
    }
    
    // CutSpawner에서 호출 - CutLine 범위 내에 있는지 확인
    public bool IsInCutLineRange()
    {
        if (!isWaitingForTouch || hasPassedCutLine) 
        {
            Debug.Log($"[Cut] IsInCutLineRange false - isWaitingForTouch: {isWaitingForTouch}, hasPassedCutLine: {hasPassedCutLine}");
            return false;
        }
        if (cutLineRectTransform == null) 
        {
            Debug.Log("[Cut] IsInCutLineRange false - cutLineRectTransform is null");
            return false;
        }
        
        float cutLineY = cutLineRectTransform.anchoredPosition.y;
        float cutY = rectTransform.anchoredPosition.y;
        float cutTopY = cutY + (rectTransform.sizeDelta.y / 2f);
        float distance = Mathf.Abs(cutTopY - cutLineY);
        float threshold = successRange * 100f;
        
        bool inRange = distance <= threshold;
        Debug.Log($"[Cut] IsInCutLineRange - cutTopY: {cutTopY:F1}, cutLineY: {cutLineY:F1}, distance: {distance:F1}, threshold: {threshold:F1}, inRange: {inRange}");
        
        return inRange;
    }
    
    // Cut의 상단 Y 좌표 반환 (정렬용)
    public float GetCutTopY()
    {
        if (rectTransform == null) return float.MaxValue;
        float cutY = rectTransform.anchoredPosition.y;
        return cutY + (rectTransform.sizeDelta.y / 2f);
    }
    
    // CutSpawner에서 호출 - 클릭 시도 (더블클릭 체크 포함)
    public bool TryProcessClick()
    {
        if (!isWaitingForTouch || hasPassedCutLine) return false;
        
        // isTape 상태일 때는 더블클릭만 인식
        if (isTape)
        {
            float currentTime = Time.time;
            float timeSinceLastClick = currentTime - lastClickTime;
            
            // 더블클릭 확인
            if (timeSinceLastClick <= doubleClickThreshold)
            {
                // 더블클릭 성공 - 판정 진행
                ProcessCutInput();
                lastClickTime = 0f; // 더블클릭 처리 후 리셋
                return true;
            }
            else
            {
                // 첫 번째 클릭 - 시간 저장하고 잠시 대기
                lastClickTime = currentTime;
                return false; // 아직 처리 안됨
            }
        }
        else
        {
            // isTape가 아니면 단일 클릭으로 판정
            ProcessCutInput();
            return true;
        }
    }
    
    private void ProcessCutInput()
    {
        // 이미 CutLine을 지나쳤거나 터치 대기 상태가 아니면 처리하지 않음
        if (hasPassedCutLine || !isWaitingForTouch) 
        {
            return;
        }
        
        // 컷라인 범위 체크
        float cutLineY = cutLineRectTransform.anchoredPosition.y;
        float cutY = rectTransform.anchoredPosition.y;
        float cutTopY = cutY + (rectTransform.sizeDelta.y / 2f);
        
        // 컷라인 범위 내에서 터치했는지 확인
        CutLine cutLineComponent = cutLineTransform.GetComponent<CutLine>();
        if (Mathf.Abs(cutTopY - cutLineY) <= successRange * 400f * (isTape? 2:1))
        {
            // 성공 판정
            onSuccess?.Invoke();
            ShowSuccessEffect();
            
            // 상태 업데이트
            hasPassedCutLine = true;
            isWaitingForTouch = false;
            
            // Cut 내부의 cutLine 게임오브젝트 비활성화
            if (cutLine != null)
            {
                cutLine.SetActive(false);
            }
            
            // Tape Prefab은 성공 애니메이션 후에 비활성화 (애니메이션 완료 후 처리)
            if (cutLineTapePrefab != null && isTape)
            {
                // 성공 애니메이션이 완료된 후 비활성화하도록 코루틴으로 처리
                StartCoroutine(DisableTapeAfterAnimation());
            }
            
            // 테이프 스파인 애니메이션 재생 (isTape 상태일 때)
            if (isTape)
            {
                PlayTapeSuccessAnimation();
            }
            
            // 컷라인 색상 변경 (성공) 및 대기 상태 해제
            if (cutLineComponent != null)
            {
                cutLineComponent.ShowSuccessFeedback();
                cutLineComponent.SetWaitingState(false);
            }
        }
        else
        {
            // 범위 밖에서 터치 - 실패
            Debug.Log("[Cut] 범위 밖에서 터치 - 실패 판정");
            onMiss?.Invoke();
            ShowMissEffect();
            
            // 상태 업데이트
            hasPassedCutLine = true;
            isWaitingForTouch = false;
            
            // Cut 내부의 cutLine 게임오브젝트 비활성화
            if (cutLine != null)
            {
                cutLine.SetActive(false);
            }
            
            // Tape Prefab 비활성화 (실패 시 즉시 비활성화)
            if (cutLineTapePrefab != null && isTape)
            {
                cutLineTapePrefab.SetActive(false);
                Debug.Log("[Cut] 실패 시 테이프 프리팹 비활성화");
            }
            
            // 컷라인 색상 변경 (실패) 및 대기 상태 해제
            if (cutLineComponent != null)
            {
                cutLineComponent.ShowMissFeedback();
                cutLineComponent.SetWaitingState(false);
            }
        }
    }
    
    
    private void ShowSuccessEffect()
    {
        Debug.Log("[Cut] ShowSuccessEffect 호출됨!");
        
        if (successEffect != null)
        {
            GameObject effect = Instantiate(successEffect, transform);
            effect.transform.SetParent(transform.parent);
            Destroy(effect, 2f);
        }
        
        // Spine 애니메이션 재생 (성공 시)
        Debug.Log("[Cut] PlaySuccessAnimation 호출 직전");
        PlaySuccessAnimation();
        Debug.Log("[Cut] PlaySuccessAnimation 호출 완료");
        
        // 성공 애니메이션 (게임 오브젝트가 활성화 상태일 때만)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(SuccessAnimation());
        }
    }
    
    private void ShowMissEffect()
    {
        if (missEffect != null)
        {
            GameObject effect = Instantiate(missEffect, transform);
            effect.transform.SetParent(transform.parent);
            Destroy(effect, 2f);
        }
        
        // 실패 애니메이션 (게임 오브젝트가 활성화 상태일 때만)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(MissAnimation());
        }
    }
    
    private System.Collections.IEnumerator SuccessAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.2f, 1f, elapsed / duration); // 0f 대신 1f로 변경
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }
    
    private System.Collections.IEnumerator MissAnimation()
    {
        Vector3 originalScale = transform.localScale;
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1f, elapsed / duration); // 0f 대신 1f로 변경
            transform.localScale = originalScale * scale;
            yield return null;
        }
    }
    
    private void DestroyCut()
    {
        // 스폰어에게 컷이 사라진다고 알림 (자신을 전달)
        if (spawnerReference != null)
        {
            spawnerReference.OnCutDestroyed(gameObject);
        }
        
        // 오브젝트 풀링을 위해 Destroy 대신 비활성화
        gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // 클릭 이벤트 제거 로직 삭제
    }
    
    /// <summary>
    /// 터치 대기 상태인지 확인
    /// </summary>
    public bool IsWaitingForTouch()
    {
        return isWaitingForTouch;
    }
    
    /// <summary>
    /// CutLine을 지나쳤는지 확인
    /// </summary>
    public bool HasPassedCutLine()
    {
        return hasPassedCutLine;
    }
    
    /// <summary>
    /// CutLine과의 거리 계산
    /// </summary>
    public float GetDistanceToCutLine()
    {
        if (cutLineRectTransform == null) return float.MaxValue;
        
        float cutLineY = cutLineRectTransform.anchoredPosition.y;
        float cutY = rectTransform.anchoredPosition.y;
        float cutTopY = cutY + (rectTransform.sizeDelta.y / 2f);
        
        return Mathf.Abs(cutTopY - cutLineY);
    }
    
    /// <summary>
    /// 테이프 애니메이션 수동 테스트 (디버그용)
    /// </summary>
    [ContextMenu("Test Tape Animation")]
    public void TestTapeAnimation()
    {
        // 테이프 애니메이션 재초기화
        InitializeTapeAnimation();
        
        // 2초 후 success 애니메이션 재생
        StartCoroutine(TestTapeAnimationCoroutine());
    }
    
    private System.Collections.IEnumerator TestTapeAnimationCoroutine()
    {
        yield return new WaitForSeconds(2f);
        PlayTapeSuccessAnimation();
    }
    
    /// <summary>
    /// 테이프 애니메이션 완료 후 테이프 프리팹을 비활성화하는 코루틴
    /// </summary>
    private System.Collections.IEnumerator DisableTapeAfterAnimation()
    {
        // 애니메이션 재생 시간 대기 (disappear 애니메이션 길이에 따라 조정)
        yield return new WaitForSeconds(1f);
        
        if (cutLineTapePrefab != null)
        {
            Debug.Log($"[Cut] 테이프 비활성화 전 상태 - activeInHierarchy: {cutLineTapePrefab.activeInHierarchy}, activeSelf: {cutLineTapePrefab.activeSelf}");
            cutLineTapePrefab.SetActive(false);
            Debug.Log($"[Cut] 테이프 애니메이션 완료 후 프리팹 비활성화 - activeInHierarchy: {cutLineTapePrefab.activeInHierarchy}, activeSelf: {cutLineTapePrefab.activeSelf}");
        }
    }
}
