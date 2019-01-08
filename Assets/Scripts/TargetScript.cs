using UnityEngine;

public class TargetScript : MonoBehaviour
{
    private bool triggered = false; //Collision Triggered
    private bool hit;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnCollisionEnter(Collision collision)
    {
        //Get collision object
        GameObject colObject = collision.gameObject;
        //gameObject.transform.parent = null;
        if (!triggered)
        {
            triggered = true;
            EventManager.TriggerEvent("floor_collision");
            //gameObject.transform.parent = colObject.transform;
            if (hit)
                UtilitiesScript.Instance.ChangeColorOutline(gameObject, Color.green);
            else
                UtilitiesScript.Instance.ChangeColorOutline(gameObject, Color.red);
        }
    }

    public void targetHit()
    {
        hit = true;
    }
}
