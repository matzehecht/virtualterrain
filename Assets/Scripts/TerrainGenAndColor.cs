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
    public float terrainOffset = 4;
    // sizeOfGaussArea of the mesh (equal x and z sizeOfGaussArea --> square)
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
    private bool double_click = false;
    private int indexActiveVert;
    public int mouse_Scale = 20;
    private double gaussianVariance = 20;
    public double sizeOfGaussArea = 1.5;
    private float doubleClickTimeLimit = 0.25f;
    //start rotation of camera
    Quaternion startView_Rot;
    //create new Quaternion to rotate Camera by 90 degrees
    Quaternion topView_Rot = Quaternion.Euler(90.0f, 0.0f, 0.0f);
    //start camera position
    Vector3 startCameraPosition;
    //GameObject of Camera
    GameObject varMainCamera;
    //GameObject of Terrain
    GameObject varTerrain;

    // Start is called before the first frame update
    void Start()
    {
        // Get the parameters past by the main menu
        terrainOffset = StaticClass.getTerrainOffset();
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

        //start routines for detecting single and double clicks
        StartCoroutine(InputListener());

        // call inital method for terrain generation with the diamond square algorithm
        CreateShape();

        this.varMainCamera  = GameObject.FindWithTag("MainCamera");
        this.varTerrain = GameObject.FindWithTag("TerrainAreaTag");

        //save position and rotation at start
        startCameraPosition = this.varMainCamera.transform.position;
        startView_Rot = this.varMainCamera.transform.rotation;
    }

    // Update is called once per frame
    private void Update()
    {
        manipulate_Mesh_Mouse();
    }

    // function for the creation of the initial terrain with the diamond square algorithm
    void CreateShape()
    {
        // define vertex and uv count 
        // need one more vertex on each coordinate than the defined division sizeOfGaussArea
        // (e.g. need 3*3 vertices for a plane out of 4 squares)
        int mVertCount = (meshDivisions + 1) * (meshDivisions + 1);
        vertices = new Vector3[mVertCount];
        uvs = new Vector2[mVertCount];

        // define count of triangle points (6 for one mesh square)
        triangles = new int[meshDivisions * meshDivisions * 6];

        // define new texture with sizeOfGaussArea of mesh divisions + 1
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
            // double the amount of squares and half the square sizeOfGaussArea in each iteration
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
        maxVertice.y -=2;

        // set mesh data and recalculate bounds and normals of the mesh afterwards
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mc.sharedMesh = mesh;
    }

    // function for the calculation of the diamond square algorithm
    void DiamondSquare(int row, int col, int sizeOfGaussArea, float offset)
    {
        // get the actual half square sizeOfGaussArea and the top left and bottom left vertice index
        int halfSize = (int)(sizeOfGaussArea * 0.5f);
        int topLeft = row * (meshDivisions + 1) + col;
        int botLeft = (row + sizeOfGaussArea) * (meshDivisions + 1) + col;

        // -- PERFORM DIAMOND STEP --
        // get the middle vertice index based on the half square sizeOfGaussArea
        int mid = (int)(row + halfSize) * (meshDivisions + 1) + (int)(col + halfSize);
        // set the height of the middle vertice to the average of the four corner points and add
        // a random value of the available offset range
        vertices[mid].y = (vertices[topLeft].y + vertices[topLeft + sizeOfGaussArea].y + vertices[botLeft].y + vertices[botLeft + sizeOfGaussArea].y) * 0.25f + UnityEngine.Random.Range(-offset, offset);

        // -- PERFORM SQUARE STEP --
        // During the square steps, points located on the edges of the array will have only three adjacent 
        // values set rather than four. That's why in the following, only three adjacent values are used 
        // for the average calculation, because this is the simplest way to handle the problem.

        // for each "diamond" vertice, calculate the average height of the three adjacent points
        // and add a random value of the available offset range
        vertices[topLeft + halfSize].y = (vertices[topLeft].y + vertices[topLeft + sizeOfGaussArea].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid - halfSize].y = (vertices[topLeft].y + vertices[botLeft].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[mid + halfSize].y = (vertices[topLeft + sizeOfGaussArea].y + vertices[botLeft + sizeOfGaussArea].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);
        vertices[botLeft + halfSize].y = (vertices[botLeft].y + vertices[botLeft + sizeOfGaussArea].y + vertices[mid].y) / 3 + UnityEngine.Random.Range(-offset, offset);

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
        //manipulation only available after double click
        if(double_click == true)
        {   
            //create new Vector for new position, relative to the position of the terrain but with increased height
            Vector3 newPosition = new Vector3(this.varTerrain.transform.position.x, this.varTerrain.transform.position.y + 30.0f, this.varTerrain.transform.position.z);

            //do actual transformations
            this.varMainCamera.transform.rotation = this.topView_Rot;
            this.varMainCamera.transform.position = newPosition;

            //unlock mouse to present for manipulation
            if(Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            //enable manipulation with clicking right on mouse
            if (Input.GetMouseButton(1))
            {
                // The assignment is only done before the raycast to improve the 
                // performance during manipulation
                this.mc.sharedMesh = this.mesh;

                // Return if selected point is not on the terrain
                if (!Physics.Raycast(
                    Camera.main.ScreenPointToRay(Input.mousePosition),
                    out RaycastHit hit,
                    300.0f
                ))
                    return;
                
                this.activeClick = true;

                // Get the index of the vertex closest to the hit point
                this.indexActiveVert = GetClosestVertexToPoint(hit);
            }

            //disable manipulation with clicking left on mouse
            if(Input.GetMouseButton(0))
            {
                this.activeClick = false;
            }

            if (this.activeClick)
            {
                //get mouse scroll wheel value
                float mouse_delta = Input.mouseScrollDelta.y;
                if (mouse_delta != 0)
                {
                    moveVerts(
                        this.indexActiveVert,
                        mouse_delta);
                    update_Mesh();
                }
            }
        }
    }

    /** Return the index of the vertex closest to the hit point. */
    private int GetClosestVertexToPoint(RaycastHit hit)
    {
        // The hit triangle
        int[] hitTriangle = new int[3] {
            this.mesh.triangles[hit.triangleIndex * 3 + 0],
            this.mesh.triangles[hit.triangleIndex * 3 + 1],
            this.mesh.triangles[hit.triangleIndex * 3 + 2]
        };

        // Loop the hit triangle for determining the vertex closest to the hit point
        float closestDistance = Vector3.Distance(
            this.vertices[hitTriangle[0]],
            hit.point
        );
        int closestVertex = hitTriangle[0];
        for (int i = 0; i < hitTriangle.Length; i++)
        {
            float distance = Vector3.Distance(
                this.vertices[hitTriangle[i]],
                hit.point
            );
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestVertex = hitTriangle[i];
            }
        }

        // Return the index of the closest vertex
        return closestVertex;
    }

    void moveVerts(int midpoint, float mouseDelta)
    {

        //loop through vertices to changes heights of vertices
        for(var i = 0; i < this.vertices.Length; i++)
        {
            //check if point within radius to make circular selection of vertices
            double distanceZ = (double)this.vertices[i].z-this.vertices[midpoint].z;
            double distanceX = (double)this.vertices[i].x-this.vertices[midpoint].x;

            //calculate distance between to points
            double distanceToCenterPoint = Math.Sqrt(Math.Pow(distanceX,2) + Math.Pow(distanceZ,2));

            //only use those vertices in radius
            if(distanceToCenterPoint <= sizeOfGaussArea)
            {
                //gaussian distribution with scaling fir better visibility
                this.vertices[i].y += (mouseDelta*mouse_Scale)*((float)((1 / (2 * Math.PI * Math.Pow(this.gaussianVariance, 2)*Math.Sqrt(1)))
                    * Math.Exp(-(1.0f / 2)
                    * (Math.Pow(distanceToCenterPoint*25, 2) / Math.Pow(this.gaussianVariance, 2))
                    )
                ));

                //check if value below zero, if yes set to zero
                this.vertices[i].y = this.vertices[i].y < 0 ? 0 : this.vertices[i].y;

                //check if lager than current max vertice to change position of snow accordingly
                if(this.vertices[i].y > this.maxVertice.y)
                {
                    // set the position of the snow falling shape above the highest terrain vertice + 2
                    maxVertice = vertices[i];
                    var shape = this.ps.shape;
                    maxVertice.y += 2;
                    shape.position = maxVertice;
                    maxVertice.y -=2;
                } 
            }
        }    
    }

    // function to reset the negative vertice heights to null and to
    // recalculate the highest vertice in the terrain (and the water texture for the shader)
    void update_Mesh()
    {
        this.mesh.vertices = this.vertices;
        this.mesh.RecalculateBounds();
        this.mesh.RecalculateNormals();
        this.mesh.RecalculateTangents();
    }

    private IEnumerator InputListener() 
    {
        while(enabled)
        { //Run as long as this is activ

            if(Input.GetMouseButtonDown(0))
                yield return ClickEvent();

            yield return null;
        }
    }

    private IEnumerator ClickEvent()
    {
        //pause a frame so system doesn't pick up the same mouse down event.
        yield return new WaitForEndOfFrame();

        float count = 0f;
        while(count < doubleClickTimeLimit)
        {
            if(Input.GetMouseButtonDown(0))
            {
                DoubleClick();
                yield break;
            }
            count += Time.deltaTime;// increment counter by change in time between frames
            yield return null; // wait for the next frame
        }
    }

    //method executed when detectin double click
    private void DoubleClick()
    {
        //check whether double click was detected before (if game was in terrain manipulation)
        //was in manipulation mode
        if(double_click == false)
        {
            // update to last camera position and rotation, to be able to reset to this
            // after reentering the flying cam mode
            startCameraPosition = this.varMainCamera.transform.position;
            startView_Rot = this.varMainCamera.transform.rotation;
            //re-enable flying controls 
            this.varMainCamera.GetComponent<FlyingCamControl>().enabled = false;
            double_click = true;

        } 
        //wasnT in manipulation mode
        else
        {
            //disable flying controls
            this.varMainCamera.GetComponent<FlyingCamControl>().enabled = true;

            //change position of capsule to start
            this.varMainCamera.transform.position = this.startCameraPosition;

            //change rotation of camera back to default 
            this.varMainCamera.transform.rotation = this.startView_Rot;
            //lock mouse to hide for control in first person
            if(Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
            double_click = false;
        }
    }
}