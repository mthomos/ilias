using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public int targets;
    public ObjectPlacer placer;
    public ClickerController clickController;
    public GazeCursor gazeCursor;
    public TextToSpeech textToSpeech;
    private bool trainingStarted;
    private GameObject focusedObject;
    private GameObject currentTarget;

    // Start is called before the first frame update
    void Start()
    {
        textToSpeech.StartSpeaking("Hello World");
        EventManager.StartListening("scan_done", prepareScene);
        if (targets == 0)
            targets = 10;
    }

    // Update is called once per frame
    void Update()
    {
        if (trainingStarted)
        {
            //Refresh focused object
            focusedObject = gazeCursor.getFocusedObject();
        }
    }

    private void prepareScene()
    {
        // Find floor position for targets
        placer.HideGridEnableOcclulsion();
        placer.CreateScene(targets);
        //Enable Clicker listeners
        EventManager.StartListening("click", clickReceived);
        //EventManager.StartListening("double_click", double_clickReceived);
        EventManager.StartListening("floor_collision", targetCollided);
        textToSpeech.StartSpeaking("Click once more to start training");
        trainingStarted = true;
        //
        appearNextTarget();
        //
    }

    private void clickReceived()
    {
        if (focusedObject != null)
        {
            // This is sucess, better not destroy item just make it green else red
            focusedObject.GetComponent<TargetScript>().targetHit();
            UtilitiesScript.Instance.ChangeColorOutline(focusedObject, Color.green);
        }
    }

    private void targetCollided()
    {
        // Here setup the scene  for next target, do it as you wish
        // I will just appear the next target exactly the next target will hit the floor
        appearNextTarget();
    }

    private void appearNextTarget()
    {
        // Choose your direction and power of force
        Vector3 forceDirection = new Vector3(0, 1, 0);
        float forcePower = 2.0f;
        currentTarget = ObjectCollectionManager.Instance.getNextTarget();
        currentTarget.SetActive(true);
        currentTarget.GetComponent<Rigidbody>().AddForce(forceDirection * forcePower);
    }
}
