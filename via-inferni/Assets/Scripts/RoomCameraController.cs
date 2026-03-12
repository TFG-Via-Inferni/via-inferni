using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public static RoomCameraController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineConfiner2D confiner2D;

    private Transform playerTarget;
    private Room currentRoom;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterPlayer(Transform player)
    {
        playerTarget = player;
    }

    public void SnapToPlayerRoom()
    {
        if (playerTarget == null || RoomManager.instance == null)
        {
            return;
        }

        Room room = RoomManager.instance.GetRoomContainingPoint(playerTarget.position);
        if (room != null)
        {
            ForceRoom(room);
        }
    }

    public IEnumerator SnapToPlayerRoomNextFrame()
    {
        yield return null; // espera un frame para que la física registre los colliders
        SnapToPlayerRoom();
    }

    // Igual que OnPlayerEnteredRoom pero sin el guard de ContainsPoint (uso interno para init)
    private void ForceRoom(Room room)
    {
        if (room == null)
        {
            return;
        }

        ApplyConfiner(room.CameraBoundsCollider);

        Transform desiredTarget = room.RequiresFixedCamera ? room.CameraCenterTarget : playerTarget;
        if (desiredTarget != null && cinemachineCamera != null)
        {
            cinemachineCamera.Follow = desiredTarget;
        }

        currentRoom = room;
    }

    public void OnPlayerEnteredRoom(Room room)
    {
        if (room == null || room == currentRoom)
        {
            return;
        }
        ApplyConfiner(room.CameraBoundsCollider);

        Transform desiredTarget = room.RequiresFixedCamera ? room.CameraCenterTarget : playerTarget;
        if (desiredTarget != null && cinemachineCamera != null)
        {
            cinemachineCamera.Follow = desiredTarget;
        }

        currentRoom = room;
    }

    private void ApplyConfiner(Collider2D roomBounds)
    {
        if (confiner2D == null || roomBounds == null)
        {
            return;
        }

        if (confiner2D.BoundingShape2D != roomBounds)
        {
            confiner2D.BoundingShape2D = roomBounds;
            confiner2D.InvalidateBoundingShapeCache();
        }
    }
}