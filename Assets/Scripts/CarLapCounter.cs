using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarLapCounter : MonoBehaviour
{
    private int _passedCheckpointNumber = 0;
    private float _timeAtLastPassedCheckpoint = 0;
    private int _numberOfPassedCheckpoints = 0;

    private int _lapsCompleted = 0;
    private const int LAPS_TO_COMPLETE = 2;

    private bool _isRaceCompleted = false;
    
    int carPosition = 0;
    
    public event Action<CarLapCounter> OnPassCheckpoint;

    public void SetCarPosition(int position)
    {
        carPosition = position;
    }

    public int GetNumberOfCheckpointsPassed()
    {
        return _numberOfPassedCheckpoints;
    }

    public float GetTimeAtLastPassedCheckpoint()
    {
        return _timeAtLastPassedCheckpoint;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Checkpoint"))
        {
            if(_isRaceCompleted) return;
            
            Checkpoint checkpoint = other.GetComponent<Checkpoint>();
            if (_passedCheckpointNumber + 1 == checkpoint.checkPointNumber)
            {
                _passedCheckpointNumber = checkpoint.checkPointNumber;
                _numberOfPassedCheckpoints++;
                
                _timeAtLastPassedCheckpoint = Time.time;

                if (checkpoint.isFinishLine)
                {
                    _passedCheckpointNumber = 0;
                    _lapsCompleted++;
                    
                    if(_lapsCompleted >= LAPS_TO_COMPLETE) _isRaceCompleted = true;
                }
                OnPassCheckpoint?.Invoke(this);
            }
        }
    }
}
