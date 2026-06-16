using UnityEngine;

public sealed class FirstPersonWeaponAnimationDebugInput : MonoBehaviour
{
    [SerializeField] private FirstPersonWeaponController _weaponController;

    [Header("Actions")]
    [SerializeField] private KeyCode _shootKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode _shootLastKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode _reloadKey = KeyCode.R;
    [SerializeField] private KeyCode _reloadFullKey = KeyCode.T;
    [SerializeField] private KeyCode _misfireKey = KeyCode.F;
    [SerializeField] private KeyCode _revivalKey = KeyCode.G;
    [SerializeField] private KeyCode _revivalLastKey = KeyCode.H;
    [SerializeField] private KeyCode _drawKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode _hideKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode _headflashKey = KeyCode.L;
    [SerializeField] private KeyCode _nvgOnKey = KeyCode.N;
    [SerializeField] private KeyCode _nvgOffKey = KeyCode.B;
    [SerializeField] private KeyCode _idleKey = KeyCode.I;
    [SerializeField] private KeyCode _walkKey = KeyCode.X;
    [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode _sprintStartKey = KeyCode.Q;
    [SerializeField] private KeyCode _sprintEndKey = KeyCode.E;

    [Header("Weapon State")]
    [SerializeField] private KeyCode _normalConditionKey = KeyCode.F1;
    [SerializeField] private KeyCode _emptyConditionKey = KeyCode.F2;
    [SerializeField] private KeyCode _jammedConditionKey = KeyCode.F3;

    private void Awake()
    {
        if (_weaponController == null)
        {
            _weaponController = GetComponent<FirstPersonWeaponController>();
        }
    }

    private void Update()
    {
        if (_weaponController == null)
        {
            return;
        }

        HandleConditionInput();
        HandleActionInput();
    }

    private void HandleConditionInput()
    {
        if (Input.GetKeyDown(_normalConditionKey))
        {
            _weaponController.SetCondition(WeaponCondition.Normal);
        }

        if (Input.GetKeyDown(_emptyConditionKey))
        {
            _weaponController.SetCondition(WeaponCondition.Empty);
        }

        if (Input.GetKeyDown(_jammedConditionKey))
        {
            _weaponController.SetCondition(WeaponCondition.Jammed);
        }
    }

    private void HandleActionInput()
    {
        if (Input.GetKeyDown(_shootKey))
        {
            _weaponController.Shoot();
        }

        if (Input.GetKeyDown(_shootLastKey))
        {
            _weaponController.Shoot(true);
        }

        if (Input.GetKeyDown(_reloadKey))
        {
            _weaponController.Reload();
        }

        if (Input.GetKeyDown(_reloadFullKey))
        {
            _weaponController.Reload(true);
        }

        if (Input.GetKeyDown(_misfireKey))
        {
            _weaponController.PlayMisfire();
        }

        if (Input.GetKeyDown(_revivalKey))
        {
            _weaponController.PlayRevival();
        }

        if (Input.GetKeyDown(_revivalLastKey))
        {
            _weaponController.PlayRevival(true);
        }

        if (Input.GetKeyDown(_drawKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.Draw);
        }

        if (Input.GetKeyDown(_hideKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.Hide);
        }

        if (Input.GetKeyDown(_headflashKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.Headflash);
        }

        if (Input.GetKeyDown(_nvgOnKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.NvgOn);
        }

        if (Input.GetKeyDown(_nvgOffKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.NvgOff);
        }

        if (Input.GetKeyDown(_idleKey))
        {
            _weaponController.PlayIdle();
        }

        if (Input.GetKeyDown(_walkKey))
        {
            _weaponController.PlayWalk();
        }

        if (Input.GetKeyDown(_sprintKey))
        {
            _weaponController.PlaySprint();
        }

        if (Input.GetKeyDown(_sprintStartKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.SprintStart);
        }

        if (Input.GetKeyDown(_sprintEndKey))
        {
            _weaponController.Play(FirstPersonWeaponAnimationKey.SprintEnd);
        }
    }
}
