using Godot;

public class WorldChunkCoordinatesConverter(Vector3I chunkSize)
{
    private Vector3I _chunkSize = chunkSize;

    public WorldChunkCoordinatesConverter()
        : this(Config.Instance.GenerationConfig.ChunkSize) { }

    public Vector3I WorldToChunk(Vector3I worldPosition)
    {
        return new Vector3I(
            Mathf.FloorToInt((float)worldPosition.X / _chunkSize.X),
            Mathf.FloorToInt((float)worldPosition.Y / _chunkSize.Y),
            Mathf.FloorToInt((float)worldPosition.Z / _chunkSize.Z)
        );
    }

    public Vector3I ChunkToWorld(Vector3I chunkPosition)
    {
        return new Vector3I(
            chunkPosition.X * _chunkSize.X,
            chunkPosition.Y * _chunkSize.Y,
            chunkPosition.Z * _chunkSize.Z
        );
    }
}
