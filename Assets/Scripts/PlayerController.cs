using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private CharacterController _cc;
    private NetworkTransform _netTransform;

    private Transform camTransform;
    // public Transform PlayerBody;

    private Vector3 _velocity = Vector3.zero;

    public float Speed = 7f;

    private float camYRotation = 0;
    public float Sensitivity = 1;

    private NetworkVariable<Vector3> _lastPos = new NetworkVariable<Vector3>();

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _netTransform = GetComponent<NetworkTransform>();

        camTransform = GetComponentInChildren<Camera>().transform;



    }



    public override void OnNetworkSpawn()
    {

        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            camTransform.GetComponent<Camera>().enabled = false; // dont need cam for non local player
            camTransform.GetComponent<AudioListener>().enabled = false;
        }

        if (IsOwner) SetLastPosServerRPC(transform.position);
        // SyncScaleServerRPC(Vector3.one);
    }

    // Start is called before the first frame update
    void Start()
    {

    }




    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;

        Vector2 mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        transform.Rotate(0, mouseInput.x * Sensitivity, 0);

        camYRotation -= mouseInput.y * Sensitivity;

        camYRotation = Mathf.Clamp(camYRotation, -80, 80);

        camTransform.localRotation = Quaternion.Euler(camYRotation, 0, 0);


        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        Vector3 inputAppliedToRotation = transform.forward * input.z + transform.right * input.x;

        Vector3 PlayerMovementDirection = inputAppliedToRotation.normalized * Speed * Time.deltaTime;

        _cc.Move(PlayerMovementDirection);

        VerifyLegalMoveServerRPC(transform.position);
        // _netTransform.
    }

    /// <summary>
    /// Sets the last pos so the server does not freak out.
    /// </summary>
    /// <param name="pos">The current pos of the player on the client.</param>
    [Rpc(SendTo.Server)]
    private void SetLastPosServerRPC(Vector3 pos)
    {
        _lastPos.Value = pos;
    }


    [Rpc(SendTo.Server)]
    private void VerifyLegalMoveServerRPC(Vector3 pos)
    {
        // TODO remove the yVel and calc separately.
        // float yVel = pos.y - _lastPos.Value.y;



        if (Vector3.Distance(_lastPos.Value, pos) > Speed * (Time.deltaTime * 2f) + 0.5f)
        {
            // correct the move.
            // TODO time.deltaTime * 2 needs to be replaced with ping delay from server to client and client to server to client
            Vector3 newPos = _lastPos.Value + (pos - _lastPos.Value).normalized * Speed * (Time.deltaTime * 2f);
            CorrectMoveOwnerRPC(newPos);
            _lastPos.Value = newPos;
        }
        else
        {
            _lastPos.Value = pos;

        }

    }

    [Rpc(SendTo.Owner)]
    private void CorrectMoveOwnerRPC(Vector3 pos)
    {
        print("Move corrected");
        transform.position = pos;
    }

    [Rpc(SendTo.Owner)]
    public void SetLocationOwnerRPC(Vector3 pos)
    {
        print("Move corrected");
        _cc.enabled = false;
        transform.position = pos;
        SetLastPosServerRPC(pos);

        _cc.enabled = true;

    }
}
