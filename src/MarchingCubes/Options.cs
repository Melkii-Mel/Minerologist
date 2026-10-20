using Tomlyn.Serialization;

public class MarchingCubesOptions
{
    public float IsoLevel { get; init; } = 0;
    public bool GenerateNormals { get; init; } = true;
    public bool GenerateUvs { get; init; } = false;
    public bool WeldVertices { get; init; } = true;

    [TomlIgnore]
    public IVertexInterpolator Interpolator { get; init; } = new LinearVertexInterpolator();

    [TomlIgnore]
    public NormalMode NormalMode { get; set; } = NormalMode.TrilinearGradient;
}
