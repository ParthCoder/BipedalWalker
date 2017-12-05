using UnityEngine;
using System.Collections;
using System;

//14 2      1 4 4 4
public class ObsticalAvoider : MonoBehaviour, IAgentTester
{

    public GameObject linePrefab;

    private NEATGeneticControllerV2 controller;
    private NEATNet net;
    private bool isActive = false;
    private bool isLoaded = false;
    private const string ACTION_ON_FINISHED = "OnFinished";
    public delegate void TestFinishedEventHandler(object source, EventArgs args);
    public event TestFinishedEventHandler TestFinished;

    private float damage = 100f;
    private bool finished = false;

    private int numberOfSensors = 12;
    private GameObject[] lineObjects;
    private LineRenderer[] lines;
    private float[] sightHit;

    private Rigidbody2D rBody;
    private Vector3 oldPosition;
    private float differencePos;
    private float pointTime = 2f;

    // Use this for initialization
    void Start()
    {
        transform.eulerAngles = new Vector3(0, 0, -90);
        rBody = GetComponent<Rigidbody2D>();
        lineObjects = new GameObject[numberOfSensors];
        lines = new LineRenderer[numberOfSensors];
        sightHit = new float[numberOfSensors];

        for (int i = 0; i < numberOfSensors; i++)
        {
            lineObjects[i] = (GameObject)Instantiate(linePrefab);
            lineObjects[i].transform.parent = transform;
            lines[i] = lineObjects[i].GetComponent<LineRenderer>();
            lines[i].SetWidth(0.1f, 0.1f);
            lines[i].material = new Material(Shader.Find("Particles/Additive"));
            lines[i].SetColors(Color.red, Color.red);
        }

        oldPosition = transform.position;
        Point();
    }

    public void Point()
    {
        differencePos = (oldPosition - transform.position).magnitude;
        oldPosition = transform.position;
        net.AddNetFitness(differencePos * differencePos);

        Invoke("Point", pointTime);
    }

    public void UpdateNet()
    {
        float angle = -90;
        float angleAdd = 22.5f;
        float distance = 3f;
        float outDistance = 0.35f;

        Vector3[] direction = new Vector3[numberOfSensors];
        Vector3[] relativePosition = new Vector3[numberOfSensors];
        RaycastHit2D[] rayHit = new RaycastHit2D[numberOfSensors];

        float redness = 1f - (damage / 100f);
        Color lineColor = new Color(1f, redness, redness);

        for (int i = 0; i < numberOfSensors; i++)
        {
            direction[i] = Quaternion.AngleAxis(angle, Vector3.forward) * transform.up;
            relativePosition[i] = transform.position + (outDistance * direction[i]);
            rayHit[i] = Physics2D.Raycast(relativePosition[i], direction[i], distance);
            lines[i].SetPosition(0, relativePosition[i]);
            sightHit[i] = -1f;

            if (rayHit[i].collider != null)
            {
                sightHit[i] = Vector2.Distance(rayHit[i].point, transform.position) / (distance);
                lines[i].SetPosition(1, rayHit[i].point);
            }
            else
            {
                lines[i].SetPosition(1, relativePosition[i]);
            }

            lines[i].SetColors(lineColor, lineColor);

            angle += angleAdd;
        }

        int num = Physics2D.OverlapCircleAll(transform.position, 0.3f).Length;
        num--;

        float[] inputValues = {
            sightHit[0], sightHit[1], sightHit[2],
            sightHit[3], sightHit[4], sightHit[5],
            sightHit[6], sightHit[7], sightHit[8],
            sightHit[9], sightHit[10], sightHit[11],
            damage /100f, num
        };

        float[] output = net.FireNet(inputValues);

        if (output[0] > 0)
            rBody.velocity = 7 * transform.up * output[0];
        else
        {
            rBody.velocity = rBody.velocity.magnitude * transform.up;
        }
        rBody.angularVelocity = 200f * output[1];

        float turn = (1f - Mathf.Abs(output[1]) / 100f);
        //net.AddNetFitness(turn/2f);  
    }

    public bool FailCheck()
    {
        if (damage <= 0)
        {
            return true;
        }

        return false;
    }

    public void CalculateFitnessOnUpdate()
    {
        net.AddTimeLived(Time.deltaTime);
    }

    //--Add your own neural net fail code here--//
    //Final fitness calculations
    public void CalculateFitnessOnFinish()
    {
        /*if (net.GetNetFitness() > 2000)
        {
            net.SetNetFitness(net.GetNetFitness() * net.GetTimeLived() * 4f);
        }
        else if (net.GetNetFitness() > 1700)
        {
            net.SetNetFitness(net.GetNetFitness() * net.GetTimeLived() * 3f);
        }
        else if (net.GetNetFitness() > 1400) {
            net.SetNetFitness(net.GetNetFitness() * net.GetTimeLived()*2f);
        }
        else if (net.GetNetFitness() > 1100)
        {
            net.SetNetFitness(net.GetNetFitness()*net.GetTimeLived());
        }*/
        float fitness = net.GetNetFitness();
        float timeLivedPoint = 1f + (net.GetTimeLived() / net.GetTestTime());
        float damagePoint = 1f/* + (damage / 100f)*/;
        net.SetNetFitness(fitness * timeLivedPoint * damagePoint);
    }

    void OnCollisionStay2D(Collision2D coll)
    {
        if (coll.collider.name.Equals("wall"))
        {
            damage -= 100f;
        }
        else
        {
            int hits = 0;

            for (int i = 0; i < numberOfSensors; i++)
            {
                if (sightHit[i] > 0 && sightHit[i] < 0.25f)
                {
                    hits++;
                }
            }

            if (hits >= 3)
            {
                damage -= 5f * (rBody.velocity.magnitude / 7f);
            }
            else
            {
                //damage -= 0.25f;
            }
        }

    }

    //---
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

    //action based on neural net faling the test
    //protected virtual
    public virtual void OnFinished()
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

    public void Activate(NEATNet net)
    {
        this.net = net;
        Invoke(ACTION_ON_FINISHED, net.GetTestTime());
        isActive = true;
    }

    public NEATNet GetNet()
    {
        return net;
    }

    public void SubscriveToEvent(NEATGeneticControllerV2 controller)
    {
        this.controller = controller;
        TestFinished += controller.OnFinished; //subscrive to an event notification
    }

    public void SetColor(Color color)
    {
            
    }
}
