using UnityEngine;
using Spine.Unity;

public class RandomToySwitcher : MonoBehaviour
{
    [Header("Spine SkeletonData Assets")]
    public SkeletonDataAsset[] toyDataAssets;

    [Header("Target SkeletonGraphic (하나만 존재)")]
    public SkeletonGraphic skeletonGraphic;

    [Header("애니메이션 이름")]
    public string idleAnim = "idle";
    public string disappearAnim = "disappear";

    void Start()
    {
        ShowRandomToy();
    }

    public void ShowRandomToy()
    {
        if (toyDataAssets == null || toyDataAssets.Length == 0 || !skeletonGraphic)
        {
            Debug.LogWarning("데이터 또는 SkeletonGraphic이 비어있음", this);
            return;
        }

        // 랜덤 SkeletonDataAsset 선택
        int idx = Random.Range(0, toyDataAssets.Length);
        var data = toyDataAssets[idx];

        // SkeletonGraphic에 적용
        skeletonGraphic.skeletonDataAsset = data;
        skeletonGraphic.Initialize(true); // 새 데이터 적용

        // idle 애니메이션 실행
        skeletonGraphic.AnimationState.SetAnimation(0, idleAnim, false);
        // 일정 시간 뒤 disappear 실행 (필요 시)
        StartCoroutine(PlayDisappearAfter(2f));
    }

    System.Collections.IEnumerator PlayDisappearAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        skeletonGraphic.AnimationState.SetAnimation(0, disappearAnim, false);
    }
}
