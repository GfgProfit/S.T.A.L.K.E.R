using DG.Tweening;
using UnityEngine;

public class MiniMapController : MonoBehaviour
{
    [SerializeField] private Camera _miniMapCamera;
    [SerializeField] [Min(1)] private int _step = 25;
    [SerializeField] private Vector2Int _minMaxZoom = new(25, 200);
    [SerializeField] [Min(0f)] private float _zoomDuration = 0.2f;
    [SerializeField] private Ease _zoomEase = Ease.OutQuad;

    [Inject] private IPlayerInput _playerInput = null;

    private IPlayerInput _fallbackPlayerInput;
    private MiniMapViewModel _viewModel;
    private MiniMapCameraView _cameraView;
    private MiniMapInputController _inputController;

    private IPlayerInput PlayerInput
    {
        get
        {
            if (_playerInput != null)
            {
                return _playerInput;
            }

            _fallbackPlayerInput ??= new LegacyPlayerInput();
            return _fallbackPlayerInput;
        }
    }

    private void OnValidate()
    {
        _step = Mathf.Max(1, _step);
        _zoomDuration = Mathf.Max(0f, _zoomDuration);

        if (_minMaxZoom.x > _minMaxZoom.y)
        {
            _minMaxZoom = new Vector2Int(_minMaxZoom.y, _minMaxZoom.x);
        }
    }

    private void Awake()
    {
        _viewModel = new MiniMapViewModel(GetInitialZoom(), _step, _minMaxZoom);
        _cameraView = new MiniMapCameraView(_miniMapCamera, _zoomDuration, _zoomEase, destroyCancellationToken);
        _inputController = new MiniMapInputController(PlayerInput, _viewModel, destroyCancellationToken);

        _cameraView.Bind(_viewModel);
    }

    private void Update() => _inputController?.Tick();

    private void OnDestroy()
    {
        _cameraView?.Dispose();
        _viewModel?.Dispose();
    }

    private float GetInitialZoom() => _miniMapCamera == null ? _minMaxZoom.x : _miniMapCamera.orthographicSize;
}