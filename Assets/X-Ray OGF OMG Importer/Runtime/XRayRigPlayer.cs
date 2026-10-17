using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace XRay.Importer
{
    /// <summary>
    /// Plays X-Ray motions on one rig through a PlayableGraph (no AnimatorController needed).
    /// The graph uses manual update so several rigs can be advanced with the same delta time
    /// (see XRayHudAnimator); with Drive Self enabled the player advances itself in Update.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [DisallowMultipleComponent]
    public sealed class XRayRigPlayer : MonoBehaviour
    {
        [SerializeField] private XRayMotionLibrary _library;
        [SerializeField] private string _initialMotion;
        [SerializeField] private bool _driveSelf = true;

        private PlayableGraph _graph;
        private AnimationMixerPlayable _mixer;
        private AnimationClipPlayable _current;
        private AnimationClipPlayable _previous;
        private float _fadeDuration;
        private float _fadeElapsed;

        public XRayMotionLibrary Library
        {
            get => _library;
            set => _library = value;
        }

        public bool DriveSelf
        {
            get => _driveSelf;
            set => _driveSelf = value;
        }

        public AnimationClip CurrentClip { get; private set; }

        public string CurrentMotion { get; private set; }

        /// <summary>Local time of the current motion in seconds (not wrapped for looping clips).</summary>
        public float Time
        {
            get => _current.IsValid() ? (float)_current.GetTime() : 0.0f;
            set
            {
                if (_current.IsValid())
                {
                    _current.SetTime(value);
                }
            }
        }

        public float Length => CurrentClip != null ? CurrentClip.length : 0.0f;

        public bool IsFinished => CurrentClip != null && !CurrentClip.isLooping && Time >= CurrentClip.length;

        public bool Play(string motionName, float fadeDuration = 0.0f, float speed = 1.0f)
        {
            if (_library == null)
            {
                Debug.LogError($"{nameof(XRayRigPlayer)} on '{name}': no motion library assigned.", this);
                return false;
            }

            if (!_library.TryGetClip(motionName, out AnimationClip clip))
            {
                Debug.LogError($"{nameof(XRayRigPlayer)} on '{name}': motion '{motionName}' not found in '{_library.name}'.", this);
                return false;
            }

            Play(clip, fadeDuration, speed);
            CurrentMotion = motionName;
            return true;
        }

        public void Play(AnimationClip clip, float fadeDuration = 0.0f, float speed = 1.0f)
        {
            if (clip == null)
            {
                Debug.LogError($"{nameof(XRayRigPlayer)} on '{name}': clip is null.", this);
                return;
            }

            EnsureGraph();
            if (_previous.IsValid())
            {
                _mixer.DisconnectInput(1);
                _previous.Destroy();
            }

            if (_current.IsValid() && fadeDuration > 0.0f)
            {
                _mixer.DisconnectInput(0);
                _previous = _current;
                _graph.Connect(_previous, 0, _mixer, 1);
                _mixer.SetInputWeight(1, 1.0f);
            }
            else if (_current.IsValid())
            {
                _mixer.DisconnectInput(0);
                _current.Destroy();
            }

            _current = AnimationClipPlayable.Create(_graph, clip);
            _current.SetApplyFootIK(false);
            _current.SetSpeed(speed);
            _graph.Connect(_current, 0, _mixer, 0);
            _fadeDuration = _previous.IsValid() ? fadeDuration : 0.0f;
            _fadeElapsed = 0.0f;
            _mixer.SetInputWeight(0, _fadeDuration > 0.0f ? 0.0f : 1.0f);
            CurrentClip = clip;
            CurrentMotion = clip.name;
            _graph.Evaluate(0.0f);
        }

        public void SetSpeed(float speed)
        {
            if (_current.IsValid())
            {
                _current.SetSpeed(speed);
            }
        }

        /// <summary>Advances the graph by deltaTime and applies the pose.</summary>
        public void Tick(float deltaTime)
        {
            if (!_graph.IsValid())
            {
                return;
            }

            if (_previous.IsValid())
            {
                _fadeElapsed += deltaTime;
                float weight = _fadeDuration > 0.0f ? Mathf.Clamp01(_fadeElapsed / _fadeDuration) : 1.0f;
                _mixer.SetInputWeight(0, weight);
                _mixer.SetInputWeight(1, 1.0f - weight);
                if (weight >= 1.0f)
                {
                    _mixer.DisconnectInput(1);
                    _previous.Destroy();
                }
            }

            _graph.Evaluate(deltaTime);
        }

        private void Start()
        {
            if (!string.IsNullOrEmpty(_initialMotion))
            {
                Play(_initialMotion);
            }
        }

        private void Update()
        {
            if (_driveSelf)
            {
                Tick(UnityEngine.Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }
        }

        private void EnsureGraph()
        {
            if (_graph.IsValid())
            {
                return;
            }

            _graph = PlayableGraph.Create($"{name}.XRayRigPlayer");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _mixer = AnimationMixerPlayable.Create(_graph, 2);
            var output = AnimationPlayableOutput.Create(_graph, "XRay", GetComponent<Animator>());
            output.SetSourcePlayable(_mixer);
            _graph.Play();
        }
    }
}
