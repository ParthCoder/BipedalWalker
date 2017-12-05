/// <summary>
/// NEAT Packet which describes how neural network information is stored in the database.
/// This class is used to map with a JSON object and get its information
/// </summary>
public class NEATPacket
{
    public int creature_id
    {
        get; set;

    } //ID of the neural network in database (primary key)

    public string creature_name
    {
        get; set;
    } //Name of the neural network

    public double creature_fitness
    {
        get; set;
    } //Fitness of the neural network

    public int node_total
    {
        get; set;
    } //Number of nodes in the neural network 

    public int node_inputs
    {
        get; set;
    } //Number of inputs nodes in the neural network

    public int node_outputs
    {
        get; set;
    } //Number of outputs in the neural network

    public int gene_total
    {
        get; set;
    } //Number of genes in the genome of the neural network

    public int genome_total
    {
        get; set;
    } //Number of genes in the master consultor genome

    public string genome
    {
        get; set;
    } //Neural network genome in string form

    public string consultor_genome
    {
        get; set;
    } //Master consultor neural network in string form
}

