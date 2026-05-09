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

[Serializable]
public class WeightedInventoryDropEntry
{
    public InventoryItemDefinition itemDefinition;
    [Min(0f)] public float weight = 1f;

    public bool IsValid()
    {
        return itemDefinition != null && itemDefinition.IsValid(out _);
    }
}

[Serializable]
public class EnemySpawnCountRule
{
    [Header("Room Filter")]
    public RoomShape roomShape = RoomShape.OneByOne;
    public bool useRoomTypeFilter;
    public RoomType[] allowedRoomTypes;

    [Header("Spawn Range")]
    [Min(0)] public int minEnemies = 1;
    [Min(0)] public int maxEnemies = 2;

    public bool Matches(RoomShape shape, RoomType roomType)
    {
        if (shape != roomShape)
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

    [Tooltip("Pool de puertas/muros para este círculo por tipo de sala.")]
    public DoorScriptable[] doorPool;

    [Tooltip("Pool de enemigos ponderado para este círculo.")]
    public WeightedEnemyEntry[] enemyPool;

    [Header("Enemy Spawn Amount")]
    [Tooltip("Rango por defecto cuando no hay regla específica para una sala.")]
    [Min(0)] public int defaultMinEnemies = 1;
    [Min(0)] public int defaultMaxEnemies = 2;

    [Tooltip("Reglas por forma/tipo de sala para controlar cantidad de enemigos por círculo.")]
    public EnemySpawnCountRule[] enemySpawnCountRules;

    [Header("Inventory Drops")]
    [Range(0f, 1f)] public float enemyDropChance = 1f;
    [Tooltip("Pool de objetos de inventario ponderado para este círculo.")]
    public WeightedInventoryDropEntry[] inventoryDropPool;

    public RoomScriptable[] GetRoomPoolOrEmpty()
    {
        return roomPool ?? Array.Empty<RoomScriptable>();
    }

    public DoorScriptable[] GetDoorPoolOrEmpty()
    {
        return doorPool ?? Array.Empty<DoorScriptable>();
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

    public bool TryPickInventoryDrop(out InventoryItemDefinition itemDefinition)
    {
        itemDefinition = null;

        if (inventoryDropPool == null || inventoryDropPool.Length == 0)
        {
            return false;
        }

        List<WeightedInventoryDropEntry> validEntries = inventoryDropPool
            .Where(entry => entry != null && entry.IsValid())
            .ToList();

        if (validEntries.Count == 0)
        {
            return false;
        }

        float totalWeight = validEntries.Sum(entry => Mathf.Max(0f, entry.weight));
        if (totalWeight <= 0f)
        {
            itemDefinition = validEntries[UnityEngine.Random.Range(0, validEntries.Count)].itemDefinition;
            return itemDefinition != null;
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        foreach (WeightedInventoryDropEntry entry in validEntries)
        {
            cumulative += Mathf.Max(0f, entry.weight);
            if (roll <= cumulative)
            {
                itemDefinition = entry.itemDefinition;
                return itemDefinition != null;
            }
        }

        itemDefinition = validEntries[validEntries.Count - 1].itemDefinition;
        return itemDefinition != null;
    }

    public void GetEnemySpawnRange(RoomShape roomShape, RoomType roomType, int fallbackMin, int fallbackMax, out int minCount, out int maxCount)
    {
        int safeFallbackMin = Mathf.Max(0, fallbackMin);
        int safeFallbackMax = Mathf.Max(safeFallbackMin, fallbackMax);

        minCount = defaultMinEnemies > 0 || defaultMaxEnemies > 0
            ? Mathf.Max(0, defaultMinEnemies)
            : safeFallbackMin;
        maxCount = defaultMinEnemies > 0 || defaultMaxEnemies > 0
            ? Mathf.Max(minCount, defaultMaxEnemies)
            : safeFallbackMax;

        if (enemySpawnCountRules == null || enemySpawnCountRules.Length == 0)
        {
            return;
        }

        for (int i = 0; i < enemySpawnCountRules.Length; i++)
        {
            EnemySpawnCountRule rule = enemySpawnCountRules[i];
            if (rule == null || !rule.Matches(roomShape, roomType))
            {
                continue;
            }

            minCount = Mathf.Max(0, rule.minEnemies);
            maxCount = Mathf.Max(minCount, rule.maxEnemies);
            return;
        }
    }
}
