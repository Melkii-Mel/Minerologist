using System;
using System.Collections.Generic;
using System.Linq;

public class VoxelGrid : IScalarField
{
    private const int NEIGHBORS_COUNT = 26;
    public readonly Vector3I ChunkPosition;
    public readonly Vector3I WorldPosition;
    public Vector3I Size { get; }

    private bool _valueInitialized = false;
    public float Min { get; private set; }
    public float Max { get; private set; }

    private bool _borderSnapshotValueInitialized = false;
    public float BorderSnapshotMin { get; private set; }
    public float BorderSnapshotMax { get; private set; }

    private readonly float[] _data;
    private float[] _borderSnapshot;
    private VoxelGrid?[] _neighbors;
    private readonly IVoxelGridContainer _container;

    public VoxelGrid(Vector3I chunkPosition, IVoxelGridContainer container, float[]? data = null)
    {
        _container = container;

        ChunkPosition = chunkPosition;
        WorldPosition = container.ChunkToWorld(chunkPosition);
        Size = container.ChunkSize;
        var dataLength = Size.X * Size.Y * Size.Z;

        if (data != null && data.Length != dataLength)
        {
            throw new ArgumentException(
                $"Data length must be equal to Size.X * Size.Y * Size.Z, i.e. {dataLength}. Actual Data length is {data.Length}"
            );
        }

        _data = data ?? new float[Size.X * Size.Y * Size.Z];
        _neighbors = new VoxelGrid?[NEIGHBORS_COUNT];
        _borderSnapshot = new float[
            Size.Y * Size.Z + Size.X * Size.Z + Size.X * Size.Y + Size.X + Size.Y + Size.Z + 1
        ];

        if (data != null)
        {
            Min = data.Min();
            Max = data.Max();
        }
    }

    public float Sample(Vector3I pos)
    {
        return Sample(pos.X, pos.Y, pos.Z);
    }

    public float Sample(int x, int y, int z)
    {
        if (x >= 0 && x < Size.X && y >= 0 && y < Size.Y && z >= 0 && z < Size.Z)
        {
            return _data[GetIndex(x, y, z)];
        }

        // if (_isSnapshot)
        // {
        //     return _borderSnapshot[PositionToBorderIndex(x, y, z)];
        // }

        var neighborX = 0;
        var neighborY = 0;
        var neighborZ = 0;

        var localX = x;
        var localY = y;
        var localZ = z;

        if (x < 0)
        {
            neighborX = -1;
            localX = x + Size.X;
        }
        else if (x >= Size.X)
        {
            neighborX = 1;
            localX = x - Size.X;
        }

        if (y < 0)
        {
            neighborY = -1;
            localY = y + Size.Y;
        }
        else if (y >= Size.Y)
        {
            neighborY = 1;
            localY = y - Size.Y;
        }

        if (z < 0)
        {
            neighborZ = -1;
            localZ = z + Size.Z;
        }
        else if (z >= Size.Z)
        {
            neighborZ = 1;
            localZ = z - Size.Z;
        }

        var neighbor = GetOrCreateNeighbor(new Vector3I(neighborX, neighborY, neighborZ));

        var sample = neighbor.Sample(localX, localY, localZ);

        return sample;
    }

    public void Set(Vector3I localPosition, float v)
    {
        Set(localPosition.X, localPosition.Y, localPosition.Z, v);
    }

    public void Set(int x, int y, int z, float v)
    {
        var index = GetIndex(x, y, z);
        var oldValue = _data[index];
        _data[index] = v;
        if (!_valueInitialized)
        {
            _valueInitialized = true;
            Min = v;
            Max = v;
        }
        if (v < Min)
        {
            Min = v;
        }
        else if (oldValue == Min && v > oldValue)
        {
            Min = _data.Min();
        }
        if (v > Max)
        {
            Max = v;
        }
        else if (oldValue == Max && v < oldValue)
        {
            Max = _data.Max();
        }
        if (x == 0)
        {
            TryGetNeighbor(new(-1, 0, 0))?.InvalidateBorder();
        }
        if (y == 0)
        {
            TryGetNeighbor(new(0, -1, 0))?.InvalidateBorder();
        }
        if (z == 0)
        {
            TryGetNeighbor(new(0, 0, -1))?.InvalidateBorder();
        }
        if (x == 0 && y == 0)
        {
            TryGetNeighbor(new(-1, -1, 0))?.InvalidateBorder();
        }
        if (x == 0 && z == 0)
        {
            TryGetNeighbor(new(-1, 0, -1))?.InvalidateBorder();
        }
        if (y == 0 && z == 0)
        {
            TryGetNeighbor(new(0, -1, -1))?.InvalidateBorder();
        }
        if (x == 0 && y == 0 && z == 0)
        {
            TryGetNeighbor(new(-1, -1, -1))?.InvalidateBorder();
        }
    }

    public bool UpdateBorderIfInvalid()
    {
        if (_borderSnapshotValueInitialized)
        {
            return false;
        }

        SnapshotBorder();

        return true;
    }

    private void InvalidateBorder()
    {
        _borderSnapshotValueInitialized = false;
    }

    private void SnapshotBorder()
    {
        for (var i = 0; i < _borderSnapshot.Length; i++)
        {
            var position = BorderIndexToPosition(i);
            var value = Sample(position.X, position.Y, position.Z);
            if (BorderSnapshotMin > value || !_borderSnapshotValueInitialized)
            {
                BorderSnapshotMin = value;
            }
            if (BorderSnapshotMax < value || !_borderSnapshotValueInitialized)
            {
                BorderSnapshotMax = value;
            }
            _borderSnapshot[i] = value;
            _borderSnapshotValueInitialized = true;
        }
    }

    private Vector3I BorderIndexToPosition(int index)
    {
        var xs = Size.Y * Size.Z;
        var ys = xs + Size.X * Size.Z;
        var zs = ys + Size.X * Size.Y;
        var xys = zs + Size.Z;
        var xzs = xys + Size.Y;
        var yzs = xzs + Size.X;
        var xyz = yzs + 1;

        if (index < 0 || index >= xyz)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index < xs)
            return new(Size.X, index / Size.Z, index % Size.Z);

        index -= xs;
        if (index < ys - xs)
            return new(index / Size.Z, Size.Y, index % Size.Z);

        index -= ys - xs;
        if (index < zs - ys)
            return new(index / Size.Y, index % Size.Y, Size.Z);

        index -= zs - ys;
        if (index < Size.Z)
            return new(Size.X, Size.Y, index);

        index -= Size.Z;
        if (index < Size.Y)
            return new(Size.X, index, Size.Z);

        index -= Size.Y;
        if (index < Size.X)
            return new(index, Size.Y, Size.Z);

        return new(Size.X, Size.Y, Size.Z);
    }

    private int PositionToBorderIndex(int x, int y, int z)
    {
        return PositionToBorderIndex(new(x, y, z));
    }

    private int PositionToBorderIndex(Vector3I pos)
    {
        var xs = Size.Y * Size.Z;
        var ys = xs + Size.X * Size.Z;
        var zs = ys + Size.X * Size.Y;
        var xys = zs + Size.Z;
        var xzs = xys + Size.Y;
        var yzs = xzs + Size.X;

        if (pos.X == Size.X && pos.Y < Size.Y && pos.Z < Size.Z)
            return pos.Y * Size.Z + pos.Z;

        if (pos.Y == Size.Y && pos.X < Size.X && pos.Z < Size.Z)
            return xs + pos.X * Size.Z + pos.Z;

        if (pos.Z == Size.Z && pos.X < Size.X && pos.Y < Size.Y)
            return ys + pos.X * Size.Y + pos.Y;

        if (pos.X == Size.X && pos.Y == Size.Y && pos.Z < Size.Z)
            return zs + pos.Z;

        if (pos.X == Size.X && pos.Z == Size.Z && pos.Y < Size.Y)
            return xys + pos.Y;

        if (pos.Y == Size.Y && pos.Z == Size.Z && pos.X < Size.X)
            return xzs + pos.X;

        if (pos.X == Size.X && pos.Y == Size.Y && pos.Z == Size.Z)
            return yzs;

        throw new ArgumentOutOfRangeException(
            nameof(pos),
            pos,
            "Position is not part of the border snapshot."
        );
    }

    private int GetIndex(int x, int y, int z)
    {
        return x + y * Size.X + z * Size.X * Size.Y;
    }

    private int GetIndex(Vector3I pos)
    {
        return GetIndex(pos.X, pos.Y, pos.Z);
    }

    private VoxelGrid GetOrCreateNeighbor(Vector3I offset)
    {
        return _neighbors[NeighborIndex(offset)] = _container.GetOrCreateVoxelGrid(
            ChunkPosition + offset
        );
    }

    private VoxelGrid? TryGetNeighbor(Vector3I offset)
    {
        return _neighbors[NeighborIndex(offset)]
            ?? (
                _neighbors[NeighborIndex(offset)] = _container.TryGetVoxelGrid(
                    ChunkPosition + offset
                )
            );
    }

    private static int NeighborIndex(Vector3I offset)
    {
        var index = offset.X + 1 + (offset.Y + 1) * 3 + (offset.Z + 1) * 9;

        // Skip the center chunk.
        return index > 13 ? index - 1 : index;
    }

    private Vector3I FlatNeighborPositionToOffset(int neighborPositionFlat)
    {
        var index = neighborPositionFlat > 12 ? neighborPositionFlat + 1 : neighborPositionFlat;

        var z = index / 9 - 1;
        var y = index % 9 / 3 - 1;
        var x = index % 3 - 1;

        return new(x, y, z);
    }

    public bool IsEmpty(float isoLevel)
    {
        if (!_borderSnapshotValueInitialized)
        {
            SnapshotBorder();
        }
        return Min > isoLevel && BorderSnapshotMin > isoLevel
            || Max < isoLevel && BorderSnapshotMax < isoLevel;
    }

    public IEnumerable<VoxelGrid> Neighbors()
    {
        for (int i = 0; i < NEIGHBORS_COUNT; i++)
        {
            yield return GetOrCreateNeighbor(FlatNeighborPositionToOffset(i));
        }
    }

    public IEnumerable<VoxelGrid?> ExistingNeighbors()
    {
        for (int i = 0; i < NEIGHBORS_COUNT; i++)
        {
            yield return TryGetNeighbor(FlatNeighborPositionToOffset(i));
        }
    }

    public VoxelGrid CreateSnapshot()
    {
        return new(ChunkPosition, _container, (float[])_data.Clone())
        {
            BorderSnapshotMin = BorderSnapshotMin,
            BorderSnapshotMax = BorderSnapshotMax,
            Max = Max,
            Min = Min,
            _borderSnapshot = (float[])_borderSnapshot.Clone(),
            _borderSnapshotValueInitialized = _borderSnapshotValueInitialized,
            _neighbors = (VoxelGrid?[])_neighbors.Clone(),
            _valueInitialized = _valueInitialized,
        };
    }

    public float GetValue(Vector3I localPosition)
    {
        return _data[GetIndex(localPosition)];
    }
}

public interface IVoxelGridContainer
{
    Vector3I ChunkSize { get; }
    int ChunkSizeFlat { get; }

    public VoxelGrid CreateVoxelGrid(Vector3I chunkPosition);
    public VoxelGrid? TryGetVoxelGrid(Vector3I chunkPosition);
    public Vector3I WorldToChunk(Vector3I worldPosition);
    public Vector3I ChunkToWorld(Vector3I chunkPosition);

    /// <summary>
    /// An atomic operation for either getting or initializing a voxel grid
    /// </summary>
    public VoxelGrid GetOrCreateVoxelGrid(Vector3I chunkPosition);
}
