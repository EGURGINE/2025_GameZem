using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;

[DisallowMultipleComponent]
public class UIItemClickToHide : MonoBehaviour, IPointerClickHandler
{
    // 매니저에서 구독 가능 (코드/인스펙터 둘 다)

    float lifeTime = 2f;
    float t = 0;

    private Coroutine lifeRoutine;

    void Start()
    {
        t = 0;

        if (lifeRoutine != null)
        {
            StopCoroutine(lifeRoutine);
        }
        lifeRoutine = StartCoroutine(LifeRoutine());
    }

    IEnumerator LifeRoutine()
    {
        while (t < lifeTime)
        {
            t += Time.deltaTime;
            yield return null;
        }

        GameManager.Instance.LoseLife();
        Destroy(gameObject);
    }

    public UnityEvent onClicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        onClicked?.Invoke();

        StopCoroutine(lifeRoutine);
        Destroy(gameObject); // 한 번 클릭 시 즉시 제거
    }
}
