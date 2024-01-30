using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.Assertions.Must;

public class PlayerShoot : MonoBehaviour
{
    [Header("Settings")]
    public int _BulletAmount;
    public int _ReserveBulletAmount;
    [SerializeField] private int _MaxBulletAmount;
    [SerializeField] private float _ReloadTime;
    [SerializeField] private LayerMask _EnemyLayer;
    [SerializeField] private LayerMask _GroundLayer;
    [SerializeField] private GameObject _BulletPrefab;
    [SerializeField] private Vector2 _SpreadRotationMultiplier;
    [SerializeField] private GameObject _UpperBody;
    [SerializeField] private GameObject _MuzzleFlash;
    [SerializeField] private AnimationCurve _ShakeCurve;
    public int _MaxUltimatePoints;
    [SerializeField] private GameObject _GrenadeLauncher;
    public GameObject[] _Guns;
    public Transform[] _ShootTransforms;
    public GameObject _GunHolder;
    public WeaponShootBehaviours _WeaponType;
    public PlayerInfo _PlayerInfo;

    [Header("KeyCodes")]
    [SerializeField] private KeyCode _ShootKey;
    [SerializeField] private KeyCode _ReloadKey;
    [SerializeField] private KeyCode _UltimateActivate;

    private bool _reloading;
    private bool _shooting;
    private Cheats _cheats;
    private WeaponSwitch _weaponSwitch;
    private Animator _anim;
    private GameObject _rayPlane;
    private GameManager _gameManager;

    public static PlayerShoot instance;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        _gameManager = GameManager.instance;
        _GrenadeLauncher.SetActive(false);
        _cheats = Cheats.instance;
        _weaponSwitch = WeaponSwitch.instance;
        _rayPlane = GameObject.Find("RayPlane");
        _anim = GetComponent<Animator>();

        _weaponSwitch.Pistol();
        //look for weapon type
        LookForWeaponType();
    }

    private void Update()
    {
        Debug.Log("Bullets: " + _BulletAmount + " Reserve: " + _ReserveBulletAmount + " Reloading: " + _reloading);
        _BulletPrefab = _WeaponType.Bullet;

        if (_WeaponType != null)
        {
            SetShootRotation();
        }

        //cheats
        if (_cheats.CheatsActive)
        {
            //max bullets
            _BulletAmount = _MaxBulletAmount;
            //hold to shoot
            if (Input.GetKey(_ShootKey) && _BulletAmount > 0)
            {
                StartCoroutine(Shoot(SetShootRotation()));
            }
        }

        //shoot
        if (Input.GetKey(_ShootKey) && _BulletAmount > 0 && _WeaponType != null && !_shooting && !_reloading)
        {
            StartCoroutine(Shoot(SetShootRotation()));
            _anim.SetBool("IsShooting", true);
        }
        else if (Input.GetKeyDown(_ShootKey) && _BulletAmount <= 0 && !_reloading && _WeaponType != null)
        {
            StartCoroutine(Reload());
            _anim.SetBool("ShootingDone", true);
        }
        else if (Input.GetKeyUp(_ShootKey) || _BulletAmount == 0)
        {
            _anim.SetBool("IsShooting", false);
            _anim.SetBool("ShootingDone", true);
        }

        //Reload
        if (Input.GetKeyDown(_ReloadKey) && !_reloading && _BulletAmount < _MaxBulletAmount)
        {
            StartCoroutine(Reload());
        }

        //gun holder
        if (_GunHolder.transform.childCount == 0)
        {
            if (_WeaponType != null)
            {
                SpawnGunModel(_WeaponType.GunModel);
            }
        }

        //ultimate
        if (_PlayerInfo._ultimatePoints >= _MaxUltimatePoints && Input.GetKey(_UltimateActivate))
        {
            ActivateUltimate();
        }
    }
    private Quaternion SetShootRotation()
    {
        _rayPlane.transform.position = new Vector3(_rayPlane.transform.position.x, _GunHolder.transform.position.y, _rayPlane.transform.position.z);
        Vector2 mousePos = Input.mousePosition;
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _GroundLayer))
        {
            Vector3 targetPos = hit.point - _GunHolder.transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(targetPos, Vector3.up);
            targetRotation.x = 0;
            targetRotation.z = 0;
            return targetRotation;
        }
        return Quaternion.identity;
    }
    private IEnumerator Shoot(Quaternion rotation)
    {
        _shooting = true;

        //shake cam
        _gameManager.CameraShake(0.06f);

        //normal weaponns
        if (!_WeaponType.IsSpecialWeapon)
        {
            Quaternion rot = _GunHolder.transform.rotation;
            //single shot
            if (_WeaponType.BulletAmount == 1 && !_WeaponType.BurstFireWeapon)
            {
                GameObject flash = Instantiate(_MuzzleFlash, _GunHolder.transform.position, rot * Quaternion.Euler(0, -90, 0));
                Destroy(flash, 0.1f);
                if (_WeaponType.AmmoType == WeaponShootBehaviours.BulletType.pistol)
                {
                    FindObjectOfType<Audio>().PlayPistolShot();
                }
                else
                {
                    FindObjectOfType<Audio>().PlayARShot();
                }
                GameObject bullet = Instantiate(_BulletPrefab, _GunHolder.transform.position, rotation);
                _BulletAmount--;
            }

            //sahotgun shot
            else if (_WeaponType.BulletAmount > 1)
            {
                Tuple<Vector3[], Quaternion[]> result = MakeBulletSpread(_WeaponType.BulletAmount, _WeaponType.BulletSpreadAmount, _GunHolder.transform.position, rotation);
                Vector3[] spreadPositions = result.Item1;
                Quaternion[] spreadRotations = result.Item2;

                for (int i = 0; i < _WeaponType.BulletAmount; i++)
                {
                    GameObject bullet = Instantiate(_BulletPrefab, spreadPositions[i], spreadRotations[i]);
                }
                FindObjectOfType<Audio>().PlayShotgunShot();
                yield return new WaitForSeconds(0.05f);
                GameObject flash = Instantiate(_MuzzleFlash, _GunHolder.transform.position, rot * Quaternion.Euler(0, 0, 0));
                Destroy(flash, 0.1f);
                _BulletAmount--;
            }

            //burst shot
            else if (_WeaponType.BurstFireWeapon)
            {
                for (int i = 0; i < _WeaponType.BurstFireShotAmount; i++)
                {
                    GameObject bullet = Instantiate(_BulletPrefab, _GunHolder.transform.position, rotation);
                    yield return new WaitForSeconds(_WeaponType.BurstTimeBetweenShot);
                    _BulletAmount--;
                }
            }
        }

        //special weapons
        else if (_WeaponType.IsSpecialWeapon)
        {
            //Minigun
            if (_WeaponType.AmmoType == WeaponShootBehaviours.BulletType.Minigun)
            {
                GameObject bullet = Instantiate(_BulletPrefab, _GunHolder.transform.position, rotation);
                Action actionDelegate = _PlayerInfo._currentUltimateBullets > 0 ? (() => _PlayerInfo._currentUltimateBullets--) : (_weaponSwitch.Pistol);
                actionDelegate.Invoke();
            }
        }

        UpdatePlayerInfo(_ReserveBulletAmount, _BulletAmount);
        yield return new WaitForSeconds(_WeaponType.TimeBetweenShots);
        _shooting = false;
    }
    private IEnumerator Reload()
    {
        _reloading = true;
        _anim.SetBool("IsReloading ", true);
        if (_ReserveBulletAmount > 0)
        {
            //reload full ammount and update playerinfo
            if (_ReserveBulletAmount - _MaxBulletAmount >= 0)
            {
                int reserve = _ReserveBulletAmount -= _MaxBulletAmount - _BulletAmount;
                int amount = _MaxBulletAmount;
                UpdatePlayerInfo(reserve, amount);

                yield return new WaitForSeconds(_WeaponType.ReloadSpeed);
                _ReserveBulletAmount -= _MaxBulletAmount - _BulletAmount;
                _BulletAmount = _MaxBulletAmount;
            }

            //reload non full amount and update playerinfo
            else if (_ReserveBulletAmount - _MaxBulletAmount < 0)
            {
                int amount = _BulletAmount + _ReserveBulletAmount;
                amount = Mathf.Clamp(amount, 0, _MaxBulletAmount);
                UpdatePlayerInfo( _ReserveBulletAmount - (_MaxBulletAmount - _BulletAmount), amount);

                yield return new WaitForSeconds(_WeaponType.ReloadSpeed);
                _BulletAmount = _ReserveBulletAmount;
                _ReserveBulletAmount = 0;
            }
            Debug.Log("Shouldnt come here");
            _reloading = false;
        }
        else
        {
            _reloading = false;
            yield return null;
        }
        _anim.SetBool("IsReloading ", false);
    }
    public void LookForWeaponType()
    {
        if (_WeaponType != null && _WeaponType.AmmoType == WeaponShootBehaviours.BulletType.pistol)
        {
            _MaxBulletAmount = _PlayerInfo._maxPistolAmmoClip;

            int bulletAmount = (_PlayerInfo._pistolAmmo == _MaxBulletAmount) ? _MaxBulletAmount : _PlayerInfo._pistolAmmo;
            _BulletAmount = bulletAmount;
            _ReserveBulletAmount = _PlayerInfo._reservePistolAmmo;
        }
        else if (_WeaponType != null && _WeaponType.AmmoType == WeaponShootBehaviours.BulletType.shotgun)
        {
            _MaxBulletAmount = _PlayerInfo._maxShotgunAmmoClip;

            int bulletAmount = (_PlayerInfo._shotgunAmmo == _MaxBulletAmount) ? _MaxBulletAmount : _PlayerInfo._shotgunAmmo;
            _BulletAmount = bulletAmount;
            _ReserveBulletAmount = _PlayerInfo._reserveShotgunAmmo;
        }
        else if (_WeaponType != null && _WeaponType.AmmoType == WeaponShootBehaviours.BulletType.AR)
        {
            _MaxBulletAmount = _PlayerInfo._maxARAmmoClip;

            int bulletAmount = (_PlayerInfo._arAmmo == _MaxBulletAmount) ? _MaxBulletAmount : _PlayerInfo._arAmmo;
            _BulletAmount = bulletAmount;
            _ReserveBulletAmount = _PlayerInfo._reserveARAmmo;
        }
    }
    private Tuple<Vector3[], Quaternion[]> MakeBulletSpread(int pBulletAmountPerShot, float pSpreadAmount, Vector3 pOriginPoint, Quaternion pOriginRotation)
    {
        //positions
        List<Vector3> resultPos = new List<Vector3>();
        for (int i = 0; i < pBulletAmountPerShot; i++)
        {
            Vector3 randomPos = pOriginPoint + new Vector3(Random.Range(-pSpreadAmount, pSpreadAmount),
                Random.Range(-pSpreadAmount, pSpreadAmount), 0);
            resultPos.Add(randomPos);
        }

        //rotations
        List<Quaternion> resultRot = new List<Quaternion>();
        for (int i = 0; i < pBulletAmountPerShot; i++)
        {
            Quaternion randomRotation = pOriginRotation * Quaternion.Euler(Random.Range(-pSpreadAmount * _SpreadRotationMultiplier.x, pSpreadAmount * _SpreadRotationMultiplier.x),
                Random.Range(-pSpreadAmount * _SpreadRotationMultiplier.y, pSpreadAmount * _SpreadRotationMultiplier.y), 0);
            resultRot.Add(randomRotation);
        }
        return Tuple.Create(resultPos.ToArray(), resultRot.ToArray());
    }
    private void UpdatePlayerInfo(int reserve, int amount)
    {
        //update pistol ammo
        if (_WeaponType.AmmoType == WeaponShootBehaviours.BulletType.pistol)
        {
            _PlayerInfo._pistolAmmo = amount;
            _PlayerInfo._reservePistolAmmo = reserve;
        }

        //update shotgun ammo
        if (_WeaponType.AmmoType == WeaponShootBehaviours.BulletType.shotgun)
        {
            _PlayerInfo._shotgunAmmo = amount;
            _PlayerInfo._reserveShotgunAmmo = reserve;
        }
        //update pistol ammo
        if (_WeaponType.AmmoType == WeaponShootBehaviours.BulletType.AR)
        {
            _PlayerInfo._arAmmo = amount;
            _PlayerInfo._reserveARAmmo = reserve;
        }
    }
    private void SpawnGunModel(GameObject pGunModel)
    {
        GameObject gunModel = Instantiate(pGunModel, _GunHolder.transform.position, _UpperBody.transform.localRotation * Quaternion.Euler(0, -90, 0));
        gunModel.transform.parent = _GunHolder.transform;
    }
    public void DeleteGunModel()
    {
        if (_GunHolder.transform.childCount > 0)
            Destroy(_GunHolder.transform.GetChild(0).gameObject);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "AmmoBox")
        {
            _PlayerInfo._pistolAmmo += 12;
            _PlayerInfo._shotgunAmmo += 8;
            _PlayerInfo._arAmmo += 30;
        }
    }
    private void ActivateUltimate()
    {
        _PlayerInfo._ultimatePoints = 0;
        _GrenadeLauncher.SetActive(true);
    }
}
