using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private BoardLayout layout;
    [SerializeField] private Transform generatedChunks;

    [Header("Randomness")]
    [SerializeField] private int seed = 12345;
    [SerializeField] private bool randomizeSeedOnGenerate;

    [Header("Placement")]
    [SerializeField] private bool generateOnStart;
    [SerializeField] private bool warnOnOverlappingChunks;

    public int CurrentSeed { get; private set; }

    private void Start()
    {
        if (generateOnStart)
            Generate();
    }

    [ContextMenu("Generate Board")]
    public void Generate()
    {
        if (layout == null)
        {
            Debug.LogError("BoardGenerator needs a BoardLayout assigned.", this);
            return;
        }

        EnsureGeneratedRoot();
        ClearGeneratedChunks();

        CurrentSeed = randomizeSeedOnGenerate ? Environment.TickCount : seed;
        System.Random random = new System.Random(CurrentSeed);
        HashSet<ChunkDefinition> usedNonRepeatingChunks = new HashSet<ChunkDefinition>();
        List<Collider> generatedColliders = new List<Collider>();

        foreach (BoardSocketDefinition socket in layout.sockets)
        {
            if (socket == null)
                continue;

            ChunkDefinition chunk = ChooseChunk(socket, random, usedNonRepeatingChunks);
            if (chunk == null || chunk.prefab == null)
            {
                Debug.LogWarning($"No valid chunk is available for socket '{socket.id}'.", this);
                continue;
            }

            Vector3 localPosition = GetSocketPosition(socket, random);
            Quaternion localRotation = GetSocketRotation(socket, chunk, random);
            GameObject instance = Instantiate(
                chunk.prefab,
                generatedChunks,
                false);

            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.name = string.IsNullOrEmpty(chunk.chunkId)
                ? socket.id
                : socket.id + "_" + chunk.chunkId;

            if (!chunk.allowRepeat)
                usedNonRepeatingChunks.Add(chunk);

            if (warnOnOverlappingChunks)
                AddGeneratedColliders(instance, generatedColliders);
        }

        Debug.Log("Generated board with seed " + CurrentSeed + ".", this);
    }

    [ContextMenu("Clear Generated Chunks")]
    public void ClearGeneratedChunks()
    {
        EnsureGeneratedRoot();

        for (int i = generatedChunks.childCount - 1; i >= 0; i--)
        {
            GameObject child = generatedChunks.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private ChunkDefinition ChooseChunk(
        BoardSocketDefinition socket,
        System.Random random,
        HashSet<ChunkDefinition> usedNonRepeatingChunks)
    {
        List<ChunkDefinition> candidates = new List<ChunkDefinition>();

        foreach (ChunkDefinition chunk in socket.allowedChunks)
        {
            if (chunk == null || chunk.prefab == null)
                continue;

            if (!string.IsNullOrEmpty(socket.socketType) &&
                !string.IsNullOrEmpty(chunk.socketType) &&
                !string.Equals(socket.socketType, chunk.socketType, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!chunk.allowRepeat && usedNonRepeatingChunks.Contains(chunk))
                continue;

            if (chunk.weight > 0)
                candidates.Add(chunk);
        }

        if (candidates.Count == 0)
            return null;

        int totalWeight = 0;
        foreach (ChunkDefinition candidate in candidates)
            totalWeight += candidate.weight;

        int value = random.Next(0, totalWeight);
        foreach (ChunkDefinition candidate in candidates)
        {
            value -= candidate.weight;
            if (value < 0)
                return candidate;
        }

        return candidates[candidates.Count - 1];
    }

    private Vector3 GetSocketPosition(BoardSocketDefinition socket, System.Random random)
    {
        if (!socket.useRandomRegion)
            return socket.localPosition;

        float x = Mathf.Lerp(
            socket.randomRegionMin.x,
            socket.randomRegionMax.x,
            (float)random.NextDouble());
        float z = Mathf.Lerp(
            socket.randomRegionMin.y,
            socket.randomRegionMax.y,
            (float)random.NextDouble());

        return new Vector3(x, socket.localPosition.y, z);
    }

    private Quaternion GetSocketRotation(
        BoardSocketDefinition socket,
        ChunkDefinition chunk,
        System.Random random)
    {
        float yRotation = chunk.allowRandomRotation ? random.Next(0, 4) * 90f : 0f;
        return Quaternion.Euler(socket.localEulerAngles + new Vector3(0f, yRotation, 0f));
    }

    private void EnsureGeneratedRoot()
    {
        if (generatedChunks != null)
            return;

        Transform existingRoot = transform.Find("GeneratedChunks");
        if (existingRoot != null)
        {
            generatedChunks = existingRoot;
            return;
        }

        GameObject root = new GameObject("GeneratedChunks");
        root.transform.SetParent(transform, false);
        generatedChunks = root.transform;
    }

    private void AddGeneratedColliders(GameObject instance, List<Collider> generatedColliders)
    {
        Collider[] colliders = instance.GetComponentsInChildren<Collider>();

        foreach (Collider collider in colliders)
        {
            foreach (Collider existingCollider in generatedColliders)
            {
                if (collider.bounds.Intersects(existingCollider.bounds))
                {
                    Debug.LogWarning(
                        "Generated chunk bounds overlap: " + instance.name,
                        instance);
                    break;
                }
            }

            generatedColliders.Add(collider);
        }
    }
}