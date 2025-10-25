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
    
    [Header("Game Objects")]
    public GameObject cutPrefab;
    public Transform cutContainer;
    public Transform cutLine;
    public CutSpawner cutSpawner; // CutSpawner 참조
    
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
        currentLives = maxLives;
        score = 0;
        currentDate = startDate;
        isGameActive = true;
        isGameCleared = false;
        
        // 이벤트로 초기 상태 알림
        OnDateChanged?.Invoke(currentDate);
        OnLivesChanged?.Invoke(currentLives);
        UpdateProgress();
    }
    
    private void StartGame()
    {
        // 게임 시작 (타임라인 기반 시스템은 자동으로 진행됨)
        playTIme = 0;
        
        if (cutSpawner == null)
        {
            Debug.LogWarning("CutSpawner not assigned! Please assign CutSpawner in GameManager.");
        }
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
            OnDateChanged?.Invoke(currentDate);
            UpdateProgress();
            GameCleared();
        }
        else
        {
            currentDate = newDate;
            OnDateChanged?.Invoke(currentDate);
            UpdateProgress();
            Debug.Log("Date increased: " + currentDate.ToString("yyyy. MM. dd"));
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
        
        OnProgressChanged?.Invoke(progress);
    }
    
    // CutSpawner에서 호출할 공개 메서드
    public void OnCutSuccessCallback()
    {
        if (!isGameActive || isGameCleared) return;
        
        // 성공 시 점수는 증가하지 않음 (생존만 중요)
        OnComboAdded?.Invoke();
        Debug.Log("Cut Success!");
        
        // 성공 효과음 재생
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlayCutSuccessSound();
        // }
    }
    
    public void OnCutMissCallback()
    {
        if (!isGameActive) return;
        
        LoseLife();
        Debug.Log("Cut Miss! Lives: " + currentLives);
        
        // 실패 효과음 재생
        // if (SoundManager.Instance != null)
        // {
        //     SoundManager.Instance.PlayCutMissSound();
        // }
    }
    
    
    public void LoseLife()
    {
        currentLives--;
        OnLivesChanged?.Invoke(currentLives);
        
        if (currentLives <= 0)
        {
            GameOver();
        }
    }
    
    private void GameOver()
    {
        isGameActive = false;
        
        // 모든 컷 제거
        if (cutSpawner != null)
        {
            cutSpawner.ClearAllCuts();
        }
        
        OnGameOver?.Invoke();

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
        
        // 모든 컷 제거
        if (cutSpawner != null)
        {
            cutSpawner.ClearAllCuts();
        }
        
        OnGameCleared?.Invoke();
        
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
    
    public void RestartGame()
    {
        // CutSpawner 리셋 (스테이지를 처음부터)
        if (cutSpawner != null)
        {
            cutSpawner.ResetStage();
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
