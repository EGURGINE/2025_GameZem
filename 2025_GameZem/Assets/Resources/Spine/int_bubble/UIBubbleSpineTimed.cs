using UnityEngine;
using Spine.Unity;
using System.Collections;

// UI Spine 버블: appear -> idle(루프) 잠시 보여주고 -> exit(pop) 후 삭제
public class UIBubbleSpineTimed : MonoBehaviour
{
    [Header("Spine")]
    public SkeletonGraphic skel;           // 같은 오브젝트의 SkeletonGraphic
    public string appearAnim = "appear";   // 등장
    public string idleAnim   = "idle";     // 대기(루프)
    public string exitAnim   = "pop";      // 사라짐(한 번 재생)

    [Header("Timing")]
    public float showSeconds = 1.5f;       // 화면에 떠 있는 시간 (idle 시간)

    void Reset() { skel = GetComponent<SkeletonGraphic>(); }
    void Awake() { if (!skel) skel = GetComponent<SkeletonGraphic>(); }

    void OnEnable()
    {
        if (!skel) return;
        var st = skel.AnimationState;

        // appear -> idle(loop)
        if (!string.IsNullOrEmpty(appearAnim))
            st.SetAnimation(0, appearAnim, false);
        if (!string.IsNullOrEmpty(idleAnim))
            st.AddAnimation(0, idleAnim, true, 0f);

        // 지정 시간 뒤에 exit 애니메이션 후 삭제
        StartCoroutine(AutoDisappear());
    }

    IEnumerator AutoDisappear()
    {
        yield return new WaitForSeconds(showSeconds);

        if (skel && !string.IsNullOrEmpty(exitAnim))
        {
            var track = skel.AnimationState.SetAnimation(0, exitAnim, false);
            track.Complete += _ => { if (this) Destroy(gameObject); };
        }
        else
        {
            if (this) Destroy(gameObject);
        }
    }
}
