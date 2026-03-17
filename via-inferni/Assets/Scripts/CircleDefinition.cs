using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class WeightedEnemyEntry
{
    public GameObject enemyPrefab;
    [Min(0f)] public float weight = 1f;
    public bool useRoomTypeFilter;
    public RoomType[] allowedRoomTypes;

    public bool IsValidFor(RoomType roomType)
    {
        if (enemyPrefab == null)
        {
            return false;
        }

        if (!useRoomTypeFilter)
        {
            return true;
        }

        return allowedRoomTypes != null && allowedRoomTypes.Contains(roomType);
    }
}

[CreateAssetMenu(fileName = "CircleDefinition", menuName = "Scriptable Objects/Circle Definition")]
public class CircleDefinition : ScriptableObject
{
    [Header("Circle")]
    [Min(1)] public int circleNumber = 1;
    public string displayName = "Primer Círculo - Limbo";

    [Header("Content Pools")]
    [Tooltip("Pool de salas disponible para este círculo. Pueden compartir shape/tipo con distintos tilesets.")]
    public RoomScriptable[] roomPool;

    [Tooltip("Pool de enemigos ponderado para este círculo.")]
    public WeightedEnemyEntry[] enemyPool;

    public RoomScriptable[] GetRoomPoolOrEmpty()
    {
        return roomPool ?? Array.Empty<RoomScriptable>();
    }

    public GameObject PickEnemyPrefab(RoomType roomType)
    {
        List<WeightedEnemyEntry> validEntries = (enemyPool ?? Array.Empty<WeightedEnemyEntry>())
            .Where(entry => entry != null && entry.IsValidFor(roomType))
            .ToList();

        if (validEntries.Count == 0)
        {
            return null;
        }

        float totalWeight = validEntries.Sum(entry => Mathf.Max(0f, entry.weight));
        if (totalWeight <= 0f)
        {
            return validEntries[UnityEngine.Random.Range(0, validEntries.Count)].enemyPrefab;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (WeightedEnemyEntry entry in validEntries)
        {
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                return entry.enemyPrefab;
            }
        }

        return validEntries[validEntries.Count - 1].enemyPrefab;
    }
}
