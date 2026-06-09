using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class VignetteController : MonoBehaviour
{
    [SerializeField] private Volume _globalVolume;

    [Space]
    [SerializeField, Range(0f, 1f)] private float _crouchIntensity = 0.35f;
    [SerializeField] private float _duration = 0.5f;

    private Vignette _vignette;
    private float _baseIntensity;
    private Tween _intensityTween;

    private void Start()
    {
        if (_globalVolume == null)
        {
            Debug.LogWarning($"{nameof(VignetteController)}: Global Volume не назначен.");
            enabled = false;
            return;
        }

        if (_globalVolume.profile == null)
        {
            _globalVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        }
        else
        {
            _globalVolume.profile = Instantiate(_globalVolume.profile);
        }

        if (!_globalVolume.profile.TryGet(out _vignette))
        {
            _vignette = _globalVolume.profile.Add<Vignette>(true);
        }

        _vignette.active = true;

        _vignette.intensity.overrideState = true;

        _baseIntensity = _vignette.intensity.value;
    }

    private void OnDestroy()
    {
        _intensityTween?.Kill();
    }

    public void AnimateIntensityCrouch()
    {
        AnimateIntensity(_crouchIntensity);
    }

    public void AnimateIntensityBase()
    {
        AnimateIntensity(_baseIntensity);
    }

    private void AnimateIntensity(float targetIntensity)
    {
        if (_vignette == null)
        {
            return;
        }

        _intensityTween?.Kill();

        _intensityTween = DOTween.To(() => _vignette.intensity.value, x => _vignette.intensity.value = x, targetIntensity, _duration).SetEase(Ease.InOutSine);
    }
}