using System.Collections.Generic;
using UnityEngine;

public class RoomSpawnGrid : MonoBehaviour, IRoomSpawnProvider
{
    [Header("Control de probabilidad")]
    [Tooltip("Peso relativo de este grid frente a otros grids de la misma sala.")]
    [SerializeField] private float spawnWeight = 1f;

    [Header("Area del grid (espacio local)")]
    [Tooltip("Desplazamiento local del centro del area de spawn.")]
    [SerializeField] private Vector2 localCenterOffset = Vector2.zero;
    [Tooltip("Ancho y alto del area donde se construye el grid.")]
    [SerializeField] private Vector2 areaSize = new Vector2(10f, 6f);
    [Tooltip("Espaciado entre celdas del grid.")]
    [SerializeField] private float cellSize = 1.5f;
    [Tooltip("Margen interior para no pegar spawns al borde del area.")]
    [SerializeField] private float edgePadding = 0.5f;

    [Header("Filtrado de celdas invalidas")]
    [Tooltip("Capas que bloquean spawn (paredes, obstaculos, etc.).")]
    [SerializeField] private LayerMask blockedLayers;
    [Tooltip("Radio usado para comprobar si una celda esta libre.")]
    [SerializeField] private float occupancyCheckRadius = 0.35f;

    public float SpawnWeight => Mathf.Max(0f, spawnWeight);

    public List<Vector3> GetAllSpawnPositions()
    {
        return CollectAvailablePositions();
    }

    private List<Vector3> CollectAvailablePositions()
    {
        List<Vector3> positions = new List<Vector3>();

        if (cellSize <= 0.01f)
        {
            return positions;
        }

        float halfWidth = areaSize.x * 0.5f;
        float halfHeight = areaSize.y * 0.5f;

        float minX = -halfWidth + edgePadding;
        float maxX = halfWidth - edgePadding;
        float minY = -halfHeight + edgePadding;
        float maxY = halfHeight - edgePadding;

        if (minX > maxX || minY > maxY)
        {
            return positions;
        }

        for (float x = minX; x <= maxX; x += cellSize)
        {
            for (float y = minY; y <= maxY; y += cellSize)
            {
                Vector3 localPos = new Vector3(localCenterOffset.x + x, localCenterOffset.y + y, 0f);
                Vector3 worldPos = transform.TransformPoint(localPos);
                bool isBlocked = Physics2D.OverlapCircle(worldPos, occupancyCheckRadius, blockedLayers) != null;
                if (!isBlocked)
                {
                    positions.Add(worldPos);
                }
            }
        }

        return positions;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = transform.TransformPoint(new Vector3(localCenterOffset.x, localCenterOffset.y, 0f));
        Vector3 size = new Vector3(areaSize.x, areaSize.y, 0f);
        Gizmos.DrawWireCube(center, size);

        List<Vector3> previewPositions = CollectAvailablePositions();
        foreach (Vector3 position in previewPositions)
        {
            Gizmos.DrawWireSphere(position, 0.25f);
        }
    }
}
