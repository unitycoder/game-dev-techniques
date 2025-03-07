using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

  int chunkSize = 8;
  float chunkScale = 1f;
  float isoLevel = 0.5f;
  int seed = 0;
  float elevation = 1f;

  private CubeVertex[,,] vertices = null;
  private SurfaceManager surfaceManager = null;
  private MeshFilter meshFilter = null;

  void Start()
  {
    surfaceManager = FindObjectOfType<SurfaceManager>();
    meshFilter = GetComponent<MeshFilter>();
    if (meshFilter == null)
    {
      meshFilter = gameObject.AddComponent<MeshFilter>();
    }
    GetConfig();
    GenerateChunk();
  }

  private void GetConfig()
  {
    if (surfaceManager != null)
    {
      chunkSize = surfaceManager.GetChunkSize();
      chunkScale = surfaceManager.GetChunkScale();
      isoLevel = surfaceManager.GetIsoLevel();
      seed = surfaceManager.GetSeed();
      elevation = surfaceManager.GetElevation();
    }
  }

  // ====================== Generation ====================== //

  private void GenerateChunk()
  {
    DistributeVertices();
    GenerateMesh();
  }

  /**
	 * Distribute vertices in a grid
	 */
  private void DistributeVertices()
  {
    vertices = new CubeVertex[chunkSize, chunkSize, chunkSize];
    for (int x = 0; x < chunkSize; x++)
    {
      for (int y = 0; y < chunkSize; y++)
      {
        for (int z = 0; z < chunkSize; z++)
        {
          Vector3Int position = new Vector3Int(x, y, z);
          Vector3 transformPosition = new Vector3(x, y, z) * chunkScale;
          Vector3 worldPosition = transform.position + transformPosition;
          vertices[x, y, z] = new CubeVertex(GenerateValue(position, transformPosition, worldPosition), transformPosition);
        }
      }
    }
  }

  /**
	 * Generate chunk mesh
	 */
  private void GenerateMesh()
  {
    Vector3[] cubeVertices = new Vector3[0];
    List<int> triangles = new List<int>();

    for (int x = 0; x < chunkSize - 1; x++)
    {
      for (int y = 0; y < chunkSize - 1; y++)
      {
        for (int z = 0; z < chunkSize - 1; z++)
        {
          int offset = cubeVertices.Length;
          Vector3Int position = new Vector3Int(x, y, z);

          cubeVertices = ConcatVertices(cubeVertices, GetVertices(position));
          triangles.AddRange(GetTriangles(position, offset));
        }
      }
    }

    Mesh mesh = new Mesh();
    meshFilter.mesh = mesh;

    mesh.vertices = cubeVertices;
    mesh.triangles = triangles.ToArray();
    mesh.RecalculateNormals();
  }

  // ====================== Utils ====================== //

  /**
	 * Interpolate between two points (P1, P2) by a factor of the inverse lerp
   * between the iso level and the two points's values (V1, V2)
   * P = P1 + (isoLevel - V1)(P2 - P1) / (V2 - V1)
	 */
  private Vector3 VertexInterp(Vector3Int position, int index1, int index2)
  {

    CubeVertex vertex1 = GetVertex(position, index1);
    CubeVertex vertex2 = GetVertex(position, index2);

    float v1 = vertex1.GetValue();
    float v2 = vertex2.GetValue();

    float lerpState = Mathf.InverseLerp(v1, v2, isoLevel);

    Vector3 p1 = vertex1.GetPosition();
    Vector3 p2 = vertex2.GetPosition();

    return Vector3.Lerp(p1, p2, lerpState);
  }

  public CubeVertex GetVertex(Vector3Int position, int vertex)
  {
    Vector3Int index = position + GetVertexOffset(vertex);
    return vertices[index.x, index.y, index.z];
  }

  public Vector3Int GetVertexOffset(int index)
  {
    switch (index)
    {
      case 0: return new Vector3Int(0, 0, 1);
      case 1: return new Vector3Int(1, 0, 1);
      case 2: return new Vector3Int(1, 0, 0);
      case 3: return new Vector3Int(0, 0, 0);
      case 4: return new Vector3Int(0, 1, 1);
      case 5: return new Vector3Int(1, 1, 1);
      case 6: return new Vector3Int(1, 1, 0);
      case 7: return new Vector3Int(0, 1, 0);
      default: return Vector3Int.zero;
    }
  }

  public int[] GetEdgeVertices(int edge)
  {
    switch (edge)
    {
      case 0: return new int[] { 0, 1 };
      case 1: return new int[] { 1, 2 };
      case 2: return new int[] { 2, 3 };
      case 3: return new int[] { 3, 0 };
      case 4: return new int[] { 4, 5 };
      case 5: return new int[] { 5, 6 };
      case 6: return new int[] { 6, 7 };
      case 7: return new int[] { 7, 4 };
      case 8: return new int[] { 4, 0 };
      case 9: return new int[] { 5, 1 };
      case 10: return new int[] { 6, 2 };
      case 11: return new int[] { 7, 3 };
      default: return new int[] { -1, -1 };
    }
  }

  public Vector3[] ConcatVertices(Vector3[] vertices1, Vector3[] vertices2)
  {
    Vector3[] vertices = new Vector3[vertices1.Length + vertices2.Length];
    vertices1.CopyTo(vertices, 0);
    vertices2.CopyTo(vertices, vertices1.Length);
    return vertices;
  }

  float GenerateValue(Vector3Int position, Vector3 objectPosition, Vector3 worldPosition)
  {
    // Perlin noise based ⬇️

    // float highPerlinFrequency = 0.04f;
    // float lowPerlinFrequency = 0.5f;

    // float value = 1 - ((objectPosition.y + elevation) / (chunkHeight * chunkDensity));
    // value += Mathf.PerlinNoise(worldPosition.x * highPerlinFrequency, worldPosition.z * highPerlinFrequency) * .5f;
    // value += Mathf.PerlinNoise(worldPosition.x * lowPerlinFrequency, worldPosition.z * lowPerlinFrequency) * 5f;
    // value = value / 6.5f;

    // RNG based ⬇️

    if (position.y == 0)
    {
      return 1f;
    }
    if (position.x == 0 || position.z == 0 || position.x == chunkSize - 1 || position.z == chunkSize - 1)
    {
      return 1 / (1 + position.y);
    }
    float height = position.y;

    float expectedValue = 1 / Mathf.Exp(height);
    float value = UnityEngine.Random.Range(expectedValue / 2, 3 * expectedValue / 2) * UnityEngine.Random.Range(0f, elevation);

    // Debug.Log(value);
    return value;
  }

  // ====================== Table Logic ====================== //

  private int GetCubeIndex(Vector3Int position)
  {
    int cubeindex = 0;

    for (int i = 0; i < 8; i++)
    {
      if (GetVertex(position, i).GetValue() > isoLevel) cubeindex |= (1 << i);
    }

    return cubeindex;
  }

  private Vector3[] GetVertices(Vector3Int position)
  {
    int cubeIndex = GetCubeIndex(position);
    int edges = Tables.edgeTable[cubeIndex];
    Vector3[] cubeVertices = new Vector3[12];

    if (edges == 0)
    {
      return cubeVertices;
    }

    for (int i = 0; i < 12; i++)
    {
      int edgeCode = 1 << i;
      if ((edges & edgeCode) > 0)
      {
        int[] edgeVertices = GetEdgeVertices(i);
        cubeVertices[i] = VertexInterp(position, edgeVertices[0], edgeVertices[1]);
      }
    }

    return cubeVertices;
  }

  private List<int> GetTriangles(Vector3Int position, int offset)
  {
    int cubeIndex = GetCubeIndex(position);
    List<int> triangles = new List<int>();

    for (int i = 0; Tables.triTable[cubeIndex, i] != -1; i += 3)
    {
      triangles.Add(Tables.triTable[cubeIndex, i + 2] + offset);
      triangles.Add(Tables.triTable[cubeIndex, i + 1] + offset);
      triangles.Add(Tables.triTable[cubeIndex, i] + offset);
    }

    return (triangles);
  }

  // ====================== Gizmos ====================== //

  void OnDrawGizmosSelected()
  {
    if (Application.isPlaying && chunkSize <= 16)
    {
      for (int x = 0; x < chunkSize; x++)
      {
        for (int y = 0; y < chunkSize; y++)
        {
          for (int z = 0; z < chunkSize; z++)
          {
            CubeVertex vertex = vertices[x, y, z];
            float shade = vertex.GetValue() >= isoLevel ? 1f : 0f;
            Gizmos.color = new Color(shade, shade, shade, 1f);
            Gizmos.DrawSphere(vertex.GetPosition() + transform.position, 0.03f * chunkScale);
          }
        }
      }
    }
    Gizmos.color = Color.white;
    Vector3 dimension = new Vector3(chunkSize - 1, chunkSize - 1, chunkSize - 1);
    Gizmos.DrawWireCube(transform.position + dimension / 2f, dimension);
  }
}
