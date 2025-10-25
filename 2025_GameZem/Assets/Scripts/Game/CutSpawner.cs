using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CutSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] cutPrefabs; // 다양한 컷 프리팹들
    public Transform spawnContainer;
    public float spawnDelay = 0.5f; // 컷 스폰 딜레이 (초)
    public float spawnInterval = 2f;
    public float spawnIntervalVariation = 0.5f; // 스폰 간격 변화량
    
    [Header("Spawn Position")]
    public float spawnYOffset = -100f; // 화면 하단에서의 오프셋
    public float screenWidthPadding = 50f; // 화면 양쪽 여백
    
    [Header("Cut Speed Settings")]
    public float baseCutSpeed = 10f; // 기본 컷 속도
    public float normalSpeedCap = 2.5f; // 일반 상황 속도 상한선 (배수)
    public float editorPressureSpeedCap = 4.0f; // 편집자 독촉 시 속도 상한선 (배수)
    
    [Header("Difficulty Settings")]
    public float difficultyIncreaseRate = 0.1f; // 난이도 증가율
    public float maxSpawnInterval = 0.5f; // 최소 스폰 간격
    public float speedIncreaseRate = 0.02f; // 속도 증가율 (초당 배수 증가량)
    
    [Header("Object Pooling")]
    public int poolSize = 10; // 풀 크기
    
    [Header("Stage System")]
    public List<MonthStageData> monthStages = new List<MonthStageData>();
    public int currentStageIndex = 0;
    
    [Header("Obstacle Manager Reference")]
    public ObstacleManager obstacleManager;
    
    private float currentSpawnInterval;
    private float currentSpeedMultiplier = 1f;
    
    // 오브젝트 풀
    private Queue<GameObject> cutPool = new Queue<GameObject>();
    private List<GameObject> allCuts = new List<GameObject>(); // 모든 생성된 컷들
    private List<GameObject> activeCuts = new List<GameObject>(); // 현재 활성화된 컷들
    
    // 스테이지 진행 상황
    private int cutsSpawnedInStage = 0;
    private int obstaclesSpawnedInStage = 0;
    private List<ObstacleSpawnInfo> currentStageObstacles = new List<ObstacleSpawnInfo>();
    private int currentObstacleIndex = 0;
    
    // 테이프 이벤트 플래그
    private bool nextCutHasTape = false;
    
    // 편집자 원고 독촉 이벤트
    private bool isEditorPressureActive = false;
    
    // 타임라인 기반 스폰 시스템
    [Header("Timeline Settings")]
    public float monthDuration = 5f; // 한 달 지속 시간 (초)
    private float stageTimer = 0f;
    private List<SpawnEvent> spawnTimeline = new List<SpawnEvent>();
    private int nextSpawnIndex = 0;
    
    private void Start()
    {
        currentSpawnInterval = spawnInterval;
        InitializePool();
        
        // monthStages가 비어있으면 기본 스테이지 데이터 초기화
        if (monthStages.Count == 0)
        {
            InitializeDefaultStages();
        }
        
        StartStage(currentStageIndex);
    }
    
    private void Update()
    {
        // 게임이 활성화 상태이고 스테이지가 진행 중일 때만
        if (!GameManager.Instance.IsGameActive()) return;
        if (currentStageIndex >= monthStages.Count) return;
        
        stageTimer += Time.deltaTime;
        
        // 시간에 따른 난이도 점진적 증가
        IncreaseDifficulty();
        
        // 타임라인 체크 - 스폰할 이벤트가 있는지
        while (nextSpawnIndex < spawnTimeline.Count && spawnTimeline[nextSpawnIndex].spawnTime <= stageTimer)
        {
            SpawnEvent spawnEvent = spawnTimeline[nextSpawnIndex];
            
            if (spawnEvent.isCut)
            {
                SpawnCut();
            }
            else
            {
                SpawnObstacleByType(spawnEvent.obstacleType, spawnEvent.randomPosition);
            }
            
            nextSpawnIndex++;
        }
        
        // 터치 입력 처리 (중앙 관리)
        HandleCutTouchInput();
        
        // 스테이지 완료 체크 (모든 이벤트 소환 완료)
        if (nextSpawnIndex >= spawnTimeline.Count && spawnTimeline.Count > 0)
        {
            Debug.Log($"Stage {monthStages[currentStageIndex].monthName} completed! All {spawnTimeline.Count} events spawned.");
            
            // GameManager에 한 달 증가 알림
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMonth();
            }
            
            currentStageIndex++;
            
            if (currentStageIndex < monthStages.Count)
            {
                StartStage(currentStageIndex);
            }
            else
            {
                // 모든 스테이지 완료 (엔딩)
                Debug.Log("All stages completed! Game Ending!");
                OnGameEnding();
            }
        }
    }
    
    private void HandleCutTouchInput()
    {
        // 터치 또는 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // CutLine 범위 내에 있는 Cut들 찾기
            List<Cut> cutsInRange = new List<Cut>();
            foreach (GameObject cutObj in activeCuts)
            {
                if (cutObj == null || !cutObj.activeInHierarchy) continue;
                
                Cut cutScript = cutObj.GetComponent<Cut>();
                if (cutScript != null && cutScript.IsInCutLineRange())
                {
                    cutsInRange.Add(cutScript);
                }
            }
            
            // CutLine 범위 내에 Cut이 없으면 리턴
            if (cutsInRange.Count == 0) return;
            
            // Y 좌표가 가장 낮은 Cut 찾기 (화면에서 가장 아래쪽, CutLine에 가장 가까운)
            Cut topMostCut = cutsInRange[0];
            float lowestY = topMostCut.GetCutTopY();
            
            for (int i = 1; i < cutsInRange.Count; i++)
            {
                float currentY = cutsInRange[i].GetCutTopY();
                if (currentY < lowestY)
                {
                    lowestY = currentY;
                    topMostCut = cutsInRange[i];
                }
            }
            
            // 가장 위에 있는 Cut만 터치 처리
            topMostCut.TryProcessClick();
        }
    }
    
    private void InitializeDefaultStages()
    {
        // 스프레드시트 데이터에 맞춰 월별 스테이지 초기화
        if (monthStages.Count == 0)
        {
            // 2006_9: Cut 5, 없음
            monthStages.Add(new MonthStageData("2006_9", 5, new List<ObstacleSpawnInfo>()));
            
            // 2006_10: Cut 5, A 2
            monthStages.Add(new MonthStageData("2006_10", 5, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true)
            }));
            
            // 2006_11: Cut 5, B 2
            monthStages.Add(new MonthStageData("2006_11", 5, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true)
            }));
            
            // 2006_12: Cut 3, A 2, C 1
            monthStages.Add(new MonthStageData("2006_12", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false)
            }));
            
            // 2007_1: Cut 8, A 3, C 1, Editor Pressure (편집자 원고 독촉 시작)
            monthStages.Add(new MonthStageData("2007_1", 8, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 3, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2007_2: Cut 4, A 2, D 1, Editor Pressure (편집자 원고 독촉 종료)
            monthStages.Add(new MonthStageData("2007_2", 4, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2007_3: Cut 2, A 2 (조율의 낙서 시작/종료)
            monthStages.Add(new MonthStageData("2007_3", 2, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true)
            }));
            
            // 2007_4: Cut 4, C 1
            monthStages.Add(new MonthStageData("2007_4", 4, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false)
            }));
            
            // 2007_5: Cut 5, D 1
            monthStages.Add(new MonthStageData("2007_5", 5, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 1, false)
            }));
            
            // 2007_6: Cut 8, A 2, B 2, C 1, Editor Pressure (편집자 원고 독촉 시작)
            monthStages.Add(new MonthStageData("2007_6", 8, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2007_7: Cut 4, C 1, Editor Pressure (편집자 원고 독촉 종료)
            monthStages.Add(new MonthStageData("2007_7", 4, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2007_8: Cut 3, D 2
            monthStages.Add(new MonthStageData("2007_8", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 2, false)
            }));
            
            // 2007_9: Cut 5, A 2
            monthStages.Add(new MonthStageData("2007_9", 5, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true)
            }));
            
            // 2007_10: Cut 3, A 2, B 2
            monthStages.Add(new MonthStageData("2007_10", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true)
            }));
            
            // 2007_11: Cut 3, C 1
            monthStages.Add(new MonthStageData("2007_11", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false)
            }));
            
            // 2007_12: Cut 8, A 2, C 3, Editor Pressure (편집자 원고 독촉 시작)
            monthStages.Add(new MonthStageData("2007_12", 8, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 3, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2008_1: Cut 4, A 2, D 1, Editor Pressure (편집자 원고 독촉 종료)
            monthStages.Add(new MonthStageData("2008_1", 4, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2008_2: Cut 2, 없음 (조율의 낙서 시작/종료)
            monthStages.Add(new MonthStageData("2008_2", 2, new List<ObstacleSpawnInfo>()));
            
            // 2008_3: Cut 4, C 2
            monthStages.Add(new MonthStageData("2008_3", 4, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 2, false)
            }));
            
            // 2008_4: Cut 3, A 2, C 1, D 1
            monthStages.Add(new MonthStageData("2008_4", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 1, false)
            }));
            
            // 2008_5: Cut 3, C 2
            monthStages.Add(new MonthStageData("2008_5", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 2, false)
            }));
            
            // 2008_6: Cut 8, A 2, B 2, C 1, Editor Pressure (편집자 원고 독촉 시작)
            monthStages.Add(new MonthStageData("2008_6", 8, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2008_7: Cut 4, C 1, Editor Pressure (편집자 원고 독촉 종료)
            monthStages.Add(new MonthStageData("2008_7", 4, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2008_8: Cut 3, D 2
            monthStages.Add(new MonthStageData("2008_8", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 2, false)
            }));
            
            // 2008_9: Cut 5, B 2
            monthStages.Add(new MonthStageData("2008_9", 5, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true)
            }));
            
            // 2008_10: Cut 3, A 2, B 2
            monthStages.Add(new MonthStageData("2008_10", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true)
            }));
            
            // 2008_11: Cut 5, C 2 (조율의 낙서 시작/종료)
            monthStages.Add(new MonthStageData("2008_11", 5, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 2, false)
            }));
            
            // 2008_12: Cut 8, A 2, C 3, Editor Pressure (편집자 원고 독촉 시작)
            monthStages.Add(new MonthStageData("2008_12", 8, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 3, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2009_1 ~ 2019_12: 2007_2 ~ 2008_12 반복 (23개월 패턴, 132개월)
            int repeatStartIndex = 5; // 2007_2가 시작하는 인덱스
            int repeatEndIndex = 27; // 2008_12가 끝나는 인덱스
            int repeatPatternLength = repeatEndIndex - repeatStartIndex + 1; // 23개월 패턴
            
            int totalMonthCount = 0; // 2009_1부터의 월 카운터
            for (int year = 2009; year <= 2019; year++)
            {
                for (int month = 1; month <= 12; month++)
                {
                    // 23개월 패턴을 순환하며 반복
                    int patternIndex = (totalMonthCount % repeatPatternLength) + repeatStartIndex;
                    MonthStageData originalStage = monthStages[patternIndex];
                    
                    // 새로운 MonthStageData 생성 (깊은 복사)
                    List<ObstacleSpawnInfo> copiedObstacles = new List<ObstacleSpawnInfo>();
                    foreach (var obstacle in originalStage.obstacles)
                    {
                        copiedObstacles.Add(new ObstacleSpawnInfo(obstacle.type, obstacle.count, obstacle.randomPosition));
                    }
                    
                    string monthName = $"{year}_{month}";
                    monthStages.Add(new MonthStageData(monthName, originalStage.cutCount, copiedObstacles));
                    
                    // 디버그: EditorPressure가 포함되었는지 확인
                    bool hasEditorPressure = copiedObstacles.Exists(o => o.type == ObstacleType.EditorPressure);
                    if (hasEditorPressure)
                    {
                        Debug.Log($"[Stage Pattern Copy] {monthName}: EditorPressure copied from {originalStage.monthName}");
                    }
                    
                    totalMonthCount++;
                }
            }
            
            // 2020_1: Cut 0, A 1, C 3, Editor Pressure (편집자 원고 독촉 종료)
            monthStages.Add(new MonthStageData("2020_1", 0, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 1, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 3, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2020_2: Cut 0, B 2, C 3
            monthStages.Add(new MonthStageData("2020_2", 0, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 3, false)
            }));
            
            // 2020_3: Cut 3, D 2
            monthStages.Add(new MonthStageData("2020_3", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.SenseisenFoot, 2, false)
            }));
            
            // 2020_4: Cut 2, 없음 (조율의 낙서 시작/종료)
            monthStages.Add(new MonthStageData("2020_4", 2, new List<ObstacleSpawnInfo>()));
            
            // 2020_5: Cut 8, A 2, B 2, C 1, Editor Pressure (편집자 원고 독촉 시작)
            monthStages.Add(new MonthStageData("2020_5", 8, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 2, true),
                new ObstacleSpawnInfo(ObstacleType.SpeechBubbleOverlay, 2, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 1, false),
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2020_6: Cut 2, Editor Pressure (편집자 원고 독촉 종료)
            monthStages.Add(new MonthStageData("2020_6", 2, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.EditorPressure, 1, false)
            }));
            
            // 2020_7: Cut 3, A 1, C 2 (게임 종료/엔딩)
            monthStages.Add(new MonthStageData("2020_7", 3, new List<ObstacleSpawnInfo> {
                new ObstacleSpawnInfo(ObstacleType.ThrowingObjects, 1, true),
                new ObstacleSpawnInfo(ObstacleType.CutLineTape, 2, false)
            }));
            
            // 전체 스테이지 중 EditorPressure가 포함된 스테이지 개수 확인
            int editorPressureCount = 0;
            foreach (var stage in monthStages)
            {
                if (stage.obstacles.Exists(o => o.type == ObstacleType.EditorPressure))
                {
                    editorPressureCount++;
                    Debug.Log($"[Stage Check] {stage.monthName} has EditorPressure");
                }
            }
            
            Debug.Log($"Default stages initialized: {monthStages.Count} stages (2006_9 ~ 2020_7)");
            Debug.Log($"Total stages with EditorPressure: {editorPressureCount}");
        }
    }
    
    private void StartStage(int stageIndex)
    {
        if (stageIndex >= monthStages.Count)
        {
            Debug.Log("All stages completed!");
            return;
        }
        
        currentStageIndex = stageIndex;
        MonthStageData stage = monthStages[stageIndex];
        
        // 스테이지 상태 리셋
        cutsSpawnedInStage = 0;
        obstaclesSpawnedInStage = 0;
        stageTimer = 0f;
        nextSpawnIndex = 0;
        
        // 현재 스테이지의 방해 요소 리스트 설정
        currentStageObstacles = new List<ObstacleSpawnInfo>(stage.obstacles);
        
        // 타임라인 생성 (Cut과 Obstacle을 0.1~0.5초 간격으로 순차 배치)
        GenerateSpawnTimeline(stage);
        
        float totalDuration = spawnTimeline.Count > 0 ? spawnTimeline[spawnTimeline.Count - 1].spawnTime : 0f;
        Debug.Log($"Starting stage {stage.monthName}: {stage.cutCount} cuts, {spawnTimeline.Count} total spawns over ~{totalDuration:F2} seconds");
    }
    
    private void GenerateSpawnTimeline(MonthStageData stage)
    {
        spawnTimeline.Clear();
        
        float currentTime = 0f;
        float minInterval = 1f; // 최소 간격
        float maxInterval = 2f; // 최대 간격
        
        // Cut 스폰 시간 생성 (1~2초 간격으로 랜덤)
        for (int i = 0; i < stage.cutCount; i++)
        {
            currentTime += Random.Range(minInterval, maxInterval);
            spawnTimeline.Add(new SpawnEvent
            {
                spawnTime = currentTime,
                isCut = true
            });
        }
        
        // Obstacle 스폰 시간 생성 (1~2초 간격으로 랜덤)
        foreach (var obstacleInfo in stage.obstacles)
        {
            for (int i = 0; i < obstacleInfo.count; i++)
            {
                currentTime += Random.Range(minInterval, maxInterval);
                spawnTimeline.Add(new SpawnEvent
                {
                    spawnTime = currentTime,
                    isCut = false,
                    obstacleType = obstacleInfo.type,
                    randomPosition = obstacleInfo.randomPosition
                });
            }
        }
        
        // 시간 순으로 정렬
        spawnTimeline.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        
        float totalDuration = spawnTimeline.Count > 0 ? spawnTimeline[spawnTimeline.Count - 1].spawnTime : 0f;
        Debug.Log($"Generated timeline with {spawnTimeline.Count} events over {totalDuration:F2}s");
        
        // 디버그: 타임라인 출력
        foreach (var evt in spawnTimeline)
        {
            string type = evt.isCut ? "Cut" : evt.obstacleType.ToString();
            Debug.Log($"  {evt.spawnTime:F2}s: {type}");
        }
    }
    
    private void InitializePool()
    {
        // 오브젝트 풀 초기화
        for (int i = 0; i < poolSize; i++)
        {
            if (cutPrefabs != null && cutPrefabs.Length > 0)
            {
                GameObject prefab = cutPrefabs[Random.Range(0, cutPrefabs.Length)];
                GameObject pooledCut = Instantiate(prefab, spawnContainer);
                pooledCut.SetActive(false);
                cutPool.Enqueue(pooledCut);
                allCuts.Add(pooledCut);
            }
        }
        
        Debug.Log($"Object pool initialized with {poolSize} cuts");
    }
    
    private void SpawnCut()
    {
        if (cutPool.Count == 0)
        {
            Debug.LogWarning("Cut pool is empty! Cannot spawn cut.");
            return;
        }
        
        // 풀에서 컷 가져오기
        GameObject pooledCut = cutPool.Dequeue();
        
        // 컷 활성화
        pooledCut.SetActive(true);
        
        // Cut 컴포넌트 설정
        Cut cutScript = pooledCut.GetComponent<Cut>();
        if (cutScript != null)
        {
            // 컷 상태 초기화
            cutScript.ResetCutState();
            
            float cutSpeed = baseCutSpeed * currentSpeedMultiplier; // 기본 속도에 배수 적용
            
            // X축 랜덤 위치 계산 후 Initialize에 전달
            float randomX = CalculateRandomX();
            
            // 테이프 플래그 사용 (ObstacleManager에서 설정)
            bool hasTape = nextCutHasTape;
            
            cutScript.Initialize(hasTape, cutSpeed, randomX, OnCutSuccess, OnCutMiss);
            
            // 컷이 사라질 때 스폰어에게 알림을 받도록 설정
            cutScript.SetSpawnerReference(this);
        }
        
        cutsSpawnedInStage++;
        
        // 활성 컷 리스트에 추가
        activeCuts.Add(pooledCut);
        
        // 테이프 플래그 리셋
        nextCutHasTape = false;
        
        Debug.Log($"Cut spawned at time {stageTimer:F2}. Total: {cutsSpawnedInStage}. Pool size: {cutPool.Count}");
    }
    
    private void SpawnObstacleByType(ObstacleType type, bool randomPosition = false)
    {
        if (obstacleManager == null)
        {
            Debug.LogWarning("ObstacleManager reference is missing!");
            return;
        }
        
        // 위치 계산 (랜덤 or 고정)
        Vector2 spawnPosition;
        if (randomPosition)
        {
            // 랜덤 위치
            float randomX = Random.Range(-300f, 300f);
            float randomY = Random.Range(-200f, 200f);
            spawnPosition = new Vector2(randomX, randomY);
        }
        else
        {
            // 고정 위치 (화면 중앙 또는 특정 위치)
            spawnPosition = Vector2.zero;
        }
        
        // ObstacleManager를 통해 스폰 (테이프일 경우 자동으로 nextCutHasTape 플래그 설정됨)
        obstacleManager.SpawnObstacleFromExternal(type, spawnPosition);
        
        Debug.Log($"Obstacle spawned: {type} at time {stageTimer:F2}, Position: {spawnPosition}, Random: {randomPosition}");
    }
    
    
    // ObstacleManager에서 테이프를 스폰할 때 호출
    public void SetNextCutHasTape()
    {
        nextCutHasTape = true;
        Debug.Log("Next cut will have tape (double-click required)");
    }
    
    // 편집자 원고 독촉 이벤트 토글
    public void ToggleEditorPressure()
    {
        isEditorPressureActive = !isEditorPressureActive;
        
        if (isEditorPressureActive)
        {
            Debug.Log($"[Editor Pressure] STARTED! Speed cap increased to {editorPressureSpeedCap}x");
        }
        else
        {
            Debug.Log($"[Editor Pressure] ENDED! Speed cap returned to {normalSpeedCap}x");
            // 현재 속도가 일반 상한선을 초과하면 서서히 감소하도록 조정
            if (currentSpeedMultiplier > normalSpeedCap)
            {
                currentSpeedMultiplier = normalSpeedCap;
            }
        }
    }
    
    // 편집자 원고 독촉 활성 상태 확인
    public bool IsEditorPressureActive()
    {
        return isEditorPressureActive;
    }
    
    private Vector3 CalculateSpawnPosition()
    {
        float xPosition = 0;
        float yPosition = spawnYOffset;
        
        return new Vector3(xPosition, yPosition, 0);
    }
    
    private float CalculateRandomX()
    {
        // 화면 너비 내에서 랜덤 X 위치 계산 (여백 고려)
        float screenHalfWidth = Screen.width / 2f;
        float minX = -screenHalfWidth + screenWidthPadding;
        float maxX = screenHalfWidth - screenWidthPadding;
        
        return Random.Range(minX, maxX);
    }
    
    // 컷이 사라질 때 호출되는 메서드 (화면 밖으로 나갈 때만)
    public void OnCutDestroyed(GameObject destroyedCut)
    {
        // 활성 컷 리스트에서 제거
        if (activeCuts.Contains(destroyedCut))
        {
            activeCuts.Remove(destroyedCut);
        }
        
        // 풀로 반환
            ReturnCutToPool(destroyedCut);
        Debug.Log($"Cut destroyed and returned to pool. Active cuts: {activeCuts.Count}");
    }
    
    private void ReturnCutToPool(GameObject cut)
    {
        if (cut != null)
        {
            // 컷 비활성화
            cut.SetActive(false);
            
            // 풀에 다시 추가
            cutPool.Enqueue(cut);
            
            Debug.Log($"Cut returned to pool. Pool size: {cutPool.Count}");
        }
    }
    
    private void IncreaseDifficulty()
    {
        // 스폰 간격 감소 (더 빠른 스폰)
        currentSpawnInterval = Mathf.Max(maxSpawnInterval, currentSpawnInterval - difficultyIncreaseRate * Time.deltaTime);
        
        // 속도 증가 (상한선 적용)
        float speedCap = isEditorPressureActive ? editorPressureSpeedCap : normalSpeedCap;
        currentSpeedMultiplier = Mathf.Min(speedCap, currentSpeedMultiplier + speedIncreaseRate * Time.deltaTime);
    }
    
    private void OnCutSuccess()
    {
        Debug.Log("Cut success!");
        
        // GameManager에 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCutSuccessCallback();
        }
        
        // SpawnCutsCoroutine에서 자동으로 다음 컷을 스폰하므로 여기서는 알림만
    }
    
    private void OnCutMiss()
    {
        Debug.Log("Cut miss!");
        
        // GameManager에 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCutMissCallback();
        }
        
        // SpawnCutsCoroutine에서 자동으로 다음 컷을 스폰하므로 여기서는 알림만
    }
    
    public void ResetDifficulty()
    {
        currentSpawnInterval = spawnInterval;
        currentSpeedMultiplier = 1f;
    }
    
    public void ResetStage()
    {
        // 스테이지 리셋
        currentStageIndex = 0;
        cutsSpawnedInStage = 0;
        obstaclesSpawnedInStage = 0;
        currentObstacleIndex = 0;
        stageTimer = 0f;
        nextSpawnIndex = 0;
        
        // 타임라인 클리어
        spawnTimeline.Clear();
        
        // 현재 컷 제거
        ClearAllCuts();
        
        // 난이도 리셋
        ResetDifficulty();
        
        // ObstacleManager 리셋
        if (obstacleManager != null)
        {
            obstacleManager.ClearAllObstacles();
        }
        
        // 첫 번째 스테이지 시작
        if (monthStages.Count > 0)
        {
            StartStage(0);
        }
        
        Debug.Log("CutSpawner reset to first stage");
    }
    
    public void ClearAllCuts()
    {
        // 모든 활성 컷 제거
        foreach (GameObject cut in activeCuts)
        {
            if (cut != null)
            {
                ReturnCutToPool(cut);
            }
        }
        activeCuts.Clear();
    }
    
    public void GoToNextStage()
    {
        // 다음 스테이지로 이동
        currentStageIndex++;
        if (currentStageIndex < monthStages.Count)
        {
            StartStage(currentStageIndex);
        }
        else
        {
            Debug.Log("All stages completed!");
        }
    }
    
    public MonthStageData GetCurrentStage()
    {
        if (currentStageIndex >= 0 && currentStageIndex < monthStages.Count)
        {
            return monthStages[currentStageIndex];
        }
        return null;
    }
    
    public int GetActiveCutCount()
    {
        return activeCuts.Count;
    }
    
    public int GetPoolSize()
    {
        return cutPool.Count;
    }
    
    public float GetCurrentDifficulty()
    {
        return currentSpeedMultiplier;
    }
    
    private void OnGameEnding()
    {
        // 모든 활성 컷과 방해 요소 제거
        ClearAllCuts();
        
        if (obstacleManager != null)
        {
            obstacleManager.ClearAllObstacles();
        }
        
        // GameManager에 게임 클리어 알림 (엔딩 패널 표시)
        if (GameManager.Instance != null)
        {
            // GameManager의 GameCleared 메서드가 이미 AddMonth에서 호출되었을 것이므로
            // 여기서는 추가 처리가 필요 없음
        }
        
        Debug.Log("Game Ending: All cuts and obstacles cleared");
    }
    
    private void OnDestroy()
    {
        ClearAllCuts();
    }
}

// 월별 스테이지 데이터
[System.Serializable]
public class MonthStageData
{
    public string monthName; // 예: "2006_9", "2006_10"
    public int cutCount; // 절취선(기본) 발생 횟수
    public List<ObstacleSpawnInfo> obstacles; // 방해 요소 리스트
    
    public MonthStageData(string month, int cuts, List<ObstacleSpawnInfo> obstacleList)
    {
        monthName = month;
        cutCount = cuts;
        obstacles = obstacleList;
    }
}

// 방해 요소 스폰 정보
[System.Serializable]
public class ObstacleSpawnInfo
{
    public ObstacleType type; // 방해 요소 타입
    public int count; // 소환 횟수
    public bool randomPosition; // true: 랜덤 위치, false: 고정 위치
    public int spawnedCount; // 실제 소환된 횟수 (런타임)
    
    public ObstacleSpawnInfo(ObstacleType obstacleType, int spawnCount, bool isRandomPosition)
    {
        type = obstacleType;
        count = spawnCount;
        randomPosition = isRandomPosition;
        spawnedCount = 0;
    }
}

// 타임라인 스폰 이벤트
[System.Serializable]
public class SpawnEvent
{
    public float spawnTime; // 스폰 시간 (초)
    public bool isCut; // true: Cut, false: Obstacle
    public ObstacleType obstacleType; // isCut이 false일 때 사용
    public bool randomPosition; // 랜덤 위치 여부
}
