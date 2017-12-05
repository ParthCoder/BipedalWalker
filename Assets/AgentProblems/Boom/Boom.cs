using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boom : MonoBehaviour {

    /// <summary>
    /// Event subscriptions to notify controller when test is finished
    /// </summary>
    /// <param name="source">Source of the event (this)</param>
    /// <param name="args">Nothing</param>
    public delegate void TestFinishedEventHandler(object source, EventArgs args);
    public event TestFinishedEventHandler TestFinished;

    private bool isActive = false; // is this agent active
    private bool finished = false; // is this agent finished.  Making sure only 1 event is sent.

    private NEATNet net; //The brain

    private const string ACTION_ON_FINISHED = "OnFinished"; //On finished method

    private NEATGeneticControllerV2 controller; //Controller

    private Transform hex;
    private Rigidbody2D rbody;

    /// <summary>
    /// Set Color to this agent. Looks visually pleasing and may help in debugging? 
    /// </summary>
    /// <param name="color"> color</param>
    public void SetColor(Color color)
    {
        Renderer[] childRend = transform.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < childRend.Length; i++)
        {
            childRend[i].material.color = color;
        }
        rbody = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Start up tasks for this agent game object.
    /// </summary>
    void Start()
    {
        hex = GameObject.Find("Hexagon").transform;
        
    }

    /// <summary>
    /// Tick
    /// </summary>
    public void UpdateNet()
    {
        float[] input = new float[1];
        float angle = transform.eulerAngles.z % 360;
        angle = angle < 0 ? angle + 360 : angle;
        Vector2 deltaVector = (hex.position - transform.position);
        float deltaVectorAngle = Mathf.Atan2(deltaVector.y, deltaVector.x);
        deltaVectorAngle *= Mathf.Rad2Deg;
        deltaVectorAngle = deltaVectorAngle % 360;
        deltaVectorAngle = deltaVectorAngle < 0 ? deltaVectorAngle + 360 : deltaVectorAngle;
        deltaVectorAngle = 90f - deltaVectorAngle;
        deltaVectorAngle = deltaVectorAngle < 0 ? deltaVectorAngle + 360 : deltaVectorAngle;

        deltaVectorAngle = 360f - deltaVectorAngle;
        deltaVectorAngle -= angle;

        if (deltaVectorAngle > 180f)
        {
            deltaVectorAngle = deltaVectorAngle - 360f;
        }
        deltaVectorAngle = deltaVectorAngle / 180f; // [-1 , 1];

        input[0] = deltaVectorAngle;

        float[] output = net.FireNet(input);
        rbody.velocity = transform.up * 2.5f;
        rbody.angularVelocity = output[0] * 500f;

        net.AddNetFitness(1f-Mathf.Abs(deltaVectorAngle)); // inc fitness if delta angle == 0... and no change if angle == -180 or 180
    }

    /// <summary>
    /// Some fail condition for this agent
    /// </summary>
    /// <returns></returns>
    public bool FailCheck()
    {
        return false;
    }

    /// <summary>
    /// Fitness update per tick. Does not have to happen here! But good practice.
    /// </summary>
    public void CalculateFitnessOnUpdate()
    {

    }

    /// <summary>
    /// Final fitness calculation once this agent is finished or failed
    /// </summary>
    public void CalculateFitnessOnFinish()
    {

    }

    /// <summary>
    /// No need to worry about this method! You just need to code in UpdateNet and CalculateFitnessOnUpdate :D
    /// </summary>
    void FixedUpdate()
    {
        if (isActive == true)
        {
            UpdateNet(); //update neural net
            CalculateFitnessOnUpdate(); //calculate fitness

            if (FailCheck() == true)
            {
                OnFinished();
            }
        }
    }



    /// <summary>
    /// OnFinished is called when we want to notify controller this agent is done. 
    /// Automatically handels notification.
    /// </summary>
    public void OnFinished()
    {
        if (TestFinished != null)
        {
            if (!finished)
            {
                finished = true;
                CalculateFitnessOnFinish();
                TestFinished(net.GetNetID(), EventArgs.Empty);
                TestFinished -= controller.OnFinished; //unsubscrive from the event notification
                Destroy(gameObject); //destroy this gameobject
            }
        }
    }

    /// <summary>
    /// Activated the agent when controller give it a brain. 
    /// </summary>
    /// <param name="net">The brain</param>
    public void Activate(NEATNet net)
    {
        this.net = net;
        Invoke(ACTION_ON_FINISHED, net.GetTestTime());
        isActive = true;
    }

    /// <summary>
    /// Getting net. 
    /// This could be used by some other objects that have reference to this game object 
    /// and want to see the brain.
    /// </summary>
    /// <returns> The brain</returns>
    public NEATNet GetNet()
    {
        return net;
    }

    /// <summary>
    /// Adds controller and subscribes to an event listener in controller
    /// </summary>
    /// <param name="controller">Controller</param>
    public void SubscriveToEvent(NEATGeneticControllerV2 controller)
    {
        this.controller = controller;
        TestFinished += controller.OnFinished; //subscrive to an event notification
    }

}
