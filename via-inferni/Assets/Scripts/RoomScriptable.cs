using UnityEngine;

[CreateAssetMenu(fileName = "Room", menuName = "Scriptable Objects/Room")]
public class RoomScriptable : ScriptableObject
{
    [Header("Room Configuration")]
    public RoomType roomType;
    public RoomShape roomShape;

    [Header("Grid Ocupation")]
    public int[] occupiedTiles;
    
    [Header("Visual Variations")]
    [Tooltip("Añade aquí múltiples prefabs de habitaciones con la misma forma pero diferente contenido (piedras, obstáculos, etc.)")]
    public GameObject[] roomVariations;
}
