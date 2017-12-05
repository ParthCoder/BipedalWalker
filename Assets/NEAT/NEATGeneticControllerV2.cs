using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Collections;
using ProgressBar;
using UnityEngine.UI;

/// <summary>
/// This is a general purpose genetic controller, which handles generational computation if a given tester, specification calculation and visual display and mechanics of the panel system.
/// </summary>
public class NEATGeneticControllerV2 : MonoBehaviour
{

    //All public variables are inputs are from the editor
    public GameObject testPrefab; //Prebaf of the object to test   
    public GameObject progressBar; //Progress bar to showcase amount finished
    public GameObject netDrawer; //Draws neural network 
    public GameObject lineGraph; //Draws graph information and displays text based information
    public GameObject arrow; //Shows user where they are pointing (only used when for recording video). 
    public GameObject enviromentPrefab; //Prefab of the enviroment in which the tester is tested

    public int numberOfInputNeurons = 0; //Number of input neurons of neural network (including bias)
    public int numberOfOutputNeurons = 0; //Number of output neurons 
    public int populationSize = 0; //Population traning size

    public float testTime = 25f; //Time to test each generation
    public float elite = 0.1f; //Elite from each species
    public float beta = 1.5f; //Beta boost for a creature's fitness
    public float removeWorst = 0.5f; //Remove % worst

    public float deltaThreshold = 1f; //2 different organism with delta less than this threshold are considered to be in the same species
    public float disjointCoefficient = 1f; //how much affect disjoint genes have on delta calculation (disjoint means genes that go not line up) 
    public float excessCoefficient = 1f; //how much affect excess genes have on delta calculation (like excess, but outside the other species innovation number) 
    public float averageWeightDifferenceCoefficient = 1f; //how much affect the absolute average weight differences have on delta calculation

    public bool worldActivation = false; //Creating enviroment on each generation

    //public string networkName = "Creature"; //Name of the network (used for saving to and retrieving from database
    public bool spawnInGrid = false;
    public bool randomAngle = false;

    private GameObject enviroment; //Instantiated enviroment prefab

    private Semaphore finished; //mutex lock for when test if finished and updating test counter

    private NEATConsultor consultor; //Handles consultor genome sequence

    private NEATNet bestNet; //Best network from current computed generation

    private DatabaseOperation operations; //Handles databse operation of saving and retrieving neural network

    //private List<List<NEATNet>> species = new List<List<NEATNet>>(); //List of list of species (There can be multiple species in a population)

    private Species speciesManager = null;

    private const string ACTION_ACTIVATE = "Activate"; //Activate fucntion name in Tester class
    private const string ACTION_SUBSCRIBE = "SubscriveToEvent";
    private int numberOfGenerationsToRun = 0; //Number of generation to run, chosen from the panel
    private int generationNumber = 0; //Current generation number
    private int testCounter; //Counter for population testing, goes from 0 to population size

    private float timeScale = 0; //Current time scale
    private float currentIncrement = 0; //Current increment rate for progress bar
    private float[,] colors; //Species color indexing (up to 100 species)

    private bool viewMode = false; //If user is in view mode
    private bool computing = false; //If a generation is currently computing
    private bool load = false; //load from database or create random neural network

    public TextMesh generationNumberText;

    public InputField creatureName;
    private TextureDraw draw;

    /// <summary>
    /// Setting up initial systems
    /// </summary>
    private void Start()
    {
        Application.runInBackground = true; //Run application in background

        SetCameraLocation(); //Sets camera location to panel 

        if (ErrorCheck() == false)
        { //Make sure all parameters given are valid
            draw = GetComponent<TextureDraw>();
            draw.AddManager(this);
            Time.timeScale = 1f; //Time scale set to 1x
            timeScale = Time.timeScale;

            testCounter = 0;
            finished = new Semaphore(1, 1); //initialize a binary semaphore and set it to unlocked

            operations = new DatabaseOperation(); //initialize database oepration 
            colors = new float[populationSize, 3];

            GenerateSpeciesColor(); //Create random color for each species

            lineGraph.GetComponent<DataDrawer>().CalibrateGraph(200f, 50f, 0.5f, 0.04f, 0.15f, 0.15f); //Calibrate line graph
        }
    }

    /// <summary>
    /// Generating random colors to identify each species 
    /// </summary>
    private void GenerateSpeciesColor()
    {
        //Set color for the first species
        colors[0, 0] = UnityEngine.Random.Range(0f, 1f);
        colors[0, 1] = UnityEngine.Random.Range(0f, 1f);
        colors[0, 2] = UnityEngine.Random.Range(0f, 1f);

        for (int i = 1; i < colors.GetLength(0); i++)
        { // run through all other colors in the array
            bool found = false; //found a new color within threshold

            while (!found)
            { //loop till found is true
                found = true; // suppose found

                //create random color
                colors[i, 0] = UnityEngine.Random.Range(0f, 1f);
                colors[i, 1] = UnityEngine.Random.Range(0f, 1f);
                colors[i, 2] = UnityEngine.Random.Range(0f, 1f);

                for (int j = 0; j < i; j++)
                { //run through all other found colors to make sure this new found color is atleast a certain threshold away  
                    if (!(Mathf.Abs(((colors[i, 0] + colors[i, 1] + colors[i, 2]) - (colors[j, 0] + colors[j, 1] + colors[j, 2]))) >= 0.0001f))
                    { //if new color is not a certain threshold away then not found
                        found = false;
                    }
                }
            }
        }

    }

    /// <summary>
    /// Start computation for number of generations given
    /// </summary>
    /// <param name="gens">Number of generation to compute</param>
    public void ActionGeneration(int gens)
    {
        if (numberOfGenerationsToRun == 0 && speciesManager != null)
        { //If not computing and species exist then start
            progressBar.GetComponent<ProgressRadialBehaviour>().Value = 0; //progress bar set to 0%
            computing = true; //computing
            currentIncrement = 100f / gens; //increment of progress bar per generation
            numberOfGenerationsToRun = gens; //number of generation to run
            GeneratePopulation(); //Generate population with neural networks
            lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Action: Running " + gens + " generations"); //information for the user on action performed
        }
        else
        { //Currently simulation is running
            lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Action: Simulation currently running");  //information for the user on action performed
        }
    }

    /// <summary>
    /// View current neural networks 
    /// </summary>
    public void ActionViewCurrent()
    {
        /*if (numberOfGenerationsToRun == 0)
        { //if not computing
            viewMode = true; //view mode
            computing = true; //computing
            SetCameraLocation(); //change camera location to view 
            GeneratePopulation(); //Generate population with neural networks
        }*/
    }

    /// <summary>
    /// Take the best creature from previous generation and use it to populate the next generation
    /// </summary>
    public void ActionBest()
    {
        /*if (numberOfGenerationsToRun == 0) { //if not computing
            species = new List<List<NEATNet>>(); //create an empty population list
            List<NEATNet> newSpecies = new List<NEATNet>(); //create new species

            for (int i = 0; i < populationSize; i++) { //populate new species
                newSpecies.Add(new NEATNet(bestNet));
            }
            species.Add(newSpecies); //add new species to the species list
        }*/
    }

    /// <summary>
    /// Generating neural networks and consultor to test
    /// </summary>
    public void ActionCreateNew()
    {
        if (load == false)
        { //if loading from database is fasle
            consultor = new NEATConsultor(numberOfInputNeurons, numberOfOutputNeurons, deltaThreshold, disjointCoefficient, excessCoefficient, averageWeightDifferenceCoefficient);  //create a consultor with perceptron and coefficients
            GenerateInitialNets(); //generate standard NEAT nets
            lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Action: New neural networks created"); //information for the user on action performed
        }
        else
        { //loading from database is true
            if (operations.retrieveNet == null)
            {
                consultor = new NEATConsultor(numberOfInputNeurons, numberOfOutputNeurons, deltaThreshold, disjointCoefficient, excessCoefficient, averageWeightDifferenceCoefficient);  //create a consultor with perceptron and coefficients
                GenerateInitialNets(); //generate standard NEAT nets
                lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Action: New neural networks created"); //information for the user on action performed
            }
            else
            {
                consultor = new NEATConsultor(operations.retrieveNet[operations.retrieveNet.Length - 1], deltaThreshold, disjointCoefficient, excessCoefficient, averageWeightDifferenceCoefficient); //create a consultor with NEAT packet retrieved from database and coefficients
                NEATNet net = new NEATNet(operations.retrieveNet[operations.retrieveNet.Length - 1], consultor); //create a net from NEAT packet retrieved from database and consultor 
                lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Action: Neural networks copied from sample"); //information for the user on action performed
                GenerateInitialNetsAsCopy(net);
            }
        }
    }

    /// <summary>
    /// Change time scale to make simulation compute faster
    /// </summary>
    /// <param name="timeScale">The new time scale to change to</param>
    public void ActionSetTimeScale(float timeScale)
    {
        this.timeScale = timeScale; //set time scale to parameter (when checking time scale this local copy will be used rather than the global copy)
        Time.timeScale = timeScale; //set time scale to parameter
        lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Action: Time changed to " + timeScale); //information for the user on action performed
    }

    /// <summary>
    /// Save best neural network to database
    /// </summary>
    public void ActionSaveCurrent()
    {
        Debug.Log("Saving");
        if (creatureName.text.Equals(""))
        {
            lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Please give correct name.");
        }
        else
        {
            StartCoroutine(operations.SaveNet(bestNet, creatureName.text)); //start coroutine to save network
            lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Saved " + creatureName.text);
        }

            
    }

    /// <summary>
    /// Retrieve all neural networks from database given a network name
    /// </summary>
    public void ActionDatabaseLoad()
    {
        if (creatureName.text.Equals(""))
        {
            lineGraph.GetComponent<DataDrawer>().DisplayActionInformation("Please give correct name.");
        }
        else
        {
            StartCoroutine(operations.GetNet(creatureName.text)); //start coroutine to retrieve networks
            load = true; //loading from database is true
        }
        
    }

    /// <summary>
    /// Change camera location based on view mode
    /// </summary>
    private void SetCameraLocation()
    {
        if (viewMode == true)
        { //view mode is true
            //Camera.main.transform.position = new Vector3(0, 0, -100); //go to view location
        }
        else
        { //view mode is false
            //Camera.main.transform.position = new Vector3(0, transform.position.y, -100); //go to panel location
        }
    }

    /// <summary>
    /// Updating once per frame to display arrow
    /// </summary>
    private void Update()
    {
        if (timeScale <= 2)
        { //if time scale is less than or equal to 2x
            //move arrow to mouse location
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldPos.z = -1f;
            arrow.transform.position = worldPos;
        }
    }

    /// <summary>
    /// Destroy the enviroment on each generational simulation finish
    /// </summary>
    private void DeleteWorld()
    {
        if (worldActivation == true)
        { //if world activation is allowed
            Destroy(enviroment); //destroy enviroment
        }
    }

    /// <summary>
    /// Create an enviroment on each generational simulation start
    /// </summary>
    private void ResetWorld()
    {
        if (worldActivation == true)
        { //if world activation is allowed
            enviroment = (GameObject)Instantiate(enviromentPrefab); //instantiate enviroment
        }
    }

    private void GenerateInitialNetsAsCopy(NEATNet net)
    {
        generationNumber = 0; //0 since simulaiton has no been started yet
        testCounter = 0; //none have been tested
        speciesManager = new Species(this);

        for (int i = 0; i < populationSize; i++)
        { //run through population size
            NEATNet netCopy = new NEATNet(net); //create net with consultor, neuron information and test time
            netCopy.SetNetFitness(0f);
            netCopy.SetTimeLived(0f);
            netCopy.SetTestTime(testTime);
            netCopy.ClearNodeValues();
            netCopy.GenerateNeuralNetworkFromGenome();  //< NEW LSES ADDITION

            Population population = speciesManager.ClosestSpecies(netCopy);

            if (population == null)
            {
                population = speciesManager.CreateNewSpecie(new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)));
                population.Add(netCopy);
            }
            else
            {
                population.Add(netCopy);
            }

        }
    }

    /// <summary>
    /// Generate initial neural network, where all inputs neurons are connected to all output neurons. 
    /// Each of these nerual networks is mutated one time for slight change.
    /// And then they are paired up into species.
    /// </summary>
    private void GenerateInitialNets()
    {
        generationNumber = 0; //0 since simulaiton has no been started yet
        testCounter = 0; //none have been tested
        speciesManager = new Species(this);
        //species = new List<List<NEATNet>>(); //create an empty population list

        for (int i = 0; i < populationSize; i++)
        { //run through population size
            NEATNet net = new NEATNet(consultor, numberOfInputNeurons, numberOfOutputNeurons, testTime); //create net with consultor, perceptron information and test time
            net.Mutate(); //mutate once for diversity (must make sure SJW's aren't triggered)
            net.GenerateNeuralNetworkFromGenome();  //< NEW LSES ADDITION

            Population population = speciesManager.ClosestSpecies(net);

            if (population == null)
            {
                population = speciesManager.CreateNewSpecie(new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)));
            }
            population.Add(net);
        }
    }

    int actualPopulationSize = 0;
    /// <summary>
    /// Generate tester gameobjects given the species list
    /// </summary>
    private void GeneratePopulation()
    {
        generationNumberText.text = "Generation Number: " + (generationNumber + 1);
        ResetWorld(); //reset world
        actualPopulationSize = 0;
        List<int[]> allID = new List<int[]>(); //2D id

        //x,y,z of placing the objects
        float x = -90f;
        float y = 0;
        float z = 80f;
        
 
        List<Population> species = speciesManager.GetSpecies();
        int numberOfSpecies = species.Count; //save species count

        //create id's based on number of species in the species list
        for (int i = 0; i < species.Count; i++)
        {
            int numberOfNets = species[i].GetPopulation().Count;
            for (int j = 0; j < numberOfNets; j++)
            {
                allID.Add(new int[] { i, j });
            }
        }

        for (int i = 0; i < species.Count; i++)
        {
            List<NEATNet> population = species[i].GetPopulation();
            for (int j = 0; j < population.Count; j++)
            {

                //pick a random id, save it locally and remove it from the id list
                int randomIndex = UnityEngine.Random.Range(0, allID.Count);
                int[] randomId = allID[randomIndex];
                allID.RemoveAt(randomIndex);


                if (x >= 90 && x > 0)
                {
                    x = -90f;
                    z += -25f;
                }
                else
                {
                    x += 20f;
                }
                    
                if (spawnInGrid == true)
                    CreateIndividual(new Vector3(x, y, z), species[randomId[0]].GetPopulation()[randomId[1]], species[i].GetColor(), randomId, randomAngle);
                else
                    CreateIndividual(Vector3.zero, species[randomId[0]].GetPopulation()[randomId[1]], species[i].GetColor(), randomId, randomAngle);

                actualPopulationSize++;
            }
        }
        draw.AddColorData(speciesManager.GetSpeciesColorData());
    }

    /// <summary>
    /// Create a gameobject to test
    /// </summary>
    /// <param name="position">Position to place the object in world space</param>
    /// <param name="net">Network to activate within this gameobject</param>
    /// <param name="color">Color of this gameobject</param>
    /// <param name="id">ID of this gameobject</param>
    private void CreateIndividual(Vector3 position, NEATNet net, Color color, int[] id, bool randomEular)
    {
        GameObject tester;
        if (randomEular)
            tester = (GameObject)Instantiate(testPrefab, position, Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0)); //Instantiate gameobject with prebaf with random angle
        else
            tester = (GameObject)Instantiate(testPrefab); //Instantiate gameobject with prebaf 

        tester.name = id[0] + "_" + id[1]; //set tester name to match id
        tester.SendMessage(ACTION_ACTIVATE, net); //activate tester with net
        tester.SendMessage(ACTION_SUBSCRIBE, this);
        tester.SendMessage("SetColor", color);
    }

    /// <summary>
    /// Delegate will inform this event method when tester object finishs its simulation
    /// </summary>
    /// <param name="source">NOT USED</param>
    /// <param name="ev">NOT USED</param>
    public void OnFinished(object source, EventArgs ev)
    {
        finished.WaitOne(); //mutex lock since test counter is a shared resource

        testCounter++; //increment counter 
        if (testCounter == actualPopulationSize)
        { //if all tests are finished
            DeleteWorld(); //delete enviroment
            TestFinished();
            //Invoke("TestFinished",2f); //wait 1 second to make sure all gameobjects that need to be deleted are deleted
        }

        finished.Release(); //release lock
    }

    /// <summary>
    /// Calculate next size of species population using explicit fitness sharing distribution and breed previous generation asexually (elite) and sexually.
    /// </summary>
    private void TestFinished()
    {
        testCounter = 0; //reset test counter

        if (numberOfGenerationsToRun > 0)
        { //if computing
            generationNumber++; // increment generation number

            List<NEATNet> top = speciesManager.GetTopBrains(1);

            if (top != null && top.Count >= 1)
            {
                bestNet = new NEATNet(top[0]); //set best net from the best index
                bestNet.SetNetFitness(top[0].GetNetFitness());
                lineGraph.GetComponent<DataDrawer>().PlotData(top[0].GetNetFitness(), "Generation Number: " + generationNumber + ", Highest Fitness: " + top[0].GetNetFitness() + ", Delta: " + consultor.GetDeltaThreshold()); //plot highest fitness on graph
            }

            netDrawer.SendMessage("DrawNet", bestNet); //Draw best network
            speciesManager.GenerateNewGeneration(populationSize);

        
        } //number of generation > 0

        if (numberOfGenerationsToRun > 0)
        { //if computing
            numberOfGenerationsToRun--; //decrement number of generations to run
            progressBar.GetComponent<ProgressRadialBehaviour>().IncrementValue(currentIncrement); //increment progress bar by previously caluclated increment value
            GeneratePopulation(); //Generate population with neural networks
        }
        else
        { //done computing
            viewMode = false; //view is false
            computing = false; //computing is false
            SetCameraLocation(); //ser camera location back to panel
        }
    }

    /// <summary>
    /// Create and return sorted fitness with indicies corresponding to the list of neural networks provided
    /// </summary>
    /// <param name="networks">Networks to use to create a sorted list</param>
    /// <returns>Sorted fitness of 2D array</returns>
    private float[,] SortedFitnessIndicies(List<NEATNet> networks)
    {
        float[,] sorted = new float[networks.Count, 2]; //sorted 2D array of networks, first row represents index and second represents fitness

        for (int i = 0; i < sorted.GetLength(0); i++)
        { //run through number of networks
            sorted[i, 0] = i;
            sorted[i, 1] = networks[i].GetNetFitness();
        }

        return SortFitness(sorted); //return sorted fitness
    }

    /// <summary>
    /// Simple bubble sort to sort and return fitness of a given 2D array. 
    /// First row is supposed to be indicies which is swapped around while second row which is fitness is being sorted. 
    /// </summary>
    /// <param name="sort">Fitness to sort</param>
    /// <returns>Sorted fitness of 2D array</returns>
    private float[,] SortFitness(float[,] sort)
    {
        float[] tempFitness = new float[2]; //temp 
        bool swapped = true; //sawp has occurred
        int j = 0; //counter

        while (swapped == true)
        {
            swapped = false; //suppose no swap has occurred
            j++; //increment counter

            for (int i = 0; i < sort.GetLength(0) - j; i++)
            { //run through sort length - j
                if (sort[i, 1] > sort[i + 1, 1])
                { //if fitness at i is greater than fitness at i + 1
                    //save index i information in temp
                    tempFitness[0] = sort[i, 0];
                    tempFitness[1] = sort[i, 1];

                    //swap information from index i with i+1
                    sort[i, 0] = sort[i + 1, 0];
                    sort[i, 1] = sort[i + 1, 1];

                    //fill index i+1 with temp 
                    sort[i + 1, 0] = tempFitness[0];
                    sort[i + 1, 1] = tempFitness[1];

                    swapped = true; //swap has occurred
                }
            }
        }

        return sort; //return sort
    }

    /// <summary>
    /// Basic error check to make sure all inputs are valid
    /// </summary>
    /// <returns>True or false if an error has occurred</returns>
    private bool ErrorCheck()
    {
        bool error = false;

        //incorrect number of input perceptrons
        if (numberOfInputNeurons <= 0)
        {
            Debug.LogError("Need one or more input perceptrons.");
            error = true;
        }

        //incorrect number of output perceptrons
        if (numberOfOutputNeurons <= 0)
        {
            Debug.LogError("Need one or more output perceptrons.");
            error = true;
        }

        //incorrect population size
        if (populationSize <= 0)
        {
            Debug.LogError("Population size must be greater than 0.");
            error = true;
        }

        if (populationSize % 2 != 0 || populationSize % 4 != 0)
        {
            Debug.LogError("Population size must be divisible by 2 and 4.");
            error = true;
        }

        //incorrect test time
        if (testTime <= 0)
        {
            Debug.LogError("Time to test must be greater than 0 seconds.");
            error = true;
        }

        //error has occurred if error is true
        if (error == true)
        {
            Debug.LogError("One or more issues found. Exiting.");
            return true;
        }
        else
        {
            return false;
        }
    }
}
