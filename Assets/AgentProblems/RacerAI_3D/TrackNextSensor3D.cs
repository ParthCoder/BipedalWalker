using UnityEngine;
using System.Collections;

public class TrackNextSensor3D : MonoBehaviour
{
    public Transform nextSensor;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {

        if (other.name.Contains("_"))
        {

            other.transform.SendMessage("NewPos", (object)nextSensor);
        }
    }
}
