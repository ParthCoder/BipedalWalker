using System;

/// <summary>
/// Describes gene information of in-comming and out-going nodes
/// A gene can be turned on or off rendering it to be surpressed or expresses
/// </summary>
public class NEATGene : IEquatable<NEATGene>
{

    public const int GENE_INFORMATION_SIZE = 4; // 4 pieces of information that make up a gene

    private int inno; //innovation number of this gene

    private int inID;
    private int outID;
    private float weight;
    private bool on;

    /// <summary>
    /// Deep copy a given gene
    /// </summary>
    /// <param name="copy">Gene to copy</param>
    public NEATGene(NEATGene copy)
    {
        //copy all the properties of the given gene
        this.inno = copy.inno;
        this.inID = copy.inID;
        this.outID = copy.outID;
        this.weight = copy.weight;
        this.on = copy.on;
    }

    /// <summary>
    /// Create a new gene with the given parameters
    /// </summary>
    /// <param name="inno">Innovation number of this gene</param>
    /// <param name="inID">Input node</param>
    /// <param name="outID">Output node</param>
    /// <param name="weight">Weight of this connection</param>
    /// <param name="on">State of this gene, true or false</param>
    public NEATGene(int inno, int inID, int outID, float weight, bool on)
    {
        this.inno = inno;
        this.inID = inID;
        this.outID = outID;
        this.weight = weight;
        this.on = on;
    }

    /// <summary>
    /// Get gene in node id
    /// </summary>
    /// <returns>Return in node id</returns>
    public int GetInID()
    {
        return inID;
    }

    /// <summary>
    /// Get gene out node id
    /// </summary>
    /// <returns>Return out node id</returns>
    public int GetOutID()
    {
        return outID;
    }

    /// <summary>
    /// Get gene innovation number
    /// </summary>
    /// <returns>Return innovation number</returns>
    public int GetInnovation()
    {
        return inno;
    }

    /// <summary>
    /// Get gene weight
    /// </summary>
    /// <returns>Return weight</returns>
    public float GetWeight()
    {
        return weight;
    }

    /// <summary>
    /// Get gene state
    /// </summary>
    /// <returns>Return state</returns>
    public bool GetGeneState()
    {
        return on;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="on"></param>
    public void SetGeneState(bool on)
    {
        this.on = on;
    }

    /// <summary>
    /// Set weight of this gene
    /// </summary>
    /// <param name="weight">Weight value to set</param>
    public void SetWeight(float weight)
    {
        this.weight = weight;
    }

    /// <summary>
    /// Convert this gene into a string
    /// </summary>
    /// <returns>String version of the gene</returns>
    public string GetGeneString()
    {
        string gene = inID + "_" + outID + "_" + weight + "_";

        if (on == true)
        {
            gene += 1; //1 means active
        }
        else
        {
            gene += 0; //0 means inactive
        }

        return gene;
    }

    /// <summary>
    /// Check if two genes are the same. 
	/// Two genes are the same if they share the same in and out nodes.
    /// </summary>
    /// <param name="other">Gene to compare with this gene</param>
    /// <returns>True if genes are the same else false</returns>
    public bool Equals(NEATGene other)
    {
        if (other == null)
        {
            return false;
        }

        if (inID == other.inID && outID == other.outID)
        {
            return true;
        }

        return false;
    }
}
