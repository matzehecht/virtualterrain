using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenAndColor : MonoBehaviour
{
    Mesh mesh;
    MeshCollider mc;
    Renderer rend;
    // vertices, triangles and uv array for mesh generation
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    // base height of the mesh (editable in the IDE)
    public float meshHeight = 4;
    // size of the mesh (equal x and z size --> square)
    float meshSize = 30;
    // count of divisions on the mesh square (e.g. if set to 5, 25 partial squares result) 
    int meshDivisions = 128;

    // variables for maximum terrain height and binary water texture for color calculation in the shader afterwards
    float maxTerrainHeight;
    Texture2D waterTex;

    // init variables for click interactions with the terrain
    bool activeClick = false;
    Vector3 clickpos;
    Vector3 lastClick;
    Vector3 activeVert;
    int indexActiveVert = 0;
    public double gaussianVariance = 20;
    int size;

    // Start is called before the first frame update
    void Start()
    {
        // add new mesh object to the mesh filter and mesh collider defined on the empty game object
        mesh = new Mesh();
        //set index format to 32 bit, -> more than 65K vertices can be rendered
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        GetComponent<MeshFilter>().mesh = mesh;
        mc = GetComponent<MeshCollider>();
        mc.sharedMesh = mesh;

        rend = GetComponent<Renderer>();

        CreateShape();
    }

    // Update is called once per frame
    private void Update()
    {
        size = Convert.ToInt32(gaussianVariance*0.15);
        manipulate_Mesh_Mouse();
    }

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
        waterTex = new Texture2D(meshDivisions+1, meshDivisions+1);

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

        // initiate the corner points with random values of the available height space
        vertices[0].y = UnityEngine.Random.Range(-meshHeight, meshHeight);
        vertices[meshDivisions].y = UnityEngine.Random.Range(-meshHeight, meshHeight);
        vertices[vertices.Length - 1].y = UnityEngine.Random.Range(-meshHeight, meshHeight);
        vertices[vertices.Length - 1 - meshDivisions].y = UnityEngine.Random.Range(-meshHeight, meshHeight);

        // calculate the diamond square algorithm and set the terrain heights
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

        // reset negative height values to 0 and set pixels of the water texture
        // --> water pixels (height value 0) to white, terrain pixels (height value > 0) to black
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                if (vertices[i * (meshDivisions + 1) + j].y <= 0)
                {
                    vertices[i * (meshDivisions + 1) + j].y = 0;
                    waterTex.SetPixel(i, j, Color.white);
                }
                else
                {
                    waterTex.SetPixel(i, j, Color.black);
                }
            }
        }

        // apply the calculated texture
        waterTex.Apply();

        // set the calculated maximum terrain height and water texture to the material, 
        // that the shader can work with this information
        rend.material.SetFloat("_maxTerrainHeight", maxTerrainHeight);
        rend.material.SetTexture("_WaterTex", waterTex);

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
        int halfSize = (int)(size * 0.5f);
        int topLeft = row * (meshDivisions + 1) + col;
        int botLeft = (row + size) * (meshDivisions + 1) + col;

        int mid = (int)(row + halfSize) * (meshDivisions + 1) + (int)(col + halfSize);
        vertices[mid].y = (vertices[topLeft].y + vertices[topLeft + size].y + vertices[botLeft].y + vertices[botLeft + size].y) * 0.25f + UnityEngine.Random.Range(-offset, offset);

        vertices[topLeft + halfSize].y = (vertices[topLeft].y + vertices[topLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid - halfSize].y = (vertices[topLeft].y + vertices[botLeft].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid + halfSize].y = (vertices[topLeft + size].y + vertices[botLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[botLeft + halfSize].y = (vertices[botLeft].y + vertices[botLeft + size].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);

        // set the maximum terrain height variable for the shader
        float maxValue = Math.Max(vertices[mid].y, Math.Max(vertices[topLeft + halfSize].y, Math.Max(vertices[mid - halfSize].y, Math.Max(vertices[mid + halfSize].y, vertices[botLeft + halfSize].y))));
        if (maxValue > maxTerrainHeight)
            maxTerrainHeight = maxValue;
    }

    //function for manipulating the mesh based on mouse input
    void manipulate_Mesh_Mouse()
    {
        //on update check if eft mouse key was pressed
        if (Input.GetMouseButton(0))
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

    // function to recalculate the maximum terrain height and the water texture for the shader
    void update_Mesh()
    {
        // reset maximum terrain height for the new calculation
        maxTerrainHeight = 0;

        // reset negative height values to 0 and set pixels of the water texture
        // --> water pixels (height value 0) to white, terrain pixels (height value > 0) to black
        for (int i = 0; i <= meshDivisions; i++)
        {
            for (int j = 0; j <= meshDivisions; j++)
            {
                if (mesh.vertices[i * (meshDivisions + 1) + j].y <= 0)
                {
                    mesh.vertices[i * (meshDivisions + 1) + j].y = 0;
                    waterTex.SetPixel(i, j, Color.white);
                }
                else
                {
                    if(mesh.vertices[i*(meshDivisions+1) + j].y > maxTerrainHeight)
                    {
                        maxTerrainHeight = mesh.vertices[i * (meshDivisions + 1) + j].y;
                    }
                    waterTex.SetPixel(i, j, Color.black);
                }
            }
        }

        // apply the calculated texture
        waterTex.Apply();

        // set the calculated maximum terrain height and water texture to the material, 
        // that the shader can work with this information
        rend.material.SetFloat("_maxTerrainHeight", maxTerrainHeight);
        rend.material.SetTexture("_WaterTex", waterTex);

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mc.sharedMesh = mesh;
    }
}
