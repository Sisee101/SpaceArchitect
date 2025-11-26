using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Started life with a snippet from https://catlikecoding.com/unity/tutorials/procedural-grid/
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PorkchopMesh : MonoBehaviour
{
    [SerializeField]
    private int xSize = 10;

    [SerializeField]
    private int ySize = 10;

    [SerializeField]
    private bool useZ = true;

    [SerializeField]
    private Camera viewCamera = null;

    private enum DataMorph  { LINEAR, LOG10, LOGE };

    [SerializeField]
    private DataMorph dataMorph = DataMorph.LINEAR;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector2[] uv; 

    public delegate float XYFunction(float x, float y);

    public delegate void GridClicked(int x, int y);
    private GridClicked gridClicked;

    private DataGrid dataGrid;

    private float xScale;
    private float yScale; 

    private float Ztest(float x, float y)
    {
        return Mathf.Sin(x ) + Mathf.Sin(y );
    }

    // Start is called before the first frame update
    void Awake()
    {
        // Generate(Ztest);
    }

    /// <summary>
    /// Create a mesh from a DataGrid. 
    /// 
    /// This code makes a mesh size that is 1-1 with the intervals in the data grid to allow a
    /// simple backwards mapping when a point in the mesh is clicked. 
    /// </summary>
    /// <param name="dataGrid"></param>
    public void GenerateFromDataGrid(DataGrid dataGrid, GridClicked gridClicked)
    {
        this.dataGrid = dataGrid;
        this.gridClicked = gridClicked;

        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        float fMax = float.MinValue;
        float fMin = float.MaxValue;
        int xLimit = dataGrid.xIntervals;
        int yLimit = dataGrid.yIntervals;

        xScale = xSize / (float) dataGrid.xIntervals;
        yScale = ySize / (float) dataGrid.yIntervals;

        vertices = new Vector3[(xLimit + 1) * (yLimit + 1)];
        for (int i = 0, y = 0; y <= yLimit; y++) {
            for (int x = 0; x <= xLimit; x++, i++) {
                // data is used for vertex info, so demote to float
                float data = (float) dataGrid.GetData(x, y);
                switch (dataMorph) {
                    case DataMorph.LOG10:
                        data = Mathf.Log10(data);
                        break;

                    case DataMorph.LOGE:
                        data = Mathf.Log(data);
                        break;

                    default:
                        break;

                }
                if (!float.IsNaN(data)) {
                    fMax = Mathf.Max(fMax, data);
                    fMin = Mathf.Min(fMin, data);
                } 
                vertices[i] = new Vector3(x*xScale, y*yScale, data);
            }
        }

        float fScale = 1.0f;
        if (Mathf.Abs(fMax - fMin) > 1E-5f)
            fScale = 1 / (fMax - fMin);
        Debug.LogFormat("Scaling fMin={0} fMax={1} fScale={2}", fMin, fMax, fScale);

        uv = new Vector2[(dataGrid.xIntervals + 1) * (dataGrid.yIntervals + 1)];
        for (int i = 0, y = 0; y <= yLimit; y++) {
            for (int x = 0; x <= xLimit; x++, i++) {
                float f = 0.0f;
                // if NaN use (u,v) = (0,0) otherwise take specturm from the top edge
                // set z to 0 for points with no data
                if (!float.IsNaN(vertices[i].z)) {
                    f = Mathf.Clamp((vertices[i].z - fMin) * fScale, 0.0f, 1.0f);
                    uv[i] = new Vector2(f, 0.9f);
                } else {
                    // to avoid wrap on the texture take the black offset from 0,0
                    vertices[i] = new Vector3(vertices[i].x, vertices[i].y, f);
                    uv[i] = new Vector2(.1f, 0.1f);
                }
                if (!useZ)
                    vertices[i] = new Vector3(vertices[i].x, vertices[i].y, 0);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;

        int[] triangles = new int[xLimit * yLimit * 6];
        for (int ti = 0, vi = 0, y = 0; y < yLimit; y++, vi++) {
            for (int x = 0; x < xLimit; x++, ti += 6, vi++) {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xLimit + 1;
                triangles[ti + 5] = vi + xLimit + 2;
            }
        }
        mesh.triangles = triangles;

        mesh.RecalculateNormals();

        // MUST assign mesh once it has been filled in and not before!
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void Generate(XYFunction zfuntion)
    {
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        float fMax = float.MinValue;
        float fMin = float.MaxValue;

        vertices = new Vector3[(xSize + 1) * (ySize + 1)];
        for (int i = 0, y = 0; y <= ySize; y++) {
            for (int x = 0; x <= xSize; x++, i++) {
                vertices[i] = new Vector3(x, y, zfuntion(x,y));
                fMax = Mathf.Max(fMax, vertices[i].z);
                fMin = Mathf.Min(fMin, vertices[i].z);
            }
        }
        float fScale = 1.0f;
        if (Mathf.Abs(fMax - fMin) > 1E-5f)
            fScale = 1/(fMax - fMin);
        uv = new Vector2[(xSize + 1) * (ySize + 1)];
        for (int i = 0, y = 0; y <= ySize; y++) {
            for (int x = 0; x <= xSize; x++, i++) {
                float f = Mathf.Clamp((vertices[i].z - fMin) * fScale, 0.0f, 1.0f);
                uv[i] = new Vector2(f, f);
                if (!useZ)
                    vertices[i] = new Vector3(vertices[i].x, vertices[i].y, 0);
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;

        int[] triangles = new int[xSize * ySize * 6];
        for (int ti = 0, vi = 0, y = 0; y < ySize; y++, vi++) {
            for (int x = 0; x < xSize; x++, ti += 6, vi++) {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + xSize + 1;
                triangles[ti + 5] = vi + xSize + 2;
            }
        }
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }

    void OnMouseDown()
    {
        Debug.Log("Clicked " + gameObject.name);
        // find the triangle that was hit 
        // https://docs.unity3d.com/ScriptReference/RaycastHit-triangleIndex.html
        RaycastHit hit;
        if (!Physics.Raycast(viewCamera.ScreenPointToRay(Input.mousePosition), out hit))
            return;

        MeshCollider meshCollider = hit.collider as MeshCollider;
        if (meshCollider == null || meshCollider.sharedMesh == null)
            return;
        Debug.Log("Hit triangle = " + hit.triangleIndex);
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector3 p0 = vertices[triangles[hit.triangleIndex * 3 + 0]];
        Vector3 p1 = vertices[triangles[hit.triangleIndex * 3 + 1]];
        Vector3 p2 = vertices[triangles[hit.triangleIndex * 3 + 2]];
        Vector2 uv = mesh.uv[triangles[hit.triangleIndex * 3 + 0]];

        //Debug.LogFormat("Triangle is p0={0} p1={1} p2={2} uv={3}", p0, p1, p2, uv);
        gridClicked((int)(p0.x/xScale), (int)(p0.y/yScale)); 
    }
}
