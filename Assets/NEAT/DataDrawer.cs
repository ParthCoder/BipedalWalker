using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Draws line data, displays action information, and generational fitness information
/// </summary>
public class DataDrawer : MonoBehaviour
{

    public GameObject linePrefab; //line prebaf for displaying line in graph
    public TextMesh displayGenerationalText; //displaying fitness information onto a text mesh
    public TextMesh displayActionText; //displaying action information onto a text mesh

    private List<GameObject> lines = null; //list of line gameobjects to create
    private float maxY, maxX, seperationX, seperationY, axisLineWidth, plotLineWidth; //line data 
    private Vector2 oldLocation;
    private Vector2 centerLocation;
    private LineRenderer linePlot;
    private List<Vector2> linePlotList = new List<Vector2>();
    private int vertextCount = 2;

    // Generational and action information stored in list
    private List<string> generationalInformationList = new List<string>();
    private List<string> actionInformationList = new List<string>();
    private const int INFORMATION_LIST_SIZE = 5;

    void Start()
    {
        lines = new List<GameObject>();
        
        Debug.Log(centerLocation);

    }

    /// <summary>
    /// Calibrates the graph.
    /// </summary>
    /// <param name="maxY">Maximum height of y fitness</param>
    /// <param name="maxX">Maximum width of x generationvalue</param>
    /// <param name="seperationX">Low sepration = Many generations displayed, High Seperation = Less generations displayed</param>
    /// <param name="seperationY"></param>
    /// <param name="axisLineWidth">Axis line width.</param>
    /// <param name="plotLineWidth">Plot line width.</param>
    public void CalibrateGraph(float maxY, float maxX, float seperationX, float seperationY, float axisLineWidth, float plotLineWidth)
    {
        centerLocation = transform.position;
        if (lines!=null)
            for (int i = 0; i < lines.Count; i++)
            {
                Destroy(lines[i]);
            }

        this.maxX = maxX;
        this.maxY = maxY;
        this.seperationX = seperationX;
        this.seperationY = seperationY;
        this.axisLineWidth = axisLineWidth;
        this.plotLineWidth = plotLineWidth;

        lines = new List<GameObject>();

        GameObject horizontal = (GameObject)Instantiate(linePrefab);
        horizontal.transform.parent = transform;
        LineRenderer lineHori = horizontal.GetComponent<LineRenderer>();
        lineHori.SetPosition(0, Vector2.zero + centerLocation);
        lineHori.SetPosition(1, new Vector2(seperationX * maxX, 0f) + centerLocation);
        lineHori.SetWidth(axisLineWidth, axisLineWidth);
        lineHori.material = new Material(Shader.Find("Particles/Additive"));
        lineHori.SetColors(Color.white, Color.white);

        GameObject vertical = (GameObject)Instantiate(linePrefab);
        vertical.transform.parent = transform;
        LineRenderer lineVert = vertical.GetComponent<LineRenderer>();
        lineVert.SetPosition(0, Vector2.zero + centerLocation);
        lineVert.SetPosition(1, new Vector2(0f, seperationY * maxY) + centerLocation);
        lineVert.SetWidth(axisLineWidth, axisLineWidth);
        lineVert.material = new Material(Shader.Find("Particles/Additive"));
        lineVert.SetColors(Color.white, Color.white);

        GameObject plot = (GameObject)Instantiate(linePrefab);
        plot.transform.parent = transform;
        linePlot = plot.GetComponent<LineRenderer>();
        linePlot.SetPosition(0, centerLocation);
        linePlot.SetPosition(1, centerLocation);
        linePlot.SetWidth(plotLineWidth, plotLineWidth);
        linePlot.material = new Material(Shader.Find("Particles/Additive"));
        linePlot.SetColors(Color.red, Color.red);

        lines.Add(horizontal);
        lines.Add(vertical);
        lines.Add(plot);

        oldLocation = centerLocation;

    }

    public void PlotData(float dataPoint, string info)
    {
        float y = (seperationY * dataPoint) + centerLocation.y;
        float x = seperationX + oldLocation.x;


        /*vertextCount++;
        linePlot.SetVertexCount(vertextCount);

        Vector2 newPosition = new Vector2(x,y);
        linePlot.SetPosition(vertextCount-1,newPosition);
        oldLocation = newPosition;*/


        Vector2 newPosition = new Vector2(x, y);
        if (linePlotList.Count > maxX)
        {
            linePlotList.RemoveAt(linePlotList.Count - 1);
        }
        linePlotList.Insert(0, newPosition);

        linePlot.SetVertexCount(linePlotList.Count);

        float newX = centerLocation.x;
        for (int i = 0; i < linePlotList.Count; i++)
        {
            Vector2 pos = linePlotList[i];
            pos.x = newX;
            newX += seperationX;
            linePlot.SetPosition(i, pos);
        }

        DisplayGenerationalInformation(info);
    }


    public void DisplayGenerationalInformation(string info)
    {
        if (generationalInformationList.Count >= INFORMATION_LIST_SIZE)
        {
            generationalInformationList.RemoveAt(INFORMATION_LIST_SIZE - 1);
        }

        generationalInformationList.Insert(0, info);
        displayGenerationalText.text = "";

        for (int i = 0; i < generationalInformationList.Count; i++)
        {
            displayGenerationalText.text += generationalInformationList[i] + "\n";
        }
    }

    public void DisplayActionInformation(string info)
    {
        if (actionInformationList.Count >= INFORMATION_LIST_SIZE)
        {
            actionInformationList.RemoveAt(INFORMATION_LIST_SIZE - 1);
        }

        actionInformationList.Insert(0, info);
        displayActionText.text = "";

        for (int i = 0; i < actionInformationList.Count; i++)
        {
            displayActionText.text += actionInformationList[i] + "\n";
        }

    }
}
