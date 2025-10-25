// InputBlocker.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class InputBlocker : MonoBehaviour
{
    public static InputBlocker Instance { get; private set; }

    [Header("Overlay (full-screen Raycast catcher)")]
    [SerializeField] private Image overlay;     // 투명 전체화면 이미지(레이캐스트 타겟 ON)

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        if (overlay == null)
        {
            // 자동 생성 (Canvas 하위)
            var go = new GameObject("InputBlockerOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            overlay = go.GetComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0f); // 완전 투명
            overlay.raycastTarget = true;

            var rt = (RectTransform)overlay.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        overlay.gameObject.SetActive(false);
    }

    public void BlockForSeconds(float seconds)
    {
        StopAllCoroutines();
        StartCoroutine(CoBlock(seconds));
    }

    private IEnumerator CoBlock(float seconds)
    {
        overlay.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        overlay.gameObject.SetActive(false);
    }
}
