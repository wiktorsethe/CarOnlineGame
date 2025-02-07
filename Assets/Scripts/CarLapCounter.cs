using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

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

    private List<Checkpoint> _checkpoints = new List<Checkpoint>();
    private Tween _warningTween;
    private int _lastCheckpointNumber = 0;
    public override void OnStartServer()
    {
        base.OnStartServer();
        // Zapisujemy połączenie właściciela; zakładamy, że każdy samochód jest player-owned
        _ownerConnection = connectionToClient;
    }
    
    public void Awake()
    {
        matchController = FindObjectOfType<MatchController>();

        foreach (var checkpoint in GameObject.FindGameObjectsWithTag("Checkpoint"))
        {
            _checkpoints.Add(checkpoint.GetComponent<Checkpoint>());
        }
    }

    private void Update()
    {
        if (isLocalPlayer && Input.GetKeyDown(KeyCode.R))
        {
            CmdHideWarningAlert();
            CmdResetCarPosition(_passedCheckpointNumber + 1);
        }
    }

    [Command]
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
            else if(_passedCheckpointNumber + 1 < checkpoint.checkPointNumber)
            {
                CmdWrongCheckpointAlert();
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
        matchController.lapCounterText.text = "Laps: " + (_lapsCompleted + 1);  
    }

    [Command]
    private void CmdWrongCheckpointAlert()
    {
        TargetWrongCheckpointAlert(_ownerConnection);
    }

    [TargetRpc]
    private void TargetWrongCheckpointAlert(NetworkConnection target)
    {
        matchController.infoText.color = Color.yellow;
        matchController.infoText.text = "Wrong checkpoint press R to reset!";
        _warningTween = matchController.infoText.DOFade(0, 1f) // Znika w 0.5 sekundy
            .SetLoops(-1, LoopType.Yoyo) // Powtarza w nieskończoność (znika i pojawia się)
            .SetEase(Ease.Linear);
        
    }

    [Command]
    private void CmdHideWarningAlert()
    {
        TargetHideWarningAlert(_ownerConnection);
    }
    [TargetRpc]
    private void TargetHideWarningAlert(NetworkConnection target)
    {
        _warningTween.Kill(); // Zatrzymuje miganie
        matchController.infoText.DOFade(0, 0.2f).OnComplete(() => matchController.infoText.text = "");
    }

    [Command]
    private void CmdResetCarPosition(int checkPointNumber)
    {
        RpcResetCarPosition(checkPointNumber);
    }

    [ClientRpc]
    private void RpcResetCarPosition(int checkPointNumber)
    {
        foreach (var checkpoint in _checkpoints)
        {
            if (checkpoint.checkPointNumber == checkPointNumber)
            {
                transform.position = checkpoint.restartPosition.position;
            }
        }
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
