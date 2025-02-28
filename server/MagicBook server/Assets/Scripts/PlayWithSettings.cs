using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayWithSettings : MonoBehaviour
{
    public float StepSeconds = 10f;
    public TMP_Dropdown LODDropdown;
    public TMP_Dropdown FloodTypeDropdown;

    int currentStep = 0;

    public void StartPlaying(bool play)
    {
        CancelInvoke(nameof(NextSettings));

        if(play)
            InvokeRepeating(nameof(NextSettings), 0, Mathf.Max(StepSeconds, 1f));
    }

    void NextSettings()
    {
        const int numCombinations = 5;

        const int highLOD = 1;
        const int lowLOD = 2;

        const int floodMud = 0;
        const int depthRed = 1;
        const int floodSpeed = 2;
        const int floodBlue = 3;
        const int depthHazard = 4;

        switch (currentStep)
        {
            case 0: // High LOD, Mud
                //LODDropdown.value = highLOD;
                FloodTypeDropdown.value = floodMud;
                break;
            case 1: // High LOD, Blue
                //LODDropdown.value = highLOD;
                FloodTypeDropdown.value = floodBlue;
                break;
            case 2: // High LOD, Red
                //LODDropdown.value = highLOD;
                FloodTypeDropdown.value = depthRed;
                break;
            case 3: // High LOD, hazard
                //LODDropdown.value = highLOD;
                FloodTypeDropdown.value = depthHazard;
                break;
            case 4: // Low LOD, Mud
                //LODDropdown.value = lowLOD;
                FloodTypeDropdown.value = floodSpeed;
                break;
            default:
                break;
        }

        currentStep = (currentStep + 1) % numCombinations;
    }
}
