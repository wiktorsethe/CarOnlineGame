using System;
using UnityEngine;
using Mirror;

public class CarLapCounter : NetworkBehaviour
{
    private int _passedCheckpointNumber = 0;
    private float _timeAtLastPassedCheckpoint = 0;
    private int _numberOfPassedCheckpoints = 0;

    private int _lapsCompleted = 0;
    private const int LAPS_TO_COMPLETE = 2;

    private bool _isRaceCompleted = false;
    
    int _carPosition = 0;
    
    [SerializeField] private MatchController matchController;
    
    private NetworkConnection _ownerConnection;
    public event Action<CarLapCounter> OnPassCheckpoint;

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Zapisujemy połączenie właściciela; zakładamy, że każdy samochód jest player-owned
        _ownerConnection = connectionToClient;
    }
    
    public void Awake()
    {
        matchController = FindObjectOfType<MatchController>();
    }
    
    [Command(requiresAuthority = false)]
    public void CmdSetCarPosition(int position)
    {
        _carPosition = position;
        TargetUpdatePosition(_ownerConnection, _carPosition);

    }
    
    [TargetRpc]
    private void TargetUpdatePosition(NetworkConnection target, int newPosition)
    {
        matchController.positionText.text = "Pos: " + newPosition;  
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
                    
                    if(_lapsCompleted >= LAPS_TO_COMPLETE)
                    {
                        _isRaceCompleted = true;
                        matchController.CmdDisablePlayerCars();
                        matchController.ResetCarLapCounters();
                        matchController.CmdShowWinner(GetComponent<NetworkIdentity>());
                    }
                }
                OnPassCheckpoint?.Invoke(this);
            }
        }
    }
    
    [Command]
    private void CmdIncreaseLap()
    {
        _lapsCompleted++;
        TargetUpdateLaps(connectionToClient, _lapsCompleted);
    }
    
    [TargetRpc]
    private void TargetUpdateLaps(NetworkConnection target, int newLaps)
    {
        _lapsCompleted = newLaps;
        matchController.lapCounterText.text = "Laps: " + (_lapsCompleted+1);  
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
