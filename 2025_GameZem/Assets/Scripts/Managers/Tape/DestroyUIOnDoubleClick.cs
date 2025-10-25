using UnityEngine;
using UnityEngine.EventSystems;

public class DestroyUIOnDoubleClick : MonoBehaviour, IPointerClickHandler
{
    private float lastClickTime = 0f;
    private float doubleClickInterval = 0.3f;

    public void OnPointerClick(PointerEventData eventData)
    {
        float now = Time.time;
        if (now - lastClickTime < doubleClickInterval)
        {
            Destroy(gameObject);
        }
        else
        {
            lastClickTime = now;
        }
    }
}
