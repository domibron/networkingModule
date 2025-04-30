using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private CharacterController _cc;
    private NetworkTransform _netTransform;

    private Transform _camTransform;

    public float Speed = 7f;

    private float _camYRotation = 0;
    public float Sensitivity = 1;

    private NetworkVariable<Vector3> _lastPos = new NetworkVariable<Vector3>();

    public float CSGasEffectDuration = 5f;

    public NetworkVariable<float> CSGasEffectTimer = new NetworkVariable<float>(0);

    private Coroutine _tickDamageCoroutine;

    private Health _health;

    public float TickDamageAmount = 5f;

    private Vector3 _velocity;

    public float JumpHeight = 3f;

    public float Gravity = -(9.81f * 2f);

    private bool _isGrounded;

    private AudioLowPassFilter _lowPassFilter;

    private Animator _animator;

    public AudioClip FootStep;
    private AudioSource _audioSource;

    public float StepRate = 2f;

    private float _stepTimer = 0.1f;

    #region Awake
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _netTransform = GetComponent<NetworkTransform>();

        _camTransform = GetComponentInChildren<Camera>().transform;
        _lowPassFilter = _camTransform.GetComponent<AudioLowPassFilter>();
        _health = GetComponent<Health>();

        _animator = GetComponentInChildren<Animator>();

        _audioSource = GetComponent<AudioSource>();
    }
    #endregion

    #region OnNetworkSpawn
    public override void OnNetworkSpawn()
    {

        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            _camTransform.GetComponent<Camera>().enabled = false; // dont need cam for non local player
            _camTransform.GetComponent<AudioListener>().enabled = false;
        }

        if (IsOwner) SetLastPosServerRPC(transform.position);
        // SyncScaleServerRPC(Vector3.one);
    }
    #endregion

    #region Update
    // Update is called once per frame
    void Update()
    {
        if (IsHost || IsServer) SortPlayerStuffOnServer();

        if (!IsOwner) return;
        HandleLook();
        HandleMovement();
        HandleGroundCheck();
        HandleGravity();
        HandleJumping();


        _cc.Move(_velocity * Time.deltaTime);


        // _netTransform.

        _lowPassFilter.enabled = CSGasEffectTimer.Value > 1f;

    }
    #endregion

    #region HandleGroundCheck
    private void HandleGroundCheck()
    {
        // ~((1 << 7) | (1 << 8)) ignore player and body layers.
        _isGrounded = Physics.Raycast(transform.position, -transform.up, 1.1f, ~((1 << 7) | (1 << 8)));
    }
    #endregion

    #region HandleGravity
    private void HandleGravity()
    {
        if (_isGrounded && _velocity.y < -2)
        {
            _velocity.y = -2f;
        }
        else
        {
            _velocity.y += Gravity * Time.deltaTime;
        }
    }
    #endregion

    #region HandleJumping
    private void HandleJumping()
    {
        if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
        {
            _velocity.y = Mathf.Sqrt(JumpHeight * -2f * Gravity);
            _animator.SetTrigger("Jump");
        }
    }
    #endregion

    #region HandleMovement
    private void HandleMovement()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        Vector3 inputAppliedToRotation = transform.forward * input.z + transform.right * input.x;

        Vector3 PlayerMovementDirection = inputAppliedToRotation.normalized * Speed * Time.deltaTime;

        _cc.Move(PlayerMovementDirection);

        _animator.SetFloat("XVel", input.x);
        _animator.SetFloat("ZVel", input.z);

        if (_stepTimer > 0f && input.normalized.magnitude > 0 && _isGrounded) _stepTimer -= Time.deltaTime;
        else if (_stepTimer <= 0f && _isGrounded)
        {
            _stepTimer += 1 / StepRate;
            _audioSource.PlayOneShot(FootStep, 0.8f);
        }


        VerifyLegalMoveServerRPC(transform.position, _velocity);
    }
    #endregion

    #region HandleLook
    private void HandleLook()
    {
        Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        transform.Rotate(0, mouseInput.x * Sensitivity, 0);

        _camYRotation -= mouseInput.y * Sensitivity;

        _camYRotation = Mathf.Clamp(_camYRotation, -80, 80);

        _camTransform.localRotation = Quaternion.Euler(_camYRotation, 0, 0);
    }
    #endregion

    #region SortPlayerStuffOnServer
    private void SortPlayerStuffOnServer()
    {
        ConfirmPositionToServerOwnerRPC(transform.position);

        if (CSGasEffectTimer.Value > 0)
        {
            CSGasEffectTimer.Value -= Time.deltaTime;

            TickDamage();
        }

    }
    #endregion

    #region ConfirmPositionToServerOwnerRPC
    [Rpc(SendTo.Owner)]
    private void ConfirmPositionToServerOwnerRPC(Vector3 serverPos)
    {
        Vector3 planeOfPos = transform.position;

        serverPos.y = 0;

        planeOfPos.y = 0;


        if (Vector3.Distance(planeOfPos, serverPos) > 2)
        {
            transform.position = new Vector3(serverPos.x, transform.position.y, serverPos.z);
            print("corrected bad position");
        }
    }
    #endregion

    #region TickDamage
    private void TickDamage()
    {
        if (_tickDamageCoroutine != null) return;

        _tickDamageCoroutine = StartCoroutine(TickEverySecond());
    }
    #endregion

    #region TickEverySecond
    private IEnumerator TickEverySecond()
    {
        _health.AddToHealth(-TickDamageAmount);
        yield return new WaitForSeconds(1);
        _tickDamageCoroutine = null;
    }
    #endregion

    #region SetLastPosServerRPC
    /// <summary>
    /// Sets the last pos so the server does not freak out.
    /// </summary>
    /// <param name="pos">The current pos of the player on the client.</param>
    [Rpc(SendTo.Server)]
    private void SetLastPosServerRPC(Vector3 pos)
    {
        _lastPos.Value = pos;
    }
    #endregion

    #region VerifyLegalMoveServerRPC
    [Rpc(SendTo.Server)]
    private void VerifyLegalMoveServerRPC(Vector3 pos, Vector3 velocity)
    {
        // TODO remove the yVel and calc separately.
        // float yVel = pos.y - _lastPos.Value.y;

        Vector3 lastPos = _lastPos.Value;

        lastPos.y = 0f;

        Vector3 currentPos = pos;

        currentPos.y = 0;

        // TODO do something with velocity


        if (Vector3.Distance(lastPos, currentPos) > Speed * (Time.deltaTime * 2f) + 0.5f)
        {
            // correct the move.
            // TODO time.deltaTime * 2 needs to be replaced with ping delay from server to client and client to server to client
            Vector3 newPos = lastPos + (currentPos - lastPos).normalized * Speed * (Time.deltaTime * 2f);

            newPos.y = pos.y;

            CorrectMoveOwnerRPC(newPos);
            _lastPos.Value = newPos;
        }
        else
        {
            _lastPos.Value = pos;

        }

    }
    #endregion

    #region GiveCSGasEffectServerRPC
    [Rpc(SendTo.Server)]
    public void GiveCSGasEffectServerRPC()
    {
        CSGasEffectTimer.Value = CSGasEffectDuration;
    }
    #endregion

    #region CorrectMoveOwnerRPC
    [Rpc(SendTo.Owner)]
    private void CorrectMoveOwnerRPC(Vector3 pos)
    {
        print("Move corrected");
        transform.position = pos;
    }
    #endregion

    #region SetLocationOwnerRPC
    [Rpc(SendTo.Owner)]
    public void SetLocationOwnerRPC(Vector3 pos)
    {
        print("Move corrected");
        _cc.enabled = false;
        transform.position = pos;
        SetLastPosServerRPC(pos);

        _cc.enabled = true;

    }
    #endregion
}
