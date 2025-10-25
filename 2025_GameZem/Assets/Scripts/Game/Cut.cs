using UnityEngine;
using UnityEngine.UI;
using System;

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
    private CutSpawner spawnerReference; // 스폰어 참조


    
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        cutImage = GetComponent<Image>();
    }
    
    private void Start()
    {
        // Cut Line 찾기
        GameObject cutLineObj = GameObject.FindGameObjectWithTag("CutLine");
        if (cutLineObj != null)
        {
            cutLineTransform = cutLineObj.transform;
        }
        
        // 랜덤 이미지 설정
        SetRandomImage();
    }
    
    private void Update()
    {
        MoveCut();
        CheckCutLine();
        HandleTouchInput();
    }
    
    public void Initialize(float speed, float xPosition, Action successCallback, Action missCallback)
    {
        moveSpeed = speed;
        onSuccess = successCallback;
        onMiss = missCallback;

        cutLine.SetActive(true);
        
        // 화면 하단에서 시작 (X축은 랜덤)
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(xPosition, -Screen.height * 0.5f);
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
        
        // 컷라인 대기 상태로 설정
        if (cutLineTransform != null)
        {
            CutLine cutLineComponent = cutLineTransform.GetComponent<CutLine>();
            if (cutLineComponent != null)
            {
                cutLineComponent.SetWaitingState(true);
            }
        }
        
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
        
        Vector2 currentPos = rectTransform.anchoredPosition;
        currentPos.y += moveSpeed * Time.deltaTime * 100f; // UI 스케일 조정
        rectTransform.anchoredPosition = currentPos;
        
        // 화면 상단을 벗어나면 제거
        if (currentPos.y > Screen.height + 100f)
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
        if (cutLineTransform == null || hasPassedCutLine) return;
        
        float cutLineY = cutLineTransform.GetComponent<RectTransform>().anchoredPosition.y;
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
    
    private void HandleTouchInput()
    {
        if (!isWaitingForTouch || hasPassedCutLine) return;
        
        // 터치 또는 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // 컷라인 범위 체크
            float cutLineY = cutLineTransform.GetComponent<RectTransform>().anchoredPosition.y;
            float cutY = rectTransform.anchoredPosition.y;
            float cutTopY = cutY + (rectTransform.sizeDelta.y / 2f);
            
            // 컷라인 범위 내에서 터치했는지 확인
            CutLine cutLineComponent = cutLineTransform.GetComponent<CutLine>();
            if (Mathf.Abs(cutTopY - cutLineY) <= successRange * 100f)
            {
                // 성공 판정
                onSuccess?.Invoke();
                ShowSuccessEffect();
                
                // Cut 내부의 cutLine 게임오브젝트 비활성화
                if (cutLine != null)
                {
                    cutLine.SetActive(false);
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
                onMiss?.Invoke();
                ShowMissEffect();
                
                // Cut 내부의 cutLine 게임오브젝트 비활성화
                if (cutLine != null)
                {
                    cutLine.SetActive(false);
                }
                
                // 컷라인 색상 변경 (실패) 및 대기 상태 해제
                if (cutLineComponent != null)
                {
                    cutLineComponent.ShowMissFeedback();
                    cutLineComponent.SetWaitingState(false);
                }
            }
            
            hasPassedCutLine = true;
            isWaitingForTouch = false; // 판정 후 터치 입력 비활성화
        }
    }
    
    
    private void ShowSuccessEffect()
    {
        if (successEffect != null)
        {
            GameObject effect = Instantiate(successEffect, transform);
            effect.transform.SetParent(transform.parent);
            Destroy(effect, 2f);
        }
        
        // 성공 애니메이션
        StartCoroutine(SuccessAnimation());
    }
    
    private void ShowMissEffect()
    {
        if (missEffect != null)
        {
            GameObject effect = Instantiate(missEffect, transform);
            effect.transform.SetParent(transform.parent);
            Destroy(effect, 2f);
        }
        
        // 실패 애니메이션
        StartCoroutine(MissAnimation());
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
}
