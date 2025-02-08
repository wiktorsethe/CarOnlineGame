using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PositionHandler : MonoBehaviour
{
    public List<CarLapCounter> carLapCounters = new ();

    private void Start()
    {
        CarLapCounter[] carLapCounterArray = FindObjectsOfType<CarLapCounter>();
        
        // Convert the array to a list
        carLapCounters = carLapCounterArray.ToList();
        
        // Subscribe to the OnPassCheckpoint event for each car
        foreach (var carLapCounter in carLapCounters)
        {
            carLapCounter.OnPassCheckpoint += OnPassCheckpoint;
        }
    }

    private void OnPassCheckpoint(CarLapCounter carLapCounter)
    {
        // Sort the cars based on the number of checkpoints passed (descending order)
        // If two cars have passed the same number of checkpoints, sort by the time at the last checkpoint (ascending order)
        carLapCounters = carLapCounters.OrderByDescending(s => s.GetNumberOfCheckpointsPassed())
            .ThenBy(s => s.GetTimeAtLastPassedCheckpoint())
            .ToList();
        
        // Determine the position of the car in the race
        int playerPlace = carLapCounters.IndexOf(carLapCounter) + 1;
        
        // Update the player's place in the race
        carLapCounter.CmdSetPlayerPlaceInRace(playerPlace);
        
        Debug.Log(carLapCounter.GetNumberOfCheckpointsPassed() + " checkpoints passed");
    }
}