using UnityEngine;

[CreateAssetMenu(fileName = "Door", menuName = "Scriptable Objects/Door")]
public class DoorScriptable : ScriptableObject
{
    public RoomType roomType;
    public GameObject upDoor;
    public GameObject downDoor;
    public GameObject leftDoor;
    public GameObject rightDoor;
    
    [Header("Walls")]
    public GameObject upWall;
    public GameObject downWall;
    public GameObject leftWall;
    public GameObject rightWall;
}
