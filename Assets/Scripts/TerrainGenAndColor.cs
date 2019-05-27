using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenAndColor : MonoBehaviour
{
    Mesh mesh;
    MeshCollider mc;
    // vertices, triangles and color array for mesh generation
    Vector3[] vertices;
    int[] triangles;
    Color[] colors;

    // base height of the mesh (editable in the IDE)
    public float meshHeight = 4;
    // size of the mesh (equal x and z size --> square)
    float meshSize = 30;
    // count of divisions on the mesh square (e.g. if set to 5, 25 partial squares result) 
    int meshDivisions = 64;

    // gradient (colormap) for the terrain
    Gradient gradient;
    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;
    float maxTerrainHeight;

    // init variables for click interactions with the terrain
    bool activeClick = false;
    Vector3 clickpos;
    Vector3 lastClick;
    Vector3 activeVert;
    int indexActiveVert = 0;

    // Start is called before the first frame update
    void Start()
    {
        // add new mesh object to the mesh filter and mesh collider defined on the empty game object
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mc = GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        // Populate the color keys at the relative time 0, 0.5 and 1 (0, 50 and 100%)
        // results in a colormap from green (low areas) over yellow to red (high areas)
        gradient = new Gradient();
        colorKey = new GradientColorKey[3];
        colorKey[0].color = Color.green;
        colorKey[0].time = 0.0f;
        colorKey[1].color = Color.yellow;
        colorKey[1].time = 0.5f;
        colorKey[2].color = Color.red;
        colorKey[2].time = 1.0f;

        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);

        CreateShape();
    }

    // Update is called once per frame
    private void Update()
    {
        manipulate_Mesh_Mouse();
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

        float halfSize = meshSize * 0.5f;
        float divisionSize = meshSize / meshDivisions;
        int triOffset = 0;

        // loop over all resulting vertices to assign each vertex position and the triangle points
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                // assign vertex position with x, y (0 -> will be set afterwards) and z coordinate
                vertices[i * (meshDivisions + 1) + j] = new Vector3(-halfSize + j * divisionSize, 0.0f, halfSize - i * divisionSize);

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

        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                if (vertices[i * (meshDivisions + 1) + j].y < 0)
                {
                    vertices[i * (meshDivisions + 1) + j].y = 0;
                    colors[i * (meshDivisions + 1) + j] = Color.blue;
                }

                else
                {
                    float height = Mathf.InverseLerp(0, maxTerrainHeight, vertices[i * (meshDivisions + 1) + j].y);
                    colors[i * (meshDivisions + 1) + j] = gradient.Evaluate(height);
                }
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
        vertices[mid + halfSize].y = (vertices[topLeft + size].y + vertices[botLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[botLeft + halfSize].y = (vertices[botLeft].y + vertices[botLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);

        float maxValue = Math.Max(vertices[mid].y, Math.Max(vertices[topLeft + halfSize].y, Math.Max(vertices[mid - halfSize].y, Math.Max(vertices[mid + halfSize].y, vertices[botLeft + halfSize].y))));

        // set the maximum terrain height variable for the
        // colorizing of the terrain afterwards over a gradient
        if (maxValue > maxTerrainHeight)
            maxTerrainHeight = maxValue;
    }

    //function for manipulating the mesh based on mouse input
    void manipulate_Mesh_Mouse()
    {
        //on update check if eft mouse key was pressed
        if (Input.GetMouseButton(0))
        {
            //Debug.Log("Left mouse key pressed");
            activeClick = true;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (mc.Raycast(ray, out hit, 200.0f))
            {
                Debug.Log("hit mesh");
                clickpos = hit.point;
                Debug.Log(clickpos);
                lastClick = clickpos;
                Vector3 nearestVertex = Vector3.zero;
                int index = 0;
                float minDistanceSqr = Mathf.Infinity;

                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 diff = lastClick - vertex;
                    float distSqr = diff.sqrMagnitude;
                    if (distSqr < minDistanceSqr)
                    {
                        indexActiveVert = index;
                        minDistanceSqr = distSqr;
                        nearestVertex = vertex;
                    }
                    index++;
                }
                activeVert = nearestVertex;
            }
            else
            {
                activeClick = false;
            }

            if (activeClick)
            {

                Vector2 delta = Input.mouseScrollDelta;

                if (delta.y != 0)
                {
                    Vector3[] verts = mesh.vertices;
                    mesh.vertices = moveVerts(verts, indexActiveVert, (delta.y * 0.1f));
                    update_Mesh();
                }
            }
        }
    }

    Vector3[] moveVerts(Vector3[] verts, int indexActiveVert, float delta)
    {
        Vector3[] resultVerts = verts;
        for (int i = 0; i < verts.Length; i++)
        {
            if (i == indexActiveVert)
            {
                //Debug.Log(resultVerts[i].y);
                //Debug.Log(delta);
                if ((resultVerts[i].y += delta) < 0)
                {
                    resultVerts[i].y = 0;
                }
            }
        }
        return resultVerts;
    }

    void update_Mesh()
    {
        //recalculate colors
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                if (mesh.vertices[i * (meshDivisions + 1) + j].y < 0)
                {
                    mesh.vertices[i * (meshDivisions + 1) + j].y = 0;
                    mesh.colors[i * (meshDivisions + 1) + j] = Color.blue;
                }
                else
                {
                    float height = Mathf.InverseLerp(0, maxTerrainHeight, mesh.vertices[i * (meshDivisions + 1) + j].y);
                    mesh.colors[i * (meshDivisions + 1) + j] = gradient.Evaluate(height);
                }
            }
        }

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
}
