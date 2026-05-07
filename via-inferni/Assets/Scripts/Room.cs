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

public interface IRoomSpawnProvider
{
    float SpawnWeight { get; }
    List<Vector3> GetAllSpawnPositions();
}

public class Room : MonoBehaviour
{
    [Header("Enemy Spawning")]
    public GameObject enemyPrefab;
    public int minEnemies = 1;
    public int maxEnemies = 2;
    public float spawnRadius = 5f;

    private List<EnemyController> activeEnemies = new List<EnemyController>();
    private WeightedEnemyEntry enemySpawnEntry;
    private List<Door> roomDoors = new List<Door>();
    private List<IRoomSpawnProvider> spawnGrids = new List<IRoomSpawnProvider>();
    private bool playerHasEntered = false;
    private Cell currentCell;
    private Collider2D cameraBoundsCollider;
    private Transform cameraCenterTarget;

    public Collider2D CameraBoundsCollider => cameraBoundsCollider;
    public Transform CameraCenterTarget => cameraCenterTarget;
    public bool RequiresFixedCamera => currentCell != null && currentCell.roomShape == RoomShape.OneByOne;
    public Cell CellData => currentCell;

    public void ConfigureEnemySpawnEntry(WeightedEnemyEntry spawnEntry)
    {
        enemySpawnEntry = spawnEntry;
    }

    public void SetupRoom(Cell currentCell, RoomScriptable room)
    {
        this.currentCell = currentCell;
        spawnGrids.Clear();

        // Instanciar el prefab visual de la habitación
        if (room != null && room.roomVariations.Length > 0)
        {
            var selectedPrefab = room.roomVariations[Random.Range(0, room.roomVariations.Length)];
            if (selectedPrefab != null)
            {
                var roomInstance = Instantiate(selectedPrefab, transform);
                roomInstance.transform.localPosition = Vector3.zero;

                spawnGrids = roomInstance
                    .GetComponentsInChildren<MonoBehaviour>(true)
                    .OfType<IRoomSpawnProvider>()
                    .ToList();
                
                var tilemap = roomInstance.GetComponentInChildren<Tilemap>();
                if (tilemap != null)
                {
                    tilemap.CompressBounds();
                    var bounds = tilemap.localBounds;
                    roomInstance.transform.localPosition = -bounds.center;
                }

                // If this cell is a Shop room, ensure a Shop exists under the instantiated visual.
                if (currentCell != null && currentCell.roomType == RoomType.Shop)
                {
                    // Try to find an existing ShopManager in the room prefab variation
                    ShopManager existingShop = roomInstance.GetComponentInChildren<ShopManager>(true);
                    if (existingShop != null)
                    {
                        // Ensure it's populated now (helps when Shop was added manually in prefab)
                        existingShop.PopulateShop();
                    }
                    else
                    {
                        // If RoomManager provides a shop prefab, instantiate it as child to avoid duplicates
                        if (RoomManager.instance != null && RoomManager.instance.shopPrefab != null)
                        {
                            var shopGo = Instantiate(RoomManager.instance.shopPrefab, roomInstance.transform);
                            shopGo.transform.localPosition = Vector3.zero;
                        }
                    }
                }

                // If this cell is a Treasure room (RoomType.Item), ensure a TreasureManager exists
                if (currentCell != null && currentCell.roomType == RoomType.Item)
                {
                    // Try to find an existing TreasureManager in the room prefab variation
                    TreasureManager existingTreasure = roomInstance.GetComponentInChildren<TreasureManager>(true);
                    if (existingTreasure != null)
                    {
                        // Ensure it's populated now
                        existingTreasure.PopulateTreasure();
                    }
                    else
                    {
                        // If RoomManager provides a treasure prefab, instantiate it as child to avoid duplicates
                        if (RoomManager.instance != null && RoomManager.instance.treasurePrefab != null)
                        {
                            var treasureGo = Instantiate(RoomManager.instance.treasurePrefab, roomInstance.transform);
                            treasureGo.transform.localPosition = Vector3.zero;
                        }
                    }
                }

                if (currentCell != null && currentCell.roomType == RoomType.Secret)
                {
                    if (RoomManager.instance != null && RoomManager.instance.baptismalFontPrefab != null)
                    {
                        var baptismalFont = Instantiate(RoomManager.instance.baptismalFontPrefab, roomInstance.transform);
                        baptismalFont.transform.localPosition = new Vector3(24f, -11.5f, 0f);
                    }
                    else
                    {
                        Debug.LogWarning("No se pudo instanciar la pila bautismal: falta RoomManager.instance.baptismalFontPrefab.");
                    }
                }
            }
        }

        CacheCameraData();

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

        // Spawnear enemigos después de configurar la sala, salvo en la sala inicial o salas no regulares
        if (ShouldSpawnEnemies())
        {
            SpawnEnemies();
        }
    }

    public void RegisterDoor(Door door)
    {
        if (door != null && !roomDoors.Contains(door))
        {
            roomDoors.Add(door);
        }
    }

    public bool ContainsPoint(Vector2 point)
    {
        return cameraBoundsCollider != null && cameraBoundsCollider.OverlapPoint(point);
    }

    private void CacheCameraData()
    {
        cameraBoundsCollider = GetComponentsInChildren<Collider2D>(true)
            .FirstOrDefault(col => col.name.Equals("CameraBounds", System.StringComparison.OrdinalIgnoreCase));

        if (cameraCenterTarget == null)
        {
            var centerObj = new GameObject("CameraCenterTarget");
            cameraCenterTarget = centerObj.transform;
            cameraCenterTarget.SetParent(transform);
        }

        Vector3 centerPosition = transform.position;
        if (cameraBoundsCollider != null)
        {
            centerPosition = cameraBoundsCollider.bounds.center;
            centerPosition.z = transform.position.z;
        }

        cameraCenterTarget.position = centerPosition;
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
        Cell foundCell = null;

        if (neighborIndex >= 0 && neighborIndex < floorplan.Length)
        {
            if (floorplan[neighborIndex] == 1)
            {
                foundCell = cellList.FirstOrDefault(x => x.cellList.Contains(neighborIndex));
                if (foundCell == null)
                {
                    shouldPlaceDoor = false;
                }
                else if (foundCell.roomType != RoomType.Secret)
                {
                    shouldPlaceDoor = true;

                    if (RoomManager.instance == null || RoomManager.instance.doorPrefab == null)
                    {
                        Debug.LogWarning("No se pudo instanciar puerta: falta RoomManager o doorPrefab.");
                    }
                    else
                    {
                        var door = Instantiate(RoomManager.instance.doorPrefab, transform);
                        door.transform.position = (Vector2)transform.position + positionOffset;
                        SetupDoor(door, direction, currentCell.roomType == RoomType.Regular ? foundCell.roomType : currentCell.roomType);
                        roomDoors.Add(door); // Guardar referencia
                    }
                }
            }
        }

        if (!shouldPlaceDoor)
        {
            bool isBreakableSecretWall = foundCell != null && foundCell.roomType == RoomType.Secret;
            PlaceWall(positionOffset, direction, currentCell.roomType, isBreakableSecretWall);
        }
    }

    private void PlaceWall(Vector2 positionOffset, EdgeDirection direction, RoomType roomType, bool isBreakable = false)
    {
        var doorTypes = GetDoorOptions(roomType);
        if (doorTypes == null)
        {
            Debug.LogWarning($"No hay DoorScriptable configurado para roomType={roomType}. No se pudo colocar muro.");
            return;
        }

        GameObject wallPrefab = null;
        GameObject doorPrefab = null;

        switch (direction)
        {
            case EdgeDirection.Up:
                wallPrefab = doorTypes.upWall;
                doorPrefab = doorTypes.upDoor;
                break;
            
            case EdgeDirection.Down:
                wallPrefab = doorTypes.downWall;
                doorPrefab = doorTypes.downDoor;
                break;
            
            case EdgeDirection.Left:
                wallPrefab = doorTypes.leftWall;
                doorPrefab = doorTypes.leftDoor;
                break;
            
            case EdgeDirection.Right:
                wallPrefab = doorTypes.rightWall;
                doorPrefab = doorTypes.rightDoor;
                break;
        }

        if (wallPrefab != null)
        {
            var wall = Instantiate(wallPrefab, transform);
            wall.transform.position = (Vector2)transform.position + positionOffset;

            if (isBreakable)
            {
                BreakableWall breakableWall = wall.GetComponent<BreakableWall>();
                if (breakableWall == null)
                {
                    breakableWall = wall.AddComponent<BreakableWall>();
                }

                if (RoomManager.instance != null && RoomManager.instance.doorPrefab != null)
                {
                    breakableWall.ConfigureReplacementDoor(
                        RoomManager.instance.doorPrefab.gameObject,
                        doorPrefab,
                        wallPrefab,
                        direction,
                        roomType,
                        roomDoors,
                        this);
                }
            }
        }
    }

    private void SetupDoor(Door door, EdgeDirection direction, RoomType roomType)
    {
        var doorTypes = GetDoorOptions(roomType);
        if (doorTypes == null)
        {
            Debug.LogWarning($"No hay DoorScriptable configurado para roomType={roomType}. Se elimina la puerta sin configurar para evitar NullReference.");
            if (door != null)
            {
                Destroy(door.gameObject);
            }
            return;
        }

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
        if (RoomManager.instance == null)
        {
            return null;
        }

        return RoomManager.instance.GetDoorOptions(roomType);
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
        if (spawnGrids.Count == 0) return;

        int enemyCount = Random.Range(minEnemies, maxEnemies + 1);

        List<SpawnZoneData> spawnZones = new List<SpawnZoneData>();
        HashSet<Vector2Int> usedKeys = new HashSet<Vector2Int>();

        foreach (IRoomSpawnProvider grid in spawnGrids)
        {
            List<Vector3> rawPositions = grid.GetAllSpawnPositions();
            List<Vector3> uniqueZonePositions = new List<Vector3>();

            foreach (Vector3 pos in rawPositions)
            {
                Vector2Int key = new Vector2Int(Mathf.RoundToInt(pos.x * 100f), Mathf.RoundToInt(pos.y * 100f));
                if (usedKeys.Add(key))
                {
                    uniqueZonePositions.Add(pos);
                }
            }

            if (uniqueZonePositions.Count > 0)
            {
                spawnZones.Add(new SpawnZoneData
                {
                    Weight = Mathf.Max(0f, grid.SpawnWeight),
                    Positions = uniqueZonePositions
                });
            }
        }

        if (spawnZones.Count == 0)
        {
            return;
        }

        int finalCount = Mathf.Min(enemyCount, spawnZones.Sum(zone => zone.Positions.Count));

        for (int i = 0; i < finalCount; i++)
        {
            SpawnZoneData selectedZone = PickWeightedZone(spawnZones);
            if (selectedZone == null || selectedZone.Positions.Count == 0)
            {
                break;
            }

            int randomIndex = Random.Range(0, selectedZone.Positions.Count);
            Vector3 spawnPosition = selectedZone.Positions[randomIndex];
            selectedZone.Positions.RemoveAt(randomIndex);

            WeightedEnemyEntry spawnEntry = PickEnemyEntryForThisSpawn();
            if (spawnEntry == null || spawnEntry.enemyPrefab == null)
            {
                continue;
            }

            // Instanciar enemigo
            GameObject enemyObj = Instantiate(spawnEntry.enemyPrefab, spawnPosition, Quaternion.identity, transform);
            EnemyController enemy = enemyObj.GetComponent<EnemyController>();
            if (enemy != null)
            {
                enemy.Configure(spawnEntry.enemyDefinition, this);
                enemy.SetDormant(true);
                activeEnemies.Add(enemy);
            }
        }

        // NO bloquear puertas aquí, esperar a que el player entre
    }

    private WeightedEnemyEntry PickEnemyEntryForThisSpawn()
    {
        CircleDefinition definition = CircleManager.instance != null
            ? CircleManager.instance.CurrentCircleDefinition
            : null;

        if (definition != null)
        {
            WeightedEnemyEntry circleEntry = definition.PickEnemyEntry(currentCell != null ? currentCell.roomType : RoomType.Regular);
            if (circleEntry != null && circleEntry.enemyPrefab != null)
            {
                return circleEntry;
            }
        }

        if (enemySpawnEntry != null && enemySpawnEntry.enemyPrefab != null)
        {
            return enemySpawnEntry;
        }

        if (enemyPrefab != null)
        {
            return new WeightedEnemyEntry { enemyPrefab = enemyPrefab };
        }

        return null;
    }

    private SpawnZoneData PickWeightedZone(List<SpawnZoneData> spawnZones)
    {
        List<SpawnZoneData> zonesWithPositions = spawnZones
            .Where(zone => zone.Positions.Count > 0)
            .ToList();

        if (zonesWithPositions.Count == 0)
        {
            return null;
        }

        float totalWeight = zonesWithPositions.Sum(zone => zone.Weight > 0f ? zone.Weight : 1f);
        float randomValue = Random.value * totalWeight;
        float accumulated = 0f;

        foreach (SpawnZoneData zone in zonesWithPositions)
        {
            accumulated += zone.Weight > 0f ? zone.Weight : 1f;
            if (randomValue <= accumulated)
            {
                return zone;
            }
        }

        return zonesWithPositions[zonesWithPositions.Count - 1];
    }

    private class SpawnZoneData
    {
        public float Weight;
        public List<Vector3> Positions;
    }

    public void OnPlayerEnter(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
        {
            return;
        }

        // Cuando el Player entra por primera vez, bloquear puertas si hay enemigos
        if (!playerHasEntered)
        {
            playerHasEntered = true;
            ActivateEnemies();
                        
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

    private bool ShouldSpawnEnemies()
    {
        if (currentCell == null)
        {
            return false;
        }

        if (currentCell.roomType == RoomType.Item || currentCell.roomType == RoomType.Boss)
        {
            return false;
        }

        return currentCell.cellList == null || !currentCell.cellList.Contains(45);
    }

    private void ActivateEnemies()
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].SetDormant(false);
            }
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
        // After clearing the room, recharge cloak for the player if they own it
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Player player = playerObj.GetComponent<Player>();
            if (player != null)
            {
                player.RechargeCloakIfOwned();
                player.RechargeShieldIfOwned();
            }
        }
    }
}
