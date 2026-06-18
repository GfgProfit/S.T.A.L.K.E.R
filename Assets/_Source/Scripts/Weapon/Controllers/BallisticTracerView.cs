using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class BallisticTracerView : MonoBehaviour
{
    private const string TRACER_SHADER_NAME = "Game/BallisticTracer";
    private const int TRACER_SORTING_ORDER = 100;

    private readonly List<TracerPoint> _points = new();

    private LineRenderer _lineRenderer;
    private float _ignitionDistanceMeters;
    private float _burnDurationSeconds;
    private float _trailDurationSeconds;
    private float _minimumVertexDistanceSquared;
    private float _ignitionTimeSeconds = -1f;
    private Vector3 _lastRecordedPosition;
    private bool _recording;
    private bool _completed;

    public static BallisticTracerView Create(ItemData ammoData, Vector3 position)
    {
        if (ammoData == null || ammoData.AmmoTracerEnabled == false || ammoData.AmmoTracerWidthMeters <= 0f)
        {
            return null;
        }

        Material material = ammoData.AmmoTracerMaterial;

        if (material == null)
        {
            Shader shader = Shader.Find(TRACER_SHADER_NAME);

            if (shader == null)
            {
                Debug.LogWarning($"[{nameof(BallisticTracerView)}] {TRACER_SHADER_NAME} shader was not found. Tracer rendering is disabled.");
                return null;
            }

            material = new Material(shader)
            {
                name = "Runtime Ballistic Tracer Material",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        GameObject tracerObject = new("Ballistic Tracer");
        tracerObject.transform.position = position;
        BallisticTracerView tracerView = tracerObject.AddComponent<BallisticTracerView>();
        tracerView.Initialize(ammoData, material, position);
        return tracerView;
    }

    public void MoveTo(Vector3 position, float distanceTravelledMeters, float elapsedLifetimeSeconds)
    {
        if (_completed)
        {
            return;
        }

        transform.position = position;
        bool reachedIgnitionDistance = distanceTravelledMeters >= _ignitionDistanceMeters;

        if (reachedIgnitionDistance && _ignitionTimeSeconds < 0f)
        {
            _ignitionTimeSeconds = elapsedLifetimeSeconds;
        }

        bool shouldRecord = reachedIgnitionDistance &&
                            (_burnDurationSeconds <= 0f || elapsedLifetimeSeconds - _ignitionTimeSeconds <= _burnDurationSeconds);

        if (shouldRecord == false)
        {
            _recording = false;
            return;
        }

        if (_recording == false)
        {
            _points.Clear();
            AddPoint(position);
            _recording = true;
            RefreshLineRenderer();
            return;
        }

        if ((position - _lastRecordedPosition).sqrMagnitude < _minimumVertexDistanceSquared)
        {
            return;
        }

        AddPoint(position);
        RefreshLineRenderer();
    }

    public void Complete()
    {
        _completed = true;
        _recording = false;

        if (_points.Count == 0)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        RemoveExpiredPoints();

        if (_completed && _points.Count == 0)
        {
            Destroy(gameObject);
        }
    }

    private void Initialize(ItemData ammoData, Material material, Vector3 position)
    {
        _ignitionDistanceMeters = ammoData.AmmoTracerIgnitionDistanceMeters;
        _burnDurationSeconds = ammoData.AmmoTracerBurnDurationSeconds;
        _trailDurationSeconds = Mathf.Max(0.01f, ammoData.AmmoTracerTrailDurationSeconds);
        float minimumVertexDistance = Mathf.Max(0.001f, ammoData.AmmoTracerWidthMeters * 0.5f);
        _minimumVertexDistanceSquared = minimumVertexDistance * minimumVertexDistance;

        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.sharedMaterial = material;
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = false;
        _lineRenderer.alignment = LineAlignment.View;
        _lineRenderer.textureMode = LineTextureMode.Stretch;
        _lineRenderer.widthMultiplier = ammoData.AmmoTracerWidthMeters;
        _lineRenderer.widthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        _lineRenderer.numCornerVertices = 2;
        _lineRenderer.numCapVertices = 2;
        _lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _lineRenderer.receiveShadows = false;
        _lineRenderer.generateLightingData = false;
        _lineRenderer.lightProbeUsage = LightProbeUsage.Off;
        _lineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _lineRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        _lineRenderer.allowOcclusionWhenDynamic = false;
        _lineRenderer.sortingOrder = TRACER_SORTING_ORDER;
        _lineRenderer.positionCount = 0;

        Color tracerColor = ammoData.AmmoTracerColor;
        Color emissionColor = new(
            tracerColor.r * ammoData.AmmoTracerEmissionIntensity,
            tracerColor.g * ammoData.AmmoTracerEmissionIntensity,
            tracerColor.b * ammoData.AmmoTracerEmissionIntensity,
            tracerColor.a);
        _lineRenderer.startColor = new Color(1f, 1f, 1f, 0f);
        _lineRenderer.endColor = Color.white;

        MaterialPropertyBlock propertyBlock = new();
        propertyBlock.SetColor("_BaseColor", emissionColor);
        _lineRenderer.SetPropertyBlock(propertyBlock);

        if (_ignitionDistanceMeters <= 0f)
        {
            _ignitionTimeSeconds = 0f;
            AddPoint(position);
            _recording = true;
            RefreshLineRenderer();
        }
    }

    private void AddPoint(Vector3 position)
    {
        _points.Add(new TracerPoint(position, Time.time));
        _lastRecordedPosition = position;
    }

    private void RemoveExpiredPoints()
    {
        float expirationTime = Time.time - _trailDurationSeconds;
        int expiredPointCount = 0;

        while (expiredPointCount < _points.Count && _points[expiredPointCount].CreationTime <= expirationTime)
        {
            expiredPointCount++;
        }

        if (expiredPointCount <= 0)
        {
            return;
        }

        _points.RemoveRange(0, expiredPointCount);
        RefreshLineRenderer();
    }

    private void RefreshLineRenderer()
    {
        _lineRenderer.positionCount = _points.Count;

        for (int i = 0; i < _points.Count; i++)
        {
            _lineRenderer.SetPosition(i, _points[i].Position);
        }
    }

    private readonly struct TracerPoint
    {
        public TracerPoint(Vector3 position, float creationTime)
        {
            Position = position;
            CreationTime = creationTime;
        }

        public Vector3 Position { get; }
        public float CreationTime { get; }
    }
}
