using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class BootstrapLoadingController : MonoBehaviour
{
    private const string LOADING_SCENE_TITLE = "Загрузка сцены...";
    private const string CONTINUE_TITLE = "Нажмите любую клавишу, что бы продолжить.";
    private const float SCENE_READY_PROGRESS = 0.9f;

    [SerializeField] private string _gameSceneName = "Test";
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private CanvasGroup _titleCanvasGroup;
    [SerializeField] private TMP_Text _progressText;
    [SerializeField] private Image _fillImage;
    [SerializeField] [Range(0f, 1f)] private float _continueTextMinimumAlpha = 0.25f;
    [SerializeField] [Min(0.1f)] private float _continueTextBlinkCycleSeconds = 1.2f;

    private void Start()
    {
        RunLoadingAsync(destroyCancellationToken).Forget(Debug.LogException);
    }

    private async UniTask RunLoadingAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();
        SetOverallProgress(0f);
        _titleCanvasGroup.alpha = 1f;

        _titleText.text = LOADING_SCENE_TITLE;
        AsyncOperation sceneLoadOperation = SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Single);

        if (sceneLoadOperation == null)
        {
            throw new InvalidOperationException($"Scene '{_gameSceneName}' could not be loaded.");
        }

        sceneLoadOperation.allowSceneActivation = false;

        while (sceneLoadOperation.progress < SCENE_READY_PROGRESS)
        {
            float sceneProgress = Mathf.Clamp01(sceneLoadOperation.progress / SCENE_READY_PROGRESS);
            SetOverallProgress(sceneProgress);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        SetOverallProgress(1f);
        _titleText.text = CONTINUE_TITLE;
        await WaitForContinueInputAsync(cancellationToken);
        _titleCanvasGroup.alpha = 1f;
        sceneLoadOperation.allowSceneActivation = true;
    }

    private async UniTask WaitForContinueInputAsync(CancellationToken cancellationToken)
    {
        float elapsed = 0f;

        while (Input.anyKeyDown == false)
        {
            float normalizedAlpha = (Mathf.Cos(elapsed / _continueTextBlinkCycleSeconds * Mathf.PI * 2f) + 1f) * 0.5f;
            _titleCanvasGroup.alpha = Mathf.Lerp(_continueTextMinimumAlpha, 1f, normalizedAlpha);
            elapsed += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
    }

    private void SetOverallProgress(float progress)
    {
        float normalizedProgress = Mathf.Clamp01(progress);
        _progressText.text = $"{Mathf.RoundToInt(normalizedProgress * 100f)}%";
        _fillImage.fillAmount = normalizedProgress;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_gameSceneName))
        {
            throw new InvalidOperationException("Game scene name is not configured.");
        }

        if (_titleText == null || _titleCanvasGroup == null || _progressText == null || _fillImage == null)
        {
            throw new InvalidOperationException("Bootstrap loading UI references are not configured.");
        }
    }
}
