using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PositionHandler : MonoBehaviour
{
    public List<CarLapCounter> carLapCounters = new List<CarLapCounter>();

    private void Start()
    {
        CarLapCounter[] carLapCounterArray = FindObjectsOfType<CarLapCounter>();
        carLapCounters = carLapCounterArray.ToList<CarLapCounter>();
        foreach (var carLapCounter in carLapCounters)
        {
            carLapCounter.OnPassCheckpoint += OnPassCheckpoint;
        }
    }

    private void OnPassCheckpoint(CarLapCounter carLapCounter)
    {
        carLapCounters = carLapCounters.OrderByDescending(s => s.GetNumberOfCheckpointsPassed()).ThenBy(s => s.GetTimeAtLastPassedCheckpoint()).ToList();
        
        int carPosition = carLapCounters.IndexOf(carLapCounter) + 1;
        
        carLapCounter.CmdSetCarPosition(carPosition);
        
        Debug.Log(carLapCounter.GetNumberOfCheckpointsPassed() + " checkpoints passed");
    }
}
