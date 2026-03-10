using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum EdgeDirection
{
    Up,
    Down,
    Left,
    Right
}

public class Room : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public int minEnemies = 1;
    public int maxEnemies = 2;
    public float spawnRadius = 5f;

    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private List<Door> roomDoors = new List<Door>();
    private bool playerHasEntered = false;

    public void SetupRoom(Cell currentCell, RoomScriptable room)
    {
        // Instanciar el prefab visual de la habitación
        if (room != null && room.roomVariations.Length > 0)
        {
            var selectedPrefab = room.roomVariations[Random.Range(0, room.roomVariations.Length)];
            if (selectedPrefab != null)
            {
                var roomInstance = Instantiate(selectedPrefab, transform);
                roomInstance.transform.localPosition = Vector3.zero;
                
                var tilemap = roomInstance.GetComponentInChildren<Tilemap>();
                if (tilemap != null)
                {
                    tilemap.CompressBounds();
                    var bounds = tilemap.localBounds;
                    roomInstance.transform.localPosition = -bounds.center;
                }
            }
        }

        if (currentCell.roomType == RoomType.Secret) return;

        var floorplan = MapGenerator.instance.getFloorPlan;
        var cellList = MapGenerator.instance.getSpawnedCells;

        switch (currentCell.roomShape)
        {
            case RoomShape.OneByOne:
                SetupOneByOne(currentCell, floorplan, cellList);
                break;

            case RoomShape.OneByTwo:
                SetupOneByTwo(currentCell, floorplan, cellList);
                break;

            case RoomShape.TwoByOne:
                SetupTwoByOne(currentCell, floorplan, cellList);
                break;

            case RoomShape.TwoByTwo:
                SetupTwoByTwo(currentCell, floorplan, cellList);
                break;

            case RoomShape.LShape:
                SetupLShapeRoom(currentCell, floorplan, cellList);
                break;
            
            default:
                break;
        }

        // Spawnear enemigos después de configurar la sala
        SpawnEnemies();
    }

    public void SetupOneByOne(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var currentCell = cell.cellList[0];

        TryPlaceDoor(currentCell, new Vector2(-1, 3.5f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(0, 3.5f), EdgeDirection.Up, floorplan, cellList, cell);

        TryPlaceDoor(currentCell, new Vector2(-1, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(0, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(currentCell, new Vector2(-8f, -0.5f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(currentCell, new Vector2(7f, -0.5f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupOneByTwo(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];

        TryPlaceDoor(cellA, new Vector2(0f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-1f, 8f), EdgeDirection.Up, floorplan, cellList, cell);

        TryPlaceDoor(cellA, new Vector2(-8f, 4f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(7f, 4f), EdgeDirection.Right, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(-1f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(0f, -9f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(-8f, -5f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(7f, -5f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupTwoByOne(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];

        TryPlaceDoor(cellA, new Vector2(-8f, 3.5f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-9f, 3.5f), EdgeDirection.Up, floorplan, cellList, cell);

        TryPlaceDoor(cellA, new Vector2(-16f, -0.5f), EdgeDirection.Left, floorplan, cellList, cell);

        TryPlaceDoor(cellA, new Vector2(-9f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-8f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(8f, 3.5f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(7f, 3.5f), EdgeDirection.Up, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(7f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(8f, -4.5f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(15f, -0.5f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupTwoByTwo(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];
        var cellC = cell.cellList[2];
        var cellD = cell.cellList[3];

        TryPlaceDoor(cellA, new Vector2(-8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellA, new Vector2(-9f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
        
        TryPlaceDoor(cellB, new Vector2(8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
        TryPlaceDoor(cellB, new Vector2(7f, 8f), EdgeDirection.Up, floorplan, cellList, cell);

        TryPlaceDoor(cellA, new Vector2(-16f, 4f), EdgeDirection.Left, floorplan, cellList, cell);
        TryPlaceDoor(cellC, new Vector2(-16f, -5f), EdgeDirection.Left, floorplan, cellList, cell);

        TryPlaceDoor(cellC, new Vector2(-9f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellC, new Vector2(-8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
        
        TryPlaceDoor(cellD, new Vector2(7f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
        TryPlaceDoor(cellD, new Vector2(8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);

        TryPlaceDoor(cellB, new Vector2(15f, 4f), EdgeDirection.Right, floorplan, cellList, cell);
        TryPlaceDoor(cellD, new Vector2(15f, -5f), EdgeDirection.Right, floorplan, cellList, cell);
    }

    public void SetupLShapeRoom(Cell cell, int[] floorplan, List<Cell> cellList)
    {
        var cellA = cell.cellList[0];
        var cellB = cell.cellList[1];
        var cellC = cell.cellList[2];

        if (cellA + 1 == cellB && cellA + 10 == cellC)
        {
            TryPlaceDoor(cellA, new Vector2(-8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-16f, 4f), EdgeDirection.Left, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(7f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(15f, 4f), EdgeDirection.Right, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(7f, 0f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(8f, 0f), EdgeDirection.Down, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(-9f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(-8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(-1f, -5f), EdgeDirection.Right, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(-16f, -5f), EdgeDirection.Left, floorplan, cellList, cell);
        }
        else if (cellA + 1 == cellB && cellB + 10 == cellC)
        {
            TryPlaceDoor(cellA, new Vector2(-8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-16f, 4f), EdgeDirection.Left, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9f, 0f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-8f, 0f), EdgeDirection.Down, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(7f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(15f, 4f), EdgeDirection.Right, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(7f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(15f, -5f), EdgeDirection.Right, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(0f, -5f), EdgeDirection.Left, floorplan, cellList, cell);
        }
        else if (cellA + 10 == cellB)
        {
            TryPlaceDoor(cellA, new Vector2(-8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-9f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-16f, 4f), EdgeDirection.Left, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(-1f, 4f), EdgeDirection.Right, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(-9f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-16f, -5f), EdgeDirection.Left, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(8f, -1f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(7f, -1f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(7f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(15f, -5f), EdgeDirection.Right, floorplan, cellList, cell);
        }
        else if (cellA + 10 == cellC)
        {
            TryPlaceDoor(cellA, new Vector2(8f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(7f, 8f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(0f, 4f), EdgeDirection.Left, floorplan, cellList, cell);
            TryPlaceDoor(cellA, new Vector2(15f, 4f), EdgeDirection.Right, floorplan, cellList, cell);

            TryPlaceDoor(cellB, new Vector2(-8f, -1f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-9f, -1f), EdgeDirection.Up, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-9f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellB, new Vector2(-16f, -5f), EdgeDirection.Left, floorplan, cellList, cell);

            TryPlaceDoor(cellC, new Vector2(7f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(8f, -9f), EdgeDirection.Down, floorplan, cellList, cell);
            TryPlaceDoor(cellC, new Vector2(15f, -5f), EdgeDirection.Right, floorplan, cellList, cell);
        }
    }

    private void TryPlaceDoor(int fromIndex, Vector2 positionOffset, EdgeDirection direction, int[] floorplan, List<Cell> cellList, Cell currentCell)
    {
        int neighborIndex = fromIndex + GetOffset(direction);
        bool shouldPlaceDoor = false;

        if (neighborIndex >= 0 && neighborIndex < floorplan.Length)
        {
            if (floorplan[neighborIndex] == 1)
            {
                var foundCell = cellList.FirstOrDefault(x => x.cellList.Contains(neighborIndex));

                if (foundCell.roomType != RoomType.Secret)
                {
                    shouldPlaceDoor = true;
                    var door = Instantiate(RoomManager.instance.doorPrefab, transform);
                    door.transform.position = (Vector2)transform.position + positionOffset;
                    SetupDoor(door, direction, currentCell.roomType == RoomType.Regular ? foundCell.roomType : currentCell.roomType);
                    roomDoors.Add(door); // Guardar referencia
                }
            }
        }

        if (!shouldPlaceDoor)
        {
            PlaceWall(positionOffset, direction, currentCell.roomType);
        }
    }

    private void PlaceWall(Vector2 positionOffset, EdgeDirection direction, RoomType roomType)
    {
        var doorTypes = GetDoorOptions(roomType);
        GameObject wallPrefab = null;

        switch (direction)
        {
            case EdgeDirection.Up:
                wallPrefab = doorTypes.upWall;
                break;
            
            case EdgeDirection.Down:
                wallPrefab = doorTypes.downWall;
                break;
            
            case EdgeDirection.Left:
                wallPrefab = doorTypes.leftWall;
                break;
            
            case EdgeDirection.Right:
                wallPrefab = doorTypes.rightWall;
                break;
        }

        if (wallPrefab != null)
        {
            var wall = Instantiate(wallPrefab, transform);
            wall.transform.position = (Vector2)transform.position + positionOffset;
        }
    }

    private void SetupDoor(Door door, EdgeDirection direction, RoomType roomType)
    {
        var doorTypes = GetDoorOptions(roomType);
        GameObject doorPrefab = null;
        GameObject wallPrefab = null;

        switch (direction)
        {
            case EdgeDirection.Up:
                doorPrefab = doorTypes.upDoor;
                wallPrefab = doorTypes.upWall;
                break;
            
            case EdgeDirection.Down:
                doorPrefab = doorTypes.downDoor;
                wallPrefab = doorTypes.downWall;
                break;
            
            case EdgeDirection.Left:
                doorPrefab = doorTypes.leftDoor;
                wallPrefab = doorTypes.leftWall;
                break;
            
            case EdgeDirection.Right:
                doorPrefab = doorTypes.rightDoor;
                wallPrefab = doorTypes.rightWall;
                break;
        }

        door.SetDoorPrefab(doorPrefab);
        door.SetWallPrefab(wallPrefab, direction, roomType);
    }

    private DoorScriptable GetDoorOptions(RoomType roomType)
    {
        return RoomManager.instance.doors.FirstOrDefault(x => x.roomType == roomType);
    }

    private int GetOffset(EdgeDirection direction)
    {
        switch (direction)
        {
            case EdgeDirection.Up:
                return -10;
            
            case EdgeDirection.Down:
                return 10;
            
            case EdgeDirection.Right:
                return 1;
            
            case EdgeDirection.Left:
                return -1;
        }

        return 0;
    }

    private void SpawnEnemies()
    {
        // Solo spawnear si hay prefab asignado
        if (enemyPrefab == null) return;

        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        for (int i = 0; i < enemyCount; i++)
        {
            // Generar posición aleatoria dentro del radio de spawn
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomPos.x, randomPos.y, 0);

            // Instanciar enemigo
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
            EnemyController enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                activeEnemies.Add(enemy);
            }
        }

        // NO bloquear puertas aquí, esperar a que el player entre
    }

    public void OnPlayerEnter(Collider2D collision)
    {        
        // Cuando el Player entra por primera vez, bloquear puertas si hay enemigos
        if (!playerHasEntered && collision.CompareTag("Player"))
        {
            playerHasEntered = true;
                        
            if (activeEnemies.Count > 0)
            {
                LockDoors();
            }
        }
    }

    public void OnEnemyDestroyed(EnemyController enemy)
    {
        activeEnemies.Remove(enemy);

        // Si no quedan enemigos, desbloquear puertas
        if (activeEnemies.Count == 0)
        {
            UnlockDoors();
        }
    }

    private void LockDoors()
    {
        foreach (var door in roomDoors)
        {
            if (door != null)
            {
                door.Lock();
            }
        }
    }

    private void UnlockDoors()
    {
        foreach (var door in roomDoors)
        {
            if (door != null)
            {
                door.Unlock();
            }
        }
    }
}
