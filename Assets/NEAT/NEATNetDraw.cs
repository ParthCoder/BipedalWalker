using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Draws the network
/// </summary>
public class NEATNetDraw : MonoBehaviour
{
    public GameObject linePrefab;
    public GameObject nodePrefab;

    private List<GameObject> lineList;
    private List<GameObject> nodeList;
    private Vector3[] locations;
    private Vector3 topLeft;
    private float factor = 0.5f;
    /// <summary>
    /// Initilize line and node list, followed by getting the position where this object is placed.
    /// </summary>
    void Start()
    {
        lineList = new List<GameObject>();
        nodeList = new List<GameObject>();
        topLeft = transform.position; // a reference point to index on
    }

    /// <summary>
    /// Draw neural network
    /// </summary>
    /// <param name="net"></param>
    public void DrawNet(NEATNet net)
    {
        Clear(); // clear previous network

        // get network information from MEATNet
        int numberOfInputs = net.GetNumberOfInputNodes();
        int numberOfOutputs = net.GetNumberOfOutputNodes();
        int numberOfNodes = net.GetNodeCount();
        int numberOfHiddens = numberOfNodes - (numberOfInputs + numberOfOutputs);
        int hiddenStartIndex = numberOfInputs + numberOfOutputs;
        locations = new Vector3[net.GetNodeCount()];
        int locationIndex = 0;

        //Create input node objects 
        float staryY = topLeft.y;
        for (int i = 0; i < numberOfInputs; i++)
        {
            Vector3 loc = new Vector3(topLeft.x, staryY, 0);
            GameObject node = (GameObject)Instantiate(nodePrefab, loc, nodePrefab.transform.rotation);
            node.transform.parent = transform;
            node.GetComponent<Renderer>().material.color = Color.green;
            nodeList.Add(node);
            staryY = staryY - (factor);

            locations[locationIndex] = loc;
            locationIndex++;
        }

        //create output node objects	
        staryY = (topLeft.y);
        for (int i = numberOfInputs; i < hiddenStartIndex; i++)
        {
            Vector3 loc = new Vector3(topLeft.x + 7f, staryY, 0);
            GameObject node = (GameObject)Instantiate(nodePrefab, loc, nodePrefab.transform.rotation);
            node.transform.parent = transform;
            node.GetComponent<Renderer>().material.color = Color.white;
            nodeList.Add(node);
            staryY = staryY - (factor);

            locations[locationIndex] = loc;
            locationIndex++;
        }

        //create hidden nodes in a circle formation 
        float xn = 0;
        float yn = 0;
        float angle = 0;
        for (int i = hiddenStartIndex; i < numberOfNodes; i++)
        {
            xn = Mathf.Sin(Mathf.Deg2Rad * angle) * (2 * factor);
            yn = Mathf.Cos(Mathf.Deg2Rad * angle) * (2 * factor);

            Vector3 loc = new Vector3(xn + (5f * factor) + topLeft.x, ((yn + topLeft.y) - (numberOfInputs / 2f)) - (7f * factor), 0);
            GameObject node = (GameObject)Instantiate(nodePrefab, loc, nodePrefab.transform.rotation);
            node.transform.parent = transform;
            node.GetComponent<Renderer>().material.color = Color.red;
            nodeList.Add(node);
            angle += (360f / numberOfHiddens);

            locations[locationIndex] = loc;
            locationIndex++;
        }

        float[][] geneConnections = net.GetGeneDrawConnections(); //get gene connection list
        int colSize = geneConnections.GetLength(0);

        //create line connection objects
        for (int i = 0; i < colSize; i++)
        {
            if (geneConnections[i][2] != 0f)
            {
                GameObject lineObj = (GameObject)Instantiate(linePrefab);
                lineObj.transform.parent = transform;
                lineList.Add(lineObj);
                LineRenderer lineRen = lineObj.GetComponent<LineRenderer>();
                lineRen.SetPosition(0, locations[(int)geneConnections[i][0]]);
                if ((int)geneConnections[i][0] != (int)geneConnections[i][1])
                    lineRen.SetPosition(1, locations[(int)geneConnections[i][1]]);
                else
                    lineRen.SetPosition(1, locations[(int)geneConnections[i][1]] + new Vector3(1f, 0, 0));
                lineRen.material = new Material(Shader.Find("Particles/Additive"));
                float size = 0.1f;
                float weight = geneConnections[i][2];
                float factor = Mathf.Abs(weight);
                Color color;

                if (weight > 0)
                    color = Color.green;
                else if (weight < 0)
                    color = Color.red;
                else
                    color = Color.cyan;

                size = size * factor;
                if (size < 0.05f)
                    size = 0.05f;
                if (size > 0.15f)
                    size = 0.15f;

                lineRen.SetColors(color, color);
                lineRen.SetWidth(size, size);
            }
        }
    }

    /// <summary>
    /// Clear previous nodes and linez
	/// </summary>
    public void Clear()
    {
        for (int i = 0; i < lineList.Count; i++)
        {
            Destroy(lineList[i]);
        }

        for (int i = 0; i < nodeList.Count; i++)
        {
            Destroy(nodeList[i]);
        }

        lineList.Clear();
        nodeList.Clear();
    }
}
