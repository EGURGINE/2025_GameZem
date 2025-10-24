using UnityEngine;
using UnityEngine.UI;

public class CutLine : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color lineColor = Color.red;
    public Color waitingColor = Color.yellow; // 대기 중 색상
    public Color successColor = Color.green;
    public Color missColor = Color.red;
    public float lineThickness = 3f;
    public float pulseSpeed = 2f;
    public float colorChangeDuration = 0.3f; // 색상 변화 지속 시간
    
    private RectTransform rectTransform;
    private Image lineImage;
    private Vector3 originalScale;
    private bool hasActiveCut = false; // 활성 컷이 있는지 확인
    private bool isShowingFeedback = false; // 피드백 표시 중인지
    private bool isWaiting = false; // 대기 중인지
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        lineImage = GetComponent<Image>();
        originalScale = transform.localScale;
        
        SetupCutLine();
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
            lineImage.color = lineColor;
            
        }
    }
    
    
    private void PulseAnimation()
    {
        if (lineImage == null || isShowingFeedback) return;
        
        // 대기 중이면 노란색, 아니면 빨간색
        lineImage.color = isWaiting ? waitingColor : lineColor;
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
    }
    
    public void ShowSuccessFeedback()
    {
        StartCoroutine(ShowColorFeedback(successColor));
    }
    
    public void ShowMissFeedback()
    {
        StartCoroutine(ShowColorFeedback(missColor));
    }
    
    private System.Collections.IEnumerator ShowColorFeedback(Color feedbackColor)
    {
        isShowingFeedback = true;
        isWaiting = false; // 피드백 중에는 대기 상태 해제
        
        if (lineImage != null)
        {
            lineImage.color = feedbackColor;
        }
        
        yield return new WaitForSeconds(colorChangeDuration);
        
        if (lineImage != null)
        {
            lineImage.color = lineColor;
        }
        
        isShowingFeedback = false;
    }
    
    private void OnDrawGizmos()
    {
        // 에디터에서 컷라인 시각화
        if (rectTransform != null)
        {
            Vector3 linePos = transform.position;
            Gizmos.color = lineColor;
            Gizmos.DrawWireCube(linePos, new Vector3(Screen.width, lineThickness, 0));
        }
    }
}
