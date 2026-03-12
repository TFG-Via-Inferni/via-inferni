using UnityEngine;

/// <summary>
/// Colócalo en el hijo "RoomTriggerEnter" de cada Room.
/// Solo notifica al controlador de cámara; no toca enemigos ni puertas.
/// </summary>
public class RoomCameraTrigger : MonoBehaviour
{
    private Room parentRoom;

    void Start()
    {
        parentRoom = GetComponentInParent<Room>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        if (parentRoom != null && RoomCameraController.Instance != null)
        {
            RoomCameraController.Instance.OnPlayerEnteredRoom(parentRoom);
        }
    }
}
