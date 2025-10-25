using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PanelManager : MonoBehaviour
{
    [Header("Panels (CanvasGroup)")]
    public CanvasGroup helpPanel;
    public CanvasGroup menuPanel;
    public CanvasGroup creditsPanel;      // ★ 크레딧 패널 추가

    [Header("Buttons (icons)")]
    public GameObject helpButton;         // 물음표 아이콘
    public GameObject menuButton;         // 메뉴 아이콘
    public GameObject creditButton;       // 좌하단 '크레딧' 버튼(보였다/숨김 제어용)

    [Header("Close (X) Buttons inside panels")]
    public Button helpClose;              // HelpPanel 안의 X
    public Button menuClose;              // MenuPanel 안의 X
    public Button creditsClose;           // CreditsPanel 안의 X

    [Header("Effects")]
    [Tooltip("0이면 즉시 전환, >0이면 페이드(초)")]
    public float fade = 0.15f;

    private CanvasGroup _current;         // 현재 열려있는 패널(배타 토글용)

    void Awake()
    {
        HideImmediate(helpPanel);
        HideImmediate(menuPanel);
        HideImmediate(creditsPanel);
        ShowTopButtons(true);

        if (helpClose)    helpClose.onClick.AddListener(CloseAll);
        if (menuClose)    menuClose.onClick.AddListener(CloseAll);
        if (creditsClose) creditsClose.onClick.AddListener(CloseAll);
    }

    // ── 외부(버튼 OnClick)에서 호출 ──
    public void ToggleHelp()    => ToggleExclusive(helpPanel);
    public void ToggleMenu()    => ToggleExclusive(menuPanel);
    public void ToggleCredits() => ToggleExclusive(creditsPanel);

    public void CloseAll()
    {
        Hide(helpPanel);
        Hide(menuPanel);
        Hide(creditsPanel);
        _current = null;
        ShowTopButtons(true);   // 닫으면 아이콘/크레딧 버튼 복귀
    }

    // ── 핵심: 하나만 열리게 ──
    void ToggleExclusive(CanvasGroup target)
    {
        if (!target) return;

        // 같은 패널 다시 누르면 닫기
        if (_current == target)
        {
            CloseAll();
            return;
        }

        // 다른 패널이 열려 있으면 닫음
        if (_current) Hide(_current);

        // 이번 것만 열기
        Show(target);
        _current = target;

        // 패널 열려있을 때 상단/좌하단 버튼 숨김
        ShowTopButtons(false);
    }

    // ── 상단/좌하단 버튼 보이기/숨기기 ──
    void ShowTopButtons(bool show)
    {
        if (helpButton)   helpButton.SetActive(show);
        if (menuButton)   menuButton.SetActive(show);
        if (creditButton) creditButton.SetActive(show);
    }

    // ── 공통 Show/Hide ──
    void Show(CanvasGroup cg)
    {
        if (!cg) return;
        cg.gameObject.SetActive(true);
        if (fade <= 0f)
        {
            cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true;
            return;
        }
        StopAllCoroutines();
        StartCoroutine(FadeTo(cg, 1f, true));
    }

    void Hide(CanvasGroup cg)
    {
        if (!cg || !cg.gameObject.activeSelf) return;
        if (fade <= 0f)
        {
            cg.alpha = 0f; cg.interactable = false; cg.blocksRaycasts = false;
            cg.gameObject.SetActive(false);
            return;
        }
        StopAllCoroutines();
        StartCoroutine(FadeTo(cg, 0f, false, deactivate: true));
    }

    void HideImmediate(CanvasGroup cg)
    {
        if (!cg) return;
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.gameObject.SetActive(false);
    }

    IEnumerator FadeTo(CanvasGroup cg, float to, bool interact, bool deactivate = false)
    {
        float from = cg.alpha, t = 0f;
        // 패널이 열릴 때는 미리 Raycast 막아 깜빡임 클릭 방지
        cg.blocksRaycasts = true;

        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fade);
            yield return null;
        }

        cg.alpha = to;
        cg.interactable = interact;
        cg.blocksRaycasts = interact;
        if (deactivate) cg.gameObject.SetActive(false);
    }
}
