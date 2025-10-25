using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(RectTransform), typeof(Image), typeof(CanvasGroup))]
public class SpeechBubbleController : MonoBehaviour
{
    [Header("Lifetime")]
    public float showSeconds = 1.5f;
    public float fadeOutSeconds = 0.35f;

    CanvasGroup cg;
    Image img;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        img = GetComponent<Image>();
        if (cg) cg.alpha = 1f;
    }

    public void Init(Sprite sprite, Vector2 size)
    {
        var rt = (RectTransform)transform;
        rt.sizeDelta = size;
        if (img) img.sprite = sprite;
        // 9-slice 스프라이트면 Image.type을 Sliced로
        if (img && img.sprite && img.sprite.border != Vector4.zero)
            img.type = Image.Type.Sliced;
    }

    void OnEnable() => StartCoroutine(LifeRoutine());

    IEnumerator LifeRoutine()
    {
        // 대기
        yield return new WaitForSeconds(showSeconds);

        // 페이드 아웃
        float t = 0f;
        while (t < fadeOutSeconds)
        {
            t += Time.unscaledDeltaTime;
            if (cg) cg.alpha = Mathf.Lerp(1f, 0f, t / fadeOutSeconds);
            yield return null;
        }
        Destroy(gameObject);
    }
}
