using System;
using UnityEngine;
using HoloToolkit.Unity;
using UnityEngine.Events;

public class SpatialUnderstandingState : Singleton<SpatialUnderstandingState>
{
    //Public Variables - For Editor
    public float MinAreaForStats = 2.0f; // both floor and wall surfaces
    public float MinAreaForComplete = 4.0f; // for floor
    public float MinHorizAreaForComplete = 1.0f; // for horizontal surfaces not only walls
    public float MinWallAreaForComplete = 0.0f; // for walls only
    //Text to speech script
    public TextToSpeech textToSpeech;
    //Debug displays
    public TextMesh DebugDisplay;
    public string SpaceQueryDescription;
    //Private Variables
    private bool _triggered;
    private bool speechTriggered;
    private bool scanReady = false;
    private UnityAction TapListener;

    private bool DoesScanMeetMinBarForCompletion
    {
        get
        {
            if ((SpatialUnderstanding.Instance.ScanState != SpatialUnderstanding.ScanStates.Scanning) ||
                (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding))
                return false;

            IntPtr statsPtr = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceStatsPtr();
            if (SpatialUnderstandingDll.Imports.QueryPlayspaceStats(statsPtr) == 0)
                return false;

            SpatialUnderstandingDll.Imports.PlayspaceStats stats = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticPlayspaceStats();

            // Check our preset requirements
            if ((stats.TotalSurfaceArea > MinAreaForComplete) ||
                (stats.HorizSurfaceArea > MinHorizAreaForComplete) ||
                (stats.WallSurfaceArea > MinWallAreaForComplete))
                return true;
            else
                return false;
        }
    }
    //OK
    private string PrimaryText
    {
        get
        {
            // Display the space and object query results (has priority)
            if (!string.IsNullOrEmpty(SpaceQueryDescription))
                return SpaceQueryDescription;

            // Scan state
            if (SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
            {
                switch (SpatialUnderstanding.Instance.ScanState)
                {
                    case SpatialUnderstanding.ScanStates.Scanning:
                        if (DoesScanMeetMinBarForCompletion)
                            return "Space scanned, air tap to finalize your playspace";
                        return "Walk around and scan in your playspace";
                    case SpatialUnderstanding.ScanStates.Finishing:
                        return "Finalizing scan";
                    case SpatialUnderstanding.ScanStates.Done:
                        return "";
                    default:
                        return "";
                }
            }
            else
                return string.Empty;
        }
    }

    public Color PrimaryColor
    {
        get
        {
            scanReady = DoesScanMeetMinBarForCompletion;
            if (SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Scanning && scanReady)
                return Color.yellow;
            else
                return Color.white;
        }
    }

    private void Update_DebugDisplay()
    {
        // Update display text
        DebugDisplay.text = PrimaryText;
        DebugDisplay.color = PrimaryColor;
    }

    private void Start()
    {
        TapListener = new UnityAction(Tap_Triggered);
        EventManager.StartListening("click", TapListener);
    }

    private void Tap_Triggered()
    {
        if (scanReady &&
            (SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Scanning) &&
             !SpatialUnderstanding.Instance.ScanStatsReportStillWorking)
            SpatialUnderstanding.Instance.RequestFinishScan();
    }

    // Update is called once per frame
    private void Update()
    {
        // Update Display if exist
        Update_DebugDisplay();

        if (SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Scanning &&
            DoesScanMeetMinBarForCompletion && !speechTriggered)
        {
            if (!textToSpeech.IsSpeaking())
            {
                speechTriggered = true;
                textToSpeech.StartSpeaking("Scan Finished. Click once more to procced");
            }
        }
        
        if (!_triggered && SpatialUnderstanding.Instance.ScanState == SpatialUnderstanding.ScanStates.Done)
        {
            _triggered = true;
            //Stop listening for clicks
            EventManager.StopListening("click", TapListener);
            //Trigger event for scan_done
            EventManager.TriggerEvent("scan_done");
        }
    }
}