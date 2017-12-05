using System;

/// <summary>
/// Acts like the individual neuron of a network.
/// </summary>
public class NEATNode
{

    //constant node types
    public const int INPUT_NODE = 0;
    public const int INPUT_BIAS_NODE = 1;
    public const int HIDDEN_NODE = 2;
    public const int OUTPUT_NODE = 3;

    private int ID;
    private int type;

    private float value;

    /// <summary>
    /// Deep copy a given copy node
    /// </summary>
    /// <param name="copy">The node to copy</param>
    public NEATNode(NEATNode copy)
    {
        this.ID = copy.ID;
        this.type = copy.type;

        // if this is the bias node set it to 1, else reset value to 0
        if (this.type == INPUT_BIAS_NODE)
        {
            this.value = 1f;
        }
        else
        {
            this.value = 0f;
        }

        //this.value = copy.value; << BUG FIXED!
    }

    /// <summary>
    /// Create a node with an id and type
    /// </summary>
    /// <param name="ID">ID of this node</param>
    /// <param name="type">Type of this node</param>
    public NEATNode(int ID, int type)
    {
        this.ID = ID;
        this.type = type;

        if (this.type == INPUT_BIAS_NODE)
        {
            this.value = 1f;
        }
        else
        {
            this.value = 0f;
        }
    }

    /// <summary>
    /// Get the ID of this node.
    /// </summary>
    /// <returns>Node ID</returns>
    public int GetNodeID()
    {
        return ID;
    }

    /// <summary>
    /// Get the type of this node
    /// </summary>
    /// <returns>Node type</returns>
    public int GetNodeType()
    {
        return type;
    }

    /// <summary>
    /// Get node value
    /// </summary>
    /// <returns>Node value</returns>
    public float GetValue()
    {
        return value;
    }

    /// <summary>
    /// Set value of the node if it's not a biased node
    /// </summary>
    /// <param name="value">Value to set</param>
    public void SetValue(float value)
    {
        if (type != INPUT_BIAS_NODE)
            this.value = value;
    }

    /// <summary>
    /// Run the value through hyperbolic tangent approx
    /// </summary>
    public void Activation()
    {
        value = (float)Math.Tanh(value);
        //value= 1.0f / (1.0f + (float)Math.Exp(-value));
    }
}
