using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenAndColor : MonoBehaviour
{
    Mesh mesh;
    // vertices, triangles and color array for mesh generation
    Vector3[] vertices;
    int[] triangles;
    Color[] colors;

    // --- public variables are editable in the Unity Inspector afterwards
    // size of the mesh (equal x and z size --> square)
    public float meshSize;
    // count of divisions on the mesh square (e.g. if set to 5, 25 partial squares result) 
    public int meshDivisions;
    // base height of the mesh
    public float meshHeight;

    // gradient (colormap) for the terrain
    public Gradient gradient;

    float minTerrainHeight;
    float maxTerrainHeight;

    // Start is called before the first frame update
    void Start()
    {
        CreateShape();
    }

    void CreateShape()
    {
        // define vertex count 
        // need one more vertex on each coordinate than the defined division size
        // (e.g. need 3*3 vertices for a plane out of 4 squares)
        int mVertCount = (meshDivisions + 1) * (meshDivisions + 1);
        vertices = new Vector3[mVertCount];
        colors = new Color[vertices.Length];

        // define count of triangle points (6 for one mesh square)
        triangles = new int[meshDivisions * meshDivisions * 6];

        // add new mesh object to the mesh filter defined on the empty game object
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        float halfSize = meshSize * 0.5f;
        float divisionSize = meshSize / meshDivisions;
        int triOffset = 0;

        // loop over all resulting vertices to assign each vertex position and the triangle points
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                // assign vertex position with x, y (0 -> will be set afterwards) and z coordinate
                vertices[i*(meshDivisions+1)+j] = new Vector3(-halfSize + j*divisionSize, 0.0f, halfSize - i*divisionSize);

                // set triangle points
                if (i < meshDivisions && j < meshDivisions)
                {
                    int topLeft = i * (meshDivisions + 1) + j;
                    int botLeft = (i + 1) * (meshDivisions + 1) + j;

                    // first triangle (bottom left, top left, bottom right of a quad)
                    triangles[triOffset] = topLeft;
                    triangles[triOffset + 1] = topLeft + 1;
                    triangles[triOffset + 2] = botLeft + 1;

                    // second triangle (bottom right, top left, top right of a quad)
                    triangles[triOffset + 3] = topLeft;
                    triangles[triOffset + 4] = botLeft + 1;
                    triangles[triOffset + 5] = botLeft;

                    triOffset += 6;
                }
            }
        }

        vertices[0].y = UnityEngine.Random.Range(-meshHeight, meshHeight);
        vertices[meshDivisions].y = UnityEngine.Random.Range(-meshHeight, meshHeight);
        vertices[vertices.Length - 1].y = UnityEngine.Random.Range(-meshHeight, meshHeight);
        vertices[vertices.Length - 1 - meshDivisions].y = UnityEngine.Random.Range(-meshHeight, meshHeight);

        int iterations = (int)Mathf.Log(meshDivisions, 2);
        int numSquares = 1;
        int squareSize = meshDivisions;
        for (int i = 0; i < iterations; i++)
        {
            int row = 0;
            for (int j = 0; j < numSquares; j++)
            {
                int col = 0;
                for (int k = 0; k < numSquares; k++)
                {
                    DiamondSquare(row, col, squareSize, meshHeight);
                    col += squareSize;
                }
                row += squareSize;
            }
            numSquares *= 2;
            squareSize /= 2;
            meshHeight *= 0.5f;
        }

        for (int c = 0, i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[c].y);
                colors[i * (meshDivisions + 1) + j] = gradient.Evaluate(height);
                Debug.Log(c);
                c++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    void DiamondSquare(int row, int col, int size, float offset)
    {
        int halfSize = (int)(size * 0.5f);
        int topLeft = row * (meshDivisions + 1) + col;
        int botLeft = (row + size) * (meshDivisions + 1) + col;

        int mid = (int)(row + halfSize) * (meshDivisions + 1) + (int)(col + halfSize);
        vertices[mid].y = (vertices[topLeft].y + vertices[topLeft + size].y + vertices[botLeft].y + vertices[botLeft + size].y) * 0.25f + UnityEngine.Random.Range(-offset, offset);

        vertices[topLeft + halfSize].y = (vertices[topLeft].y + vertices[topLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid - halfSize].y = (vertices[topLeft].y + vertices[botLeft].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid + halfSize].y = (vertices[topLeft+size].y + vertices[botLeft+size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[botLeft+halfSize].y = (vertices[botLeft].y + vertices[botLeft+size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);

        float maxValue = Math.Max(vertices[mid].y, Math.Max(vertices[topLeft + halfSize].y, Math.Max(vertices[mid - halfSize].y, Math.Max(vertices[mid + halfSize].y, vertices[botLeft + halfSize].y))));
        float minValue = Math.Min(vertices[mid].y, Math.Min(vertices[topLeft + halfSize].y, Math.Min(vertices[mid - halfSize].y, Math.Min(vertices[mid + halfSize].y, vertices[botLeft + halfSize].y))));

        // set the maximum and minimum terrain height variables for the
        // colorizing of the terrain afterwards over a gradient
        if (maxValue > maxTerrainHeight)
            maxTerrainHeight = maxValue;

        if (minValue < minTerrainHeight)
            minTerrainHeight = minValue;
    }
}
