using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UIItemClickToHide : MonoBehaviour, IPointerClickHandler
{
    // 매니저에서 구독 가능 (코드/인스펙터 둘 다)
    public UnityEvent onClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke();
        Destroy(gameObject); // 한 번 클릭 시 즉시 제거
    }
}
