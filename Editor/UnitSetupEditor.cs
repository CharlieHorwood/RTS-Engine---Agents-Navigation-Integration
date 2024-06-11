using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.VisualScripting;
using ProjectDawn.Navigation.Hybrid;
using RTSEngine.Movement;

[CustomEditor(typeof(UnitSetup))]
public class UnitSetupEditor : Editor
{
    
    protected UnitSetup Instance {  get; set; }
    SerializedProperty ConfigCompleted;

    SerializedProperty UnitType;

    SerializedProperty AgentsNavController;
    SerializedProperty Agent;
    SerializedProperty Shape;
    SerializedProperty NavMesh;
    SerializedProperty Avoid;
    SerializedProperty SmartStop;

    private void OnEnable()
    {
        Instance = (UnitSetup)target;

        ConfigCompleted = serializedObject.FindProperty("ConfigCompleted");

        AgentsNavController = serializedObject.FindProperty("AgentsNavController");
        UnitType = serializedObject.FindProperty("UnitType");
        Agent = serializedObject.FindProperty("Agent");
        Shape = serializedObject.FindProperty("Shape");
        NavMesh = serializedObject.FindProperty("NavMesh");
        Avoid = serializedObject.FindProperty("Avoid");
        SmartStop = serializedObject.FindProperty("SmartStop");

    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        string[] mainRow = { "Configuration", "Debug" };
        Instance.TabID = GUILayout.SelectionGrid(Instance.TabID, mainRow, 2);

        switch (Instance.TabID)
        {
            case 0:
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.PropertyField(ConfigCompleted);
                EditorGUILayout.PropertyField(UnitType);
                if(Instance.UnitType != global::UnitType.None)
                {
                    if (GUILayout.Button("Configure Unit"))
                    {
                        if (Instance.transform.TryGetComponent(out NavMeshAgent OldAgent))
                        {
                            DestroyImmediate(OldAgent);
                        }

                        if (Instance.transform.TryGetComponent(out NavMeshObstacle OldObstacle))
                        {
                            DestroyImmediate(OldObstacle);
                        }

                        Instance.AgentsNavController = Instance.AddComponent<AgentsNavController>();
                        Instance.AgentsNavController.navAgent = Instance.AddComponent<AgentAuthoring>();
                        Instance.AgentsNavController.agentShape = Instance.AddComponent<AgentCylinderShapeAuthoring>();
                        Instance.AgentsNavController.agentNavmesh = Instance.AddComponent<AgentNavMeshAuthoring>();
                        Instance.AgentsNavController.agentAvoidance = Instance.AddComponent<AgentAvoidAuthoring>();
                        Instance.AddComponent<AgentSmartStopAuthoring>();
                        switch (Instance.UnitType)
                        {
                            case global::UnitType.Infantry:
                                Instance.AddComponent<InfantryLocomotionAuthoring>();
                                break;
                            case global::UnitType.Wheeled:
                                Instance.AddComponent<WheeledLocomotionAuthoring>();
                                break;
                            case global::UnitType.Tracked:
                                Instance.AddComponent<TrackedLocomotionAuthoring>();
                                break;
                            case global::UnitType.Air:
                                Instance.AddComponent<AircraftLocomotionAuthoring>();
                                break;
                        }

                    }
                }
                
                EditorGUILayout.EndVertical();
                break;
            case 1:
                EditorGUILayout.BeginVertical("box");


                EditorGUILayout.EndVertical();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

}
