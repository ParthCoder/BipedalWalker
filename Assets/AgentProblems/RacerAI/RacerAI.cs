using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

public class RacerAI : MonoBehaviour
{

    private NEATGeneticControllerV2 controller;

    public List<Rigidbody2D> bodies;

    private NEATNet net;
    private bool isActive = false;
    private bool isLoaded = false;
    private const string ACTION_ON_FINISHED = "OnFinished";

    public delegate void TestFinishedEventHandler(object source, EventArgs args);
    public event TestFinishedEventHandler TestFinished;

    List<Vector2> points = new List<Vector2>();
    float damage = 100f;

    bool finished = false;

    public GameObject linePrefab;
    int numberOfSensors = 7;
    private GameObject[] lineObjects = new GameObject[7];
    private LineRenderer[] lines = new LineRenderer[7];
    private float[] sightHit = new float[7];

    Vector2 posCheck = Vector2.zero;
    void Start()
    {
        for (int i = 0; i < numberOfSensors; i++)
        {
            lineObjects[i] = (GameObject)Instantiate(linePrefab);
            lineObjects[i].transform.parent = transform;
            lines[i] = lineObjects[i].GetComponent<LineRenderer>();
            lines[i].startWidth = 0.1f;
            lines[i].endWidth = 0.1f;
            lines[i].material = new Material(Shader.Find("Particles/Additive"));
            lines[i].startColor = Color.red;
            lines[i].endColor = Color.red;
        }

       
        TakePoint();


    }

    float avgAngle = 0;
    void TakePoint()
    {

        if (posCheck == Vector2.zero)
        {
            posCheck = (Vector2)bodies[0].transform.position + new Vector2(0.00001f, 0.0001f);
        }
        else if (Vector2.Distance(posCheck, (Vector2)bodies[0].transform.position) <= 2)
        {
            OnFinished();
        }
        else
        {
            posCheck = bodies[0].transform.position;
        }
        Invoke("TakePoint", 1f);
    }

    void FixedUpdate()
    {
        if (isActive == true)
        {
            UpdateNet(); //update neural net
            CalculateFitness(); //calculate fitness

            if (FailCheck() == true)
            {
                OnFinished();
            }
        }
    }

    public void Activate(NEATNet net)
    {
        this.net = net;
        Invoke(ACTION_ON_FINISHED, net.GetTestTime());
        isActive = true;
    }

    //action based on neural net faling the test
    protected virtual void OnFinished()
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

    Vector3 newPos = Vector3.zero;
    float distanceToNewPos = 0f;
    int posNum = 1;
    public void NewPos(object newTransform)
    {
        Transform newTransformTemp = (Transform)newTransform;
        int tempPosNum = int.Parse(newTransformTemp.name);

        int checkPos = tempPosNum;
        if (checkPos == 1)
            checkPos = 19;
        else
            checkPos = checkPos - 1;

        if (checkPos == posNum)
        {
            posNum = tempPosNum;
            Vector3 pos = newTransformTemp.position;
            pos.z = 0f;
            distanceToNewPos = Vector3.Distance(bodies[0].transform.position, pos);
            newPos = pos;

            net.AddNetFitness((net.GetNetFitness()) + 1f);

        }
        else if (!(tempPosNum == posNum))
        {
            OnFinished();
        }
    }


    //--Add your own neural net update code here--//
    //Updates nerual net with new inputs from the agent
    private void UpdateNet()
    {
        UpdateOverTime();

        float angle = -90;
        float angleAdd = 180f / (numberOfSensors - 1);
        float distance = 4f;
        if (distance <= 0)
            distance = 0f;
        float outDistance = 0f;
        int ignoreLayers = ~((1 << 8) | (1 << 9));


        Vector3[] direction = new Vector3[numberOfSensors];
        Vector3[] relativePosition = new Vector3[numberOfSensors];
        RaycastHit2D[] rayHit = new RaycastHit2D[numberOfSensors];

        float redness = 1f - (damage / 100f);
        Color lineColor = new Color(1f, redness, redness);

        for (int i = 0; i < numberOfSensors; i++)
        {
            direction[i] = Quaternion.AngleAxis(angle, Vector3.forward) * bodies[0].transform.up;
            relativePosition[i] = bodies[0].transform.position + (outDistance * direction[i]);
            rayHit[i] = Physics2D.Raycast(relativePosition[i], direction[i], distance, ignoreLayers);
            lines[i].SetPosition(0, relativePosition[i]);
            sightHit[i] = -1f;

            if (rayHit[i].collider != null) //&& rayHit[i].collider.isTrigger == false)
            {
                sightHit[i] = Vector2.Distance(rayHit[i].point, bodies[0].transform.position) / (distance);
                lines[i].SetPosition(1, rayHit[i].point);
            }
            else
            {
                lines[i].SetPosition(1, relativePosition[i]);
            }
            lines[i].endColor = lineColor;
            lines[i].endColor = lineColor;

            angle += angleAdd;
        }

        /*float angle = -90;
        float angleAdd = 18f;
        float distance = 10f;
        float outDistance = 0.35f;
        int ignoreFoodLayer = ~((1 << 8) | (1 << 9));
        int numerOfSensors = 11;

        Vector3[] direction = new Vector3[numerOfSensors];
        Vector3[] relativePosition = new Vector3[numerOfSensors];
        RaycastHit2D[] rayHit = new RaycastHit2D[numerOfSensors];

        float redness = 1f - (damage / 100f);
        Color lineColor = new Color(1f, redness, redness);


        for (int i = 0; i < numerOfSensors; i++)
        {
            direction[i] = Quaternion.AngleAxis(angle, Vector3.forward) * bodies[0].transform.up;
            relativePosition[i] = bodies[0].transform.position + (outDistance * direction[i]);
            rayHit[i] = Physics2D.Raycast(relativePosition[i], direction[i], distance, ignoreFoodLayer);
            lines[i].SetPosition(0, relativePosition[i]);
            sightHit[i] = -1f;

            if (rayHit[i].collider != null) //&& rayHit[i].collider.isTrigger == false)
            {
                sightHit[i] = Vector2.Distance(rayHit[i].point, bodies[0].transform.position) / distance;
                lines[i].SetPosition(1, rayHit[i].point);
            }
            else
            {
                lines[i].SetPosition(1, relativePosition[i]);
            }

            lines[i].SetColors(lineColor, lineColor);

            angle += angleAdd;
        }*/
    }

    float velo = 0f;
    public void UpdateOverTime()
    {
        Vector2 dir = bodies[0].transform.up;

        float d = Vector3.Distance(bodies[0].transform.position, newPos);
        float[] inputValues = {
            sightHit[0], sightHit[1], sightHit[2]
        };

        float[] output = net.FireNet(inputValues);
        /*if (this.name.Equals("0_0"))
            Debug.Log(bodies[0].angularVelocity);*/



        if (output[0] > 0)
            bodies[0].velocity = 15f * dir * output[0];
        else
            bodies[0].velocity = -5f * dir * output[0];

        bodies[0].angularVelocity = 250f * output[1];

        /*
        int index = 0;
        float value = output[0];
        for (int i = 1; i < output.Length; i++)
        {
            if (output[i] > value)
            {
                index = i;
                value = output[i];
            }
        }
        bodies[0].velocity = dir * 5f;

        if (index == 0)
        {
            bodies[0].angularVelocity = 75f;
        }
        else if(index == 1)
        {
            bodies[0].angularVelocity = -75f;
        }
        else if (index == 2)
        {
            bodies[0].angularVelocity = 175f;
        }
        else if (index == 3)
        {
            bodies[0].angularVelocity = -175f;
        }*/


        /*if (output[0] > output[1])
            bodies[0].angularVelocity += 1f;
        else
            bodies[0].angularVelocity -= 1f;*/



        //bodies[0].angularVelocity = -100f;


        /*float angular = bodies[0].angularVelocity;
        angular += output[1] * Time.deltaTime;
        if (Mathf.Abs(angular) < 25f)
            bodies[0].angularVelocity = angular ;

        float tempVelo = velo;
        tempVelo += output[0] * Time.deltaTime * 10f;
        if (Mathf.Abs(tempVelo) < 10f)
        {
            velo = tempVelo;
            bodies[0].velocity = dir * velo;
        }*/

        /*Vector2 velo = bodies[0].velocity;
        velo += dir * output[0] * Time.deltaTime * 10f;
        if(velo.magnitude<10f)
            bodies[0].velocity = velo;

        float angular = bodies[0].angularVelocity;
        angular += output[1] * Time.deltaTime;
        if (Mathf.Abs(angular)<25f)
            bodies[0].angularVelocity = angular * 10f; */

        /*if (output[0] > 0)
            bodies[0].velocity = 15f * dir * output[0];

        bodies[0].angularVelocity = 250f * output[1];*/
    }

    //--Add your own neural net fail code here--//
    //restrictions on the test to fail bad neural networks faster
    private bool FailCheck()
    {

        if (damage <= 0)
            return true;

        return false;
    }

    //--Add your own neural net fail code here--//
    //Fitness calculation
    private void CalculateFitness()
    {


    }


    //--Add your own neural net fail code here--//
    //Final fitness calculations
    private void CalculateFitnessOnFinish()
    {

        /*float fit = net.GetNetFitness();
        float angle = bodies[0].transform.eulerAngles.z;
        if (angle > 180)
            angle = 360 - angle;
        angle = angle * Mathf.Deg2Rad;

        fit = Mathf.Pow(fit,angle);
        this.net.SetNetFitness(fit);*/

        //this.net.SetNetFitness(1f/bodies[0].velocity.magnitude);
        //float avg = bodies[0].transform.position.x + bodies[1].transform.position.x + bodies[2].transform.position.x + bodies[3].transform.position.x + bodies[4].transform.position.x;
        //avg = avg / 5f;
        /*float velo = 0;
        if (points.Count > 0)
        {
            

            for (int i = 0; i < points.Count; i++)
            {
                velo += points[i].x;

            }
            velo = velo / points.Count;
        }

        float avg = bodies[0].transform.localPosition.x * velo;
        if (bodies[0].transform.localPosition.x < 0 || avg<0)
            avg = 0;


        this.net.SetNetFitness(avg);*/

        /*float life = this.net.GetTimeLived();
        life = (life / net.GetTestTime()) * 2f;

        float fit = this.net.GetNetFitness();
        fit = Mathf.Pow(fit,life);

        this.net.SetNetFitness(fit);*/

        //this.net.SetNetFitness(Mathf.Pow((1f / Vector2.Distance(bodies[0].transform.position, pos.position)), life));

        /*float life = this.net.GetTimeLived();
        float factor = (life / net.GetTestTime()) *2f;

        float totalDistanceFit = 0;
        if (points.Count > 0)
        {
            for (int i = 1; i < points.Count; i++)
            {
                float dis = Mathf.Pow(Vector2.Distance(points[i], points[i - 1]), 2);
                totalDistanceFit += dis;
            }
            totalDistanceFit /= 50f;
        }*/


        /*float dis = Vector3.Distance(bodies[0].transform.position, newPos);
        float disFit = distanceToNewPos - dis;
        net.AddNetFitness((disFit / distanceToNewPos)*(net.GetNetFitness()));*/


    }

    public void OtherActivity(int type)
    {

        /*if (type == 0) {
            // net.AddNetFitness(1f);
            damage += 25f;
            if (damage > 100)
                damage = 100f;
        }

        if (type == 1) {
            net.SetNetFitness(net.GetNetFitness()*0.5f);
            OnFinished();
        }*/
        if (type == 0)
        {
            //net.SetNetFitness(net.GetNetFitness() * 0.5f);
            OnFinished();
        }
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
        transform.GetComponentInChildren<Renderer>().material.color = color;
        transform.GetChild(0).GetComponentInChildren<Renderer>().material.color = color;
    }
}
