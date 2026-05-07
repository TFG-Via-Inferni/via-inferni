using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    private List<Room> createdRooms;

    [Header("Offset variables")]
    public float offsetX;
    public float offsetY;

    [Header("Prefab References")]
    public Room roomPrefab;
    public Door doorPrefab;
    public GameObject enemyPrefab;
    public GameObject shopPrefab;
    public GameObject treasurePrefab;
    public GameObject baptismalFontPrefab;

    [Header("Scriptable Obeject References ")]
    public DoorScriptable[] doors;
    public RoomScriptable[] rooms;

    [Header("Fallback Enemy Pool")]
    [Tooltip("Se usa solo si el circulo actual no tiene enemyPool configurado.")]
    public GameObject[] enemyPoolFallback;

    public static RoomManager instance;

    private void Awake()
    {
        instance = this;
        createdRooms = new List<Room>();
    }

    public void SetUpRooms(List<Cell> spawnedCells)
    {
        for(int i = createdRooms.Count - 1; i >= 0; i--)
        {
            Destroy(createdRooms[i].gameObject);
        }

        createdRooms.Clear();

        RoomScriptable[] activeRoomPool = GetActiveRoomPool();
        if (activeRoomPool.Length == 0)
        {
            Debug.LogWarning("No hay roomPool configurado para el circulo actual. No se instanciaron habitaciones.");
            return;
        }

        foreach (var currentCell in spawnedCells)
        {
            var foundRoom = activeRoomPool.FirstOrDefault(x => x.roomShape == currentCell.roomShape && x.roomType == currentCell.roomType && DoesTileMatchCell(x.occupiedTiles, currentCell));
            if (foundRoom == null)
            {
                continue;
            }
        
            var currentPosition = currentCell.transform.position;

            var convertedPosition = new Vector2(currentPosition.x * offsetX, currentPosition.y * offsetY);
        
            var spawnedRoom = Instantiate(roomPrefab, convertedPosition, Quaternion.identity);
        
            spawnedRoom.ConfigureEnemySpawnEntry(PickEnemyEntryForCell(currentCell));

            spawnedRoom.SetupRoom(currentCell, foundRoom);

            createdRooms.Add(spawnedRoom);
        }
    }

    private RoomScriptable[] GetActiveRoomPool()
    {
        CircleDefinition definition = CircleManager.instance != null
            ? CircleManager.instance.CurrentCircleDefinition
            : null;

        if (definition != null)
        {
            return definition.GetRoomPoolOrEmpty();
        }

        return rooms ?? Array.Empty<RoomScriptable>();
    }

    public DoorScriptable GetDoorOptions(RoomType roomType)
    {
        DoorScriptable[] activeDoorPool = GetActiveDoorPool();

        DoorScriptable exact = activeDoorPool.FirstOrDefault(x => x != null && x.roomType == roomType);
        if (exact != null)
        {
            return exact;
        }

        return activeDoorPool.FirstOrDefault(x => x != null && x.roomType == RoomType.Regular);
    }

    private DoorScriptable[] GetActiveDoorPool()
    {
        CircleDefinition definition = CircleManager.instance != null
            ? CircleManager.instance.CurrentCircleDefinition
            : null;

        if (definition != null)
        {
            DoorScriptable[] circleDoorPool = definition.GetDoorPoolOrEmpty();
            if (circleDoorPool.Length > 0)
            {
                return circleDoorPool;
            }
        }

        return doors ?? Array.Empty<DoorScriptable>();
    }

    private WeightedEnemyEntry PickEnemyEntryForCell(Cell cell)
    {
        CircleDefinition definition = CircleManager.instance != null
            ? CircleManager.instance.CurrentCircleDefinition
            : null;

        if (definition != null)
        {
            // Si hay definicion del circulo, esa configuracion manda.
            // Si no hay enemigos en ese circulo, no se hace spawn.
            return definition.PickEnemyEntry(cell.roomType);
        }

        if (enemyPoolFallback != null && enemyPoolFallback.Length > 0)
        {
            List<GameObject> valid = enemyPoolFallback.Where(enemy => enemy != null).ToList();
            if (valid.Count > 0)
            {
                return new WeightedEnemyEntry
                {
                    enemyPrefab = valid[UnityEngine.Random.Range(0, valid.Count)]
                };
            }
        }

        return enemyPrefab != null
            ? new WeightedEnemyEntry { enemyPrefab = enemyPrefab }
            : null;
    }

    public Room GetRoomContainingPoint(Vector2 point)
    {
        return createdRooms.FirstOrDefault(room => room != null && room.ContainsPoint(point));
    }

    private bool DoesTileMatchCell(int[] occupiedTiles, Cell cell)
    {
        if(occupiedTiles.Length != cell.cellList.Count)
            return false;

        int minIndex = cell.cellList.Min();
        List<int> normalizedCell = new List<int>();

        foreach(int index in cell.cellList)
        {
            int dx = (index % 10) - (minIndex % 10);
            int dy = (index / 10) - (minIndex / 10);

            normalizedCell.Add(dy * 10 + dx);
        }

        normalizedCell.Sort();
        int[] sortedOccupied = (int[])occupiedTiles.Clone();
        Array.Sort(sortedOccupied);

        return normalizedCell.SequenceEqual(sortedOccupied);
    }
}
