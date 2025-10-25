using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    public int maxLives = 3;
    public float cutSpeed = 2f;
    public float cutSpacing = 3f;
    public float successThreshold = 0.5f; // 컷 라인 근처 성공 범위
    public float touchWindowTime = 1f; // 터치 가능 시간 (초)
    
    [Header("Date Score Settings")]
    public float monthIncreaseInterval = 5f; // 몇 초마다 1개월 증가
    private System.DateTime startDate = new System.DateTime(2006, 9, 8);
    private System.DateTime endDate = new System.DateTime(2020, 7, 27);
    private System.DateTime currentDate;
    private float playTIme = 0;
    
    // Game State
    private int currentLives;
    private int score;
    private bool isGameActive = true;
    private bool isGameCleared = false;
    
    // Events
    public System.Action<System.DateTime> OnDateChanged; // 날짜 변경 이벤트
    public System.Action<int> OnLivesChanged;
    public System.Action OnGameOver;
    public System.Action OnGameCleared; // 게임 클리어 이벤트
    public System.Action OnComboAdded;
    public System.Action<float> OnProgressChanged; // 진행도 변경 이벤트


    // Save Data
    private const string SAVE_KEY = "GameRecords";
    private const int MAX_RECORDS = 5; // 최대 저장 개수
    public List<SaveData> saveDataList = new List<SaveData>();
    public Dictionary<string, SaveData> recordsDictionary = new Dictionary<string, SaveData>();
    
    protected override void Awake()
    {
        base.Awake();
        LoadRecords();
        InitializeGame();
    }
    
    private void Start()
    {
        // Time.timeScale을 확실히 1로 설정 (일시정지 상태 해제)
        Time.timeScale = 1f;
        StartGame();
    }
    
    private void Update()
    {
        if (isGameActive && !isGameCleared)
        {
            UpdateDateProgress();
        }
    }
    
    private void InitializeGame()
    {
        // Time.timeScale을 확실히 1로 설정
        Time.timeScale = 1f;
        
        currentLives = maxLives;
        score = 0;
        currentDate = startDate;
        isGameActive = true;
        isGameCleared = false;
        
        // CutSpawner 상태도 초기화 (씬 전환 시 안정성을 위해)
        CutSpawner cutSpawner = FindFirstObjectByType<CutSpawner>();
        if (cutSpawner != null)
        {
            cutSpawner.ResetGameState();
            Debug.Log("[GameManager] CutSpawner 게임 상태 초기화됨");
        }
        else
        {
            Debug.LogWarning("[GameManager] CutSpawner를 찾을 수 없습니다!");
        }
        
        // 필요한 객체들이 null이면 자동으로 찾기
        //FindRequiredObjects();
        
        // 이벤트로 초기 상태 알림 (안전하게 호출)
        SafeInvokeEvent(() => OnDateChanged?.Invoke(currentDate), "OnDateChanged");
        SafeInvokeEvent(() => OnLivesChanged?.Invoke(currentLives), "OnLivesChanged");
        UpdateProgress();
    }
    
    private void StartGame()
    {
        // 게임 시작 (타임라인 기반 시스템은 자동으로 진행됨)
        playTIme = 0;
    }
    
    private void UpdateDateProgress()
    {
        // 플레이 타임만 업데이트 (날짜는 CutSpawner에서 관리)
        playTIme += Time.deltaTime;
    }
    
    public void AddMonth()
    {
        if (!isGameActive || isGameCleared) return;
        
        // 1개월 추가
        System.DateTime newDate = currentDate.AddMonths(1);
        
        // 종료일을 넘지 않도록 체크
        if (newDate > endDate)
        {
            currentDate = endDate;
            SafeInvokeEvent(() => OnDateChanged?.Invoke(currentDate), "OnDateChanged");
            UpdateProgress();
            GameCleared();
        }
        else
        {
            currentDate = newDate;
            SafeInvokeEvent(() => OnDateChanged?.Invoke(currentDate), "OnDateChanged");
            UpdateProgress();
            Debug.Log("Date increased: " + currentDate.ToString("yyyy. MM"));
        }
    }
    
    private void UpdateProgress()
    {
        // 전체 기간 대비 현재 진행도 계산
        double totalDays = (endDate - startDate).TotalDays;
        double currentDays = (currentDate - startDate).TotalDays;
        float progress = (float)(currentDays / totalDays);
        
        // 0~1 범위로 제한
        progress = Mathf.Clamp01(progress);
        
        SafeInvokeEvent(() => OnProgressChanged?.Invoke(progress), "OnProgressChanged");
    }
    
    // CutSpawner에서 호출할 공개 메서드
    public void OnCutSuccessCallback()
    {
        if (!isGameActive || isGameCleared) return;
        
        // 성공 시 점수는 증가하지 않음 (생존만 중요)
        SafeInvokeEvent(() => OnComboAdded?.Invoke(), "OnComboAdded");
        Debug.Log("Cut Success!");
        
        // 성공 효과음 재생
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlayCutSuccessSound();
        // }
    }
    
    public void OnCutMissCallback()
    {
        Debug.Log("[GameManager] OnCutMissCallback 호출됨 - isGameActive: " + isGameActive + ", currentLives: " + currentLives);
        
        if (!isGameActive) 
        {
            Debug.Log("[GameManager] 게임이 비활성화 상태라서 피 감소 안함");
            return;
        }
        
        Debug.Log("[GameManager] 피 감소 전 - 현재 생명: " + currentLives);
        LoseLife();
        Debug.Log("[GameManager] Cut Miss! Lives: " + currentLives);
        
        // 실패 효과음 재생
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlayCutMissSound();
        // }
    }
    
    
    public void LoseLife()
    {
        Debug.Log($"[GameManager] LoseLife 시작 - currentLives: {currentLives}");
        currentLives--;
        Debug.Log($"[GameManager] currentLives 감소 후: {currentLives}");
        
        // 이벤트 호출
        SafeInvokeEvent(() => OnLivesChanged?.Invoke(currentLives), "OnLivesChanged");
        
        if (currentLives <= 0)
        {
            Debug.Log("[GameManager] 생명이 0 이하 - GameOver 호출");
            GameOver();
        }
    }
    
    private void GameOver()
    {
        isGameActive = false;
        
        // 모든 컷 제거 및 게임 상태 설정
        CutSpawner cutSpawner = FindFirstObjectByType<CutSpawner>();
        if (cutSpawner != null)
        {
            cutSpawner.ResetStage();
            cutSpawner.SetGameActive(false);
        }

        
        
        SafeInvokeEvent(() => OnGameOver?.Invoke(), "OnGameOver");

        // 게임 기록 저장
        SaveData saveData = new SaveData
        {
            date = currentDate.ToString("yyyy. MM. dd"),
            time = playTIme,
            isCleared = false
        };
        saveDataList.Add(saveData);
        SaveRecords();
        
        TestScore.Instance?.ManualSyncRecords();
    }
    
    private void GameCleared()
    {
        isGameActive = false;
        isGameCleared = true;
        
        // 모든 컷 제거 및 게임 상태 설정
        CutSpawner cutSpawner = FindFirstObjectByType<CutSpawner>();
        if (cutSpawner != null)
        {
            cutSpawner.ClearAllCuts();
            cutSpawner.SetGameCleared(true);
        }
        
        SafeInvokeEvent(() => OnGameCleared?.Invoke(), "OnGameCleared");
        
        // 게임 클리어 기록 저장
        SaveData saveData = new SaveData
        {
            date = currentDate.ToString("yyyy. MM. dd"),
            time = (int)playTIme,
            isCleared = true
        };
        saveDataList.Add(saveData);
        SaveRecords();

        TestScore.Instance?.ManualSyncRecords();
        
        Debug.Log("Game Cleared! Reached: " + endDate.ToString("yyyy. MM. dd"));
        
        // 게임 클리어 효과음 재생
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlayGameClearedSound();
        // }
    }
    
    public int GetScore()
    {
        return score;
    }
    
    public int GetCurrentLives()
    {
        return currentLives;
    }
    
    public System.DateTime GetCurrentDate()
    {
        return currentDate;
    }
    
    public System.DateTime GetEndDate()
    {
        return endDate;
    }
    
    public bool IsGameActive()
    {
        return isGameActive;
    }
    
    public bool IsGameCleared()
    {
        return isGameCleared;
    }
    
    /// <summary>
    /// 이벤트를 안전하게 호출하는 헬퍼 메서드
    /// </summary>
    private void SafeInvokeEvent(System.Action eventAction, string eventName)
    {
        if (eventAction != null)
        {
            try
            {
                // 이벤트 구독자들을 안전하게 호출
                var invocationList = eventAction.GetInvocationList();
                if (invocationList != null && invocationList.Length > 0)
                {
                    foreach (var handler in invocationList)
                    {
                        try
                        {
                            handler.DynamicInvoke();
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"[GameManager] {eventName} 이벤트 구독자 호출 실패: {e.Message}");
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] {eventName} 이벤트 호출 실패: {e.Message}");
            }
        }
    }
    
    public void RestartGame()
    {
        // Time.timeScale을 확실히 1로 설정 (일시정지 상태 해제)
        Time.timeScale = 1f;
        
        // CutSpawner 리셋 (스테이지를 처음부터)
        CutSpawner cutSpawner = FindFirstObjectByType<CutSpawner>();
        if (cutSpawner != null)
        {
            cutSpawner.ResetStage();
            cutSpawner.ResetGameState();
            cutSpawner.SyncWithGameManager();
        }
        
        // 게임 재시작
        InitializeGame();
        StartGame();
    }
    
    // 기록 저장 (딕셔너리 기반)
    private void SaveRecords()
    {
        // 새 기록을 딕셔너리에 추가 (날짜+시간을 키로 사용)
        SaveData latestRecord = saveDataList[saveDataList.Count - 1];
        string recordKey = $"{latestRecord.date}_{latestRecord.time:F2}";
        
        if (!recordsDictionary.ContainsKey(recordKey))
        {
            recordsDictionary[recordKey] = latestRecord;
        }
        
        // 딕셔너리를 리스트로 변환하여 정렬
        List<SaveData> sortedRecords = new List<SaveData>(recordsDictionary.Values);
        sortedRecords.Sort((a, b) => b.time.CompareTo(a.time)); // 시간 기준 내림차순 정렬
        
        // 상위 5개만 유지
        if (sortedRecords.Count > MAX_RECORDS)
        {
            sortedRecords = sortedRecords.Take(MAX_RECORDS).ToList();
        }
        
        // 딕셔너리 업데이트
        recordsDictionary.Clear();
        foreach (var record in sortedRecords)
        {
            string key = $"{record.date}_{record.time:F2}";
            recordsDictionary[key] = record;
        }
        
        // 딕셔너리를 JSON으로 저장
        SaveDataList dataList = new SaveDataList();
        dataList.records = sortedRecords;
        
        string json = JsonUtility.ToJson(dataList, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        
        Debug.Log($"Records saved to dictionary: {recordsDictionary.Count} records (top {MAX_RECORDS})");
    }
    
    // 기록 불러오기 (딕셔너리 기반)
    private void LoadRecords()
    {
        if (PlayerPrefs.HasKey(SAVE_KEY))
        {
            string json = PlayerPrefs.GetString(SAVE_KEY);
            SaveDataList dataList = JsonUtility.FromJson<SaveDataList>(json);
            if (dataList != null && dataList.records != null)
            {
                saveDataList = dataList.records;
                
                // 딕셔너리에 기록 추가
                recordsDictionary.Clear();
                foreach (var record in saveDataList)
                {
                    string recordKey = $"{record.date}_{record.time:F2}";
                    recordsDictionary[recordKey] = record;
                }
                
                Debug.Log($"Records loaded to dictionary: {recordsDictionary.Count} records");
            }
        }
        else
        {
            Debug.Log("No saved records found");
        }
    }
    
    // 기록 가져오기 (시간 순으로 정렬)
    public List<SaveData> GetRecords()
    {
        // 시간 기준으로 내림차순 정렬하여 반환
        List<SaveData> sortedRecords = new List<SaveData>(saveDataList);
        sortedRecords.Sort((a, b) => b.time.CompareTo(a.time));
        return sortedRecords;
    }
    
    // 딕셔너리에서 기록 가져오기 (순위별)
    public Dictionary<string, SaveData> GetRecordsDictionary()
    {
        return recordsDictionary;
    }
    
    // 딕셔너리에서 순위별 기록 가져오기
    public List<SaveData> GetRecordsFromDictionary()
    {
        List<SaveData> sortedRecords = new List<SaveData>(recordsDictionary.Values);
        sortedRecords.Sort((a, b) => b.time.CompareTo(a.time)); // 시간 기준 내림차순 정렬
        return sortedRecords;
    }
    
    // 기록 삭제
    public void ClearRecords()
    {
        saveDataList.Clear();
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("All records cleared");
    }
    
    // 최고 기록 가져오기 (가장 오래 버틴 기록)
    public SaveData GetBestRecord()
    {
        if (saveDataList == null || saveDataList.Count == 0)
            return null;
        
        SaveData bestRecord = saveDataList[0];
        
        return bestRecord;
    }
    
    // 클리어 기록만 가져오기
    public List<SaveData> GetClearedRecords()
    {
        List<SaveData> clearedRecords = new List<SaveData>();
        foreach (SaveData record in saveDataList)
        {
            if (record.isCleared)
            {
                clearedRecords.Add(record);
            }
        }
        return clearedRecords;
    }
    
    /// <summary>
    /// CutSpawner에서 객체들을 설정했는지 확인 (CutSpawner가 자동으로 처리함)
    /// </summary>
    private void FindRequiredObjects()
    {
    }
}

[System.Serializable]
public class SaveData
{
    public string date;
    public float time;
    public bool isCleared;
}

[System.Serializable]
public class SaveDataList
{
    public List<SaveData> records = new List<SaveData>();
}
