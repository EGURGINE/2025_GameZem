using UnityEngine;

public class StopButton : MonoBehaviour
{
    [Header("정지 버튼 설정")]
    private bool isPaused = false;
    
    /// <summary>
    /// 버튼 클릭 시 호출되는 메서드
    /// </summary>
    public void OnStopButtonClick()
    {
        TogglePause();
    }
    
    /// <summary>
    /// 게임 정지/재개 토글
    /// </summary>
    private void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }
    
    /// <summary>
    /// 게임 정지
    /// </summary>
    private void PauseGame()
    {
        Time.timeScale = 0f; // 게임 시간 정지
        Debug.Log("게임이 정지되었습니다.");
        
        // 정지 UI 표시 (선택사항)
        // pausePanel.SetActive(true);
    }
    
    /// <summary>
    /// 게임 재개
    /// </summary>
    private void ResumeGame()
    {
        Time.timeScale = 1f; // 게임 시간 재개
        Debug.Log("게임이 재개되었습니다.");
        
        // 정지 UI 숨김 (선택사항)
        // pausePanel.SetActive(false);
    }
    
    /// <summary>
    /// 외부에서 정지 상태 확인용
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
}
