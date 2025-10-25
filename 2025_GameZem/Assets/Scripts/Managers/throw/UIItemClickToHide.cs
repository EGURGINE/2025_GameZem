using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using Spine.Unity;

public class UIItemClickToHide : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("disappear 애니메이션 길이만큼(예: 0.25~0.5)")]
    public float hideDelay = 0.4f;

    public bool destroyOnHide = true;

    // 🔹 클릭 알림(원하면 씀), 🔹 '완전히 사라진 뒤' 알림(스포너는 이걸 사용)
    public UnityEvent onClicked;
    public UnityEvent onHidden;

    ToyAnimator toyAnimator;

    void Awake() => toyAnimator = GetComponent<ToyAnimator>();

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke();

        // 클릭 시 바로 disappear 재생
        if (toyAnimator) toyAnimator.PlayDisappearNow();

        // 애니 끝날 때까지 기다렸다가 제거 + 통지
        StartCoroutine(HideAfterDelay());
    }

    IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        onHidden?.Invoke();   // ← 스포너는 여기에 반응

        if (destroyOnHide) Destroy(gameObject);
        else gameObject.SetActive(false);
    }
}
