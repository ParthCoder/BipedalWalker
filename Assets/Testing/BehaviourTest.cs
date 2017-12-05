using UnityEngine;
using System.Collections;

public class BehaviourTest : MonoBehaviour {
    public GameObject linePrefab;
    private GameObject lineObjects;
    private LineRenderer lines;
    private float sightHit;
    // Use this for initialization
    void Start () {
        lineObjects = (GameObject)Instantiate(linePrefab);
        lineObjects.transform.parent = transform;
        lines = lineObjects.GetComponent<LineRenderer>();
        lines.SetWidth(0.1f, 0.1f);
        lines.material = new Material(Shader.Find("Particles/Additive"));
        lines.SetColors(Color.red, Color.red);
    }
	
	// Update is called once per frame
	void Update () {

        float angle = 0;
        float distance = 3f;
        float outDistance = 0.35f;

        Vector3 direction = new Vector3();
        Vector3 relativePosition = new Vector3();
        RaycastHit2D rayHit = new RaycastHit2D();

        Color lineColor = new Color(1f, 0f, 0f);

       
        direction = Quaternion.AngleAxis(angle, Vector3.forward) * transform.up;
        relativePosition = transform.position + (outDistance * direction);
        rayHit = Physics2D.Raycast(relativePosition, direction, distance);
        lines.SetPosition(0, relativePosition);
        sightHit = -1f;

        if (rayHit.collider != null)
        {
            sightHit = Vector2.Distance(rayHit.point, transform.position) / (distance);
            lines.SetPosition(1, rayHit.point);
        }
        else
        {
            lines.SetPosition(1, relativePosition);
        }

        lines.SetColors(lineColor, lineColor);

        Debug.Log(sightHit);
    }
}
