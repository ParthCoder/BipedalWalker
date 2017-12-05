using UnityEngine;
using System.Collections;

public class CollsionCheck : MonoBehaviour
{

    void OnCollisionEnter2D(Collision2D coll)
    {
        //if (coll.collider.name.Contains("G"))
        //transform.parent.SendMessage("OtherActivity", (object)0);
        //else
        //transform.parent.SendMessage("OtherActivity", (object)1);

        /*if (coll.collider.name.Contains("Food"))
        {
            transform.parent.SendMessage("OtherActivity", (object)0);
            Destroy(coll.gameObject);
        }
        else if (coll.collider.name.Contains("Ground")){
            transform.parent.SendMessage("OtherActivity", (object)1);
        }
        else  {
            transform.parent.SendMessage("OtherActivity", (object)2);
        }*/

        //Debug.Log("here!");
        if (coll.collider.name.Contains("G"))
            transform.parent.SendMessage("OtherActivity", (object)0);

    }

    void OnCollisionExit2D(Collision2D coll)
    {
        //if (coll.collider.name.Contains("B"))
        //transform.parent.SendMessage("OtherActivity", (object)2);
    }

    void OnCollisionStay2D(Collision2D coll)
    {
        //if (coll.collider.name.Contains("B"))
        //transform.parent.SendMessage("OtherActivity", (object)3);
        //transform.parent.SendMessage("OtherActivity", (object)0);
    }



}
