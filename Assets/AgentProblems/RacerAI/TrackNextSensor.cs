using UnityEngine;
using System.Collections;

public class TrackNextSensor : MonoBehaviour
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.name.Contains("Body"))
            other.transform.parent.SendMessage("NewPos", (object)nextSensor);
    }
}
