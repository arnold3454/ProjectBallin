using UnityEngine;

[CreateAssetMenu(menuName = "Project Ballin/Board Chunk", fileName = "ChunkDefinition")]
public class ChunkDefinition : ScriptableObject
{
    [Tooltip("Stable name used in generated object names and debug output.")]
    public string chunkId;

    public GameObject prefab;

    [Tooltip("Only sockets with a matching type can use this chunk. Leave empty to allow any socket.")]
    public string socketType;

    [Min(0)]
    public int weight = 1;

    [Tooltip("If disabled, this chunk can be selected only once per generated board.")]
    public bool allowRepeat = true;

    [Tooltip("Allows the generator to choose a random rotation around the board's up axis.")]
    public bool allowRandomRotation;
}