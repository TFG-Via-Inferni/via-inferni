using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using Unity.Cinemachine;

public class MapGenerator : MonoBehaviour
{
    private int[] floorPlan;
    public int[] getFloorPlan => floorPlan;

    private int floorPlantCount;
    private int minRooms;
    private int maxRooms;
    private List<int> endRooms;

    private int bossRoomIndex;
    private int secretRoomIndex;
    private int shopRoomIndex;
    private int itemRoomIndex;

    public Cell cellPrefab;
    public GameObject playerPrefab;
    private Player playerInstance;
    private float cellSize;
    
    [Header("Cinemachine")]
    public CinemachineCamera cinemachineCamera;
    public RoomCameraController roomCameraController;
    private Queue<int> cellQueue;
    private List<Cell> spawnedCells;

    public List<Cell> getSpawnedCells => spawnedCells; 

    private List<int> bigRoomIndexes;

    [Header("Sprite References")]
    [SerializeField] private Sprite item;
    [SerializeField] private Sprite shop;
    [SerializeField] private Sprite boss;
    [SerializeField] private Sprite secret;

    [Header("Room Varations")]
    [SerializeField] private Sprite largeRoom;
    [SerializeField] private Sprite verticalRoom;
    [SerializeField] private Sprite horizontalRoom;
    [SerializeField] private Sprite lShapeRoom;

    [Header("Minimap")]
    [SerializeField] private float visitedAlpha = 0.4f;
    [SerializeField] private float currentRoomAlpha = 1f;
    [SerializeField] private float undiscoveredAlpha = 0f;

    public static MapGenerator instance;
    private HashSet<Cell> visitedRooms = new();
    private Cell currentRoomCell;
    private Room lastPlayerRoom;

    private static readonly List<int[]> roomShapes = new()
    {
        new int[]{-1 },
        new int[]{1 },

        new int[]{10 },
        new int[]{-10 },

        new int[] {1,10 },
        new int[] {1,11 },
        new int[] {10,11 },

        new int[] {9,10 },
        new int[] {-1, 9},
        new int[] {-1,10 },

        new int[] {1, -10 },
        new int[] {1, -9 },
        new int[] {-9, -10 },

        new int[] {-1, -10},
        new int[] {-1, -11 },
        new int[]{-10,-11 },

        new int[] { 1,10,11 },
        new int[] {1,-9,-10 },
        new int[] {-1, 9, 10},
        new int[] {-1, -10, -11}
    };

    void Start()
    {
        instance = this;

        minRooms = 7;
        maxRooms = 15;
        cellSize = 0.5f;
        spawnedCells = new();

        SetupDungeon();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SetupDungeon();
        }

        UpdateMinimapCurrentRoomFromPlayer();
    }

    public void SetupDungeon()
    {
        ClearWorldInventoryPickups();

        for (int i = 0; i < spawnedCells.Count; i++)
        {
            Destroy(spawnedCells[i].gameObject);
        }

        spawnedCells.Clear();

        floorPlan = new int[100];
        floorPlantCount = default;
        cellQueue = new Queue<int>();
        endRooms = new List<int>();
        bigRoomIndexes = new List<int>();
        visitedRooms.Clear();
        currentRoomCell = null;
        lastPlayerRoom = null;

        VisitCell(45);

        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        while(cellQueue.Count > 0)
        {
            int index = cellQueue.Dequeue();
            int x = index % 10;

            bool created = false;

            if (x > 1) created |= VisitCell(index - 1);
            if (x < 9) created |= VisitCell(index + 1);
            if (index > 20) created |= VisitCell(index - 10);
            if (index < 70) created |= VisitCell(index + 10);

            if (created == false)
            {
                endRooms.Add(index);
            }
        }

        if (floorPlantCount < minRooms)
        {
            SetupDungeon();
            return;
        }

        CleanEndRoomList();

        SetupSpecialRooms();
    }

    void CleanEndRoomList()
    {
        endRooms.RemoveAll(item => bigRoomIndexes.Contains(item) || GetNeighborCount(item) > 1);
    }

    void SetupSpecialRooms()
    {
        bossRoomIndex = endRooms.Count > 0 ? endRooms[endRooms.Count - 1] : -1;

        if (bossRoomIndex != -1)
        {
            endRooms.RemoveAt(endRooms.Count - 1);
        }

        itemRoomIndex = RandomEndRoom();
        shopRoomIndex = RandomEndRoom();
        secretRoomIndex = PickSecretRoom();

        if (itemRoomIndex == -1 || shopRoomIndex == -1 || bossRoomIndex == -1 || secretRoomIndex == -1)
        {
            SetupDungeon();
            return;
        }

        SpawnRoom(secretRoomIndex);

        UpdateSpecialRoomVisuals();
        InitializeMinimapFog();
        RoomManager.instance.SetUpRooms(spawnedCells);
        SpawnPlayer();
    }

    private static void ClearWorldInventoryPickups()
    {
        WorldInventoryPickup[] pickups = UnityEngine.Object.FindObjectsByType<WorldInventoryPickup>(FindObjectsSortMode.None);
        for (int i = 0; i < pickups.Length; i++)
        {
            if (pickups[i] != null)
            {
                Destroy(pickups[i].gameObject);
            }
        }
    }

    private void InitializeMinimapFog()
    {
        visitedRooms.Clear();
        currentRoomCell = null;
        lastPlayerRoom = null;
        RefreshMinimapVisuals();
    }

    private void UpdateMinimapCurrentRoomFromPlayer()
    {
        if (playerInstance == null || RoomManager.instance == null)
        {
            return;
        }

        Room room = RoomManager.instance.GetRoomContainingPoint(playerInstance.transform.position);
        if (room == null)
        {
            if (currentRoomCell != null)
            {
                currentRoomCell = null;
                lastPlayerRoom = null;
                RefreshMinimapVisuals();
            }

            return;
        }

        if (room == lastPlayerRoom)
        {
            return;
        }

        lastPlayerRoom = room;

        if (room.CellData != null)
        {
            OnPlayerEnteredRoom(room.CellData);
        }
    }

    public void OnPlayerEnteredRoom(Cell enteredCell)
    {
        if (enteredCell == null)
        {
            return;
        }

        visitedRooms.Add(enteredCell);
        bool hasRoomChange = currentRoomCell != enteredCell;
        currentRoomCell = enteredCell;

        if (hasRoomChange)
        {
            RefreshMinimapVisuals();
        }
    }

    private void RefreshMinimapVisuals()
    {
        foreach (Cell cell in spawnedCells)
        {
            bool isCurrent = cell == currentRoomCell;
            bool isVisited = visitedRooms.Contains(cell);
            float alpha = isCurrent ? currentRoomAlpha : (isVisited ? visitedAlpha : undiscoveredAlpha);
            SetCellAlpha(cell, alpha);
        }
    }

    private void SetCellAlpha(Cell cell, float alpha)
    {
        if (cell == null)
        {
            return;
        }

        bool visible = alpha > 0.001f;

        if (cell.roomSprite != null)
        {
            cell.roomSprite.enabled = visible;
            Color roomColor = cell.roomSprite.color;
            roomColor.a = alpha;
            cell.roomSprite.color = roomColor;
        }

        if (cell.spriteRenderer != null)
        {
            cell.spriteRenderer.enabled = visible;
            Color specialColor = cell.spriteRenderer.color;
            specialColor.a = alpha;
            cell.spriteRenderer.color = specialColor;
        }
    }

    void SpawnPlayer()
    {
        // Buscar la celda con índice 45 (habitación central)
        Cell centralCell = spawnedCells.Find(cell => cell.cellList.Contains(45));
        
        UnityEngine.Vector2 position;
        if (centralCell != null)
        {
            // Usar la posición de la celda convertida con el offset del RoomManager
            // para que el jugador aparezca en la habitación renderizada real
            var cellPosition = centralCell.transform.position;
            position = new UnityEngine.Vector2(
                cellPosition.x * RoomManager.instance.offsetX, 
                cellPosition.y * RoomManager.instance.offsetY
            );
        }
        else
        {
            // Fallback: calcular posición manualmente si no se encuentra la celda
            position = new UnityEngine.Vector2(5 * cellSize, -4 * cellSize);
        }

        GameObject playerObj;
        Player player;

        if (playerInstance != null)
        {
            player = playerInstance;
            player.TeleportTo(position);
            playerObj = player.gameObject;
        }
        else
        {
            playerObj = Instantiate(playerPrefab, position, UnityEngine.Quaternion.identity);

            player = playerObj.GetComponent<Player>();
            if (player == null)
            {
                player = playerObj.AddComponent<Player>();
            }

            player.TeleportTo(position);
            playerInstance = player;
        }

        playerInstance = player;

        if (centralCell != null)
        {
            OnPlayerEnteredRoom(centralCell);
        }

        if (roomCameraController != null)
        {
            roomCameraController.RegisterPlayer(playerObj.transform);
            StartCoroutine(roomCameraController.SnapToPlayerRoomNextFrame());
        }
        else if (cinemachineCamera != null)
        {
            // Fallback si no hay controlador dinámico en escena
            cinemachineCamera.Follow = playerObj.transform;
        }
    }

    void UpdateSpecialRoomVisuals()
    {
        foreach (var cell in spawnedCells)
        {
            if (cell.index == bossRoomIndex)
            {
                cell.SetSpecialRoomSprite(boss);
                cell.SetRoomType(RoomType.Boss);
            }
            else if (cell.index == itemRoomIndex)
            {
                cell.SetSpecialRoomSprite(item);
                cell.SetRoomType(RoomType.Item);
            }
            else if (cell.index == shopRoomIndex)
            {
                cell.SetSpecialRoomSprite(shop);
                cell.SetRoomType(RoomType.Shop);
            }
            else if (cell.index == secretRoomIndex)
            {
                cell.SetSpecialRoomSprite(secret);
                cell.SetRoomType(RoomType.Secret);
            }
        }
    }

    int RandomEndRoom()
    {
        if (endRooms.Count == 0)
        {
            return -1;
        }

        int randomRoom = Random.Range(0, endRooms.Count);
        int index = endRooms[randomRoom];

        endRooms.RemoveAt(randomRoom);
    
        return index;
    }

    int PickSecretRoom()
    {
        for (int attemp = 0; attemp < 900; attemp++)
        {
            int x = Mathf.FloorToInt(Random.Range(0f, 1f) * 9) + 1;
            int y = Mathf.FloorToInt(Random.Range(0f, 1f) * 8) + 2;
            
            int index = x + y * 10;

            if (floorPlan[index] != 0)
            {
                continue;
            }

            if (bossRoomIndex == index - 1 || bossRoomIndex == index + 1 || bossRoomIndex == index - 10 || bossRoomIndex == index + 10)
            {
                continue;
            }

            if (index - 1 < 0 || index + 1 > floorPlan.Length || index - 10 < 0 || index + 10 > floorPlan.Length)
            {
                continue;
            }

            int neighbours = GetNeighborCount(index);

            if (neighbours >= 3 || (attemp > 300 && neighbours >= 2) || (attemp > 600 && neighbours >= 1))
            {
                return index;
            }
        }

        return -1;
    }

    private int GetNeighborCount(int index)
    {
        return floorPlan[index - 10] + floorPlan[index - 1] + floorPlan[index + 1] + floorPlan[index + 10];
    }

    private bool VisitCell(int index)
    {
        if (floorPlan[index] != 0 || GetNeighborCount(index) > 1 || floorPlantCount > maxRooms || Random.value < 0.5f)
        {
            return false;
        }
        if (Random.value < 0.3f && index != 45)
        {
            foreach (var shape in roomShapes.OrderBy(_ => Random.value))
            {
                if (TryPlaceroom(index, shape))
                {
                    return true;
                }
            }
        }

        cellQueue.Enqueue(index);
        floorPlan[index] = 1;
        floorPlantCount++;

        SpawnRoom(index);

        return true;
    }

    private void SpawnRoom(int index)
    {
        int x = index % 10;
        int y = index / 10;
        UnityEngine.Vector2 position = new UnityEngine.Vector2(x * cellSize, -y * cellSize);

        Cell newCell = Instantiate(cellPrefab, position, UnityEngine.Quaternion.identity);
        newCell.value = 1;
        newCell.index = index;
        newCell.SetRoomShape(RoomShape.OneByOne);
        newCell.SetRoomType(RoomType.Regular);

        newCell.cellList.Add(index);

        spawnedCells.Add(newCell);
    }

    private bool TryPlaceroom(int origin, int[] offsets)
    {
        List<int> currentRoomIndexes = new List<int> { origin };

        foreach (var offset in offsets)
        {
            int currentIndexChecked = origin + offset;

            if (currentIndexChecked - 10 < 0 || currentIndexChecked + 10 > floorPlan.Length)
            {
                return false;
            }

            if (floorPlan[currentIndexChecked] != 0)
            {
                return false;
            }
            if (currentIndexChecked == origin)
            {
                continue;
            }
            if (currentIndexChecked % 10 == 0)
            {
                continue;
            }

            currentRoomIndexes.Add(currentIndexChecked);
        }

        if (currentRoomIndexes.Count == 1)
        {
            return false;
        }

        foreach (int index in currentRoomIndexes)
        {
            floorPlan[index] = 1;
            floorPlantCount++;
            cellQueue.Enqueue(index);

            bigRoomIndexes.Add(index);
        }

        SpawnLargeRoom(currentRoomIndexes);

        return true;
    }

    private void SpawnLargeRoom(List<int> largeRoomIndexes)
    {
        Cell newCell = null;

        int combinedX = default;
        int combinedY = default;
        float offset = cellSize / 2;

        for (int i = 0; i < largeRoomIndexes.Count; i++)
        {
            int x = largeRoomIndexes[i] % 10;
            int y = largeRoomIndexes[i] / 10;
            combinedX += x;
            combinedY += y;
        }

        if (largeRoomIndexes.Count == 4)
        {
            UnityEngine.Vector2 position = new UnityEngine.Vector2(combinedX / 4 * cellSize + offset, -combinedY / 4 * cellSize - offset);

            newCell = Instantiate(cellPrefab, position, UnityEngine.Quaternion.identity);
            newCell.SetRoomSprite(largeRoom);
            newCell.SetRoomShape(RoomShape.TwoByTwo);
        }

        if (largeRoomIndexes.Count == 3)
        {
            UnityEngine.Vector2 position = new UnityEngine.Vector2(combinedX / 3 * cellSize + offset, -combinedY / 3 * cellSize - offset);
            newCell = Instantiate(cellPrefab, position, UnityEngine.Quaternion.identity);
            newCell.SetRoomSprite(lShapeRoom);
            newCell.RotateCell(largeRoomIndexes);
            newCell.SetRoomShape(RoomShape.LShape);
        }

        if (largeRoomIndexes.Count == 2)
        {
            if (largeRoomIndexes[0] + 10 == largeRoomIndexes[1] || largeRoomIndexes[0] - 10 == largeRoomIndexes[1])
            {
                UnityEngine.Vector2 position = new UnityEngine.Vector2(combinedX / 2 * cellSize, -combinedY / 2 * cellSize - offset);
                newCell = Instantiate(cellPrefab, position, UnityEngine.Quaternion.identity);
                newCell.SetRoomSprite(verticalRoom);
                newCell.SetRoomShape(RoomShape.OneByTwo);
            }
            else if (largeRoomIndexes[0] + 1 == largeRoomIndexes[1] || largeRoomIndexes[0] - 1 == largeRoomIndexes[1])
            {
                UnityEngine.Vector2 position = new UnityEngine.Vector2(combinedX / 2 * cellSize + offset, -combinedY / 2 * cellSize);
                newCell = Instantiate(cellPrefab, position, UnityEngine.Quaternion.identity);
                newCell.SetRoomSprite(horizontalRoom);
                newCell.SetRoomShape(RoomShape.TwoByOne);
            }
        }

        newCell.cellList = largeRoomIndexes;
        newCell.cellList.Sort();
        spawnedCells.Add(newCell);
    } 

}

