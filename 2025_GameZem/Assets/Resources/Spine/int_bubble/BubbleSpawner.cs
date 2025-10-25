using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BubbleSpawner : MonoBehaviour
{
    [Header("Spawn to (UI)")]
    public RectTransform spawnArea;              // Canvas 하위 SpawnArea
    public RectTransform uiParent;               // 비워두면 spawnArea 사용

    [Header("Spine UI Bubble Prefabs (3종)")]
    public RectTransform[] bubblePrefabs;        // 프리팹 3개(파란 큐브)

    [Header("Timing")]
    public float firstDelay = 1.0f;
    public Vector2 spawnIntervalRange = new Vector2(2f, 4f); // 스폰 간 최소/최대 간격(초)
    public bool loop = true;
    [Tooltip("벽시계 기준 총 동작 시간(초). 0 이하면 제한 없음")]
    public float activeDuration = 10f;

    [Header("Bubble Lifetime")]
    public Vector2 showSecondsRange = new Vector2(1.2f, 2.0f); // 개별 버블 표시 시간 범위

    [Header("Placement")]
    [Tooltip("가장자리 여백(버블 크기 추가로 더해짐)")]
    public Vector2 padding = new Vector2(20f, 20f);
    [Tooltip("0,0 이면 프리팹 원본 크기 사용")]
    public Vector2 overrideSize = Vector2.zero;

    // --- 내부 상태(셔플백 + 즉시 반복 방지) ---
    List<int> bag = new List<int>();
    int lastIndex = -1;
    Coroutine loopCo;

    void OnEnable()  { loopCo = StartCoroutine(SpawnLoop()); }
    void OnDisable() { if (loopCo != null) StopCoroutine(loopCo); }

    IEnumerator SpawnLoop()
    {
        if (firstDelay > 0f) yield return new WaitForSeconds(firstDelay);

        float endTime = (activeDuration > 0f) ? (Time.time + activeDuration) : float.PositiveInfinity;

        while (Time.time < endTime)
        {
            SpawnOnce();

            if (!loop) yield break;

            float wait = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            // 남은 시간보다 대기시간이 더 길면 남은 시간만큼만 기다리고 종료
            if (Time.time + wait > endTime) wait = Mathf.Max(0f, endTime - Time.time);
            yield return new WaitForSeconds(wait);
        }
    }

    void SpawnOnce()
    {
        if (bubblePrefabs == null || bubblePrefabs.Length == 0 || !spawnArea) return;

        // 1) 랜덤 프리팹(셔플백 사용으로 체감 랜덤 향상)
        int idx = NextIndex();
        var prefab = bubblePrefabs[idx];

        // 2) 인스턴스
        var parent = (RectTransform)(uiParent ? uiParent : spawnArea);
        var inst = Instantiate(prefab, parent);
        var rt = inst; // RectTransform

        // 3) 크기 확정
        if (overrideSize != Vector2.zero)
            rt.sizeDelta = overrideSize;

        // 4) spawnArea 내부 안전 좌표(버블 크기 + 패딩 고려)
        rt.SetParent(spawnArea, worldPositionStays: false);
        // 레이아웃 갱신(초기 size가 0일 가능성 방지)
        Canvas.ForceUpdateCanvases();

        Vector2 size = (overrideSize != Vector2.zero) ? overrideSize : rt.rect.size;
        // pivot 보정: 좌우/상하로 실제 반경 계산
        float halfW = Mathf.Lerp(0f, size.x, rt.pivot.x);           // 좌
        float halfWRight = size.x - halfW;                          // 우
        float halfH = Mathf.Lerp(0f, size.y, rt.pivot.y);           // 아래
        float halfHTop = size.y - halfH;                            // 위

        var r = spawnArea.rect;
        float xMin = r.xMin + Mathf.Max(padding.x, halfW);
        float xMax = r.xMax - Mathf.Max(padding.x, halfWRight);
        float yMin = r.yMin + Mathf.Max(padding.y, halfH);
        float yMax = r.yMax - Mathf.Max(padding.y, halfHTop);

        // 영역이 너무 좁아 음수가 되면 중앙 고정
        float x = (xMin <= xMax) ? Random.Range(xMin, xMax) : (r.xMin + r.width * 0.5f);
        float y = (yMin <= yMax) ? Random.Range(yMin, yMax) : (r.yMin + r.height * 0.5f);

        rt.anchoredPosition = new Vector2(x, y);

        // 5) 개별 버블의 표시시간 랜덤 적용
        var timed = inst.GetComponent<UIBubbleSpineTimed>();
        if (timed != null)
            timed.showSeconds = Random.Range(showSecondsRange.x, showSecondsRange.y);
    }

    // --- 셔플백 ---
    int NextIndex()
    {
        if (bag.Count == 0)
        {
            bag.Clear();
            for (int i = 0; i < bubblePrefabs.Length; i++) bag.Add(i);
            Shuffle(bag);
            // 첫 원소가 직전과 같으면 스왑
            if (bag[0] == lastIndex && bag.Count > 1)
            {
                int swap = Random.Range(1, bag.Count);
                (bag[0], bag[swap]) = (bag[swap], bag[0]);
            }
        }
        int pick = bag[0];
        bag.RemoveAt(0);
        lastIndex = pick;
        return pick;
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
