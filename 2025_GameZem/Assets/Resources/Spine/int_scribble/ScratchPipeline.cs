using UnityEngine;
using UnityEngine.Events;

public class ScratchPipeline : MonoBehaviour
{
    [Header("Targets")]
    public GameObject overlayGO;           // DoodleOverlay 오브젝트 (ScratchOverlayImage 포함)
    public ScratchOverlayImage overlay;    // ScratchOverlayImage 스크립트 직접 연결
    public CanvasGroup blockOtherUI;       // 선택: 낙서 중 막을 다른 UI가 있으면 여기에 연결

    [Header("Events")]
    public UnityEvent onScratchStart;      // 낙서 시작 시 실행할 이벤트
    public UnityEvent onScratchCleared;    // 낙서 완료 시 실행할 이벤트
    public UnityEvent onScratchFailed;     // 낙서 실패 시 실행할 이벤트

    // === 낙서 시작 ===
    public void StartScratch()
    {
        if (overlayGO) overlayGO.SetActive(true);

        // 선택: 낙서 중 다른 UI 막기
        if (blockOtherUI)
        {
            blockOtherUI.interactable = false;
            blockOtherUI.blocksRaycasts = false;
        }

        onScratchStart?.Invoke();
    }

    // === 낙서 완료 ===
    public void HandleCleared()
    {
        Debug.Log("Scratch Cleared - Pipeline Triggered");
        onScratchCleared?.Invoke();
        Cleanup();
    }

    // === 낙서 실패 ===
    public void HandleFailed()
    {
        Debug.Log("Scratch Failed - Pipeline Triggered");
        onScratchFailed?.Invoke();
        Cleanup();
    }

    // === 정리 ===
    void Cleanup()
    {
        if (blockOtherUI)
        {
            blockOtherUI.interactable = true;
            blockOtherUI.blocksRaycasts = true;
        }
    }
}
