using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    private Room parentRoom;

    void Start()
    {
        // Buscar el componente Room en el padre
        parentRoom = GetComponentInParent<Room>();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (parentRoom != null)
        {
            parentRoom.OnPlayerEnter(collision);
        }
    }
}
