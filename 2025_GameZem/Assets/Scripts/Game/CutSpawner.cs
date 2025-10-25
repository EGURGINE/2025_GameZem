using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CutSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public GameObject[] cutPrefabs; // 다양한 컷 프리팹들
    public Transform spawnContainer;
    public Transform cutLine; // 컷 라인 (GameManager에 제공할 용도)
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
    
    [Header("Sequence System")]
    public List<SequenceData> sequenceData = new List<SequenceData>();
    public int currentSequenceIndex = 0;
    public int currentMonth = 5; // 순서 5 기준으로 1달이 지남
    public int targetMonth = 7; // 2020_7월까지
    
    [Header("Obstacle Manager Reference")]
    public ObstacleManager obstacleManager;
    
    private float currentSpawnInterval;
    private float currentSpeedMultiplier = 1f;
    
    // 오브젝트 풀
    private Queue<GameObject> cutPool = new Queue<GameObject>();
    private List<GameObject> allCuts = new List<GameObject>(); // 모든 생성된 컷들
    private List<GameObject> activeCuts = new List<GameObject>(); // 현재 활성화된 컷들
    
    // 시퀀스 진행 상황
    private int cutsSpawnedInSequence = 0;
    private int obstaclesSpawnedInSequence = 0;
    private int currentSequenceStep = 0;
    
    // 테이프 이벤트 플래그
    private bool nextCutHasTape = false;
    
    // 편집자 원고 독촉 이벤트
    private bool isEditorPressureActive = false;
    
    // CutSpawner 자체 게임 상태 관리 (씬 전환 시 안정성을 위해)
    private bool isGameActive = true;
    private bool isGameCleared = false;
    
    // 타임라인 기반 스폰 시스템
    [Header("Timeline Settings")]
    public float monthDuration = 5f; // 한 달 지속 시간 (초)
    private float stageTimer = 0f;
    private List<SpawnEvent> spawnTimeline = new List<SpawnEvent>();
    private int nextSpawnIndex = 0;
    
    private void Start()
    {
        // 게임 상태 초기화 (씬 전환 시 안정성을 위해)
        isGameActive = true;
        isGameCleared = false;
        
        // GameManager가 있으면 상태 동기화
        if (GameManager.Instance != null)
        {
            Debug.Log("[CutSpawner] GameManager와 상태 동기화 중...");
            // GameManager의 상태를 CutSpawner에 반영
            isGameActive = GameManager.Instance.IsGameActive();
            isGameCleared = GameManager.Instance.IsGameCleared();
            Debug.Log($"[CutSpawner] 동기화 완료 - isGameActive: {isGameActive}, isGameCleared: {isGameCleared}");
        }
        
        // Time.timeScale도 확실히 1로 설정
        Time.timeScale = 1f;
        
        currentSpawnInterval = spawnInterval;
        InitializePool();
        
        // sequenceData가 비어있으면 기본 시퀀스 데이터 초기화
        if (sequenceData.Count == 0)
        {
            InitializeSequenceData();
        }
        
        // CutLine 초기화 (Inspector에서 설정되지 않았으면 자동으로 찾기)
        if (cutLine == null)
        {
            cutLine = GetCutLine();
            if (cutLine != null)
            {
                Debug.Log($"[CutSpawner] CutLine을 자동으로 초기화했습니다: {cutLine.name}");
            }
            else
            {
                Debug.LogWarning("[CutSpawner] CutLine을 찾을 수 없습니다. Inspector에서 수동으로 설정해주세요.");
            }
        }
        else
        {
            Debug.Log($"[CutSpawner] CutLine이 이미 설정되어 있습니다: {cutLine.name}");
        }
        
        StartSequence();
    }
    
    private void Update()
    {
        // 게임이 비활성화 상태이거나 클리어된 상태면 대기
        if (!isGameActive || isGameCleared) return;
        if (currentMonth > targetMonth) return;
        
        stageTimer += Time.deltaTime;
        
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
        
        // 시퀀스 완료 체크 (모든 이벤트 소환 완료)
        if (nextSpawnIndex >= spawnTimeline.Count && spawnTimeline.Count > 0)
        {
            Debug.Log($"Sequence {currentSequenceIndex + 1} completed! All {spawnTimeline.Count} events spawned.");
            
            // GameManager에 한 달 증가 알림
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddMonth();
            }
            
            currentMonth++;
            
            if (currentMonth <= targetMonth)
            {
                StartSequence();
            }
            else
            {
                // 모든 시퀀스 완료 (엔딩)
                Debug.Log("All sequences completed! Game Ending!");
                OnGameEnding();
            }
        }
    }
    
    private void HandleCutTouchInput()
    {
        // 터치 또는 마우스 클릭 감지
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            // UI 요소를 터치했는지 확인
            if (IsUITouched())
            {
                Debug.Log("[CutSpawner] UI 터치 감지 - Cut 터치 처리 건너뜀");
                return;
            }
            // CutLine과 가장 가까운 Cut 찾기
            Cut closestCut = null;
            float closestDistance = float.MaxValue;
            Debug.Log($"[CutSpawner] activeCuts.Count: {activeCuts.Count}");
            
            foreach (GameObject cutObj in activeCuts)
            {
                if (cutObj == null || !cutObj.activeInHierarchy) 
                {
                    Debug.Log($"[CutSpawner] Cut 제외 - null: {cutObj == null}, activeInHierarchy: {cutObj?.activeInHierarchy}");
                    continue;
                }
                
                Cut cutScript = cutObj.GetComponent<Cut>();
                if (cutScript != null)
                {
                    // 터치 대기 상태이고 아직 처리되지 않은 Cut만 고려
                    if (cutScript.IsWaitingForTouch() && !cutScript.HasPassedCutLine())
                    {
                        // CutLine과의 거리 계산
                        float distance = cutScript.GetDistanceToCutLine();
                        Debug.Log($"[CutSpawner] Cut 거리: {distance:F1}");
                        
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestCut = cutScript;
                            Debug.Log($"[CutSpawner] 가장 가까운 Cut 업데이트 - 거리: {distance:F1}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[CutSpawner] Cut 제외 - 터치 대기 상태 아님 또는 이미 처리됨");
                    }
                }
                else
                {
                    Debug.Log("[CutSpawner] Cut 컴포넌트 없음");
                }
            }

            Debug.Log($"closestCut: {(closestCut != null ? "찾음" : "없음")}, 거리: {closestDistance:F1}");
            
            // 가장 가까운 Cut이 없으면 리턴
            if (closestCut == null) 
            {
                Debug.Log("[CutSpawner] 터치 가능한 Cut이 없음");
                return;
            }
            
            // 가장 가까운 Cut만 터치 처리
            Debug.Log($"[CutSpawner] 가장 가까운 Cut 터치 처리: {closestCut.name}");
            closestCut.TryProcessClick();
        }
    }
    
    private void InitializeSequenceData()
    {
        // 스프레드시트 데이터에 맞춰 시퀀스 초기화 (순서 1-30)
        if (sequenceData.Count == 0)
        {
            // 스프레드시트 데이터를 순서대로 입력
            sequenceData.Add(new SequenceData(1, SpawnType.Cut, null)); // 순서 1: Cut
            sequenceData.Add(new SequenceData(2, SpawnType.Cut, null)); // 순서 2: Cut
            sequenceData.Add(new SequenceData(3, SpawnType.Cut, null)); // 순서 3: Cut
            sequenceData.Add(new SequenceData(4, SpawnType.ThrowingObjects, null)); // 순서 4: ThrowingObjects
            sequenceData.Add(new SequenceData(5, SpawnType.Cut, null)); // 순서 5: Cut
            sequenceData.Add(new SequenceData(6, SpawnType.Cut, SpawnType.SpeechBubbleOverlay)); // 순서 6: Cut + SpeechBubbleOverlay
            sequenceData.Add(new SequenceData(7, SpawnType.Cut, null)); // 순서 7: Cut
            sequenceData.Add(new SequenceData(8, SpawnType.CutLineTape, null)); // 순서 8: CutLineTape
            sequenceData.Add(new SequenceData(9, SpawnType.Cut, null)); // 순서 9: Cut
            sequenceData.Add(new SequenceData(10, SpawnType.CutLineTape, null)); // 순서 10: CutLineTape
            sequenceData.Add(new SequenceData(11, SpawnType.Cut, null)); // 순서 11: Cut
            sequenceData.Add(new SequenceData(12, SpawnType.ThrowingObjects, null)); // 순서 12: ThrowingObjects
            sequenceData.Add(new SequenceData(13, SpawnType.Cut, null)); // 순서 13: Cut
            sequenceData.Add(new SequenceData(14, SpawnType.Cut, null)); // 순서 14: Cut
            sequenceData.Add(new SequenceData(15, SpawnType.SenseisenFoot, null)); // 순서 15: SenseisenFoot
            sequenceData.Add(new SequenceData(16, SpawnType.Cut, null)); // 순서 16: Cut
            sequenceData.Add(new SequenceData(17, SpawnType.ThrowingObjects, null)); // 순서 17: ThrowingObjects
            sequenceData.Add(new SequenceData(18, SpawnType.Cut, null)); // 순서 18: Cut
            sequenceData.Add(new SequenceData(19, SpawnType.Cut, SpawnType.SpeechBubbleOverlay)); // 순서 19: Cut + SpeechBubbleOverlay
            sequenceData.Add(new SequenceData(20, SpawnType.Cut, null)); // 순서 20: Cut
            sequenceData.Add(new SequenceData(21, SpawnType.CutLineTape, null)); // 순서 21: CutLineTape
            sequenceData.Add(new SequenceData(22, SpawnType.Cut, null)); // 순서 22: Cut
            sequenceData.Add(new SequenceData(23, SpawnType.JoulDoodle, null)); // 순서 23: JoulDoodle
            sequenceData.Add(new SequenceData(24, SpawnType.Cut, null)); // 순서 24: Cut
            sequenceData.Add(new SequenceData(25, SpawnType.CutLineTape, null)); // 순서 25: CutLineTape
            sequenceData.Add(new SequenceData(26, SpawnType.Cut, SpawnType.EditorPressure)); // 순서 26: Cut + EditorPressure
            sequenceData.Add(new SequenceData(27, SpawnType.Cut, null)); // 순서 27: Cut
            sequenceData.Add(new SequenceData(28, SpawnType.Cut, null)); // 순서 28: Cut
            sequenceData.Add(new SequenceData(29, SpawnType.CutLineTape, null)); // 순서 29: CutLineTape
            sequenceData.Add(new SequenceData(30, SpawnType.Cut, SpawnType.EditorPressure)); // 순서 30: Cut + EditorPressure
            
            Debug.Log($"Sequence data initialized: {sequenceData.Count} steps (1-30)");
        }
    }
    
    private void StartSequence()
    {
        if (currentMonth > targetMonth)
        {
            Debug.Log("All sequences completed!");
            return;
        }
        
        // 시퀀스 상태 리셋
        cutsSpawnedInSequence = 0;
        obstaclesSpawnedInSequence = 0;
        currentSequenceStep = 0;
        stageTimer = 0f;
        nextSpawnIndex = 0;
        
        // 타임라인 생성 (30개 순서를 1~2초 간격으로 배치)
        GenerateSequenceTimeline();
        
        float totalDuration = spawnTimeline.Count > 0 ? spawnTimeline[spawnTimeline.Count - 1].spawnTime : 0f;
        Debug.Log($"Starting sequence for month {currentMonth}: {spawnTimeline.Count} total spawns over ~{totalDuration:F2} seconds");
    }
    
    private void GenerateSequenceTimeline()
    {
        spawnTimeline.Clear();
        
        float currentTime = 0f;
        float minInterval = 1f; // 최소 간격
        float maxInterval = 2f; // 최대 간격
        
        // 30개 순서를 1~2초 간격으로 배치
        for (int i = 0; i < sequenceData.Count; i++)
        {
            SequenceData step = sequenceData[i];
            currentTime += Random.Range(minInterval, maxInterval);
            
            // 메인 타입 스폰
            if (step.mainType == SpawnType.Cut)
            {
                spawnTimeline.Add(new SpawnEvent
                {
                    spawnTime = currentTime,
                    isCut = true
                });
            }
            else
            {
                spawnTimeline.Add(new SpawnEvent
                {
                    spawnTime = currentTime,
                    isCut = false,
                    obstacleType = ConvertSpawnTypeToObstacleType(step.mainType),
                    randomPosition = true
                });
            }
            
            // 함께 나오는 타입이 있으면 같은 시간에 스폰
            if (step.togetherType != null)
            {
                spawnTimeline.Add(new SpawnEvent
                {
                    spawnTime = currentTime,
                    isCut = false,
                    obstacleType = ConvertSpawnTypeToObstacleType(step.togetherType.Value),
                    randomPosition = true
                });
            }
        }
        
        // 시간 순으로 정렬
        spawnTimeline.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        
        float totalDuration = spawnTimeline.Count > 0 ? spawnTimeline[spawnTimeline.Count - 1].spawnTime : 0f;
        Debug.Log($"Generated sequence timeline with {spawnTimeline.Count} events over {totalDuration:F2}s");
        
        // 디버그: 타임라인 출력
        foreach (var evt in spawnTimeline)
        {
            string type = evt.isCut ? "Cut" : evt.obstacleType.ToString();
            Debug.Log($"  {evt.spawnTime:F2}s: {type}");
        }
    }
    
    private ObstacleType ConvertSpawnTypeToObstacleType(SpawnType spawnType)
    {
        switch (spawnType)
        {
            case SpawnType.ThrowingObjects:
                return ObstacleType.ThrowingObjects;
            case SpawnType.CutLineTape:
                return ObstacleType.CutLineTape;
            case SpawnType.SpeechBubbleOverlay:
                return ObstacleType.SpeechBubbleOverlay;
            case SpawnType.SenseisenFoot:
                return ObstacleType.SenseisenFoot;
            case SpawnType.JoulDoodle:
                return ObstacleType.JoulDoodle;
            case SpawnType.EditorPressure:
                return ObstacleType.EditorPressure;
            default:
                return ObstacleType.ThrowingObjects;
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
        
        cutsSpawnedInSequence++;
        
        // 활성 컷 리스트에 추가
        activeCuts.Add(pooledCut);
        
        // 테이프 플래그 리셋
        nextCutHasTape = false;
        
        Debug.Log($"Cut spawned at time {stageTimer:F2}. Total: {cutsSpawnedInSequence}. Pool size: {cutPool.Count}");
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
        
        obstaclesSpawnedInSequence++;
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
        
        // 게임이 비활성화 상태면 처리하지 않음
        if (!isGameActive || isGameCleared)
        {
            Debug.Log("[CutSpawner] 게임이 비활성화 상태라서 OnCutSuccess 처리 건너뜀");
            return;
        }
        
        // GameManager를 안전하게 가져오기
        GameManager gameManager = null;
        try
        {
            gameManager = GameManager.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CutSpawner] GameManager.Instance 접근 중 오류: {e.Message}. 직접 찾기를 시도합니다.");
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager == null)
        {
            // GameManager.Instance가 null이면 직접 찾기
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager != null)
        {
            try
            {
                gameManager.OnCutSuccessCallback();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CutSpawner] GameManager.OnCutSuccessCallback() 호출 실패: {e.Message}");
            }
        }
        
        // SpawnCutsCoroutine에서 자동으로 다음 컷을 스폰하므로 여기서는 알림만
    }
    
    private void OnCutMiss()
    {
        Debug.Log("[CutSpawner] OnCutMiss 호출됨");
        
        // 게임이 비활성화 상태면 처리하지 않음
        if (!isGameActive || isGameCleared)
        {
            Debug.Log("[CutSpawner] 게임이 비활성화 상태라서 OnCutMiss 처리 건너뜀");
            return;
        }
        
        // GameManager를 안전하게 가져오기
        GameManager gameManager = null;
        try
        {
            gameManager = GameManager.Instance;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CutSpawner] GameManager.Instance 접근 중 오류: {e.Message}. 직접 찾기를 시도합니다.");
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager == null)
        {
            // GameManager.Instance가 null이면 직접 찾기
            gameManager = FindFirstObjectByType<GameManager>();
            Debug.Log($"[CutSpawner] GameManager.Instance가 null이어서 직접 찾기: {(gameManager != null ? "찾음" : "없음")}");
        }
        
        if (gameManager != null)
        {


            
            Debug.Log("[CutSpawner] GameManager 존재함 - OnCutMissCallback() 호출");
            try
            {
                gameManager.OnCutMissCallback();
                Debug.Log("[CutSpawner] GameManager.OnCutMissCallback() 호출 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CutSpawner] GameManager.OnCutMissCallback() 호출 실패: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[CutSpawner] GameManager를 찾을 수 없습니다!");
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
        // 시퀀스 리셋
        currentSequenceIndex = 0;
        currentMonth = 5; // 순서 5 기준으로 1달이 지남
        cutsSpawnedInSequence = 0;
        obstaclesSpawnedInSequence = 0;
        currentSequenceStep = 0;
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
        
        // 첫 번째 시퀀스 시작
        StartSequence();
        
        Debug.Log("CutSpawner reset to first sequence");
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
    
    public void GoToNextSequence()
    {
        // 다음 시퀀스로 이동
        currentMonth++;
        if (currentMonth <= targetMonth)
        {
            StartSequence();
        }
        else
        {
            Debug.Log("All sequences completed!");
        }
    }
    
    public SequenceData GetCurrentSequence()
    {
        if (currentSequenceIndex >= 0 && currentSequenceIndex < sequenceData.Count)
        {
            return sequenceData[currentSequenceIndex];
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
    
    /// <summary>
    /// 게임 상태 제어 메서드들 (씬 전환 시 안정성을 위해)
    /// </summary>
    
    /// <summary>
    /// 게임을 일시정지/재개
    /// </summary>
    public void SetGameActive(bool active)
    {
        isGameActive = active;
        Debug.Log($"[CutSpawner] 게임 상태 변경: {(active ? "활성화" : "비활성화")}");
    }
    
    /// <summary>
    /// 게임 클리어 상태 설정
    /// </summary>
    public void SetGameCleared(bool cleared)
    {
        isGameCleared = cleared;
        if (cleared)
        {
            isGameActive = false;
            Debug.Log("[CutSpawner] 게임 클리어됨");
        }
    }
    
    /// <summary>
    /// 게임 상태 초기화
    /// </summary>
    public void ResetGameState()
    {
        isGameActive = true;
        isGameCleared = false;
        
        // Time.timeScale도 확실히 1로 설정
        Time.timeScale = 1f;
        
        // 스테이지 타이머와 인덱스 초기화
        stageTimer = 0f;
        nextSpawnIndex = 0;
        
        Debug.Log("[CutSpawner] 게임 상태 초기화됨");
    }
    
    /// <summary>
    /// GameManager와 상태 동기화 (필요한 시점에만 호출)
    /// </summary>
    public void SyncWithGameManager()
    {
        if (GameManager.Instance != null)
        {
            isGameActive = GameManager.Instance.IsGameActive();
            isGameCleared = GameManager.Instance.IsGameCleared();
            Debug.Log($"[CutSpawner] GameManager와 상태 동기화 - isGameActive: {isGameActive}, isGameCleared: {isGameCleared}");
        }
    }
    
    /// <summary>
    /// 현재 게임이 활성화 상태인지 확인
    /// </summary>
    public bool IsGameActive()
    {
        return isGameActive && !isGameCleared;
    }
    
    private void OnDestroy()
    {
        // 게임 상태를 비활성화로 설정
        isGameActive = false;
        isGameCleared = true;
        
        // 모든 컷 정리
        ClearAllCuts();
        
        Debug.Log("[CutSpawner] OnDestroy - 게임 상태 정리 완료");
    }
    
    /// <summary>
    /// CutLine을 자동으로 찾는 메서드 (CutSpawner 자체 초기화용)
    /// </summary>
    private Transform GetCutLine()
    {
        // CutSpawner의 cutLine이 설정되어 있으면 그것을 반환
        if (cutLine != null)
        {
            return cutLine;
        }
        
        // cutLine이 null이면 spawnContainer에서 찾기
        if (spawnContainer != null)
        {
            // CutLine을 찾는 로직
            Transform foundCutLine = spawnContainer.Find("CutLine");
            if (foundCutLine == null)
            {
                foundCutLine = spawnContainer.Find("cutLine");
            }
            if (foundCutLine == null)
            {
                // CutLine 컴포넌트를 가진 자식 찾기
                Transform[] children = spawnContainer.GetComponentsInChildren<Transform>();
                foreach (Transform child in children)
                {
                    if (child != spawnContainer && child.name.ToLower().Contains("line"))
                    {
                        foundCutLine = child;
                        break;
                    }
                }
            }
            
            // 찾은 cutLine을 CutSpawner의 cutLine에 저장
            if (foundCutLine != null)
            {
                cutLine = foundCutLine;
                Debug.Log($"[CutSpawner] CutLine을 자동으로 찾아서 설정했습니다: {cutLine.name}");
            }
            
            return foundCutLine;
        }
        return null;
    }
    
    /// <summary>
    /// 특정 UI 버튼이 터치되었는지 확인
    /// </summary>
    private bool IsUITouched()
    {
        // EventSystem을 사용하여 UI 터치 감지
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.PointerEventData pointerData;
            
            // 마우스/터치 위치에서 UI 요소 확인
            if (Input.touchCount > 0)
            {
                // 터치 입력
                pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                pointerData.position = Input.GetTouch(0).position;
            }
            else
            {
                // 마우스 입력
                pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                pointerData.position = Input.mousePosition;
            }
            
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
            
            // 특정 버튼들만 확인
            foreach (var result in results)
            {
                if (result.gameObject != null)
                {
                    // 버튼 컴포넌트 확인
                    Button button = result.gameObject.GetComponent<Button>();
                    if (button != null)
                    {
                        // 특정 태그를 가진 버튼들만 감지
                        string buttonTag = result.gameObject.tag.ToLower();
                        
                        // UI 버튼 태그 확인
                        if (buttonTag == "ui_button" || buttonTag == "game_button")
                        {
                            Debug.Log($"[CutSpawner] UI 버튼 터치 감지: {result.gameObject.name} (태그: {buttonTag})");
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }
}

// 시퀀스 데이터 (스프레드시트의 순서 1-30)
[System.Serializable]
public class SequenceData
{
    public int order; // 순서 (1-30)
    public SpawnType mainType; // 나오는 타입 (K열)
    public SpawnType? togetherType; // 같이 나오는 타입 (L열, null이면 없음)
    
    public SequenceData(int orderNumber, SpawnType main, SpawnType? together)
    {
        order = orderNumber;
        mainType = main;
        togetherType = together;
    }
}

// 스폰 타입 열거형
public enum SpawnType
{
    Cut = 0,
    ThrowingObjects = 1,
    CutLineTape = 2,
    SpeechBubbleOverlay = 3,
    SenseisenFoot = 4,
    JoulDoodle = 5,
    EditorPressure = 6
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
