using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

public class Bipedal : MonoBehaviour {

    /// <summary>
    /// Event subscriptions to notify controller when test is finished
    /// </summary>
    /// <param name="source">Source of the event (this)</param>
    /// <param name="args">Nothing</param>
    public delegate void TestFinishedEventHandler(object source, EventArgs args);
    public event Boom.TestFinishedEventHandler TestFinished;

    private bool isActive = false; // is this agent active
    private bool finished = false; // is this agent finished.  Making sure only 1 event is sent.

    private NEATNet net; //The brain

    private const string ACTION_ON_FINISHED = "OnFinished"; //On finished method

    private NEATGeneticControllerV2 controller; //Controller

    private Rigidbody2D body, thighl, thighr, legl, legr;
    private BoxCollider2D bodyBox, thighlBox, thighrBox, leglBox, legrBox, groundBox;
    private HingeJoint2D thighlHinge, thighrHinge, leglHinge, legrHinge;
    private JointMotor2D motor;
    private float prev_x_pos;
    private const float SPEED_FACTOR = 500f;
    private const float DIVISION_FACTOR = 1f;

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
    }

    /// <summary>
    /// Start up tasks for this agent game object.
    /// </summary>
    void Start()
    {
        // set rigid body components
        Rigidbody2D[] childBody = GetComponentsInChildren<Rigidbody2D>();
        body = childBody[0];
        thighl = childBody[1];
        legl = childBody[2];
        thighr = childBody[3];
        legr = childBody[4];

        // set box colliders
        BoxCollider2D[] childBox = GetComponentsInChildren<BoxCollider2D>();
        bodyBox = childBox[0];
        thighlBox = childBox[1];
        leglBox = childBox[2];
        thighrBox = childBox[3];
        legrBox = childBox[4];
        groundBox = GameObject.Find("Ground").GetComponent<BoxCollider2D>();

        // set hinge joints
        HingeJoint2D[] childJoints = GetComponentsInChildren<HingeJoint2D>();
        thighlHinge = childJoints[0];
        leglHinge = childJoints[1];
        thighrHinge = childJoints[2];
        legrHinge = childJoints[3];

        prev_x_pos = body.position.x;
    }

    /// <summary>
    /// Tick
    /// </summary>
    public void UpdateNet()
    {
        float[] input = new float[9];

        // parts angles
        float body_angle = body.transform.eulerAngles.z;
        if (body_angle > 180f) body_angle -= 360f;
        float thighl_angle = thighl.transform.eulerAngles.z;
        if (thighl_angle > 180f) thighl_angle -= 360f;
        float thighr_angle = thighr.transform.eulerAngles.z;
        if (thighr_angle > 180f) thighr_angle -= 360f;
        float legl_angle = legl.transform.eulerAngles.z;
        if (legl_angle > 180f) legl_angle -= 360f;
        float legr_angle = legr.transform.eulerAngles.z;
        if (legr_angle > 180f) legr_angle -= 360f;

        // joint angles
        float thighl_joint = (thighl_angle - body_angle) / 50f; // [-1,1]
        float thighr_joint = (thighr_angle - body_angle) / 50f; // [-1,1]
        float legl_joint = (legl_angle - thighl_angle) / 50f;
        float legr_joint = (legr_angle - thighr_angle) / 50f;

        input[0] = body_angle / 180f;
        input[1] = thighl_joint;
        input[2] = thighr_joint;
        input[3] = legl_joint;
        input[4] = legr_joint;

        // touch sensors input
        input[5] = thighlBox.IsTouching(groundBox) == true ? 1f : -1f;
        input[6] = thighrBox.IsTouching(groundBox) == true ? 1f : -1f;
        input[7] = leglBox.IsTouching(groundBox) == true ? 1f : -1f;
        input[8] = legrBox.IsTouching(groundBox) == true ? 1f : -1f;

        float[] output = net.FireNet(input);

        // control the motor speed
        motor = thighlHinge.motor;
        motor.motorSpeed = SPEED_FACTOR * output[0];
        thighlHinge.motor = motor;

        motor = leglHinge.motor;
        motor.motorSpeed = SPEED_FACTOR * output[1];
        leglHinge.motor = motor;

        motor = thighrHinge.motor;
        motor.motorSpeed = SPEED_FACTOR * output[2];
        thighrHinge.motor = motor;

        motor = legrHinge.motor;
        motor.motorSpeed = SPEED_FACTOR * output[3];
        legrHinge.motor = motor;

    }

    /// <summary>
    /// Some fail condition for this agent
    /// </summary>
    /// <returns></returns>
    public bool FailCheck()
    {
        // do damage if not moving << HOW??
        if (bodyBox.IsTouching(groundBox) || thighlBox.IsTouching(groundBox) || thighrBox.IsTouching(groundBox))
            return true;
        return false;
    }

    /// <summary>
    /// Fitness update per tick. Does not have to happen here! But good practice.
    /// </summary>
    public void CalculateFitnessOnUpdate()
    {
        // fitness on moving more fast towards right
        float diff = body.position.x - prev_x_pos;
        diff = diff < 0f ? 0f : diff;
        net.AddNetFitness(diff/DIVISION_FACTOR);
        prev_x_pos = body.position.x;
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
