using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class MarchingCubes : MonoBehaviour
{
  #region Serialized Fields

  [Header("Mesh Components")]
  [SerializeField] private MeshFilter meshFilter;
  [SerializeField] private MeshCollider meshCollider;

  [Header("Grid Settings")]
  [SerializeField, Min(1)] private int gridSize = 16;
  [SerializeField, Range(0.01f, 1f)] private float surfaceLevel = 0.5f;

  [Header("Noise Settings")]
  [SerializeField, Range(0.01f, 1f)] private float noiseScale = 0.15f;
  [SerializeField] private bool enableScroll = false;
  [SerializeField, Min(0f)] private float scrollSpeed = 1f;

  #endregion

  #region Private Properties

  private int FieldSize => gridSize + 1;

  private float ScrollValue => enableScroll ? scrollSpeed * Time.time : 0f;

  private float[] grid;

  #endregion

  #region Unity Lifecycle Methods

  private void Update()
  {
    ResetMesh();
  }

  #endregion

  #region Public Methods

  /// <summary>
  /// Resets and regenerates the mesh based on the current grid and noise settings.
  /// </summary>
  public void ResetMesh()
  {
    grid = GenerateGrid();
    Mesh mesh = CreateMesh(grid);

    if (mesh != null)
    {
      meshCollider.enabled = true;
      meshCollider.sharedMesh = mesh;
    }
    else
    {
      meshCollider.enabled = false;
    }

    meshFilter.mesh = mesh;
  }

  #endregion

  #region Grid Generation

  /// <summary>
  /// Generates the scalar field grid using 3D Perlin noise.
  /// </summary>
  /// <returns>A row-major 1D float array representing the 3D scalar field.</returns>
  private float[] GenerateGrid()
  {
    int totalSize = FieldSize * FieldSize * FieldSize;
    float[] grid = new float[totalSize];

    float curScrollVal = ScrollValue;
    Parallel.For(0, FieldSize, i =>
    {
      for (int j = 0; j < FieldSize; j++)
      {
        for (int k = 0; k < FieldSize; k++)
        {
          float x = i;
          float y = j;
          float z = k;

          // Sample scalar field calculation
          float noise = PerlinNoise3D(x * noiseScale + curScrollVal, y * noiseScale, z * noiseScale + curScrollVal);

          int index = Get1DIndex(i, j, k);
          grid[index] = noise;
        }
      }
    });

    return grid;
  }

  #endregion

  #region Mesh Creation

  /// <summary>
  /// Creates a mesh from the scalar field grid using the Marching Cubes algorithm.
  /// </summary>
  /// <param name="grid">The scalar field grid.</param>
  /// <returns>A generated Mesh object or null if no geometry is created.</returns>
  private Mesh CreateMesh(float[] grid)
  {
    Mesh mesh = new Mesh
    {
      name = "Generated Marching Cubes Mesh"
    };

    Vector3[] vertices = new Vector3[gridSize * gridSize * gridSize * 12];
    ConcurrentBag<int> triangles = new ConcurrentBag<int>();

    Parallel.For(0, gridSize * gridSize * gridSize, baseIndex =>
    {
      // Convert baseIndex to 3D coordinates
      (int i, int j, int k) = Get3DCoordinates(baseIndex);

      // Calculate the cube index based on the scalar values at the cube's corners
      int cubeIndex = CalculateCubeIndex(grid, i, j, k);

      if (CubeLookup.TriangleConnectionTable[cubeIndex][0] == -1)
        return; // No triangles for this cube

      // Generate vertices for the intersected edges
      for (int edge = 0; edge < 12; edge++)
      {
        int vertexIndex = baseIndex * 12 + edge;
        vertices[vertexIndex] = CalculateVertexPosition(grid, i, j, k, edge);
      }

      // Add triangles based on the triangle table
      for (int t = 0; t < CubeLookup.TriangleConnectionTable[cubeIndex].Length; t += 3)
      {
        if (CubeLookup.TriangleConnectionTable[cubeIndex][t] == -1)
          break;

        int tri0 = CubeLookup.TriangleConnectionTable[cubeIndex][t] + baseIndex * 12;
        int tri1 = CubeLookup.TriangleConnectionTable[cubeIndex][t + 1] + baseIndex * 12;
        int tri2 = CubeLookup.TriangleConnectionTable[cubeIndex][t + 2] + baseIndex * 12;

        triangles.Add(tri0);
        triangles.Add(tri1);
        triangles.Add(tri2);
      }
    });

    // Assign vertices and triangles to the mesh
    mesh.vertices = vertices;
    mesh.triangles = triangles.ToArray();

    // Assign colors based on vertex height
    Color[] colors = new Color[vertices.Length];
    for (int i = 0; i < vertices.Length; i++)
    {
      colors[i] = Color.Lerp(Color.green, Color.gray, vertices[i].y / gridSize);
    }
    mesh.colors = colors;

    // Validate mesh integrity
    if (mesh.triangles.Length == 0)
      return null;

    // Optimize and recalculate mesh data
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    mesh.Optimize();

    return mesh;
  }

  #endregion

  #region Helper Methods

  /// <summary>
  /// Calculates the cube index for the Marching Cubes algorithm.
  /// </summary>
  /// <param name="grid">The scalar field grid.</param>
  /// <param name="i">The x-coordinate of the cube.</param>
  /// <param name="j">The y-coordinate of the cube.</param>
  /// <param name="k">The z-coordinate of the cube.</param>
  /// <returns>The cube index indicating which vertices are inside the surface.</returns>
  private int CalculateCubeIndex(float[] grid, int i, int j, int k)
  {
    int cubeIndex = 0;

    for (int corner = 0; corner < 8; corner++)
    {
      int dx = corner & 1;
      int dy = (corner >> 1) & 1;
      int dz = (corner >> 2) & 1;

      int index = Get1DIndex(i + dx, j + dy, k + dz);

      if (grid[index] < surfaceLevel)
      {
        cubeIndex |= 1 << corner;
      }
    }

    return cubeIndex;
  }

  /// <summary>
  /// Calculates the interpolated vertex position on the specified edge of the cube.
  /// </summary>
  /// <param name="grid">The scalar field grid.</param>
  /// <param name="i">The x-coordinate of the cube.</param>
  /// <param name="j">The y-coordinate of the cube.</param>
  /// <param name="k">The z-coordinate of the cube.</param>
  /// <param name="edge">The edge index (0-11).</param>
  /// <returns>The interpolated vertex position.</returns>
  private Vector3 CalculateVertexPosition(float[] grid, int i, int j, int k, int edge)
  {
    int cornerA = CubeLookup.EdgeVertexIndices[edge, 0];
    int cornerB = CubeLookup.EdgeVertexIndices[edge, 1];

    // Convert edge corners to 3D coordinates
    (int ai, int aj, int ak) = GetCornerCoordinates(i, j, k, cornerA);
    (int bi, int bj, int bk) = GetCornerCoordinates(i, j, k, cornerB);

    // Get scalar values at the corners
    float valA = grid[Get1DIndex(ai, aj, ak)];
    float valB = grid[Get1DIndex(bi, bj, bk)];

    // Calculate interpolation factor
    float t = Mathf.Clamp01((surfaceLevel - valA) / (valB - valA));

    // Calculate world positions
    Vector3 posA = new Vector3(ai, aj, ak);
    Vector3 posB = new Vector3(bi, bj, bk);

    // Interpolate between the two positions
    return Vector3.Lerp(posA, posB, t);
  }

  /// <summary>
  /// Converts a base 1D index to 3D coordinates.
  /// </summary>
  /// <param name="baseIndex">The base 1D index.</param>
  /// <returns>A tuple containing the 3D coordinates (i, j, k).</returns>
  private (int i, int j, int k) Get3DCoordinates(int baseIndex)
  {
    int i = baseIndex / (gridSize * gridSize);
    int j = (baseIndex / gridSize) % gridSize;
    int k = baseIndex % gridSize;
    return (i, j, k);
  }

  /// <summary>
  /// Converts 3D coordinates to a 1D index.
  /// </summary>
  /// <param name="i">The x-coordinate.</param>
  /// <param name="j">The y-coordinate.</param>
  /// <param name="k">The z-coordinate.</param>
  /// <returns>The corresponding 1D index.</returns>
  private int Get1DIndex(int i, int j, int k)
  {
    return i * FieldSize * FieldSize + j * FieldSize + k;
  }

  /// <summary>
  /// Retrieves the 3D coordinates of a cube corner based on the corner index.
  /// </summary>
  /// <param name="i">The base x-coordinate of the cube.</param>
  /// <param name="j">The base y-coordinate of the cube.</param>
  /// <param name="k">The base z-coordinate of the cube.</param>
  /// <param name="corner">The corner index (0-7).</param>
  /// <returns>A tuple containing the 3D coordinates (x, y, z) of the corner.</returns>
  private (int x, int y, int z) GetCornerCoordinates(int i, int j, int k, int corner)
  {
    int x = i + (corner & 1);
    int y = j + ((corner >> 1) & 1);
    int z = k + ((corner >> 2) & 1);
    return (x, y, z);
  }

  /// <summary>
  /// Computes a 3D Perlin noise value.
  /// </summary>
  /// <param name="x">The x-coordinate.</param>
  /// <param name="y">The y-coordinate.</param>
  /// <param name="z">The z-coordinate.</param>
  /// <returns>The computed Perlin noise value.</returns>
  private float PerlinNoise3D(float x, float y, float z)
  {
    float noiseX = Mathf.PerlinNoise(y, z);
    float noiseY = Mathf.PerlinNoise(x, z);
    float noiseZ = Mathf.PerlinNoise(x, y);
    return (noiseX + noiseY + noiseZ) / 3f;
  }

  #endregion
}