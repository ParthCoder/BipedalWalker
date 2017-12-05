using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Text;
using System;

public class Neuron
{
    public int id;
    public float value;
    public List<NEATGene> incomming = new List<NEATGene>();
    public NEATGene[] incommingArray;

    public Neuron(int id, float value)
    {
        this.id = id;
        this.value = value;
    }
}

/// <summary>
/// Handels mutation, crossover, specification, feedforward activation and creation of neural network's genotype. 
/// </summary>
public class NEATNet : IEquatable<NEATNet>, IComparable<NEATNet>
{
    private List<Neuron> network;
    private Neuron[] networkArray;
    private int usedHiddenNeuronIndex;

    public void GenerateNeuralNetworkFromGenome()
    {
        network = new List<Neuron>();

        usedHiddenNeuronIndex = int.MinValue;
        for (int i = 0; i < geneList.Count; i++)
        {
            int inNode = geneList[i].GetInID();
            int outNode = geneList[i].GetOutID();

            if (usedHiddenNeuronIndex < inNode)
                usedHiddenNeuronIndex = inNode;

            if (usedHiddenNeuronIndex < outNode)
                usedHiddenNeuronIndex = outNode;
        }

        usedHiddenNeuronIndex = usedHiddenNeuronIndex + 1; //incremented as per LSES algorithm 


        for (int i = 0; i < usedHiddenNeuronIndex; i++)
        {
            Neuron neuron = new Neuron(i, 0f);
            network.Add(neuron);
        }

        network.Sort((x, y) => x.id.CompareTo(y.id));
        geneList.Sort((x, y) => x.GetOutID().CompareTo(y.GetOutID()));


        for (int i = 0; i < geneList.Count; i++)
        {
            NEATGene gene = geneList[i];

            if (gene.GetGeneState() == true)
            {
                network[gene.GetOutID()].incomming.Add(gene);
            }
        }

        networkArray = network.ToArray();

        for (int i = 0; i < networkArray.Length; i++)
        {
            networkArray[i].incomming.Sort((x, y) => x.GetInID().CompareTo(y.GetInID()));
            networkArray[i].incommingArray = networkArray[i].incomming.ToArray();
        }


        geneList.Sort((x, y) => x.GetInnovation().CompareTo(y.GetInnovation())); //reset back to sorted interms of innovation number
    }

    public float[] FireNet(float[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            networkArray[i].value = inputs[i];

        }
        float[] output = new float[numberOfOutputs];

        float[] tempValues = new float[networkArray.Length];
        for (int i = 0; i < tempValues.Length; i++)
            tempValues[i] = networkArray[i].value;

        networkArray[numberOfInputs - 1].value = 1f;

        for (int i = 0; i < networkArray.Length; i++)
        {
            float value = 0;
            Neuron neuron = networkArray[i];
            NEATGene[] incommingArray = neuron.incommingArray;

            if (incommingArray.Length > 0)
            {
                for (int j = 0; j < incommingArray.Length; j++)
                {
                    if (incommingArray[j].GetGeneState() == true)
                    {
                        value = value + (incommingArray[j].GetWeight() * tempValues[incommingArray[j].GetInID()]);
                    }
                }
                neuron.value = (float)System.Math.Tanh(value);
            }
        }



        for (int i = 0; i < output.Length; i++)
        {
            output[i] = networkArray[i + numberOfInputs].value;

        }

        return output;
    }


    //------------------------------------------------


    private NEATConsultor consultor; //Handles consultor genome sequence

    private List<NEATGene> geneList; //list of the genome sequence for this neural network
    private List<NEATNode> nodeList; //list of nodes for this neural network

    private int numberOfInputs; //Number of input perceptrons of neural network (including bias)
    private int numberOfOutputs; //Number of output perceptrons 
    //private int[] netID = new int[2]; //ID of this neural network
    private int netID;
    private float time; //time to run test on this neural network
    private float timeLived; //time the neural network actually lived in the test enviroment
    private float netFitness; //fitness of this neural network

    private static int ID_COUNT = 0;
    /// <summary>
    /// This is a deep copy constructor. 
    /// Creating neural network structure from deep copying another network
    /// </summary>
    /// <param name="copy">Neural network to deep copy</param>
    public NEATNet(NEATNet copy)
    {
        this.consultor = copy.consultor; //shallow copy consultor
        this.numberOfInputs = copy.numberOfInputs; //copy number of inputs
        this.numberOfOutputs = copy.numberOfOutputs; //copy number of outputs

        CopyNodes(copy.nodeList); //deep copy node list
        CopyGenes(copy.geneList); //deep copy gene list

        //this.netID = new int[2]; //reset ID
        this.time = 0f; //reset time
        this.netFitness = 0f; //reset fitness
        this.timeLived = 0f; //reset time lived
    }

    /// <summary>
    /// Creating neural network structure using neat packet from database
    /// </summary>
    /// <param name="packet">Neat packet received from database</param>
    /// <param name="consultor">Consultor with master genome and specification information</param>
    public NEATNet(NEATPacket packet, NEATConsultor consultor)
    {
        this.consultor = consultor; //shallow copy consultor
        this.numberOfInputs = packet.node_inputs; //copy number of inputs
        this.numberOfOutputs = packet.node_outputs; //copy number of outputs

        int numberOfNodes = packet.node_total; //number of nodes in the network from database
        int numberOfgenes = packet.gene_total; //number of genes in the network from database
        int informationSize = NEATGene.GENE_INFORMATION_SIZE; //size of genome information

        geneList = new List<NEATGene>(); //create an empty gene list

        InitilizeNodes(); //initialize initial nodes

        for (int i = numberOfInputs + numberOfOutputs; i < numberOfNodes; i++)
        { //run through the left over nodes, since (numberOfInputs + numberOfOutputs) where created by initilize node method
            NEATNode node = new NEATNode(i, NEATNode.HIDDEN_NODE); //create node with index i as id and will be hidden node
            nodeList.Add(node); //add node to node list
        }

        float[] geneInformation = packet.genome.Split('_').Select(x => float.Parse(x)).ToArray(); //using Linq libary and delimiters, parse and spilt string genome from neat packet into float array

        for (int i = 0; i < geneInformation.Length; i += informationSize)
        { //run through all gene information, 4 information make up 1 gene, thus increment by 4
            int inno = this.consultor.CheckGeneExistance((int)geneInformation[i], (int)geneInformation[i + 1]); //check if this gene exists in the consultor
            NEATGene gene = new NEATGene(inno, (int)geneInformation[i], (int)geneInformation[i + 1], geneInformation[i + 2], geneInformation[i + 3] == 1.0 ? true : false); //create gene
            geneList.Add(gene); //add gene to the gene list
        }

        //this.netID = new int[2]; //reset ID
        this.time = 0f; //reset time
        this.netFitness = 0f; //reset fitness
        this.timeLived = 0f; //reset time lived
        this.netID = ID_COUNT;
        ++ID_COUNT;
    }

    /// <summary>
    /// Creating a primitive network structure (every input connect to every output) from provided parameters
    /// </summary>
    /// <param name="consultor">Consultor with master genome and specification information</param>
    /// <param name="netID">ID of the network</param>
    /// <param name="numberOfInputs">Number of input perceptrons</param>
    /// <param name="numberOfOutputs">Number of output perceptrons</param>
    /// <param name="time">Time to test the network</param>
    public NEATNet(NEATConsultor consultor, int numberOfInputs, int numberOfOutputs, float time)
    {
        this.consultor = consultor; //shallow copy consultor
        this.numberOfInputs = numberOfInputs; //copy number of inputs
        this.numberOfOutputs = numberOfOutputs; //copy number of outputs
        this.time = time; //copy time to test

        this.netFitness = 0f; //reset net fitness
        this.timeLived = 0f; //reset time lived

        InitilizeNodes(); //initialize initial nodes
        InitilizeGenes(); //initialize initial gene sequence 
        this.netID = ID_COUNT;
        ++ID_COUNT;
    }

    /// <summary>
    /// Creating an already designed network structure from given node and gene lists
    /// </summary>
    /// <param name="consultor">Consultor with master genome and specification information</param>
    /// <param name="numberOfInputs">Number of input perceptrons</param>
    /// <param name="numberOfOutputs">Number of output perceptrons</param>
    /// <param name="copyNodes">Node list to deep copy</param>
    /// <param name="copyGenes">Gene list to deep copy</param>
    public NEATNet(NEATConsultor consultor, int numberOfInputs, int numberOfOutputs, List<NEATNode> copyNodes, List<NEATGene> copyGenes)
    {
        this.consultor = consultor; //shallow copy consultor
        this.numberOfInputs = numberOfInputs; //copy number of inputs
        this.numberOfOutputs = numberOfOutputs; //copy number of outputs

        CopyNodes(copyNodes); //deep copy node list
        CopyGenes(copyGenes); //deep copy gene list

        //this.netID = new int[2]; //reset ID
        this.time = 0f; //reset time
        this.netFitness = 0f; //reset fitness
        this.timeLived = 0f; //reset time lived
        this.netID = ID_COUNT;
        ++ID_COUNT;
    }

    /// <summary>
    /// Initilizing initial node list with given number of input perceptrons which includes the bias node
    /// </summary>
    private void InitilizeNodes()
    {
        nodeList = new List<NEATNode>(); //create an empty node list

        NEATNode node = null;

        for (int i = 0; i < numberOfInputs; i++)
        { //run through number of input neurons

            if (i == (numberOfInputs - 1)) //if this is the last input 
                node = new NEATNode(i, NEATNode.INPUT_BIAS_NODE); //make it a input bias type node  with index i as node ID
            else //if this is not the last input
                node = new NEATNode(i, NEATNode.INPUT_NODE); //make it a input type node with index i as node ID

            nodeList.Add(node); //add node to the node list
        }

        for (int i = numberOfInputs; i < numberOfInputs + numberOfOutputs; i++)
        {  //run through number of output perceptrons
            node = new NEATNode(i, NEATNode.OUTPUT_NODE); //make it a putput type node  with index i as node ID
            nodeList.Add(node); //add node to the node list
        }
    }

    /// <summary>
    /// Initilizing initial gene list with given number of input and output perceptrons to create a primitive genome (all inputs connected to all outputs)
    /// </summary>
    private void InitilizeGenes()
    {
        geneList = new List<NEATGene>(); //create an empty gene list

        for (int i = 0; i < numberOfInputs; i++)
        { //run through number of inputs
            for (int j = numberOfInputs; j < numberOfInputs + numberOfOutputs; j++)
            { //run through number of outputs
                int inno = consultor.CheckGeneExistance(i, j);  //check if gene exists in consultor
                NEATGene gene = new NEATGene(inno, i, j, UnityEngine.Random.Range(-1f, 1f), true); // create gene with default weight of 1.0 and and is active 

                InsertNewGene(gene); //insert gene to correct location in gene list
            }
        }
    }

    /// <summary>
    /// Returns the fitness of this network
    /// </summary>
    /// <returns>Fitness</returns>
    public float GetNetFitness()
    {
        return netFitness; //reutrn fitness
    }

    /// <summary>
    /// Returns the time this network has lived
    /// </summary>
    /// <returns>Time lived</returns>
    public float GetTimeLived()
    {
        return timeLived; //return time lived
    }

    /// <summary>
    /// Set ID of the network
    /// </summary>
    /// <param name="netID">Network ID to set</param>
    public void SetNetID(int netID)
    {
        this.netID = netID;
    }

    /// <summary>
    /// Set fitness to given fitness
    /// </summary>
    /// <param name="netFitness">Fitness to set network fitness to</param>
    public void SetNetFitness(float netFitness)
    {
        this.netFitness = netFitness; //set fitness
    }

    /// <summary>
    /// Add given fitness to the current fitness
    /// </summary>
    /// <param name="netFitness">Fitness to add</param>
    public void AddNetFitness(float netFitness)
    {
        this.netFitness += netFitness; //increment by given fitness
    }

    /// <summary>
    /// Set time lived of this network
    /// </summary>
    /// <param name="timeLived">Time lived to set</param>
    public void SetTimeLived(float timeLived)
    {
        this.timeLived = timeLived; //set time lived
    }

    /// <summary>
    /// Add given time lived to current time lived
    /// </summary>
    /// <param name="timeLived">Time lived to add</param>
    public void AddTimeLived(float timeLived)
    {
        this.timeLived += timeLived; //increment by given time lived
    }

    /// <summary>
    /// Return ID of this network
    /// </summary>
    /// <returns>ID of this network</returns>
    public int GetNetID()
    {
        return netID; //return network ID
    }

    /// <summary>
    /// Return test time of this network
    /// </summary>
    /// <returns>Test time</returns>
    public float GetTestTime()
    {
        return time; //return test time
    }

    /// <summary>
    /// Return total number of nodes (perceptrons) in this network
    /// </summary>
    /// <returns>Number of total nodes</returns>
    public int GetNodeCount()
    {
        return nodeList.Count; //return node code
    }

    /// <summary>
    /// Return number of genes in the genome
    /// </summary>
    /// <returns>Number of genes in the genome</returns>
    public int GetGeneCount()
    {
        return geneList.Count; //gene count
    }

    /// <summary>
    /// Return number of input perceptrons
    /// </summary>
    /// <returns>Number of input nodes</returns>
    public int GetNumberOfInputNodes()
    {
        return numberOfInputs; //return number of inputs
    }

    /// <summary>
    /// Return number of output perceptrons
    /// </summary>
    /// <returns>Number of output nodes</returns>
    public int GetNumberOfOutputNodes()
    {
        return numberOfOutputs; //return number of outputs
    }

    /// <summary>
    /// Return consultor of this network
    /// </summary>
    /// <returns>Consultor</returns>
    public NEATConsultor GetConsultor()
    {
        return consultor; //return consultor
    }

    /// <summary>
    /// Set test time to given time
    /// </summary>
    /// <param name="time">Test time</param>
    public void SetTestTime(float time)
    {
        this.time = time; //set test time
    }

    /// <summary>
    /// Compile and return gene connections information which include weight, in node, and out node in a 2D array
    /// </summary>
    /// <returns>Array of gene connections information in a 2D array</returns>
    public float[][] GetGeneDrawConnections()
    {
        int numberOfGenes = geneList.Count; //copy gene count

        float[][] connections = null; //2D connections to return 

        List<float[]> connectionList = new List<float[]>(); //empty connections list to fill with genome details

        for (int i = 0; i < numberOfGenes; i++)
        { //run through all genes
            NEATGene gene = geneList[i]; // get gene at index i

            float[] details = new float[3]; //will copy in node ID, out node ID and weight

            details[0] = gene.GetInID(); //copy in node ID
            details[1] = gene.GetOutID(); //copy out node ID

            if (gene.GetGeneState() == true) //gene is enabled
                details[2] = gene.GetWeight(); //copy weight
            else //gene is disabled
                details[2] = 0f; //set to 0

            connectionList.Add(details); //add detail to the connection list
        }

        connections = connectionList.ToArray(); //convert connection list to 2D connection array
        return connections; //return 2D connection array
    }

    /// <summary>
    /// Compile and return genome in a large string to be saved in a database
    /// </summary>
    /// <returns>Genome string</returns>
    public string GetGenomeString()
    {
        string genome = ""; //genome to return
        int numberOfGenes = geneList.Count; //get number of genes

        for (int i = 0; i < numberOfGenes; i++)
        { //run through all genes
            NEATGene gene = geneList[i]; //get gene at index i
            genome += gene.GetGeneString(); //concatenate gene string to genome

            if (i < numberOfGenes - 1)
            { //if this is not the last index
                genome += "_"; //add seperation underscore to seperate 2 different genomes
            }
        }

        return genome; //return string genome
    }

    /// <summary>
    /// Change network's input perceptron values to the given input array
    /// </summary>
    /// <param name="inputs">Replacing input perceptron values with this array</param>
    public void SetInputValues(float[] inputs)
    {
        for (int i = 0; i < numberOfInputs; i++)
        { //run through number of inputs
            if (nodeList[i].GetNodeType() == NEATNode.INPUT_NODE)
            { //only if this is a input node
                nodeList[i].SetValue(inputs[i]); //change value of node to given value at index i
            }
            else
            { //if this is not an input type node
                break;
            }
        }
    }

    /// <summary>
    /// Compile and return all node values in an array
    /// </summary>
    /// <returns>All node values in an array</returns>
    private float[] GetAllNodeValues()
    {
        float[] values = new float[nodeList.Count]; //create an array with the size of number of nodes

        for (int i = 0; i < values.Length; i++)
        { //run through number of nodes
            values[i] = nodeList[i].GetValue(); //set node values 
        }
        return values; //return all nodes value array
    }

    /// <summary>
    /// Compile and return only input node values in an array
    /// </summary>
    /// <returns>Only input node values in an array</returns>
    private float[] GetInputValues()
    {
        float[] values = new float[numberOfInputs]; //create an array with size of number of input nodes

        for (int i = 0; i < numberOfInputs; i++)
        { //run through number of inputs
            values[i] = nodeList[i].GetValue(); //set input nodes value
        }

        return values; //return input nodes value array
    }

    /// <summary>
    /// Compile and return only output node values in an array
    /// </summary>
    /// <returns>Only ouput node values in an array</returns>
    public float[] GetOutputValues()
    {
        float[] values = new float[numberOfOutputs]; //create an array with size of number of output nodes

        for (int i = 0; i < numberOfOutputs; i++)
        { //run through number of outputs
            values[i] = nodeList[i + numberOfInputs].GetValue(); //set output nodes value
        }

        return values; //return output nodes value array
    }

    /// <summary>
    /// Compile and return only hidden node values in an array
    /// </summary>
    /// <returns>Only hidden node values in an array</returns>
    private float[] GetHiddenValues()
    {
        int numberOfHiddens = nodeList.Count - (numberOfInputs + numberOfOutputs); //get number of hidden nodes that exist
        float[] values = new float[numberOfHiddens];  //create an array with size of number of hidden nodes

        for (int i = 0; i < numberOfHiddens; i++)
        {  //run through number of hiddens
            values[i] = nodeList[i + numberOfInputs + numberOfOutputs].GetValue();  //set hidden nodes value
        }

        return values; //return hidden nodes value array
    }

    /// <summary>
    /// Create node list from deep copying a given node list
    /// </summary>
    /// <param name="copyNodes">Node list to deep copy</param>
    private void CopyNodes(List<NEATNode> copyNodes)
    {
        nodeList = new List<NEATNode>(); //create an empty node list
        int numberOfNodes = copyNodes.Count; //number of nodes to copy

        for (int i = 0; i < numberOfNodes; i++)
        { //run through number of nodes to copy
            NEATNode node = new NEATNode(copyNodes[i]); //create deep copy of node at index i 
            nodeList.Add(node); //add node to node list
        }
    }

    /// <summary>
    /// Create gene list from deep copying a given gene list
    /// </summary>
    /// <param name="copyGenes">Gene list to deep copy</param>
    private void CopyGenes(List<NEATGene> copyGenes)
    {
        geneList = new List<NEATGene>(); //create an empty node list
        int numberOfGenes = copyGenes.Count; //number of nodes to copy

        for (int i = 0; i < numberOfGenes; i++)
        { //run through number of genes to copy
            NEATGene gene = new NEATGene(copyGenes[i]); //create deep copy of gene at index i 
            geneList.Add(gene); //add gene to gene list
        }
    }

    /// <summary>
    /// Feed-forward the neural network by creating a temporary phenotype from the genotype
    /// </summary>
    /// <param name="inputs">Inputs to set as the input perceptron values</param>
    /// <returns>An array of output values after feed-forward</returns>
    public float[] FireNet_OLD(float[] inputs)
    {
        int numberOfGenes = geneList.Count; //get number of genes

        SetInputValues(inputs); //set input values to the input nodes

        //set all output node values to 0
        for (int i = 0; i < numberOfOutputs; i++)
        { //run through number of outputs  
            //nodeList[i + numberOfInputs].SetValue(0f); 
        }

        //feed forward reccurent net 
        float[] tempValues = GetAllNodeValues(); //create a temporary storage of previous node values (used as a phenotype)

        for (int i = 0; i < numberOfGenes; i++)
        { //run through number of genes
            NEATGene gene = geneList[i]; //get gene at index i
            bool on = gene.GetGeneState(); //get state of the gene

            if (on == true)
            { //if gene is active
                int inID = gene.GetInID(); //get in node ID
                int outID = gene.GetOutID(); //get out node ID
                float weight = gene.GetWeight(); //get weight of the connection

                NEATNode outNode = nodeList[outID]; //get out node 

                float inNodeValue = tempValues[inID]; //get in node's value
                float outNodeValue = tempValues[outID]; //get out node's value

                float newOutNodeValue = outNodeValue + (inNodeValue * weight); //calculate new out node's value
                outNode.SetValue(newOutNodeValue); //set new value to the out node
            }
        }

        //Activation
        for (int i = 0; i < nodeList.Count; i++)
        { //run through number of nodes
            nodeList[i].Activation(); //provide an activation function over all nodes
        }

        return GetOutputValues(); //return output
    }

    /// <summary>
    /// Mutating this neural network
    /// </summary>
    public void Mutate()
    {
        int randomNumber = UnityEngine.Random.Range(1, 101); //random number between 1 and 100
        int chance = 25; //25% chance of mutation

        if (randomNumber <= chance)
        { //random number is below chance
            AddConnection(); //add connection between 2 nodes
        }
        else if (randomNumber <= (chance * 2))
        {//random number is below chance*2
            AddNode(); //add a new node bettwen an existing connection
        }

        MutateWeight(); //mutate weight


    }

    /// <summary>
    /// Adding a connection between 2 previously unconnected nodes (except no inputs shall ever connect to other inputs)  
    /// </summary>
    private void AddConnection()
    {
        int randomNodeID1, randomNodeID2, inno; //random node ID's and innovation number
        int totalAttemptsAllowed = (int)Mathf.Pow(nodeList.Count, 2); //total attempts allowed to find two unconnected nodes

        bool found = false; //used to check if a connection is found

        while (totalAttemptsAllowed > 0 && found == false)
        { //if connection is found and greater than 0 attempts left
            randomNodeID1 = UnityEngine.Random.Range(0, nodeList.Count); //pick a random node
            randomNodeID2 = UnityEngine.Random.Range(numberOfInputs, nodeList.Count); //pick a random node that is not the input

            if (!ConnectionExists(randomNodeID1, randomNodeID2))
            { //if connection does not exist with random node 1 as in node and random node 2 and out node
                inno = consultor.CheckGeneExistance(randomNodeID1, randomNodeID2); //get the new innovation number
                NEATGene gene = new NEATGene(inno, randomNodeID1, randomNodeID2, 1f, true); //create gene which is enabled and 1 as default weight

                InsertNewGene(gene); //add gene to the gene list

                found = true; //connection made
            }
            else if (nodeList[randomNodeID1].GetNodeType() > 1 && !ConnectionExists(randomNodeID2, randomNodeID1))
            { //if random node 1 isn't input type and connection does not exist with random node 2 as in node and random node 1 and out node
                inno = consultor.CheckGeneExistance(randomNodeID2, randomNodeID1); //get the new innovation number
                NEATGene gene = new NEATGene(inno, randomNodeID2, randomNodeID1, 1f, true); //create gene which is enabled and 1 as default weight

                InsertNewGene(gene); //add gene to the gene list

                found = true; //connection made
            }

            if (randomNodeID1 == randomNodeID2) //both random nodes are equal
                totalAttemptsAllowed--; //only one attemp removed becuase only 1 connection can be made
            else //both nodes are different
                totalAttemptsAllowed -= 2; //two connections can be made
        }

        if (found == false)
        { //if not found and attempts ran out
            AddNode(); //
        }
    }

    /// <summary>
    /// Adding a new node between an already existing connection. 
    /// Disable the existing connection, add a node which with connection that bbecomes the out node to the old connections in node, and a connection with in node to the old connection out node. 
    /// The first new connections gets a weight of 1.
    /// The second second new connections gets a weight of the old weight
    /// </summary>
    private void AddNode()
    {
        int firstID, secondID, thirdID, inno; //first ID is old connections in node, third ID is old connections out node, second ID is the new node, and new innovation number for the connections
        //int randomGeneIndex = Random.Range(0, geneList.Count); //find a random gene

        float oldWeight; //weight from the old gene

        //NEATGene oldGene = geneList[randomGeneIndex]; //get old gene

        NEATGene oldGene = null; //find a random old gene
        bool found = false; //used to check if old gene is found

        while (!found)
        { //run till found
            int randomGeneIndex = UnityEngine.Random.Range(0, geneList.Count); //pick random gene
            oldGene = geneList[randomGeneIndex]; //get gene at random index
            if (oldGene.GetGeneState() == true)
            { //if gene is active
                found = true; //found
            }
        }

        oldGene.SetGeneState(false); //disable this gene
        firstID = oldGene.GetInID(); //get in node ID
        thirdID = oldGene.GetOutID(); //get out node ID
        oldWeight = oldGene.GetWeight(); //get old weight

        NEATNode newNode = new NEATNode(nodeList.Count, NEATNode.HIDDEN_NODE); //create new hidden node
        nodeList.Add(newNode); //add new node to the node list
        secondID = newNode.GetNodeID(); //get new node's ID

        inno = consultor.CheckGeneExistance(firstID, secondID); //get new innovation number for new gene
        NEATGene newGene1 = new NEATGene(inno, firstID, secondID, 1f, true); //create new gene

        inno = consultor.CheckGeneExistance(secondID, thirdID); //get new innovation number for new gene
        NEATGene newGene2 = new NEATGene(inno, secondID, thirdID, oldWeight, true);  //create new gene

        //add genes to gene list
        InsertNewGene(newGene1);
        InsertNewGene(newGene2);
    }

    /// <summary>
    /// Run through all genes and randomly apply various muations with a chance of 1% 
    /// </summary>
    private void MutateWeight()
    {
        int numberOfGenes = geneList.Count; //number of genes

        for (int i = 0; i < numberOfGenes; i++)
        { //run through all genes
            NEATGene gene = geneList[i]; // get gene at index i
            float weight = 0;

            int randomNumber = UnityEngine.Random.Range(1, 101); //random number between 1 and 100

            if (randomNumber <= 1)
            { //if 1
                //flip sign of weight
                weight = gene.GetWeight();
                weight *= -1f;
                gene.SetWeight(weight);
            }
            else if (randomNumber <= 2)
            { //if 2
                //pick random weight between -1 and 1
                weight = UnityEngine.Random.Range(-1f, 1f);
                gene.SetWeight(weight);
            }
            else if (randomNumber <= 3)
            { //if 3
                //randomly increase by 0% to 100%
                float factor = UnityEngine.Random.Range(0f, 1f) + 1f;
                weight = gene.GetWeight() * factor;
                gene.SetWeight(weight);
            }
            else if (randomNumber <= 4)
            { //if 4
                //randomly decrease by 0% to 100%
                float factor = UnityEngine.Random.Range(0f, 1f);
                weight = gene.GetWeight() * factor;
                gene.SetWeight(weight);
            }
            else if (randomNumber <= 5)
            { //if 5
                //flip activation state for gene
                //gene.SetGeneState(!gene.GetGeneState());
            }
        }

    }

    /// <summary>
    /// Check if a connection exists in this gene list
    /// </summary>
    /// <param name="inID">In node in gene</param>
    /// <param name="outID">Out node in gene</param>
    /// <returns>True or false if connection exists in gene list</returns>
    private bool ConnectionExists(int inID, int outID)
    {
        int numberOfGenes = geneList.Count; //number of genes

        for (int i = 0; i < numberOfGenes; i++)
        { //run through gene list
            int nodeInID = geneList[i].GetInID(); //get in node
            int nodeOutID = geneList[i].GetOutID(); //get out node

            if (nodeInID == inID && nodeOutID == outID)
            { //check if nodes match given parameters
                return true; //return true
            }
        }

        return false; //return false if no match
    }

    /// <summary>
    /// Set all node values to 0
    /// </summary>
    public void ClearNodeValues()
    {
        int numberOfNodes = nodeList.Count; //number of nodes

        for (int i = 0; i < numberOfNodes; i++)
        { //run through all nodes
            nodeList[i].SetValue(0f); //set values to 0
        }
    }

    /// <summary>
    /// Insert new gene into its proper location the gene list.
    /// All genes are orders in asending order based on their innovation number.
    /// </summary>
    /// <param name="gene">Gene to inset into the gene list</param>
    private void InsertNewGene(NEATGene gene)
    {
        int inno = gene.GetInnovation(); //get innovation number
        int insertIndex = FindInnovationInsertIndex(inno); //get insert index

        if (insertIndex == geneList.Count)
        { //if insert index is equal to the size of the genome
            geneList.Add(gene); //add gene 
        }
        else
        { //otherwise
            geneList.Insert(insertIndex, gene); //add gene to the given insert index location
        }
    }

    /// <summary>
    /// Find the correct location to insert a given innovation number in geneList
    /// Using bianry search to find insert location.
    /// </summary>
    /// <param name="inno">Innovation to insert</param>
    /// <returns>Location to insert the innovation number</returns>
    private int FindInnovationInsertIndex(int inno)
    {
        int numberOfGenes = geneList.Count; //number of genes
        int startIndex = 0; //start index
        int endIndex = numberOfGenes - 1; //end index

        if (numberOfGenes == 0)
        { //if there are no genes
            return 0; //first location to insert
        }
        else if (numberOfGenes == 1)
        { //if there is only 1 gene
            if (inno > geneList[0].GetInnovation())
            { //if innovation is greater than the girst gene's innovation
                return 1; //insert into second location
            }
            else
            {
                return 0; //insert into first location 
            }
        }

        while (true)
        { //run till found
            int middleIndex = (endIndex + startIndex) / 2; //find middle index (middle of start and end)
            int middleInno = geneList[middleIndex].GetInnovation(); //get middle index's innovation number

            if (endIndex - startIndex == 1)
            { //if there is only 1 index between start and end index (base case on recursion)
                int endInno = geneList[endIndex].GetInnovation(); //get end inde's innovation
                int startInno = geneList[startIndex].GetInnovation(); //get start index's innovation

                if (inno < startInno)
                { //innovation is less than start innovation
                    return startIndex; //return start index
                }
                else if (inno > endInno)
                { //innovation is greater than end innovation
                    return endIndex + 1; //return end index + 1
                }
                else
                {
                    return endIndex; //otherwise right in end index
                }
            }
            else if (inno > middleInno)
            { //innovation is greater than middle innovation
                startIndex = middleIndex; //new start index will be the middle
            }
            else
            { //innovation is less than middle innovation
                endIndex = middleIndex; //new end index is middle index
            }
        }
    }

    /// <summary>
    /// Create a mutated deep copy of a given neural network
    /// </summary>
    /// <param name="net">Neural network copy to mutate</param>
    /// <returns>Mutated deep copy of the given neural network</returns>
    internal static NEATNet CreateMutateCopy(NEATNet net)
    {
        NEATNet copy = new NEATNet(net); //create deep copy of net
        copy.Mutate(); //mutate copy
        copy.GenerateNeuralNetworkFromGenome(); //< NEW LSES ADDITION

        return copy; //return mutated deep copy
    }

    /// <summary>
    /// Corssover between two parents neural networks to create a child neural network.
    /// Crossover method is as described by the NEAT algorithm.
    /// </summary>
    /// <param name="parent1">Neural network parent</param>
    /// <param name="parent2">Neural network parent</param>
    /// <returns>Child neural network</returns>
    internal static NEATNet Corssover(NEATNet parent1, NEATNet parent2)
    {
        NEATNet child = null; //child to create

        Hashtable geneHash = new Hashtable(); //hash table to be used to compared genes from the two parents

        List<NEATGene> childGeneList = new List<NEATGene>(); //new gene child gene list to be created
        List<NEATNode> childNodeList = null; //new child node list to be created

        List<NEATGene> geneList1 = parent1.geneList; //get gene list of the parent 1
        List<NEATGene> geneList2 = parent2.geneList; //get gene list of parent 2

        NEATConsultor consultor = parent1.GetConsultor(); //get consultor (consultor is the same for all neural network as it's just a pointer location)

        int numberOfGenes1 = geneList1.Count; //get number of genes in parent 1
        int numberOfGenes2 = geneList2.Count; //get number of genes in parent 2
        int numberOfInputs = parent1.GetNumberOfInputNodes(); //number of inputs (same for both parents)
        int numberOfOutputs = parent1.GetNumberOfOutputNodes(); //number of outputs (same for both parents)

        if (parent1.GetNodeCount() > parent2.GetNodeCount())
        { //if parents 1 has more nodes than parent 2
            childNodeList = parent1.nodeList; //copy parent 1's node list
        }
        else
        { //otherwise parent 2 has euqal and more nodes than parent 1
            childNodeList = parent2.nodeList; //copy parent 2's node list
        }

        for (int i = 0; i < numberOfGenes1; i++)
        { //run through all genes in parent 1
            geneHash.Add(geneList1[i].GetInnovation(), new NEATGene[] { geneList1[i], null }); //add into the hash with innovation number as the key and gene array of size 2 as value
        }

        for (int i = 0; i < numberOfGenes2; i++)
        { //run through all genes in parent 2
            int innovationNumber = geneList2[i].GetInnovation(); //get innovation number 

            if (geneHash.ContainsKey(innovationNumber) == true)
            { //if there is a key in the hash with the given innovation number
                NEATGene[] geneValue = (NEATGene[])geneHash[innovationNumber]; //get gene array value with the innovation key
                geneValue[1] = geneList2[i]; //since this array already contains value in first location, we can add the new gene in the second location
                geneHash.Remove(innovationNumber); //remove old value with the key
                geneHash.Add(innovationNumber, geneValue); //add new value with the key
            }
            else
            { //there exists no key with the given innovation number
                geneHash.Add(innovationNumber, new NEATGene[] { null, geneList2[i] }); //add into  the hash with innovation number as the key and gene array of size 2 as value
            }
        }

        ICollection keysCol = geneHash.Keys; //get all keys in the hash

        NEATGene gene = null; //

        int[] keys = new int[keysCol.Count]; //int array with size of nuumber of keys in the hash

        keysCol.CopyTo(keys, 0); //copy Icollentions keys list to keys array
        keys = keys.OrderBy(i => i).ToArray(); //order keys in asending order

        for (int i = 0; i < keys.Length; i++)
        { //run through all keys
            NEATGene[] geneValue = (NEATGene[])geneHash[keys[i]]; //get value at each index

            //compare value is used to compare gene activation states in each parent 
            int compareValue = -1;
            //0 = both genes are true, 1 = both are false, 2 = one is false other is true
            //3 = gene is dominant in one of the parents and is true, 4 = gene is dominant in one of the parents and is false

            if (geneValue[0] != null && geneValue[1] != null)
            { //gene eixts in both parents
                int randomIndex = UnityEngine.Random.Range(0, 2);

                if (geneValue[0].GetGeneState() == true && geneValue[1].GetGeneState() == true)
                { //gene is true in both
                    compareValue = 0; //set compared value to 0
                }
                else if (geneValue[0].GetGeneState() == false && geneValue[1].GetGeneState() == false)
                { //gene is false in both
                    compareValue = 1; //set compared value to 1
                }
                else
                { //gene is true in one and false in the other
                    compareValue = 2; //set compared value to 2
                }

                gene = CrossoverCopyGene(geneValue[randomIndex], compareValue); //randomly pick a gene from eaither parent and create deep copy 
                childGeneList.Add(gene); //add gene to the child gene list
            }
            else if (parent1.GetNetFitness() > parent2.GetNetFitness())
            { //parent 1's fitness is greater than parent 2
                if (geneValue[0] != null)
                { //gene value at first index from parent 1 exists
                    if (geneValue[0].GetGeneState() == true)
                    { //gene is active
                        compareValue = 3; //set compared value to 3
                    }
                    else
                    { //gene is not active
                        compareValue = 4; //set compared value to 4
                    }

                    gene = CrossoverCopyGene(geneValue[0], compareValue); //deep copy parent 1's gene
                    childGeneList.Add(gene); //add gene to the child gene list
                }
            }
            else if (parent1.GetNetFitness() < parent2.GetNetFitness())
            { //parent 2's fitness is greater than parent 1
                if (geneValue[1] != null)
                { //gene value at second index from parent 2 exists
                    if (geneValue[1].GetGeneState() == true)
                    { //gene is active
                        compareValue = 3; //set compared value to 3
                    }
                    else
                    { //gene is not active
                        compareValue = 4; //set compared value to 4
                    }

                    gene = CrossoverCopyGene(geneValue[1], compareValue); //deep copy parent 2's gene 
                    childGeneList.Add(gene); //add gene to the child gene list
                }
            }
            else if (geneValue[0] != null)
            { //both parents have equal fitness and gene value at first index from parent 1 exists
                if (geneValue[0].GetGeneState() == true)
                { //gene is active
                    compareValue = 3; //set compared value to 3
                }
                else
                { //gene is not active
                    compareValue = 4; //set compared value to 4
                }

                gene = CrossoverCopyGene(geneValue[0], compareValue); //deep copy parent 1's gene 
                childGeneList.Add(gene); //add gene to the child gene list
            }
            else if (geneValue[1] != null)
            { //both parents have equal fitness and gene value at second index from parent 2 exists
                if (geneValue[1].GetGeneState() == true)
                { //gene is active
                    compareValue = 3; //set compared value to 3
                }
                else
                { //gene is not active
                    compareValue = 4; //set compared value to 4
                }

                gene = CrossoverCopyGene(geneValue[1], compareValue); //deep copy parent 2's gene 
                childGeneList.Add(gene); //add gene to the child gene list
            }
        }

        child = new NEATNet(consultor, numberOfInputs, numberOfOutputs, childNodeList, childGeneList); //create new child neural network 
        return child; //return newly created neural network
    }

    /// <summary>
    /// Created a deep copy of a given gene. 
    /// This gene can be muated with a small chance based on the compare value. 
    /// Deactivated genes have a small chance of being activated based on the compare value. 
    /// </summary>
    /// <param name="copyGene">Gene to deep copy</param>
    /// <param name="compareValue">Value to use when activating a gene</param>
    /// <returns>Deep copied gene</returns>
    private static NEATGene CrossoverCopyGene(NEATGene copyGene, int compareValue)
    {
        NEATGene gene = new NEATGene(copyGene); //deep copy gene 

        /*int randomNumber = Random.Range(0, 20); //0-19

        if (compareValue == 2) { //if gene is false in both parents
            randomNumber = Random.Range(0, 10); //0-9
            if (randomNumber == 0) { //10% chance of activating this gene
                gene.SetGeneState(true); //activate
            }
        }
        else if (gene.GetGeneState() == false && randomNumber == 0) { //gene is false and 20% chance of activating this gene
            gene.SetGeneState(true); //activate
        }*/

        int factor = 2;
        if (compareValue == 1)
        {
            int randomNumber = UnityEngine.Random.Range(0, 25 * factor);
            if (randomNumber == 0)
            {
                gene.SetGeneState(false);
            }
        }
        else if (compareValue == 2)
        {
            int randomNumber = UnityEngine.Random.Range(0, 10 * factor);
            if (randomNumber == 0)
            {
                gene.SetGeneState(true);
            }
        }
        else
        {
            int randomNumber = UnityEngine.Random.Range(0, 25 * factor);
            if (randomNumber == 0)
            {
                gene.SetGeneState(!gene.GetGeneState());
            }
        }

        return gene; //return new gene
    }

    /// <summary>
    /// Check whether two neural networks belong to the same species based on defined coefficient values in the consultor
    /// </summary>
    /// <param name="net1">Neural network to compare</param>
    /// <param name="net2">Neural network to compare</param>
    /// <returns>True of false whether they belong to the same species</returns>
    internal static bool SameSpeciesV2(NEATNet net1, NEATNet net2)
    {
        Hashtable geneHash = new Hashtable(); //hash table to be used to compared genes from the two networks
        NEATConsultor consultor = net1.consultor; //get consultor (consultor is the same for all neural network as it's just a pointer location)
        NEATGene[] geneValue; //will be used to check whether a gene exists in both networks

        List<NEATGene> geneList1 = net1.geneList; //get first network
        List<NEATGene> geneList2 = net2.geneList; //get second network

        ICollection keysCol; //will be used to get keys from gene hash
        int[] keys; //will be used to get keys arrray from ICollections

        int numberOfGenes1 = geneList1.Count; //get number of genes in network 1
        int numberOfGenes2 = geneList2.Count; //get number of genes in network 2
        int largerGenomeSize = numberOfGenes1 > numberOfGenes2 ? numberOfGenes1 : numberOfGenes2; //get one that is larger between the 2 network
        int excessGenes = 0; //number of excess genes (genes that do match and are outside the innovation number of the other network)
        int disjointGenes = 0; //number of disjoint gene (genes that do not match in the two networks)
        int equalGenes = 0; //number of genes both neural network have

        float disjointCoefficient = consultor.GetDisjointCoefficient(); //get disjoint coefficient from consultor
        float excessCoefficient = consultor.GetExcessCoefficient(); //get excess coefficient from consultor
        float averageWeightDifferenceCoefficient = consultor.GetAverageWeightDifferenceCoefficient(); //get average weight difference coefficient
        float deltaThreshold = consultor.GetDeltaThreshold(); //get threshold 
        float similarity = 0; //similarity of the two networks 
        float averageWeightDifference = 0; //average weight difference of the two network's equal genes

        bool foundAllExcess = false; //if all excess genes are found
        bool isFirstGeneExcess = false; //if net 1 contains the excess genes

        for (int i = 0; i < geneList1.Count; i++)
        { //run through net 1's genes
            int innovation = geneList1[i].GetInnovation(); //get innovation number of gene

            geneValue = new NEATGene[] { geneList1[i], null }; //add into the hash with innovation number as the key and gene array of size 2 as value 
            geneHash.Add(innovation, geneValue);  //add into the hash with innovation number as the key and gene array of size 2 as value
        }

        for (int i = 0; i < geneList2.Count; i++)
        { //run through net 2's genes
            int innovation = geneList2[i].GetInnovation(); //get innovation number of gene

            if (!geneHash.ContainsKey(innovation))
            { //if innovation key does not exist
                geneValue = new NEATGene[] { null, geneList2[i] }; //create array of size 2 with new gene in the second position
                geneHash.Add(innovation, geneValue); //add into  the hash with innovation number as the key and gene array of size 2 as value
            }
            else
            { //key exists
                geneValue = (NEATGene[])geneHash[innovation]; //get value
                geneValue[1] = geneList2[i]; //add into second position net 2's gene
            }
        }

        keysCol = geneHash.Keys; //get all keys from gene hash
        keys = new int[keysCol.Count]; //create array with size of number of keys
        keysCol.CopyTo(keys, 0); //copy all keys from ICollections to array
        keys = keys.OrderBy(i => i).ToArray(); //order keys in ascending order

        for (int i = keys.Length - 1; i >= 0; i--)
        { //run through all keys backwards (to get all excess gene's first)
            geneValue = (NEATGene[])geneHash[keys[i]]; //get value with key

            if (foundAllExcess == false)
            { //if all excess genes have not been found
                if (i == keys.Length - 1 && geneValue[1] == null)
                { //this is the first itteration and second gene location is null
                    isFirstGeneExcess = true; //excess genes exit in net 1
                }

                if (isFirstGeneExcess == true && geneValue[1] == null)
                { //excess gene exist in net 1 and there is no gene in second location of the value
                    excessGenes++; //this is an excess gene and increment excess gene
                }
                else if (isFirstGeneExcess == false && geneValue[0] == null)
                { //excess gene exist in net 12 and there is no gene in first location of the value
                    excessGenes++; //this is an excess gene and increment excess gene
                }
                else
                { //no excess genes
                    foundAllExcess = true; //all excess genes are found
                }

            }

            if (foundAllExcess == true)
            { //if all excess genes are found
                if (geneValue[0] != null && geneValue[1] != null)
                { //both gene location are not null
                    equalGenes++; //increment equal genes
                    averageWeightDifference += Mathf.Abs(geneValue[0].GetWeight() - geneValue[1].GetWeight()); //add absolute difference between 2 weight
                }
                else
                { //this is disjoint gene
                    disjointGenes++; //increment disjoint
                }
            }
        }

        averageWeightDifference = averageWeightDifference / (float)equalGenes; //get average weight difference of equal genes

        //similarity formula -> Sim = (AVG_DIFF * AVG_COFF) + (((DISJ*DISJ_COFF) + (EXSS*EXSS_COFF)) /GENOME_SIZE)
        similarity = (averageWeightDifference * averageWeightDifferenceCoefficient) + //calculate weight difference disparity
                     (((float)disjointGenes * disjointCoefficient) / (float)largerGenomeSize) +  //calculate disjoint disparity
                     (((float)excessGenes * excessCoefficient) / (float)largerGenomeSize); //calculate excess disparity

        //if similairty is <= to threshold then return true, otherwise false
        return similarity <= deltaThreshold; //return boolean compare value
    }

    /// <summary>
    /// ---ONLY USED FOR DEBUGGING---
    /// Prints all neural network details.
    /// </summary>
    public void PrintDetails()
    {
        int numberOfNodes = nodeList.Count; //get number of nodes
        int numberOfGenes = geneList.Count; //get number of genes

        //Print various node details to Unity Log
        Debug.Log("-----------------");

        for (int i = 0; i < numberOfNodes; i++)
        {
            NEATNode node = nodeList[i];
            Debug.Log("ID:" + node.GetNodeID() + ", Type:" + node.GetNodeType());
        }

        Debug.Log("-----------------");

        for (int i = 0; i < numberOfGenes; i++)
        {
            NEATGene gene = geneList[i];
            Debug.LogWarning("Inno " + gene.GetInnovation() + ", In:" + gene.GetInID() + ", Out:" + gene.GetOutID() + ", On:" + gene.GetGeneState() + ", Wi:" + gene.GetWeight());
        }

        Debug.Log("-----------------");
    }

    public bool Equals(NEATNet other)
    {
        if (other == null)
            return false;

        return (other.netID == this.netID);
    }

    public int CompareTo(NEATNet other)
    {
        if (netFitness > other.netFitness)
            return 1;
        if (netFitness < other.netFitness)
            return -1;
        return 0;

    }

}