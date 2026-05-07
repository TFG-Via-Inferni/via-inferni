using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class BreakableWall : MonoBehaviour, IDamageable
{
    [Min(1f)] [SerializeField] private float maxHealth = 12f;

    private float currentHealth;
    private GameObject replacementDoorPrefab;
    private GameObject replacementDoorVisualPrefab;
    private GameObject replacementWallVisualPrefab;
    private EdgeDirection replacementDirection;
    private RoomType replacementRoomType;
    private List<Door> roomDoors;
    private Room parentRoom;

    public bool CanTakeDamage => currentHealth > 0f && AreAllDoorsUnlocked();

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        currentHealth = maxHealth;
    }

    public void ConfigureReplacementDoor(
        GameObject doorParentPrefab,
        GameObject doorVisualPrefab,
        GameObject wallVisualPrefab,
        EdgeDirection direction,
        RoomType roomType,
        List<Door> doorsInRoom = null,
        Room roomComponent = null)
    {
        replacementDoorPrefab = doorParentPrefab;
        replacementDoorVisualPrefab = doorVisualPrefab;
        replacementWallVisualPrefab = wallVisualPrefab;
        replacementDirection = direction;
        replacementRoomType = roomType;
        roomDoors = doorsInRoom;
        parentRoom = roomComponent;
    }

    private bool AreAllDoorsUnlocked()
    {
        if (roomDoors == null || roomDoors.Count == 0)
        {
            return true;
        }

        foreach (Door door in roomDoors)
        {
            if (door != null && door.IsLocked)
            {
                return false;
            }
        }

        return true;
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (amount <= 0f || !CanTakeDamage)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
        {
            SpawnBreakParticles();
            SpawnReplacementDoor();
            Destroy(gameObject);
        }
    }

    private void SpawnBreakParticles()
    {
        Vector2 direction = Random.insideUnitCircle;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector2.up;
        }

        Renderer wallRenderer = GetComponentInChildren<Renderer>();
        WallBreakVisual.Spawn(
            transform.position,
            new Color(0.92f, 0.86f, 0.72f, 1f),
            direction.normalized,
            1.3f,
            wallRenderer,
            15);
    }

    private void SpawnReplacementDoor()
    {
        if (replacementDoorPrefab == null || replacementDoorVisualPrefab == null)
        {
            return;
        }

        Transform parent = transform.parent;
        GameObject doorObject = Instantiate(replacementDoorPrefab, transform.position, transform.rotation, parent);

        Door door = doorObject.GetComponent<Door>();
        if (door == null)
        {
            return;
        }

        door.SetDoorPrefab(replacementDoorVisualPrefab);
        door.SetWallPrefab(replacementWallVisualPrefab, replacementDirection, replacementRoomType);

        // Register the replacement door in the parent room so it locks/unlocks with other doors
        if (parentRoom != null)
        {
            parentRoom.RegisterDoor(door);
        }
    }
}
