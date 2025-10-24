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
    
    [Header("Difficulty Settings")]
    public float difficultyIncreaseRate = 0.1f; // 난이도 증가율
    public float maxSpawnInterval = 0.5f; // 최소 스폰 간격
    public float speedIncreaseRate = 0.05f; // 속도 증가율
    
    [Header("Object Pooling")]
    public int poolSize = 10; // 풀 크기
    
    private GameObject currentCut = null; // 현재 활성 컷
    private Coroutine spawnCoroutine;
    private float currentSpawnInterval;
    private float currentSpeedMultiplier = 1f;
    private bool isWaitingForNextSpawn = false;
    
    // 오브젝트 풀
    private Queue<GameObject> cutPool = new Queue<GameObject>();
    private List<GameObject> allCuts = new List<GameObject>(); // 모든 생성된 컷들
    
    private void Start()
    {
        currentSpawnInterval = spawnInterval;
        InitializePool();
        StartSpawning();
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
    
    private void StartSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
        
        spawnCoroutine = StartCoroutine(SpawnCutsCoroutine());
    }
    
    private IEnumerator SpawnCutsCoroutine()
    {
        while (true)
        {
            // 현재 컷이 없고 다음 스폰을 기다리는 상태일 때만 새 컷 생성
            if (currentCut == null && !isWaitingForNextSpawn)
            {
                SpawnCut();
                isWaitingForNextSpawn = true;
            }
            
            // 현재 컷이 사라졌는지 확인
            if (currentCut == null && isWaitingForNextSpawn)
            {
                // 스폰 간격 계산 (변화량 포함)
                float randomVariation = Random.Range(-spawnIntervalVariation, spawnIntervalVariation);
                float waitTime = Mathf.Max(maxSpawnInterval, currentSpawnInterval + randomVariation);
                
                yield return new WaitForSeconds(waitTime);
                
                // 난이도 증가
                IncreaseDifficulty();
                
                isWaitingForNextSpawn = false;
            }
            else
            {
                yield return new WaitForSeconds(0.1f); // 짧은 간격으로 확인
            }
        }
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
        
        // 스폰 위치 계산
        Vector3 spawnPosition = CalculateSpawnPosition();
        
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
            cutScript.Initialize(cutSpeed, randomX, OnCutSuccess, OnCutMiss);
            
            // 컷이 사라질 때 스폰어에게 알림을 받도록 설정
            cutScript.SetSpawnerReference(this);
        }
        
        // 현재 컷으로 설정
        currentCut = pooledCut;
        
        Debug.Log($"Cut spawned from pool at {spawnPosition}. Pool size: {cutPool.Count}");
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
    
    private void CleanupDestroyedCuts()
    {
        // 현재 컷이 파괴되었는지 확인
        if (currentCut == null)
        {
            Debug.Log("Current cut has been destroyed");
        }
    }
    
    // 컷이 사라질 때 호출되는 메서드 (화면 밖으로 나갈 때만)
    public void OnCutDestroyed(GameObject destroyedCut)
    {
        // 현재 활성 컷이 아닌 경우에만 풀로 반환
        if (destroyedCut != currentCut)
        {
            // 이전 컷이 화면 밖으로 나간 경우
            ReturnCutToPool(destroyedCut);
            Debug.Log("Old cut destroyed and returned to pool");
        }
        else
        {
            // 현재 컷이 화면 밖으로 나간 경우 (정상적인 경우)
            ReturnCutToPool(destroyedCut);
            currentCut = null;
            Debug.Log("Current cut destroyed and returned to pool");
        }
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
        
        // 속도 증가
        currentSpeedMultiplier += speedIncreaseRate * Time.deltaTime;
        
        Debug.Log($"Difficulty increased. Spawn interval: {currentSpawnInterval:F2}, Speed multiplier: {currentSpeedMultiplier:F2}");
    }
    
    private void OnCutSuccess()
    {
        Debug.Log("Cut success!");
        
        // GameManager에 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCutSuccessCallback();
        }
        
        // 게임이 활성화된 경우에만 다음 컷 스폰
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive())
        {
            StartCoroutine(SpawnCutWithDelay());
        }
    }
    
    private void OnCutMiss()
    {
        Debug.Log("Cut miss!");
        
        // GameManager에 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCutMissCallback();
        }
        
        // 게임이 활성화된 경우에만 다음 컷 스폰
        if (GameManager.Instance != null && GameManager.Instance.IsGameActive())
        {
            StartCoroutine(SpawnCutWithDelay());
        }
    }
    
    private IEnumerator SpawnCutWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnCut();
    }
    
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }
    
    public void ResumeSpawning()
    {
        if (spawnCoroutine == null)
        {
            StartSpawning();
        }
    }
    
    public void ResetDifficulty()
    {
        currentSpawnInterval = spawnInterval;
        currentSpeedMultiplier = 1f;
    }
    
    public void ClearAllCuts()
    {
        // 현재 컷 제거
        if (currentCut != null)
        {
            ReturnCutToPool(currentCut);
            currentCut = null;
        }
    }
    
    public int GetActiveCutCount()
    {
        return currentCut != null ? 1 : 0;
    }
    
    public int GetPoolSize()
    {
        return cutPool.Count;
    }
    
    public float GetCurrentDifficulty()
    {
        return currentSpeedMultiplier;
    }
    
    private void OnDestroy()
    {
        StopSpawning();
    }
}
