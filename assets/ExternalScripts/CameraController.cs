using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 _Offset;
    public Quaternion _CameraAngle;
    [SerializeField] private float _MaxDistanceBeforeMoving;
    [SerializeField] private float _SmoothTime;
    [SerializeField] private KeyCode[] _CamMoveKeys = new KeyCode[2];
    [SerializeField] private KeyCode _CamResetKey;
    [SerializeField] private float _CamMoveAmount;
    [SerializeField] private float _CamMoveSpeed;
    [SerializeField] private float _CamMoveDuration;
    [SerializeField] private AnimationCurve _CamMoveCurve;
    [SerializeField] private GameObject _DummyCam;
    public Transform _DummyCamTransform;

    [Header("Objects/Transforms")]
    [SerializeField] private GameObject _Camera;
    [SerializeField] private GameObject _orientereo;
    public Transform _PlayerTransform;
    public Vector3 currentCamPos;

    private Vector3 _velocity = Vector3.zero;
    private bool _isMoving = false;
    private bool _camMoveing = false;
    private float _MoveSpeed = 0.1f;

    private Quaternion _camNormalRot;
    public static CameraController instance;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        _camNormalRot = transform.rotation;
        ResetCam();
        _isMoving = true;
    }

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, _PlayerTransform.position);
        _MoveSpeed *= distance / _SmoothTime;
        if (distance > _MaxDistanceBeforeMoving)
        {
            _isMoving = true;
        }
        if (_isMoving)
        {
            Vector3 targetPos = _PlayerTransform.position + new Vector3(0, _Offset.y, 0);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _SmoothTime, _MoveSpeed);
            _DummyCam.transform.position = targetPos;

            if (_isMoving && distance < _MaxDistanceBeforeMoving + 0.1f)
            {
                _isMoving = false;
            }
        }
        //move cam
        if ((Input.GetKeyDown(_CamMoveKeys[0]) || Input.GetKeyDown(_CamMoveKeys[1])) && !_camMoveing)
        {
            int direction = (Input.GetKeyDown(_CamMoveKeys[0]) ? 1 : (Input.GetKeyDown(_CamMoveKeys[1]) ? -1 : 0));
            StartCoroutine(MoveCam(direction));
        }
        if (Input.GetKeyDown(_CamResetKey))
        {
            StartCoroutine(ReturnToOriginalPos());
        }
    }
    private IEnumerator MoveCam(int pDirection, GameObject pObject = default)
    {
        _camMoveing = true;

        GameObject obj = pObject == default ? gameObject : pObject;
        Quaternion currentRot = transform.rotation;
        Quaternion targetRot =  Quaternion.Euler
 (transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + _CamMoveAmount * pDirection, transform.rotation.eulerAngles.z);
        Quaternion dummyTargetRot = Quaternion.Euler
 (_DummyCam.transform.rotation.eulerAngles.x, _DummyCam.transform.rotation.eulerAngles.y + _CamMoveAmount * pDirection, _DummyCam.transform.rotation.eulerAngles.z);
        float t = 0;
        while (t < 1)
        {
            t += _CamMoveSpeed * Time.deltaTime;

            float curveValue = _CamMoveCurve.Evaluate(t);
            Quaternion rotation = Quaternion.Lerp(currentRot, targetRot, curveValue);
            Quaternion dummyRot = Quaternion.Lerp(_DummyCam.transform.rotation, dummyTargetRot, curveValue);
            _DummyCam.transform.rotation = dummyTargetRot;
            obj.transform.rotation = rotation;
            yield return null;
        }
        _camMoveing = false;
    }
    private void ResetCam()
    {
        transform.position = _PlayerTransform.position + new Vector3(0, _Offset.y, 0);
        _DummyCam.transform.position = _PlayerTransform.position + new Vector3(0, _Offset.y, 0);
        _DummyCamTransform.position = _PlayerTransform.position + _Offset;
        transform.forward = -_PlayerTransform.right;
        _Camera.transform.position = _PlayerTransform.position + _Offset;
        _Camera.transform.rotation = Quaternion.Euler(_CameraAngle.eulerAngles.x, 0, _CameraAngle.eulerAngles.z);
        _Camera.transform.localRotation = Quaternion.Euler(_CameraAngle.eulerAngles.x, transform.rotation.y, _CameraAngle.eulerAngles.z);
        StartCoroutine(ReturnToOriginalPos());
    }
    private IEnumerator ReturnToOriginalPos()
    {
        Quaternion currentRot = transform.rotation;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            Quaternion rotation = Quaternion.Lerp(currentRot, _camNormalRot, 1f * Time.deltaTime);
            transform.rotation = rotation;
            yield return null;
        }
        yield return null;
    }
}
