using UnityEngine;
using UnityEngine.XR.WSA.Input;

public class ClickerController : MonoBehaviour
{
    private float lastClickTime = .0f;
    private bool clickTriggered;
    private bool doubleClickTriggered;
    public bool clickerEnabled;


    private void Start()
    {
        if(clickerEnabled)
            InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
    }

    private void Update()
    {
        if(clickerEnabled)
            CheckForClicker();
    }
    private void OnDestroy()
    {
        InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
    }


    private void CheckForClicker()
    {
        if (doubleClickTriggered == true)
        {
            //Reset variable
            doubleClickTriggered = false;
            EventManager.TriggerEvent("double_click");
        }
        if (clickTriggered == true)
        {
            //Reset variable
            clickTriggered = false;
            EventManager.TriggerEvent("click");
        }
    }

    private void InteractionManager_InteractionSourcePressed(InteractionSourcePressedEventArgs args)
    {
        if (args.state.source.kind == InteractionSourceKind.Controller)
        {
            if (!clickTriggered) //First click
            {
                clickTriggered = true;
                doubleClickTriggered = false;
                lastClickTime = Time.time;
            }
            else //Second click
            {
                if (Time.time - lastClickTime < .5f)
                {
                    doubleClickTriggered = true;
                    clickTriggered = false;
                }
            }
        }
    }

    public void enableClicker()
    {
        clickerEnabled = false;
        InteractionManager.InteractionSourcePressed += InteractionManager_InteractionSourcePressed;
    }

    public void disableClicker()
    {
        clickerEnabled = false;
        InteractionManager.InteractionSourcePressed -= InteractionManager_InteractionSourcePressed;
    }

}
