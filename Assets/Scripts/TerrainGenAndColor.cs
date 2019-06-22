using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenAndColor : MonoBehaviour
{
    private Mesh mesh;
    private MeshCollider mc;
    private Renderer rend;
    private ParticleSystem ps;
    // vertices, triangles and uv array for mesh generation
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    // base height of the mesh (editable in the IDE)
    private float terrainOffset;
    // size of the mesh (equal x and z size --> square)
    private float meshSize = 30;
    // count of divisions on the mesh square (e.g. if set to 5, 25 partial squares result) 
    private int meshDivisions = 128;

    /* variables for maximum terrain height and binary water texture for color calculation in 
        the shader afterwards */
    private float maxTerrainHeight;
    private Vector3 maxVertice;
    /* water texture could be used, to hand over a binary texture to the shader with information
        about which areas are water and which areas are terrain */
    // Texture2D waterTex;

    // init variables for click interactions with the terrain
    private bool activeClick = false;
    private Vector3 clickpos;
    private Vector3 lastClick;
    private Vector3 activeVert;
    private int indexActiveVert = 0;
    private double gaussianVariance;
    private int size;

    // Start is called before the first frame update
    void Start()
    {
        terrainOffset = StaticClass.getTerrainOffset();
        gaussianVariance = StaticClass.getGaussianVariance();
        // add new mesh object to the mesh filter and mesh collider defined on the empty game object
        mesh = new Mesh();
        // set index format to 32 bit -> more than 65K vertices could be rendered
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        // get some components of the game object to perform different actions with them afterwards
        GetComponent<MeshFilter>().mesh = mesh;
        mc = GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        ps = GetComponent<ParticleSystem>();
        rend = GetComponent<Renderer>();

        // call inital method for terrain generation with the diamond square algorithm
        CreateShape();
    }

    // Update is called once per frame
    private void Update()
    {
        size = Convert.ToInt32(gaussianVariance*0.15);
        manipulate_Mesh_Mouse();
    }

    // function for the creation of the initial terrain with the diamond square algorithm
    void CreateShape()
    {
        // define vertex and uv count 
        // need one more vertex on each coordinate than the defined division size
        // (e.g. need 3*3 vertices for a plane out of 4 squares)
        int mVertCount = (meshDivisions + 1) * (meshDivisions + 1);
        vertices = new Vector3[mVertCount];
        uvs = new Vector2[mVertCount];

        // define count of triangle points (6 for one mesh square)
        triangles = new int[meshDivisions * meshDivisions * 6];

        // define new texture with size of mesh divisions + 1
        // waterTex = new Texture2D(meshDivisions+1, meshDivisions+1);

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

                // assign uv vector
                uvs[i * (meshDivisions + 1) + j] = new Vector2((float)i / meshDivisions, (float)j / meshDivisions);

                // set triangle points
                if (i < meshDivisions && j < meshDivisions)
                {
                    int topLeft = i * (meshDivisions + 1) + j;
                    int botLeft = (i + 1) * (meshDivisions + 1) + j;

                    // first triangle (top left, top right, bottom right of a quad)
                    triangles[triOffset] = topLeft;
                    triangles[triOffset + 1] = topLeft + 1;
                    triangles[triOffset + 2] = botLeft + 1;

                    // second triangle (top left, bottom right, bottom left of a quad)
                    triangles[triOffset + 3] = topLeft;
                    triangles[triOffset + 4] = botLeft + 1;
                    triangles[triOffset + 5] = botLeft;

                    triOffset += 6;
                }
            }
        }

        // initiate the corner points with random values of the available terrain offset space
        vertices[0].y = UnityEngine.Random.Range(-terrainOffset, terrainOffset);
        vertices[meshDivisions].y = UnityEngine.Random.Range(-terrainOffset, terrainOffset);
        vertices[vertices.Length - 1].y = UnityEngine.Random.Range(-terrainOffset, terrainOffset);
        vertices[vertices.Length - 1 - meshDivisions].y = UnityEngine.Random.Range(-terrainOffset, terrainOffset);

        // calculate the diamond square algorithm and set the terrain heights
        // get the iterations count with the logarithmus of the mesh divisions count on base 2
        int iterations = (int)Mathf.Log(meshDivisions, 2);
        int numSquares = 1;
        int squareSize = meshDivisions;
        // for each iteration, perform one diamond and one square step for all current squares
        for (int i = 0; i < iterations; i++)
        {
            int row = 0;
            for (int j = 0; j < numSquares; j++)
            {
                int col = 0;
                for (int k = 0; k < numSquares; k++)
                {
                    // call function, which executes the diamond square algorithm
                    DiamondSquare(row, col, squareSize, terrainOffset);
                    col += squareSize;
                }
                row += squareSize;
            }
            // double the amount of squares and half the square size in each iteration
            numSquares *= 2;
            squareSize /= 2;
            // reduce the magnitude of the terrain offset in each iteration to the half
            // to have a "smoother" terrain
            terrainOffset *= 0.5f;
        }

        // reset negative height values to 0 
        // (and set pixels of the water texture --> water pixels (height value 0) to white, 
        // terrain pixels (height value > 0) to black)
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                if (vertices[i * (meshDivisions + 1) + j].y <= 0)
                {
                    vertices[i * (meshDivisions + 1) + j].y = 0;
                    // waterTex.SetPixel(i, j, Color.white);
                }
                /* else
                {
                    waterTex.SetPixel(i, j, Color.black);
                } */
            }
        }

        // apply the calculated texture
        // waterTex.Apply();

        // set the calculated maximum terrain height (and water texture to the material), 
        // that the shader can work with this information
        rend.material.SetFloat("_maxTerrainHeight", maxTerrainHeight);
        // rend.material.SetTexture("_WaterTex", waterTex);

        // set the position of the snow falling shape above the highest terrain vertice + 2
        var shape = ps.shape;
        maxVertice.y += 2;
        shape.position = maxVertice;

        // set mesh data and recalculate bounds and normals of the mesh afterwards
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mc.sharedMesh = mesh;
    }

    // function for the calculation of the diamond square algorithm
    void DiamondSquare(int row, int col, int size, float offset)
    {
        // get the actual half square size and the top left and bottom left vertice index
        int halfSize = (int)(size * 0.5f);
        int topLeft = row * (meshDivisions + 1) + col;
        int botLeft = (row + size) * (meshDivisions + 1) + col;

        // -- PERFORM DIAMOND STEP --
        // get the middle vertice index based on the half square size
        int mid = (int)(row + halfSize) * (meshDivisions + 1) + (int)(col + halfSize);
        // set the height of the middle vertice to the average of the four corner points and add
        // a random value of the available offset range
        vertices[mid].y = (vertices[topLeft].y + vertices[topLeft + size].y + vertices[botLeft].y + vertices[botLeft + size].y) * 0.25f + UnityEngine.Random.Range(-offset, offset);

        // -- PERFORM SQUARE STEP --
        // During the square steps, points located on the edges of the array will have only three adjacent 
        // values set rather than four. That's why in the following, only three adjacent values are used 
        // for the average calculation, because this is the simplest way to handle the problem.

        // for each "diamond" vertice, calculate the average height of the three adjacent points
        // and add a random value of the available offset range
        vertices[topLeft + halfSize].y = (vertices[topLeft].y + vertices[topLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid - halfSize].y = (vertices[topLeft].y + vertices[botLeft].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid + halfSize].y = (vertices[topLeft + size].y + vertices[botLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[botLeft + halfSize].y = (vertices[botLeft].y + vertices[botLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);

        // set the maximum terrain height variable for the shader
        // also get the highest vertice in the terrain for the snow position
        float maxValue = Math.Max(vertices[mid].y, Math.Max(vertices[topLeft + halfSize].y, Math.Max(vertices[mid - halfSize].y, Math.Max(vertices[mid + halfSize].y, vertices[botLeft + halfSize].y))));
        if (maxValue > maxTerrainHeight)
        {
            maxTerrainHeight = maxValue;
            if(maxValue == vertices[mid].y)
            {
                maxVertice = vertices[mid];
            } else if(maxValue == vertices[topLeft + halfSize].y)
            {
                maxVertice = vertices[topLeft + halfSize];
            } else if(maxValue == vertices[mid - halfSize].y)
            {
                maxVertice = vertices[mid - halfSize];
            } else if(maxValue == vertices[mid + halfSize].y)
            {
                maxVertice = vertices[mid + halfSize];
            } else if(maxValue == vertices[botLeft + halfSize].y)
            {
                maxVertice = vertices[botLeft + halfSize];
            }
        }
    }

    //function for manipulating the mesh based on mouse input
    void manipulate_Mesh_Mouse()
    {
        //on update check if right mouse key was pressed
        if (Input.GetMouseButton(1))
        {
            activeClick = true;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (mc.Raycast(ray, out hit, 500.0f))
            {
                clickpos = hit.point;
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
                //get mouse scroll wheel value
                Vector2 delta = Input.mouseScrollDelta;
                if (delta.y != 0)
                {
                    Vector3[] verts = mesh.vertices;
                    mesh.vertices = moveVerts(verts, indexActiveVert, (delta.y * 10.0f));
                    update_Mesh();
                }
            }
        } else{
            mc.sharedMesh = mesh;
        }
    }

    Vector3[] moveVerts(Vector3[] verts, int indexActiveVert, float delta)
    {
        Vector3[] resultVerts = verts;
        for (int i = 0; i < verts.Length; i++)
        {
            if (i == indexActiveVert)
            {
                for(int k = 1; k < size; k++){
                    //x direction to the right
                    for(int j = i; j < i+size; j++)
                    {
                        if(delta<0){
                            //new
                            resultVerts[j].y -= Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*(Math.Pow(resultVerts[j].x,2)-2*resultVerts[j].x*resultVerts[j].z+Math.Pow(resultVerts[j].z,2))))); 
                            resultVerts[j-k*meshDivisions].y -= Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j-k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j-k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                            resultVerts[j+k*meshDivisions].y -= Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j+k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j+k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                        } else{
                            //new
                            resultVerts[j].y += Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*(Math.Pow(resultVerts[j].x,2)-2*resultVerts[j].x*resultVerts[j].z+Math.Pow(resultVerts[j].z,2))))); 
                            resultVerts[j-k*meshDivisions-k].y += Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j-k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j-k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                            resultVerts[j+k*meshDivisions+k].y += Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j+k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j+k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                        }

                        //reset value to 0 if below 0
                        if(resultVerts[j].y < 0)
                        {
                            resultVerts[j].y = 0;
                        }
                        if(resultVerts[j+k*meshDivisions+k].y < 0)
                        {
                            resultVerts[j+k*meshDivisions+k].y = 0;
                        }
                        if(resultVerts[j-k*meshDivisions-k].y < 0)
                        {
                            resultVerts[j-k*meshDivisions-k].y = 0;
                        }
                    }
                    //x direction to the left
                    for(int j = i; j > i-size; j--)
                    {
                        if(delta<0){
                            //new
                            resultVerts[j].y -= Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                            resultVerts[j-k*meshDivisions].y -= Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j-k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j-k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                            resultVerts[j+k*meshDivisions].y -= Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j+k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j+k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                        } else{
                            //new
                            resultVerts[j].y += Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*(Math.Pow(resultVerts[j].x,2)-2*resultVerts[j].x*resultVerts[j].z+Math.Pow(resultVerts[j].z,2))))); 
                            resultVerts[j-k*meshDivisions-k].y += Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j-k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j-k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                            resultVerts[j+k*meshDivisions+k].y += Math.Abs(delta)*((float)((1/(2*Math.PI*gaussianVariance*gaussianVariance*Math.Sqrt(1)))*Math.Exp(-(1/2)*((Math.Pow(resultVerts[j+k*meshDivisions].x,2)/Math.Pow(gaussianVariance,2))+(Math.Pow(resultVerts[j+k*meshDivisions].z,2)/Math.Pow(gaussianVariance,2))-(0))))); 
                        }

                        //reset value to 0 if below 0
                        if(resultVerts[j].y < 0)
                        {
                            resultVerts[j].y = 0;
                        }
                        if(resultVerts[j-k*meshDivisions-k].y < 0)
                        {
                            resultVerts[j-k*meshDivisions-k].y = 0;
                        }
                        if(resultVerts[j+k*meshDivisions+k].y < 0)
                        {
                            resultVerts[j+k*meshDivisions+k].y = 0;
                        }
                    }
                }
            }
        }
        return resultVerts;
    }

    // function to reset the negative vertice heights to null and to
    // recalculate the highest vertice in the terrain (and the water texture for the shader)
    void update_Mesh()
    {
        // reset negative height values to 0 and set pixels of the water texture
        // --> water pixels (height value 0) to white, terrain pixels (height value > 0) to black
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                if (mesh.vertices[i * (meshDivisions + 1) + j].y <= 0)
                {
                    mesh.vertices[i * (meshDivisions + 1) + j].y = 0;
                    // waterTex.SetPixel(i, j, Color.white);
                }
                else
                {
                    if(mesh.vertices[i*(meshDivisions+1) + j].y > maxTerrainHeight)
                    {
                        maxVertice = mesh.vertices[i * (meshDivisions + 1) + j];
                    }
                    // waterTex.SetPixel(i, j, Color.black);
                }
            }
        }

        var shape = ps.shape;
        maxVertice.y += 2;
        shape.position = maxVertice;

        // apply the calculated texture
        // waterTex.Apply();

        // (set the water texture to the material, that the shader can work with this information)
        // rend.material.SetTexture("_WaterTex", waterTex);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        //mc.sharedMesh = mesh;
    }
}
