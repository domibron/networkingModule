using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    private CharacterController _cc;
    private AnticipatedNetworkTransform _antTransform;

    private Transform camTransform;

    private Vector3 _velocity = Vector3.zero;

    public float Speed = 7f;

    private float camYRotation = 0;
    public float Sensitivity = 1;

    private NetworkVariable<Vector3> _lastPos = new NetworkVariable<Vector3>();

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _antTransform = GetComponent<AnticipatedNetworkTransform>();

        camTransform = GetComponentInChildren<Camera>().transform;



    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            camTransform.GetComponent<Camera>().enabled = false; // dont need cam for non local player
        }

        SetPosServerRPC(transform.position);
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    [Rpc(SendTo.Server)]
    private void SetPosServerRPC(Vector3 pos)
    {
        _lastPos.Value = pos;
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
        _antTransform.AnticipateMove(transform.position);

        MovePlayerServerRPC(transform.position, Time.deltaTime);
    }

    [Rpc(SendTo.Server)]
    private void MovePlayerServerRPC(Vector3 newPosition, float deltaTime)
    {
        float yVel = newPosition.y; // we separate y because of gravity.
        Vector3 lastPosNoY = new Vector3(_lastPos.Value.x, 0, _lastPos.Value.z);

        //TODO deal with jumping and gravity checks

        newPosition.y = 0;

        Vector3 correctedMovement = Vector3.zero;


        float delta = Vector3.Distance(lastPosNoY, newPosition);

        if (delta > Speed * deltaTime + 0.05f)
        {
            // we correct the movement with the correct spacing. Dont want rubber banding to the 7th degree.
            correctedMovement = lastPosNoY + (newPosition - transform.position).normalized * Speed * deltaTime;

            correctedMovement.y = yVel;
            print(correctedMovement.y);

            _antTransform.SetState(correctedMovement);
            _lastPos.Value = correctedMovement;

            ResetPositionOwnerRPC(correctedMovement);

            print(correctedMovement);

        }
        else
        {
            newPosition.y = yVel;
            _antTransform.SetState(newPosition);
            _lastPos.Value = newPosition;
        }


    }

    [Rpc(SendTo.Owner)]
    private void ResetPositionOwnerRPC(Vector3 pos)
    {
        _antTransform.AnticipateMove(pos);
        print("corrected " + pos);

    }
}
