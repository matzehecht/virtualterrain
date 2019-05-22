using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenAndColor : MonoBehaviour
{
    Mesh mesh;
    // vertices and triangles array for mesh generation
    Vector3[] vertices;
    int[] triangles;

    // --- public variables are editable in the Unity Inspector afterwards
    // x and z size of generating mesh
    public int xSize = 20;
    public int zSize = 20;
    // height scale of the generating mesh
    public float heightScale = 3f;
    // gradient (colormap) of for the terrain
    public Gradient gradient;

    Color[] colors;
    float minTerrainHeight;
    float maxTerrainHeight;

    // Start is called before the first frame update
    void Start()
    {
        // add new mesh object to the mesh filter defined on the empty game object
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // Update is called once per frame
    private void Update()
    {
        // on every frame, create the shape and update the mesh
        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        // define vertex count 
        // need one more vertex on each coordinate than the defined mesh size
        // (e.g. need 3*3 vertices for a plane out of 4 squares)
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        // loop over all vertices to assign each position
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                // calculate height of each vertex with the perlin noise function 
                // and multiply it with the height scale set by the user
                float y = Mathf.PerlinNoise(x * .3f, z * .3f) * heightScale;

                // assign the vertex position with x, y and z coordinate
                vertices[i] = new Vector3(x, y, z);

                // set the maximum and minimum terrain height variables for the
                // colorizing of the terrain afterwards over a gradient
                if (y > maxTerrainHeight)
                    maxTerrainHeight = y;

                if (y < minTerrainHeight)
                    minTerrainHeight = y;

                i++;
            }
        }

        // define count of triangle points (6 for one mesh quad)
        triangles = new int[xSize * zSize * 6];

        // variables to track actual vertex and actual triangle
        int vert = 0;
        int tris = 0;

        // loop over all mesh quads and define the triangle points
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                // first triangle (bottom left, top left, bottom right of a quad)
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                // second triangle (bottom right, top left, top right of a quad)
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        // define color array (same length tan vertices array, because each vertex has one color)
        colors = new Color[vertices.Length];

        // loop over the vertices and set the colors (same array index in the vertices and color array) 
        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                // get height value as percentage of the minimum and maximum height of the total mesh
                float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[i].y);
                // calculate and set color at the calculated "time" (height value as percentage value) of the gradient/colormap 
                colors[i] = gradient.Evaluate(height);
                //Debug.Log("height: " + height + " color: " + colors[i]);
                i++;
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        // set all calculated or defined mesh properties
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }
}
