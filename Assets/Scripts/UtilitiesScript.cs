using HoloToolkit.Unity;
using System.Collections.Generic;
using UnityEngine;

public class UtilitiesScript : Singleton<UtilitiesScript>
{
    public void EnableOutline(GameObject focusedObject, object color)
    {
        if (focusedObject != null)
        {
            var outline = focusedObject.GetComponent<Outline>();
            if (color == null)
                color = Color.red;
            if (outline == null)
            {
                //Create Outline
                outline = gameObject.AddComponent<Outline>();
                if (outline != null)
                {
                    outline.OutlineMode = Outline.Mode.OutlineAll;
                    outline.OutlineWidth = 5f;
                    outline.OutlineColor = (Color)color;
                    outline.enabled = true;
                }
            }
            else
            {
                outline.OutlineColor = (Color)color;
                outline.enabled = true;
            }
        }
    }

    public void EnableGravity(GameObject obj)
    {
        if (obj.GetComponent<Rigidbody>() == null)
        {
            obj.AddComponent<Rigidbody>();
            obj.GetComponent<Rigidbody>().useGravity = true;
        }
        else
        {
            obj.GetComponent<Rigidbody>().useGravity = true;
        }
    }

    public float getDistanceObjects(Transform obj1, Transform obj2)
    {
        return Vector3.Magnitude(obj1.position - obj2.position);
    }

    public bool isRightObject(Vector3 pos)
    {
        Vector3 heading = pos - Camera.main.transform.position;
        Vector3 perp = Vector3.Cross(Camera.main.transform.forward, heading);
        float dot = Vector3.Dot(perp, Camera.main.transform.up);
        if (dot >= 0)
            return true;
        else
            return false;
    }

    public void ChangeColorOutline(GameObject focusedObject, Color color)
    {
        if (focusedObject != null)
        {
            var outline = focusedObject.GetComponent<Outline>();
            if (outline != null)
            {
                if (outline.enabled == false) outline.enabled = true;
                outline.OutlineColor = color;
            }
        }
    }

    public void DisableOutline(GameObject focusedObject)
    {
        if (focusedObject != null)
        {
            Outline outline = focusedObject.GetComponent<Outline>();
            if (outline != null)
                outline.enabled = false;
        }
    }

    public void ChangeObjectColor(GameObject obj, Color color)
    {
        obj.GetComponent<Renderer>().material.color = color;
    }

    public void enableObject(GameObject obj)
    {
        if (obj != null)
            obj.SetActive(true);
    }

    public void disableObject(GameObject obj)
    {
        if (obj != null)
            obj.SetActive(false);
    }
}
