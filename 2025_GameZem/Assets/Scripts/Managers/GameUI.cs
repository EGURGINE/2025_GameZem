using UnityEngine;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [Header("UI 요소")]
    public Text timeDisplayText;
    public Text rankingDisplayText;
    public Button startButton;
    public Button endButton;
    public Button clearRecordsButton;
    
    [Header("게임 상태 표시")]
    public Text gameStatusText;
    public Image gameStatusIndicator;
    
    private void Start()
    {
        SetupUI();
        UpdateUI();
    }
    
    private void Update()
    {
        UpdateUI();
    }
    
    /// <summary>
    /// UI 초기 설정
    /// </summary>
    private void SetupUI()
    {
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);
            
        if (endButton != null)
            endButton.onClick.AddListener(OnEndButtonClicked);
            
        if (clearRecordsButton != null)
            clearRecordsButton.onClick.AddListener(OnClearRecordsButtonClicked);
    }
    
    /// <summary>
    /// UI 업데이트
    /// </summary>
    private void UpdateUI()
    {
        if (TestScore.Instance == null) return;
        
        // 시간 표시 업데이트
        if (timeDisplayText != null)
        {
            timeDisplayText.text = $"시간: {TestScore.Instance.GetCurrentGameTime():F1}초";
        }
        
        // 순위 표시 업데이트
        if (rankingDisplayText != null)
        {
            UpdateRankingDisplay();
        }
        
        // 게임 상태 표시 업데이트
        UpdateGameStatusDisplay();
        
        // 버튼 상태 업데이트
        UpdateButtonStates();
    }
    
    /// <summary>
    /// 순위 표시 업데이트
    /// </summary>
    private void UpdateRankingDisplay()
    {
        if (rankingDisplayText == null || TestScore.Instance == null) return;
        
        string rankingText = "🏆 순위표\n\n";
        
        var records = TestScore.Instance.gameRecords;
        if (records.Count == 0)
        {
            rankingText += "아직 기록이 없습니다.";
        }
        else
        {
            for (int i = 0; i < records.Count && i < 5; i++) // 상위 5개만 표시
            {
                int rank = i + 1;
                string medal = GetMedalEmoji(rank);
                rankingText += $"{medal} {rank}위: {records[i].timeInSeconds:F2}초  |  {records[i].dateTime}\n";
            }
        }
        
        rankingDisplayText.text = rankingText;
    }
    
    /// <summary>
    /// 순위에 따른 메달 이모지 반환
    /// </summary>
    private string GetMedalEmoji(int rank)
    {
        switch (rank)
        {
            case 1: return "🥇";
            case 2: return "🥈";
            case 3: return "🥉";
            default: return "🏅";
        }
    }
    
    /// <summary>
    /// 게임 상태 표시 업데이트
    /// </summary>
    private void UpdateGameStatusDisplay()
    {
        if (TestScore.Instance == null) return;
        
        if (gameStatusText != null)
        {
            if (TestScore.Instance.IsGameRunning())
            {
                gameStatusText.text = "게임 진행 중";
                gameStatusText.color = Color.green;
            }
            else
            {
                gameStatusText.text = "게임 대기 중";
                gameStatusText.color = Color.white;
            }
        }
        
        if (gameStatusIndicator != null)
        {
            gameStatusIndicator.color = TestScore.Instance.IsGameRunning() ? Color.green : Color.red;
        }
    }
    
    /// <summary>
    /// 버튼 상태 업데이트
    /// </summary>
    private void UpdateButtonStates()
    {
        if (TestScore.Instance == null) return;
        
        bool isGameRunning = TestScore.Instance.IsGameRunning();
        
        if (startButton != null)
            startButton.interactable = !isGameRunning;
            
        if (endButton != null)
            endButton.interactable = isGameRunning;
    }
    
    /// <summary>
    /// 시작 버튼 클릭 이벤트
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (TestScore.Instance != null)
        {
            TestScore.Instance.StartGame();
        }
    }
    
    /// <summary>
    /// 종료 버튼 클릭 이벤트
    /// </summary>
    private void OnEndButtonClicked()
    {
        if (TestScore.Instance != null)
        {
            TestScore.Instance.EndGame();
        }
    }
    
    /// <summary>
    /// 기록 삭제 버튼 클릭 이벤트
    /// </summary>
    private void OnClearRecordsButtonClicked()
    {
        if (TestScore.Instance != null)
        {
            // 확인 대화상자 (간단한 버전)
            if (Application.isEditor || Debug.isDebugBuild)
            {
                TestScore.Instance.ClearAllRecords();
            }
            else
            {
                // 실제 빌드에서는 더 안전한 확인 방법 사용
                TestScore.Instance.ClearAllRecords();
            }
        }
    }
    
    /// <summary>
    /// 최고 기록 표시
    /// </summary>
    public void ShowBestRecord()
    {
        if (TestScore.Instance != null)
        {
            float bestTime = TestScore.Instance.GetBestTime();
            if (bestTime > 0)
            {
                Debug.Log($"최고 기록: {bestTime:F2}초");
            }
            else
            {
                Debug.Log("아직 기록이 없습니다.");
            }
        }
    }
}
