using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class FirstPersonWeaponAimPointView : MonoBehaviour
{
    private const float ALPHA_EPSILON = 0.001f;

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] [Min(0f)] private float _fadeSpeed = 12f;

    private bool _isAimActive;

    private void Awake()
    {
        ResolveCanvasGroup();
        SnapAlpha();
    }

    private void Update()
    {
        if (_canvasGroup == null)
        {
            return;
        }

        float targetAlpha = _isAimActive ? 0f : 1f;

        if (_fadeSpeed <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            return;
        }

        float t = 1f - Mathf.Exp(-_fadeSpeed * Time.deltaTime);
        float nextAlpha = Mathf.Lerp(_canvasGroup.alpha, targetAlpha, t);
        _canvasGroup.alpha = Mathf.Abs(nextAlpha - targetAlpha) <= ALPHA_EPSILON ? targetAlpha : nextAlpha;
    }

    public void SetAimActive(bool active, bool instant = false)
    {
        _isAimActive = active;

        if (instant)
        {
            SnapAlpha();
        }
    }

    private void Reset() => _canvasGroup = GetComponent<CanvasGroup>();

    private void ResolveCanvasGroup()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }

    private void SnapAlpha()
    {
        ResolveCanvasGroup();

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = _isAimActive ? 0f : 1f;
        }
    }
}
