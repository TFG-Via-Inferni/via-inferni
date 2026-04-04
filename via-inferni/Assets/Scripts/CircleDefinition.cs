using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class WeightedEnemyEntry
{
    public GameObject enemyPrefab;
    public EnemyDefinition enemyDefinition;
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
    public string displayName = "First Circle - Limbo";

    [Header("Content Pools")]
    [Tooltip("Pool de salas disponible para este círculo. Pueden compartir shape/tipo con distintos tilesets.")]
    public RoomScriptable[] roomPool;

    [Tooltip("Pool de enemigos ponderado para este círculo.")]
    public WeightedEnemyEntry[] enemyPool;

    public RoomScriptable[] GetRoomPoolOrEmpty()
    {
        return roomPool ?? Array.Empty<RoomScriptable>();
    }

    public WeightedEnemyEntry PickEnemyEntry(RoomType roomType)
    {
        List<WeightedEnemyEntry> validEntries = (enemyPool ?? Array.Empty<WeightedEnemyEntry>())
            .Where(entry => entry != null && entry.IsValidFor(roomType))
            .ToList();

        if (validEntries.Count == 0)
        {
            return null;
        }

        WeightedEnemyEntry corePoolEntry = PickFromCoreTypesIfAvailable(validEntries);
        if (corePoolEntry != null)
        {
            return corePoolEntry;
        }

        float totalWeight = validEntries.Sum(entry => Mathf.Max(0f, entry.weight));
        if (totalWeight <= 0f)
        {
            return validEntries[UnityEngine.Random.Range(0, validEntries.Count)];
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (WeightedEnemyEntry entry in validEntries)
        {
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                return entry;
            }
        }

        return validEntries[validEntries.Count - 1];
    }

    private static WeightedEnemyEntry PickFromCoreTypesIfAvailable(List<WeightedEnemyEntry> validEntries)
    {
        List<EnemyType> availableTypes = validEntries
            .Where(entry => entry.enemyDefinition != null)
            .Select(entry => entry.enemyDefinition.enemyType)
            .Distinct()
            .ToList();

        bool hasNormal = availableTypes.Contains(EnemyType.Normal);
        bool hasFly = availableTypes.Contains(EnemyType.Fly);
        bool hasTank = availableTypes.Contains(EnemyType.Tank);

        if (!hasNormal || !hasFly || !hasTank)
        {
            return null;
        }

        EnemyType selectedType = UnityEngine.Random.value switch
        {
            < 0.3333f => EnemyType.Normal,
            < 0.6666f => EnemyType.Fly,
            _ => EnemyType.Tank
        };

        List<WeightedEnemyEntry> typeEntries = validEntries
            .Where(entry => entry.enemyDefinition != null && entry.enemyDefinition.enemyType == selectedType)
            .ToList();

        if (typeEntries.Count == 0)
        {
            return null;
        }

        float totalWeight = typeEntries.Sum(entry => Mathf.Max(0f, entry.weight));
        if (totalWeight <= 0f)
        {
            return typeEntries[UnityEngine.Random.Range(0, typeEntries.Count)];
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (WeightedEnemyEntry entry in typeEntries)
        {
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                return entry;
            }
        }

        return typeEntries[typeEntries.Count - 1];
    }

    public GameObject PickEnemyPrefab(RoomType roomType)
    {
        WeightedEnemyEntry entry = PickEnemyEntry(roomType);
        return entry != null ? entry.enemyPrefab : null;
    }
}
