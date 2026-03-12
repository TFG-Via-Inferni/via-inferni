using UnityEngine;

public class RoomTrigger : MonoBehaviour
{
    private Room parentRoom;

    void Start()
    {
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
