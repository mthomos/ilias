using System.Collections.Generic;
using HoloToolkit.Unity;
using UnityEngine;
using System;

public class ObjectPlacer : MonoBehaviour
{
    public SpatialUnderstandingCustomMesh SpatialUnderstandingMesh;
    public Material OccludedMaterial;
    // Private variables
    private Queue<PlacementResult> results = new Queue<PlacementResult>();

    void Start()
    {

    }

    void Update()
    {
        ProcessPlacementResults();
    }

    public void HideGridEnableOcclulsion()
    {
        SpatialUnderstandingMesh.MeshMaterial = OccludedMaterial;
    }


    public void CreateScene()
    {
         if (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
            return;

        SpatialUnderstandingDllObjectPlacement.Solver_Init();

        List<PlacementQuery> queries = new List<PlacementQuery>();
        queries.AddRange(AddTarget());
        GetLocationsFromSolver(queries);
    }

    public void CreateScene(int targets)
    {
        if (!SpatialUnderstanding.Instance.AllowSpatialUnderstanding)
            return;

        SpatialUnderstandingDllObjectPlacement.Solver_Init();

        List<PlacementQuery> queries = new List<PlacementQuery>();
        queries.AddRange(AddTargets(targets));
        GetLocationsFromSolver(queries);
    }

    public List<PlacementQuery> AddTarget()
    {
        return CreateLocationQueriesForSolver(1, ObjectCollectionManager.Instance.TargetSize, ObjectType.Target);
    }

    public List<PlacementQuery> AddTargets(int targets)
    {
        return CreateLocationQueriesForSolver(targets, ObjectCollectionManager.Instance.TargetSize, ObjectType.Target);
    }


    private void ProcessPlacementResults()
    {
        if (results.Count > 0)
        {
            var toPlace = results.Dequeue();
            Quaternion rotation = Quaternion.LookRotation(toPlace.Normal, Vector3.up);

            switch (toPlace.ObjType)
            {
                case ObjectType.Target:
                    ObjectCollectionManager.Instance.CreateTarget(toPlace.Position, rotation);
                    break;
            }
        }
    }

    private void GetLocationsFromSolver(List<PlacementQuery> placementQueries)
    {
        for (int i = 0; i < placementQueries.Count; ++i)
        {
            var result = PlaceObject(placementQueries[i].ObjType.ToString() + i,
                                        placementQueries[i].PlacementDefinition,
                                        placementQueries[i].Dimensions,
                                        placementQueries[i].ObjType,
                                        placementQueries[i].PlacementRules,
                                        placementQueries[i].PlacementConstraints);
            if (result != null) results.Enqueue(result);
        }
    }

    private PlacementResult PlaceObject(string placementName,
        SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition placementDefinition,
        Vector3 boxFullDims,
        ObjectType objType,
        List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> placementRules = null,
        List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint> placementConstraints = null)
    {

        // New query
        if (SpatialUnderstandingDllObjectPlacement.Solver_PlaceObject(
                placementName,
                SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementDefinition),
                (placementRules != null) ? placementRules.Count : 0,
                ((placementRules != null) && (placementRules.Count > 0)) ? SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementRules.ToArray()) : IntPtr.Zero,
                (placementConstraints != null) ? placementConstraints.Count : 0,
                ((placementConstraints != null) && (placementConstraints.Count > 0)) ? SpatialUnderstanding.Instance.UnderstandingDLL.PinObject(placementConstraints.ToArray()) : IntPtr.Zero,
                SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResultPtr()) > 0)
        {
            SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult placementResult = SpatialUnderstanding.Instance.UnderstandingDLL.GetStaticObjectPlacementResult();

            return new PlacementResult(placementResult.Clone() as SpatialUnderstandingDllObjectPlacement.ObjectPlacementResult, boxFullDims, objType);
        }
        return null;
    }

    private List<PlacementQuery> CreateLocationQueriesForSolver(int prefabCount, Vector3 fullDims, ObjectType objType)
    {
        List<PlacementQuery> placementQueries = new List<PlacementQuery>();
        Vector3 halfDims = fullDims * .5f;

        for (int i = 0; i < prefabCount; i++)
        {
            var placementRules = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule> { };
            var placementConstraints = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint>();
            var placementDefinition = SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_InMidAir(halfDims);

            if (objType == ObjectType.Target)
            {
                //EDIT THIS !!!!
                placementRules = new List<SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule>
                {
                SpatialUnderstandingDllObjectPlacement.ObjectPlacementRule.Create_AwayFromOtherObjects(0.1f)
                };
                //placementConstraints.Add(SpatialUnderstandingDllObjectPlacement.ObjectPlacementConstraint.Create_NearCenter());
                placementDefinition = SpatialUnderstandingDllObjectPlacement.ObjectPlacementDefinition.Create_OnFloor(halfDims);
            }

            placementQueries.Add(
                new PlacementQuery(placementDefinition,
                    fullDims,
                    objType,
                    placementRules,
                    placementConstraints
                ));
        }
        return placementQueries;
    }

}