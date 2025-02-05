using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CarLapCounter : NetworkBehaviour
{
    private int _passedCheckpointNumber = 0;
    private float _timeAtLastPassedCheckpoint = 0;
    private int _numberOfPassedCheckpoints = 0;

    [SyncVar]
    private int _lapsCompleted = 0;
    private const int LAPS_TO_COMPLETE = 2;

    private bool _isRaceCompleted = false;
    
    int _carPosition = 0;
    
    [SerializeField] private MatchController matchController;
    
    public event Action<CarLapCounter> OnPassCheckpoint;
    public event Action<int> OnLapsUpdated; // Event do aktualizacji UI

    public void Awake()
    {
        matchController = FindObjectOfType<MatchController>();
    }
    
    public void SetCarPosition(int position)
    {
        _carPosition = position;
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
                    CmdIncreaseLap();
                }
                OnPassCheckpoint?.Invoke(this);
            }
        }
    }

    [Command]
    private void CmdIncreaseLap()
    {
        _lapsCompleted++;
        RpcUpdateLaps(_lapsCompleted);

        if(_lapsCompleted >= LAPS_TO_COMPLETE)
        {
            _isRaceCompleted = true;
            matchController.CmdDisablePlayerCars();
            matchController.ResetCarLapCounters();
            matchController.CmdShowWinner(GetComponent<NetworkIdentity>());
        }
    }
    
    [ClientRpc]
    private void RpcUpdateLaps(int newLaps)
    {
        _lapsCompleted = newLaps;
        matchController.lapCounterText.text = "Laps: " + _lapsCompleted.ToString();
        OnLapsUpdated?.Invoke(_lapsCompleted);
    }

    public void Reset()
    {
        _passedCheckpointNumber = 0;
        _timeAtLastPassedCheckpoint = 0;
        _numberOfPassedCheckpoints = 0;
        _lapsCompleted = 0;
        _isRaceCompleted = false;
        _carPosition = 0;
    }
}
