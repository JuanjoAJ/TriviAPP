using UnityEngine;
using DG.Tweening;

public class BounceLogo : MonoBehaviour
{
    public float moveDistance = 400f;
    public float duration = 0.2f;
    public float speedRotation = 100f;

    private RectTransform rectTransform;
    private Vector2 startPos;

    private Tween bounceTween;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition;

        bounceTween = rectTransform.DOAnchorPosX(startPos.x + moveDistance, duration)
                                   .SetLoops(-1, LoopType.Yoyo)
                                   .SetEase(Ease.Linear);
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * -speedRotation * Time.deltaTime);
    }

    void OnDestroy()
    {
        if (bounceTween != null && bounceTween.IsActive())
        {
            bounceTween.Kill();
        }
    }
}
