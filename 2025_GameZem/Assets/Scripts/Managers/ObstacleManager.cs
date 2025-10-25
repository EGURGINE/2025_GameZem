using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObstacleManager : MonoBehaviour
{
    [Header("Obstacle Prefabs")]
    [Tooltip("물건 던지기")]
    public GameObject throwingObjectsPrefab;
    [Tooltip("편집자 원고 독촉")]
    public GameObject editorPressurePrefab;
    [Tooltip("절취선 테이프")]
    public GameObject cutLineTapePrefab;
    [Tooltip("말풍선으로 화면 일부 가려짐")]
    public GameObject speechBubbleOverlayPrefab;
    [Tooltip("센세이센의 발")]
    public GameObject senseisenFootPrefab;
    [Tooltip("조울이의 낙서")]
    public GameObject joulDoodlePrefab;
    
    [Header("Spawn Settings")]
    public Transform obstacleContainer;
    
    private List<GameObject> activeObstacles = new List<GameObject>();
    
    private void Start()
    {
        // GameManager 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver += OnGameOver;
            GameManager.Instance.OnGameCleared += OnGameCleared;
        }
    }
    
    private void SpawnObstacle(ObstacleSpawnData spawnData)
    {
        GameObject prefabToSpawn = GetPrefabByType(spawnData.type);
        
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"Prefab for obstacle type {spawnData.type} not found!");
            return;
        }
        
        if (obstacleContainer == null)
        {
            Debug.LogWarning("Obstacle container is not assigned!");
            return;
        }
        
        // 장애물 생성
        GameObject obstacle = Instantiate(prefabToSpawn, obstacleContainer);
        
        // RectTransform이 있는 경우 위치 설정 (UI 요소)
        RectTransform rectTransform = obstacle.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = spawnData.position;
        }
        else
        {
            // Transform만 있는 경우
            obstacle.transform.localPosition = spawnData.position;
        }
        
        // 활성 장애물 리스트에 추가
        activeObstacles.Add(obstacle);
        
        Debug.Log($"Obstacle spawned: Type={spawnData.type}, Position={spawnData.position}");
    }
    
    // 외부에서 방해 요소를 스폰할 수 있는 public 메서드
    public void SpawnObstacleFromExternal(ObstacleType type, Vector2 position)
    {
        ObstacleSpawnData spawnData = new ObstacleSpawnData
        {
            type = type,
            position = position
        };
        
        SpawnObstacle(spawnData);
        
        // 테이프 타입일 경우 CutSpawner에 알림
        if (type == ObstacleType.CutLineTape)
        {
            CutSpawner cutSpawner = FindObjectOfType<CutSpawner>();
            if (cutSpawner != null)
            {
                cutSpawner.SetNextCutHasTape();
            }
        }
    }
    
    private GameObject GetPrefabByType(ObstacleType type)
    {
        switch (type)
        {
            case ObstacleType.ThrowingObjects:
                return throwingObjectsPrefab;
            case ObstacleType.EditorPressure:
                return editorPressurePrefab;
            case ObstacleType.CutLineTape:
                return cutLineTapePrefab;
            case ObstacleType.SpeechBubbleOverlay:
                return speechBubbleOverlayPrefab;
            case ObstacleType.SenseisenFoot:
                return senseisenFootPrefab;
            case ObstacleType.JoulDoodle:
                return joulDoodlePrefab;
            default:
                return null;
        }
    }
    
    public void ClearAllObstacles()
    {
        // 모든 활성 장애물 제거
        foreach (GameObject obstacle in activeObstacles)
        {
            if (obstacle != null)
            {
                Destroy(obstacle);
            }
        }
        
        activeObstacles.Clear();
        Debug.Log("All obstacles cleared");
    }
    
    public void RemoveObstacle(GameObject obstacle)
    {
        if (activeObstacles.Contains(obstacle))
        {
            activeObstacles.Remove(obstacle);
            Destroy(obstacle);
            Debug.Log("Obstacle removed");
        }
    }
    
    private void OnGameOver()
    {
        ClearAllObstacles();
        Debug.Log("Game Over - Obstacles cleared");
    }
    
    private void OnGameCleared()
    {
        ClearAllObstacles();
        Debug.Log("Game Cleared - Obstacles cleared");
    }
    
    public void ResetManager()
    {
        ClearAllObstacles();
        Debug.Log("ObstacleManager reset");
    }
    
    public int GetActiveObstacleCount()
    {
        return activeObstacles.Count;
    }
    
    private void OnDestroy()
    {
        // GameManager 이벤트 구독 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameOver -= OnGameOver;
            GameManager.Instance.OnGameCleared -= OnGameCleared;
        }
        
        ClearAllObstacles();
    }
}

[System.Serializable]
public class ObstacleSpawnData
{
    public ObstacleType type;
    public Vector2 position;
}

public enum ObstacleType
{
    ThrowingObjects = 0,      // 물건 던지기
    EditorPressure = 1,       // 편집자 원고 독촉
    CutLineTape = 2,          // 절취선 테이프
    SpeechBubbleOverlay = 3,  // 말풍선으로 화면 일부 가려짐
    SenseisenFoot = 4,        // 센세이센의 발
    JoulDoodle = 5            // 조울이의 낙서
}

