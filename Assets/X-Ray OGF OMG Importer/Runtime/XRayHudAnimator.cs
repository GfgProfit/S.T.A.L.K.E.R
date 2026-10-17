using UnityEngine;

namespace XRay.Importer
{
    /// <summary>
    /// Drives a hands rig and a weapon rig with one clock, so HUD motions that were authored as a
    /// pair (e.g. "ak74_idle" on the hands and "idle" on the weapon) stay frame-synchronous.
    /// Both rigs keep their own skeletons, clips and motion libraries.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class XRayHudAnimator : MonoBehaviour
    {
        [SerializeField] private XRayRigPlayer _hands;
        [SerializeField] private XRayRigPlayer _weapon;
        [SerializeField] private string _initialHandsMotion;
        [SerializeField] private string _initialWeaponMotion;
        [SerializeField] private float _speed = 1.0f;

        public XRayRigPlayer Hands => _hands;

        public XRayRigPlayer Weapon => _weapon;

        public float Speed
        {
            get => _speed;
            set => _speed = value;
        }

        /// <summary>Starts both motions at time zero. An empty name leaves that rig unchanged.</summary>
        public void Play(string handsMotion, string weaponMotion, float fadeDuration = 0.0f)
        {
            if (_hands != null && !string.IsNullOrEmpty(handsMotion))
            {
                _hands.Play(handsMotion, fadeDuration);
            }

            if (_weapon != null && !string.IsNullOrEmpty(weaponMotion))
            {
                _weapon.Play(weaponMotion, fadeDuration);
            }
        }

        /// <summary>Puts both rigs at the same motion time and applies the pose immediately.</summary>
        public void SetTime(float time)
        {
            if (_hands != null)
            {
                _hands.Time = time;
                _hands.Tick(0.0f);
            }

            if (_weapon != null)
            {
                _weapon.Time = time;
                _weapon.Tick(0.0f);
            }
        }

        private void Awake()
        {
            // The animator owns the clock; individual players must not advance on their own.
            if (_hands != null)
            {
                _hands.DriveSelf = false;
            }

            if (_weapon != null)
            {
                _weapon.DriveSelf = false;
            }
        }

        private void Start()
        {
            Play(_initialHandsMotion, _initialWeaponMotion);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime * _speed;
            if (_hands != null)
            {
                _hands.Tick(deltaTime);
            }

            if (_weapon != null)
            {
                _weapon.Tick(deltaTime);
            }
        }
    }
}
