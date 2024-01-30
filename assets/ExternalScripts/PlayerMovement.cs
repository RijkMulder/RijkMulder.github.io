using System.Collections;using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private CharacterController _Controller;
    [SerializeField] private GameObject _CameraRig;
    [SerializeField] private GameObject _Wheel;
    [SerializeField] private GameObject _MiddleBody;
    [SerializeField] private GameObject _GunHolder;
    public GameObject _PlayerUpperBody;

    [Header("Settings")]
    [SerializeField] private float _Speed;
    [SerializeField] private float _MoveSmoothSpeed;
    [SerializeField] private LayerMask _GroundLayer;
    [SerializeField] private float _WheelRotationSpeed;

    [Header("Dash")]
    [SerializeField] private AnimationCurve _SpeedCurve;
    public KeyCode _DashKey;
    [SerializeField] private float _DashTime;
    [SerializeField] private float _DashSpeed;
    public float _DashCooldown;
    private Transform _dashOrienter;
    public bool _dashing;

    private Vector3 _move;
    private Vector3 currentMoveVelocity;
    private Vector3 moveDampVelocity;
    private Quaternion currentRotation;
    private float currentZ;

    public static PlayerMovement instance;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        _dashOrienter = _Wheel.transform.GetChild(0);
    }
    void Update()
    {
        //Look at mousePos
        SetPlayerOrientation(_PlayerUpperBody);

        //dash orienter correct rotation
        _dashOrienter.transform.rotation = Quaternion.Euler(_Wheel.transform.rotation.x, _Wheel.transform.rotation.y, 0);

        //movement
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        GameObject camera = _CameraRig.transform.GetChild(0).gameObject;  
        GameObject orienter = camera.transform.GetChild(0).gameObject;
        orienter.transform.localRotation = Quaternion.Euler(-camera.transform.eulerAngles.x, 0, 0);
        _move = orienter.transform.right * x + orienter.transform.forward * z;
        if (_move.magnitude > 0.3f)
        {
            _move.Normalize();
            currentZ = _Wheel.transform.eulerAngles.z;
        }
        Debug.Log(_move.magnitude);

        currentMoveVelocity = Vector3.SmoothDamp(currentMoveVelocity, _move * _Speed, ref moveDampVelocity, _MoveSmoothSpeed);

        _Controller.Move(Vector3.down * 2 * Time.deltaTime);

        //only move if grounded
        if (_Controller.isGrounded)
        {
            _Controller.Move(currentMoveVelocity * Time.deltaTime);
        }

        //set rotation of wheel
        Quaternion targetRotation = Quaternion.LookRotation(new Vector3(_move.z, 0, -_move.x));
        currentRotation = Quaternion.Lerp(currentRotation, targetRotation, _WheelRotationSpeed * Time.deltaTime);
        currentRotation.z = 0;
        SetWheelOrientation(_Wheel, currentRotation, Quaternion.Euler(0, 0, currentZ + _WheelRotationSpeed));

        //dash
        if (Input.GetKeyDown(_DashKey) && !_dashing)
        {
            StartCoroutine(Dash());
        }
    }
    private void SetPlayerOrientation(GameObject pPlayerObject)
    {
        Vector2 mousePos = Input.mousePosition;
        Vector3 mousePosWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.transform.position.y));

        // if the ground position is hit, adjust mousePosWorld to it.
        RaycastHit hit;
        if (Physics.Raycast(mousePosWorld, Vector3.down, out hit, 100f, _GroundLayer))
        {
            mousePosWorld = hit.point;
        }

        //make player look at mouse pos
        mousePosWorld.y = transform.position.y;
        pPlayerObject.transform.LookAt(mousePosWorld);
        pPlayerObject.transform.Rotate(0, 90, 0);
    }
    private void SetWheelOrientation(GameObject pWheelObject, Quaternion pWheelRotation, Quaternion pMIddleRotation)
    {
        _MiddleBody.transform.rotation = pWheelRotation;
        pWheelObject.transform.rotation = pWheelRotation * pMIddleRotation;
    }
    private IEnumerator Dash()
    {
        _dashing = true;
        Vector3 direction = new Vector3(_dashOrienter.transform.up.x, 0, _dashOrienter.transform.up.z);
        float currentPower = 0f;

        float currentSpeed = 1f;
        while (currentPower < _DashTime)
        {
            currentPower += Time.deltaTime;
            currentSpeed += _SpeedCurve.Evaluate(currentSpeed / _DashSpeed) / currentPower * _DashSpeed * Time.deltaTime;
            _Controller.Move(direction * currentSpeed * Time.deltaTime);
            GetComponent<UiScript>()._dashing = true;
            yield return null;
        }
        yield return new WaitForSeconds(_DashCooldown);
        _dashing = false;
    }
}
