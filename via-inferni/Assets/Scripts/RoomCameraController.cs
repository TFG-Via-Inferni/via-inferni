using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class RoomCameraController : MonoBehaviour
{
    public static RoomCameraController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private CinemachineConfiner2D confiner2D;

    [Header("Debug")]
    [SerializeField] private bool debugCameraTransitions = false;

    private Transform playerTarget;
    private Room currentRoom;
    private Transform desiredTarget;
    private Transform followProxy;
    private Transform fixedRoomAnchor;

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
        EnsureFollowProxy();

        if (playerTarget != null)
        {
            followProxy.position = playerTarget.position;
            desiredTarget = playerTarget;
        }

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = followProxy;
        }
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

        SwitchToRoom(room, true, "InitSnap");
    }

    public void OnPlayerEnteredRoom(Room room)
    {
        LogDebug($"[CAM][Trigger] frame={Time.frameCount} time={Time.time:F3} from={FormatRoom(currentRoom)} to={FormatRoom(room)} player={FormatPos(playerTarget)}");

        if (room == null || room == currentRoom)
        {
            LogDebug($"[CAM][Skip] frame={Time.frameCount} reason={(room == null ? "room-null" : "same-room")} current={FormatRoom(currentRoom)}");
            return;
        }

        SwitchToRoom(room, false, "RoomTriggerEnter");
    }

    private void LateUpdate()
    {
        if (followProxy == null || desiredTarget == null)
        {
            return;
        }

        followProxy.position = desiredTarget.position;
    }

    private void SwitchToRoom(Room room, bool instantProxySnap, string reason)
    {
        if (room == null)
        {
            return;
        }

        bool isFixedRoom = room.RequiresFixedCamera;

        LogDebug($"[CAM][Switch-Start] frame={Time.frameCount} reason={reason} current={FormatRoom(currentRoom)} next={FormatRoom(room)} fixed={(isFixedRoom ? "yes" : "no")}");

        EnsureFollowProxy();
        desiredTarget = isFixedRoom
            ? UpdateFixedRoomAnchor(room.CameraBoundsCollider, room)
            : playerTarget;

        // Snap previo en OneByOne para evitar un frame pegado al borde del confiner.
        if (isFixedRoom && desiredTarget != null)
        {
            followProxy.position = desiredTarget.position;
        }

        ApplyConfiner(room.CameraBoundsCollider);

        if (cinemachineCamera != null)
        {
            cinemachineCamera.Follow = followProxy;
        }

        if (desiredTarget != null && instantProxySnap && !isFixedRoom)
        {
            followProxy.position = desiredTarget.position;
        }

        currentRoom = room;

        LogDebug($"[CAM][Switch-End] frame={Time.frameCount} current={FormatRoom(currentRoom)} desired={FormatPos(desiredTarget)} proxy={FormatPos(followProxy)}");
    }

    private Transform UpdateFixedRoomAnchor(Collider2D roomBounds, Room room)
    {
        EnsureFixedRoomAnchor();

        Vector3 anchorPos = room != null ? room.transform.position : Vector3.zero;
        if (roomBounds != null)
        {
            anchorPos = roomBounds.bounds.center;
        }

        anchorPos.z = 0f;
        fixedRoomAnchor.position = anchorPos;

        LogDebug($"[CAM][FixedAnchor] frame={Time.frameCount} room={FormatRoom(room)} anchor={anchorPos}");

        return fixedRoomAnchor;
    }

    private void EnsureFollowProxy()
    {
        if (followProxy != null)
        {
            return;
        }

        var proxy = new GameObject("CameraFollowProxy");
        proxy.transform.SetParent(null);
        proxy.transform.position = Vector3.zero;
        followProxy = proxy.transform;
    }

    private void EnsureFixedRoomAnchor()
    {
        if (fixedRoomAnchor != null)
        {
            return;
        }

        var anchor = new GameObject("CameraFixedRoomAnchor");
        anchor.transform.SetParent(null);
        anchor.transform.position = Vector3.zero;
        fixedRoomAnchor = anchor.transform;
    }

    private void ApplyConfiner(Collider2D roomBounds)
    {
        if (confiner2D == null || roomBounds == null)
        {
            LogDebug($"[CAM][Confiner-Skip] frame={Time.frameCount} confiner-null={(confiner2D == null ? "yes" : "no")} bounds-null={(roomBounds == null ? "yes" : "no")}");
            return;
        }

        confiner2D.enabled = true;

        if (confiner2D.BoundingShape2D != roomBounds)
        {
            confiner2D.BoundingShape2D = roomBounds;
            confiner2D.InvalidateBoundingShapeCache();

            LogDebug($"[CAM][Confiner-Set] frame={Time.frameCount} bounds={roomBounds.name} center={roomBounds.bounds.center}");
        }
    }

    private void LogDebug(string message)
    {
        if (!debugCameraTransitions)
        {
            return;
        }

        Debug.Log(message);
    }

    private string FormatRoom(Room room)
    {
        if (room == null)
        {
            return "null";
        }

        return $"{room.name}@{room.transform.position}";
    }

    private string FormatPos(Transform t)
    {
        if (t == null)
        {
            return "null";
        }

        Vector3 p = t.position;
        return $"({p.x:F2},{p.y:F2},{p.z:F2})";
    }
}