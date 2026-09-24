using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Ballin/Board Layout", fileName = "BoardLayout")]
public class BoardLayout : ScriptableObject
{
    [Tooltip("Used for random-region placement and editor validation. X is board width, Y is board length.")]
    public Vector2 boardSize = new Vector2(10f, 30f);

    public List<BoardSocketDefinition> sockets = new List<BoardSocketDefinition>();
}

[Serializable]
public class BoardSocketDefinition
{
    public string id = "Socket";

    [Tooltip("Chunks must have this socket type to be eligible. Leave empty to allow any type.")]
    public string socketType;

    [Tooltip("Position relative to the BoardGenerator transform when Use Random Region is disabled.")]
    public Vector3 localPosition;

    public Vector3 localEulerAngles;

    [Header("Chunk Selection")]
    public List<ChunkDefinition> allowedChunks = new List<ChunkDefinition>();

    [Header("Random Placement")]
    public bool useRandomRegion;

    [Tooltip("Local X/Z rectangle used when Use Random Region is enabled.")]
    public Vector2 randomRegionMin;

    public Vector2 randomRegionMax;
}