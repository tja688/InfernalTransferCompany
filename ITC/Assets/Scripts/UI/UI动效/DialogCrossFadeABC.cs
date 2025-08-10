using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class DialogCrossFadeABC_SingleImage : MonoBehaviour {
    [Header("Sprites")]
    public Sprite spriteA; // 原始图 A
    public Sprite spriteB; // 目标图 B

    [Header("Positions")]
    public Vector2 anchoredPosA;
    public Vector2 anchoredPosB;
    public Vector2 anchoredPosC;

    [Header("Durations (seconds)")]
    public float durationAB = 0.35f;
    public float durationBC = 0.30f;

    [Header("Easing")]
    public Ease moveEaseAB = Ease.InOutSine;
    public Ease moveEaseBC = Ease.InOutSine;
    public Ease fadeEaseAB = Ease.Linear;
    public Ease fadeEaseBC = Ease.Linear;

    [Header("Debug Keys")]
    public bool enableDebugKeys = true;
    public KeyCode keyTowardA = KeyCode.Q;
    public KeyCode keyTowardC = KeyCode.E;

    enum Node { C = 0, B = 1, A = 2 }
    [SerializeField] Node current = Node.C;

    Image img;
    CanvasGroup group;

    Tween tMove, tFade, tSpriteSwap;

    void Awake() {
        img = GetComponent<Image>();
        group = GetComponent<CanvasGroup>();
        SetInstant(Node.C);
    }

    void Update() {
        if (!enableDebugKeys) return;
        if (Input.GetKeyDown(keyTowardA)) StepTowardA();
        else if (Input.GetKeyDown(keyTowardC)) StepTowardC();
    }

    public void StepTowardA() {
        if (current == Node.A) return;
        var next = (Node)((int)current + 1);
        PlayTransition(current, next);
        current = next;
    }

    public void StepTowardC() {
        if (current == Node.C) return;
        var next = (Node)((int)current - 1);
        PlayTransition(current, next);
        current = next;
    }

    void PlayTransition(Node from, Node to) {
        KillTweens();

        // 位置动画
        var targetPos = GetPos(to);
        var moveDur = (to == Node.A || from == Node.A) ? durationAB : durationBC;
        var moveEase = (to == Node.A || from == Node.A) ? moveEaseAB : moveEaseBC;
        if ((GetRect().anchoredPosition - targetPos).sqrMagnitude > 0.0001f) {
            tMove = Tween.Custom(GetRect().anchoredPosition, targetPos, moveDur,
                v => GetRect().anchoredPosition = v, ease: moveEase);
        }

        // A<->B：Cross-Fade
        if ((from == Node.A && to == Node.B) || (from == Node.B && to == Node.A)) {
            Sprite startSprite = (from == Node.A) ? spriteA : spriteB;
            Sprite endSprite   = (to   == Node.A) ? spriteA : spriteB;
            CrossFadeSprite(startSprite, endSprite, durationAB, fadeEaseAB);
            group.alpha = 1f; // 保持可见
        }
        // B<->C：整体显隐
        else if ((from == Node.B && to == Node.C) || (from == Node.C && to == Node.B)) {
            float endAlpha = (to == Node.C) ? 0f : 1f;
            tFade = Tween.Custom(group.alpha, endAlpha, durationBC, a => group.alpha = a, ease: fadeEaseBC);
            if (to == Node.B) img.sprite = spriteB;
        }
        // A<->C 直接跳：拆成两段
        else if ((from == Node.A && to == Node.C) || (from == Node.C && to == Node.A)) {
            var mid = Node.B;
            PlayTransition(from, mid);
            Tween.Delay((from == Node.A) ? durationAB : durationBC)
                .OnComplete(() => PlayTransition(mid, to));
        }
    }

    void CrossFadeSprite(Sprite start, Sprite end, float duration, Ease ease) {
        img.sprite = start;
        img.color = new Color(1f, 1f, 1f, 1f);
        // 先淡出到透明 → 换图 → 淡入
        tFade = Tween.Custom(1f, 0f, duration * 0.5f, a => img.color = new Color(1f, 1f, 1f, a), ease: ease)
            .OnComplete(() => {
                img.sprite = end;
                tFade = Tween.Custom(0f, 1f, duration * 0.5f, a => img.color = new Color(1f, 1f, 1f, a), ease: ease);
            });
    }

    void SetInstant(Node node) {
        KillTweens();
        GetRect().anchoredPosition = GetPos(node);
        switch (node) {
            case Node.A:
                img.sprite = spriteA; group.alpha = 1f; break;
            case Node.B:
                img.sprite = spriteB; group.alpha = 1f; break;
            case Node.C:
                img.sprite = spriteB; group.alpha = 0f; break;
        }
        current = node;
    }

    void KillTweens() {
        tMove.Stop();
        tFade.Stop();
        tSpriteSwap.Stop();
    }

    RectTransform GetRect() => (RectTransform)transform;

    Vector2 GetPos(Node node) {
        switch (node) {
            case Node.A: return anchoredPosA;
            case Node.B: return anchoredPosB;
            default:     return anchoredPosC;
        }
    }
}
