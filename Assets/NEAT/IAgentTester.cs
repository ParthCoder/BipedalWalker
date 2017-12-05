
// Gives basic functionality to test an Agent 
using UnityEngine;

public interface IAgentTester
{

    void UpdateNet(); //Function where update will take place once per frame

    void OnFinished(); //After test is finished

    void CalculateFitnessOnUpdate(); //Calculate fitness once per frame

    void CalculateFitnessOnFinish(); //Final fitness calculation at the end

    void Activate(NEATNet net); //Activate NEATNet of agent

    void SubscriveToEvent(NEATGeneticControllerV2 controller); //Subscrive to event listener in main controller

    bool FailCheck(); //Check if agent is in a valid state

    void SetColor(Color color); //set color of this object

    NEATNet GetNet(); //Return NEATNet 

}
