using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TimedUIRandomSpawner : MonoBehaviour
{
    [Header("필수 참조")]
    [Tooltip("아이템이 나타날 사각형 영역 (이 RectTransform 내부에서만 랜덤 배치)")]
    public RectTransform spawnArea;

    [Tooltip("스폰할 UI 프리팹 (Image + UIItemClickToHide 포함 권장)")]
    public RectTransform itemPrefab;

    [Header("타이밍")]
    [Tooltip("게임 시작 후 첫 등장까지 대기 시간")]
    public float firstDelay = 3f;

    [Tooltip("아이템이 클릭으로 제거된 뒤 다음 등장까지 대기 시간")]
    public float respawnDelay = 5f;

    [Tooltip("반복 스폰 여부 (off면 한 번만 나타남)")]
    public bool loop = true;

    [Header("배치/크기")]
    [Tooltip("아이템 크기 고정 (0,0이면 프리팹 원본 크기 사용)")]
    public Vector2 overrideSize = Vector2.zero;

    [Tooltip("영역 가장자리 여백")]
    public Vector2 padding = new Vector2(20f, 20f);

    [Tooltip("스폰 시 부모로 사용할 트랜스폼 (비워두면 spawnArea 하위로 붙임)")]
    public Transform parentOverride;

    // 내부 상태
    RectTransform _current;

    void Start()
    {
        // 시작하자마자 아이템은 “없음” → firstDelay 후 스폰
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        // 첫 대기
        if (firstDelay > 0f) yield return new WaitForSeconds(firstDelay);

        do
        {
            SpawnOne();

            // 클릭으로 제거될 때까지 대기
            while (_current != null) yield return null;

            // 한 번만 띄우고 끝낼 거면 종료
            if (!loop) yield break;

            // 다음 등장까지 대기
            if (respawnDelay > 0f) yield return new WaitForSeconds(respawnDelay);

        } while (loop);
    }

    public void SpawnOne()
    {
        if (!spawnArea || !itemPrefab)
        {
            Debug.LogError("[TimedUIRandomSpawner] spawnArea 또는 itemPrefab이 비어 있습니다.");
            return;
        }

        // 생성 및 부모 지정
        RectTransform parentRT = parentOverride ? parentOverride as RectTransform : spawnArea;
        _current = Instantiate(itemPrefab, parentRT);

        // 기본 RectTransform 세팅
        _current.anchorMin = _current.anchorMax = new Vector2(0.5f, 0.5f);
        _current.pivot = new Vector2(0.5f, 0.5f);
        _current.localScale = Vector3.one;
        _current.localRotation = Quaternion.identity;

        if (overrideSize != Vector2.zero) _current.sizeDelta = overrideSize;

        // 클릭 시 사라지게 하는 컴포넌트 보장
        var click = _current.GetComponent<UIItemClickToHide>();
        if (!click) click = _current.gameObject.AddComponent<UIItemClickToHide>();

        // 클릭 이벤트에 “현재 인스턴스 비우기” 연결
        click.onClicked.AddListener(() => { _current = null; });

        // 랜덤 위치 계산 (spawnArea의 anchored 공간 기준)
        Rect r = spawnArea.rect;

        float halfW = _current.rect.width * 0.5f;
        float halfH = _current.rect.height * 0.5f;

        float minX = r.xMin + Mathf.Max(padding.x, halfW);
        float maxX = r.xMax - Mathf.Max(padding.x, halfW);
        float minY = r.yMin + Mathf.Max(padding.y, halfH);
        float maxY = r.yMax - Mathf.Max(padding.y, halfH);

        float x = (minX <= maxX) ? Random.Range(minX, maxX) : 0f;
        float y = (minY <= maxY) ? Random.Range(minY, maxY) : 0f;

        if (parentRT == spawnArea)
        {
            _current.anchoredPosition = new Vector2(x, y);
        }
        else
        {
            // 다른 부모를 쓸 때 좌표 변환
            Vector3 world = spawnArea.TransformPoint(new Vector3(x, y, 0f));
            Vector3 localToParent = parentRT.InverseTransformPoint(world);
            _current.anchoredPosition = new Vector2(localToParent.x, localToParent.y);
        }

        // 레이캐스트 가능한지 보장
        var img = _current.GetComponent<Image>();
        if (img) img.raycastTarget = true;
    }
}
