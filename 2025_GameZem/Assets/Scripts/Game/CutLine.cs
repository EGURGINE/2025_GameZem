using UnityEngine;
using UnityEngine.UI;

public class CutLine : MonoBehaviour
{
    [Header("Visual Settings")]
    public Sprite lineSprite; // 기본 대기 스프라이트
    public Sprite successSprite; // 성공 스프라이트
    public Sprite missSprite; // 실패 스프라이트
    
    public float lineThickness = 3f;
    public float pulseSpeed = 2f;
    public float colorChangeDuration = 0.3f; // 색상 변화 지속 시간
    
    private RectTransform rectTransform;
    private Image lineImage;
    private Vector3 originalScale;
    private bool hasActiveCut = false; // 활성 컷이 있는지 확인
    private bool isShowingFeedback = false; // 피드백 표시 중인지
    private bool isWaiting = false; // 대기 중인지
    private Sprite defaultSprite; // 기본 스프라이트 저장
    private Color originalColor; // 원본 색상 저장
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
        originalScale = transform.localScale;
        
        // 원본 색상 저장
        if (lineImage != null)
        {
            originalColor = lineImage.color;
        }
        
        // 기본 스프라이트 저장
        if (lineImage != null && lineImage.sprite != null)
        {
            defaultSprite = lineImage.sprite;
        }
        
        // lineSprite가 설정되어 있으면 사용
        if (lineSprite != null)
        {
            defaultSprite = lineSprite;
        }
        
        //SetupCutLine();
        // CreateSuccessZone() 제거 - 불필요한 성공 영역 생성 방지
    }
    
    private void Update()
    {
        PulseAnimation();
        CheckForActiveCuts();
    }
    
    private void SetupCutLine()
    {
        if (lineImage != null)
        {
            lineImage.color = originalColor;
        }
    }
    
    
    private void PulseAnimation()
    {
        if (lineImage == null || isShowingFeedback) return;
        
        // 대기 중이면 대기 스프라이트, 아니면 기본 스프라이트로 복귀
        if (isWaiting)
        {
            // 대기 중: 대기 스프라이트
            if (lineSprite != null)
            {
                lineImage.sprite = lineSprite;
            }
        }
        else
        {
            // 대기 상태가 아님: 기본 스프라이트로 복귀
            if (defaultSprite != null)
            {
                lineImage.sprite = defaultSprite;
            }
        }
    }
    
    private void CheckForActiveCuts()
    {
        // 활성 컷이 있는지 확인
        GameObject[] cuts = GameObject.FindGameObjectsWithTag("Cut");
        hasActiveCut = cuts.Length > 0;
        
        // successZoneImage가 없으므로 색상 업데이트 제거
    }
    
    public float GetCutLineY()
    {
        if (rectTransform != null)
        {
            return rectTransform.anchoredPosition.y;
        }
        return 0f;
    }
    
    public bool IsInSuccessZone(float cutY)
    {
        float cutLineY = GetCutLineY();
        float distance = Mathf.Abs(cutY - cutLineY);
        return distance <= rectTransform.sizeDelta.y / 2f;
    }
    
    public void SetWaitingState(bool waiting)
    {
        isWaiting = waiting;
        
        // 스프라이트 즉시 업데이트
        if (lineImage != null && !isShowingFeedback)
        {
            if (isWaiting && lineSprite != null)
            {
                lineImage.sprite = lineSprite;
            }
            else if (!isWaiting && defaultSprite != null)
            {
                lineImage.sprite = defaultSprite;
            }
        }
    }
    
    public void ShowSuccessFeedback()
    {
        StartCoroutine(ShowSpriteFeedback(successSprite));
    }
    
    public void ShowMissFeedback()
    {
        StartCoroutine(ShowSpriteFeedback(missSprite));
    }
    
    private System.Collections.IEnumerator ShowSpriteFeedback(Sprite feedbackSprite)
    {
        isShowingFeedback = true;
        isWaiting = false; // 피드백 중에는 대기 상태 해제
        
        if (lineImage != null && feedbackSprite != null)
        {
            // 피드백 스프라이트 적용
            lineImage.sprite = feedbackSprite;
        }
        
        yield return new WaitForSeconds(colorChangeDuration);
        
        if (lineImage != null)
        {
            // 기본으로 복귀
            if (defaultSprite != null)
            {
                lineImage.sprite = defaultSprite;
            }
        }
        
        isShowingFeedback = false;
    }
    
    private void OnDrawGizmos()
    {
        // 에디터에서 컷라인 시각화
        if (rectTransform != null)
        {
            Vector3 linePos = transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(linePos, new Vector3(Screen.width, lineThickness, 0));
        }
    }
}
