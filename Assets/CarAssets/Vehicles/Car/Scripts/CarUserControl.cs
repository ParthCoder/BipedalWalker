using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using Assets.CarAssets.Vehicles.Car.Scripts;

namespace Assets.CarAssets.Vehicles.Car.Scripts
{
    public class CarUserControl : MonoBehaviour, IAgentTester
    {
        private CarController m_Car; // the car controller we want to use

        private NEATGeneticControllerV2 controller;
        private Rigidbody rBody;

        bool finished = false;
        private NEATNet net;
        private bool isActive = false;
        private bool isLoaded = false;
        private const string ACTION_ON_FINISHED = "OnFinished";

        public delegate void TestFinishedEventHandler(object source, EventArgs args);

        public event TestFinishedEventHandler TestFinished;

        public GameObject linePrefab;
        int numberOfSensors = 15;

        private bool drawLineRen = true;

        private GameObject[] lineObjects;
        private LineRenderer[] lines;
        private float[] sightHit;
        Vector3 posCheck = Vector3.zero;


        void Start()
        {
            rBody = GetComponent<Rigidbody>();
            lineObjects = new GameObject[numberOfSensors];
            lines = new LineRenderer[numberOfSensors];
            sightHit = new float[numberOfSensors * 2];
            if (drawLineRen == true)
                for (int i = 0; i < numberOfSensors; i++)
                {
                    lineObjects[i] = (GameObject) Instantiate(linePrefab);
                    lineObjects[i].transform.parent = transform;
                    lines[i] = lineObjects[i].GetComponent<LineRenderer>();
                    lines[i].startWidth = 0.1f;
                    lines[i].endWidth = 0.1f;
                    lines[i].material = new Material(Shader.Find("Unlit/Texture"));
                    lines[i].material.color = Color.red;
                    lines[i].startColor = Color.red;
                    lines[i].endColor = Color.red;
                }
            Invoke("TakePoint", 2f);


        }

        float avgAngle = 0;

        void TakePoint()
        {

            if (posCheck == Vector3.zero)
            {
                posCheck = transform.position + new Vector3(0.00001f, transform.position.y, 0.00001f);
            }
            else if (Vector3.Distance(posCheck, transform.position) <= 2)
            {
                OnFinished();
            }
            else
            {
                posCheck = transform.position;
            }
            Invoke("TakePoint", 1f);
        }


        void FixedUpdate()
        {
            if (isActive)
            {
                UpdateNet();
            }

            // pass the input to the car!
            float h = CrossPlatformInputManager.GetAxis("Horizontal");
            float v = CrossPlatformInputManager.GetAxis("Vertical");

            float handbrake = CrossPlatformInputManager.GetAxis("Jump");
            m_Car.Move(h, v, v, handbrake);

        }

        Vector3 newPos = Vector3.zero;
        float distanceToNewPos = 0f;
        int posNum = 1;

        public void NewPos(object newTransform)
        {
            Transform newTransformTemp = (Transform) newTransform;
            int tempPosNum = int.Parse(newTransformTemp.name);

            int checkPos = tempPosNum;
            if (checkPos == 1)
                checkPos = 14;
            else
                checkPos = checkPos - 1;

            if (checkPos == posNum)
            {
                posNum = tempPosNum;
                Vector3 pos = newTransformTemp.position;
                pos.z = 0f;
                distanceToNewPos = Vector3.Distance(transform.position, pos);
                newPos = pos;

                net.AddNetFitness((net.GetNetFitness()) + 1f);

            }
            else if (!(tempPosNum == posNum))
            {

                OnFinished();
            }
        }

        public void UpdateNet()
        {


            float angle = -90;
            float angleAdd = 180f / (numberOfSensors - 1);


            float outDistance = 0f;
            int ignoreLayers = ~((1 << 8) | (1 << 9));


            Vector3[] direction = new Vector3[numberOfSensors];
            Vector3[] relativePosition = new Vector3[numberOfSensors];

            float redness = 0f;
            Color lineColor = new Color(1f, redness, redness);

            for (int i = 0; i < numberOfSensors; i++)
            {
                float distance = 20f;
                /*if (numberOfSensors / 2 == i)
                    distance = 20f;*/
                direction[i] = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                relativePosition[i] = (transform.position + new Vector3(0, 1f, 0)) + (outDistance * direction[i]);
                RaycastHit[] rayHit = Physics.RaycastAll(relativePosition[i], direction[i], distance, ignoreLayers);


                sightHit[(i * 2)] = -1f;
                sightHit[(i * 2) + 1] = 0f;
                if (rayHit != null && rayHit.Length > 0 && rayHit[0].collider != null
                ) //&& rayHit[i].collider.isTrigger == false)
                {
                    sightHit[(i * 2)] = Vector3.Distance(rayHit[0].point, transform.position) / (distance);
                    //sightHit[i] = Vector3.Distance(rayHit[0].point, transform.position);
                    sightHit[(i * 2) + 1] = Mathf.Acos(Vector3.Dot(direction[i], rayHit[0].normal));
                    if (drawLineRen == true)
                        lines[i].SetPosition(1, rayHit[0].point);
                }
                else
                {
                    if (drawLineRen == true)
                        lines[i].SetPosition(1, relativePosition[i]);
                }


                if (drawLineRen == true)
                {
                    lines[i].SetPosition(0, relativePosition[i]);
                    lines[i].endColor = lineColor;
                    lines[i].endColor = lineColor;
                }

                angle += angleAdd;
            }


            float[] output = net.FireNet(sightHit);
            if (this.name == "t")
                Debug.Log(sightHit[0] + " " + sightHit[1]);
            m_Car.Move(output[0], output[1], output[1], output[2]);
        }



        public void CalculateFitnessOnFinish()
        {
            /*float fit = (transform.position.z * 3f);
            if (fit <= 0)
                fit = UnityEngine.Random.Range(0f, 0.001f);
            net.SetNetFitness(fit);*/
        }

        public void CalculateFitnessOnUpdate()
        {

        }

        public bool FailCheck()
        {
            return false;
        }

        void OnCollisionEnter(Collision collision)
        {
            CallbackActivity(0);
        }

        private void Awake()
        {
            m_Car = GetComponent<CarController>();
        }

        public void Activate(NEATNet net)
        {
            this.net = net;
            Invoke(ACTION_ON_FINISHED, net.GetTestTime());
            isActive = true;
        }

        public void CallbackActivity(int type)
        {
            if (type == 0)
            {
                OnFinished();
            }
        }

        public void SubscriveToEvent(NEATGeneticControllerV2 controller)
        {
            this.controller = controller;
            TestFinished += controller.OnFinished; //subscrive to an event notification
        }

        //action based on neural net faling the test or test ending
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


        public NEATNet GetNet()
        {
            return net;
        }

        public void SetColor(Color color)
        {
            Renderer[] children = transform.Find("SkyCar").GetComponentsInChildren<Renderer>();
            for (int i = 0; i < children.Length; i++)
                children[i].material.color = color;
        }

    }

}

