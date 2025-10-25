using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 게임 기록 관리 및 순위 시스템을 담당하는 클래스
/// GameManager의 LoadRecords() 메서드와 연동하여 순위별 기록 확인 기능 제공
/// </summary>
public class TestScore : MonoBehaviour
{
    [System.Serializable]
    public class GameRecord
    {
        public float timeInSeconds;
        public string dateTime;
        public bool isCleared;
        
        public GameRecord(float time, bool cleared = true)
        {
            timeInSeconds = time;
            dateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            isCleared = cleared;
        }
        
        public GameRecord(SaveData saveData)
        {
            timeInSeconds = saveData.time;
            dateTime = saveData.date;
            isCleared = saveData.isCleared;
        }
    }
    
    // 싱글톤 패턴
    public static TestScore Instance { get; private set; }
    
    [Header("순위 시스템 설정")]
    public int maxRecordsToKeep = 10; // 최대 보관할 기록 수
    public bool autoSyncWithGameManager = true; // GameManager와 자동 동기화 여부
    
    [Header("UI 참조")]
    public UnityEngine.UI.Text rankingDisplayText; // 순위 표시용 UI 텍스트
    
    // 내부 변수
    public List<GameRecord> gameRecords = new List<GameRecord>();
    
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // GameManager에서 기록 불러오기
            LoadRecordsFromGameManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // UI 업데이트
        UpdateRankingDisplay();
    }
    
    private void Update()
    {
        // 자동 동기화가 활성화된 경우 GameManager와 동기화
        if (autoSyncWithGameManager && GameManager.Instance != null)
        {
            SyncWithGameManagerRecords();
        }
    }
    
    #region GameManager 연동 메서드
    
    /// <summary>
    /// GameManager의 LoadRecords() 메서드와 연동하여 기록 불러오기
    /// </summary>
    public void LoadRecordsFromGameManager()
    {
        const string SAVE_KEY = "GameRecords";
        string json = PlayerPrefs.GetString(SAVE_KEY);
        SaveDataList dataList = JsonUtility.FromJson<SaveDataList>(json);

        gameRecords = dataList.records.Select(data => new GameRecord(data)).ToList();
        if (GameManager.Instance != null)
        {
            // GameManager의 LoadRecords() 메서드 호출
            //GameManager.Instance.LoadRecords();

            
            // GameManager에서 기록 가져오기
            List<SaveData> gameManagerRecords = GameManager.Instance.GetRecords();
            
            // 기존 기록 초기화
            gameRecords.Clear();
            
            // GameManager의 기록을 GameRecord로 변환
            foreach (SaveData saveData in gameManagerRecords)
            {
                GameRecord gameRecord = new GameRecord(saveData);
                gameRecords.Add(gameRecord);
            }
            
            // 시간 순으로 정렬 (낮은 시간이 1위)
            SortRecordsByTime();
            
            Debug.Log($"GameManager에서 {gameRecords.Count}개 기록을 불러왔습니다.");
        }
        else
        {
            Debug.LogWarning("GameManager.Instance가 없습니다.");
        }
    }
    
    /// <summary>
    /// GameManager와 기록 동기화
    /// </summary>
    private void SyncWithGameManagerRecords()
    {
        if (GameManager.Instance != null)
        {
            List<SaveData> gameManagerRecords = GameManager.Instance.GetRecords();
            
            // 기록 수가 다르면 동기화
            if (gameManagerRecords.Count != gameRecords.Count)
            {
                LoadRecordsFromGameManager();
                UpdateRankingDisplay();
            }
        }
    }
    
    #endregion
    
    #region 순위 표시 및 UI 관리
    
    /// <summary>
    /// 순위 표시 업데이트
    /// </summary>
    private void UpdateRankingDisplay()
    {
        if (rankingDisplayText != null)
        {
            string rankingText = "🏆 순위표 🏆\n";
            rankingText += "==================\n";
            
            if (gameRecords.Count == 0)
            {
                rankingText += "기록이 없습니다.\n";
                rankingText += "게임을 플레이해보세요!";
            }
            else
            {
                for (int i = 0; i < gameRecords.Count && i < 10; i++) // 상위 10개 표시
                {
                    int rank = i + 1;
                    GameRecord record = gameRecords[i];
                    string medal = GetMedalEmoji(rank);
                    string status = record.isCleared ? "✅ 클리어" : "❌ 게임오버";
                    
                    rankingText += $"{medal} {rank}위: {record.timeInSeconds:F2}초\n";
                    rankingText += $"   📅 {record.dateTime}\n";
                    rankingText += $"   {status}\n\n";
                }
            }
            
            rankingDisplayText.text = rankingText;
        }
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
    
    #endregion
    
    #region 기록 관리 메서드
    
    /// <summary>
    /// 모든 기록을 시간 순으로 정렬 (낮은 시간이 1위)
    /// </summary>
    public void SortRecordsByTime()
    {
        gameRecords = gameRecords.OrderBy(record => record.timeInSeconds).ToList();
        Debug.Log("기록이 시간 순으로 정렬되었습니다.");
    }
    
    /// <summary>
    /// 모든 기록 삭제
    /// </summary>
    public void ClearAllRecords()
    {
        gameRecords.Clear();
        UpdateRankingDisplay();
        Debug.Log("모든 기록이 삭제되었습니다.");
    }
    
    /// <summary>
    /// 수동으로 GameManager 기록 동기화 (UI 버튼에서 호출 가능)
    /// </summary>
    public void ManualSyncRecords()
    {
        LoadRecordsFromGameManager();
        UpdateRankingDisplay();
        Debug.Log("수동 동기화 완료");
    }
    
    #endregion
    
    #region 순위 조회 메서드
    
    /// <summary>
    /// 최고 기록 가져오기
    /// </summary>
    public float GetBestTime()
    {
        if (gameRecords.Count > 0)
        {
            return gameRecords[0].timeInSeconds;
        }
        return 0f;
    }
    
    /// <summary>
    /// 전체 기록 수 가져오기
    /// </summary>
    public int GetTotalRecordCount()
    {
        return gameRecords.Count;
    }
    
    /// <summary>
    /// 특정 순위의 기록 가져오기 (1부터 시작)
    /// </summary>
    public GameRecord GetRecordByRank(int rank)
    {
        if (rank > 0 && rank <= gameRecords.Count)
        {
            return gameRecords[rank - 1];
        }
        return null;
    }
    
    /// <summary>
    /// 특정 시간의 순위 가져오기 (1부터 시작)
    /// </summary>
    public int GetRankByTime(float time)
    {
        if (gameRecords.Count == 0) return 1;
        
        int rank = 1;
        foreach (var record in gameRecords)
        {
            if (time > record.timeInSeconds)
            {
                rank++;
            }
        }
        
        return rank;
    } 
    
    /// <summary>
    /// 상위 N개 기록 가져오기
    /// </summary>
    public List<GameRecord> GetTopRecords(int count)
    {
        return gameRecords.Take(count).ToList();
    }
    
    #endregion
    
    #region 호환성 메서드 (다른 스크립트와의 호환성을 위해 유지)
    
    /// <summary>
    /// 게임 시작 (다른 스크립트 호환성을 위해 유지)
    /// </summary>
    public void StartGame()
    {
        Debug.Log("TestScore: 게임 시작");
        LoadRecordsFromGameManager();
    }
    
    /// <summary>
    /// 게임 종료 (다른 스크립트 호환성을 위해 유지)
    /// </summary>
    public void EndGame()
    {
        Debug.Log("TestScore: 게임 종료");
        LoadRecordsFromGameManager();
    }
    
    /// <summary>
    /// 현재 게임 시간 가져오기 (다른 스크립트 호환성을 위해 유지)
    /// </summary>
    public float GetCurrentGameTime()
    {
        return GetBestTime();
    }
    
    /// <summary>
    /// 게임이 실행 중인지 확인 (다른 스크립트 호환성을 위해 유지)
    /// </summary>
    public bool IsGameRunning()
    {
        return false; // 실제 게임 상태는 GameManager에서 관리
    }
    
    #endregion
}
