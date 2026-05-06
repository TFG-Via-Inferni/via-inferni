using UnityEngine;

public class Door : MonoBehaviour
{
    private GameObject doorVisual;
    private GameObject wallVisual;
    private GameObject currentDoorPrefab;
    private GameObject currentWallPrefab;
    private EdgeDirection direction;
    private RoomType roomType;
    private bool isLocked = false;

    public bool IsLocked => isLocked;

    public void SetDoorPrefab(GameObject doorPrefab)
    {
        currentDoorPrefab = doorPrefab;
        if (doorPrefab != null)
        {
            doorVisual = Instantiate(doorPrefab, transform);
            doorVisual.transform.localPosition = Vector3.zero;
            doorVisual.transform.localRotation = Quaternion.identity;
        }
    }

    public void SetWallPrefab(GameObject wallPrefab, EdgeDirection dir, RoomType type)
    {
        currentWallPrefab = wallPrefab;
        direction = dir;
        roomType = type;
    }

    public void Lock()
    {
        if (isLocked) return;
        isLocked = true;

        // Destruir la puerta visual
        if (doorVisual != null)
        {
            Destroy(doorVisual);
        }

        // Crear el muro
        if (currentWallPrefab != null)
        {
            wallVisual = Instantiate(currentWallPrefab, transform);
            wallVisual.transform.localPosition = Vector3.zero;
            wallVisual.transform.localRotation = Quaternion.identity;
        }
    }

    public void Unlock()
    {
        if (!isLocked) return;
        isLocked = false;

        // Destruir el muro
        if (wallVisual != null)
        {
            Destroy(wallVisual);
        }

        // Recrear la puerta
        if (currentDoorPrefab != null)
        {
            doorVisual = Instantiate(currentDoorPrefab, transform);
            doorVisual.transform.localPosition = Vector3.zero;
            doorVisual.transform.localRotation = Quaternion.identity;
        }
    }
}
