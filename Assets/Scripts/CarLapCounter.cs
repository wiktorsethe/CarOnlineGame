using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using DG.Tweening;

public class CarLapCounter : NetworkBehaviour
{
    #region Variables
    private const int LAPS_TO_COMPLETE = 2;
    private int _passedCheckpointNumber = 0;
    private int _numberOfPassedCheckpoints = 0;
    private int _lapsCompleted = 0;
    private int _playerPlaceInRace = 0;

    private float _timeAtLastPassedCheckpoint = 0;

    private bool _isRaceCompleted = false;
    private bool _isKeyActive = false;
    #endregion
    
    #region Properties & Events
    [SerializeField] private MatchController matchController;
    public event Action<CarLapCounter> OnPassCheckpoint;

    private NetworkConnection _ownerConnection;
    private List<Checkpoint> _checkpoints = new ();
    private Tween _warningTween;
    #endregion
    
    #region Networking
    public override void OnStartServer()
    {
        base.OnStartServer();
        
        _ownerConnection = connectionToClient;
    }
    
    [Command]
    public void CmdSetPlayerPlaceInRace(int position)
    {
        _playerPlaceInRace = position;
        TargetSetPlayerPlaceInRace(_ownerConnection, _playerPlaceInRace);
    }
    
    [TargetRpc]
    private void TargetSetPlayerPlaceInRace(NetworkConnection target, int newPosition)
    {
        matchController.positionText.text = "Pos: " + newPosition;
    }
    
    [Command]
    private void CmdUpdateLapsText()
    {
        _lapsCompleted++;
        TargetUpdateLapsText(connectionToClient, _lapsCompleted);
    }
    
    [TargetRpc]
    private void TargetUpdateLapsText(NetworkConnection target, int newLaps)
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
        // Display warning message for wrong checkpoint
        matchController.infoText.color = Color.yellow;
        matchController.infoText.text = "Wrong checkpoint press R to reset!";
        _warningTween = matchController.infoText.DOFade(0, 1f) 
            .SetLoops(-1, LoopType.Yoyo) 
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
        // Stop the warning text animation and hide the message
        _warningTween.Kill(); 
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
        // Find the checkpoint with the specified number and reset car position to it
        foreach (var checkpoint in _checkpoints)
        {
            if (checkpoint.checkPointNumber == checkPointNumber)
            {
                transform.position = checkpoint.restartTransform.position;
                transform.rotation = checkpoint.restartTransform.rotation;
            }
        }
    }
    
    #endregion

    #region Unity Callbacks
    public void Awake()
    {
        // Find the MatchController in the scene
        matchController = FindObjectOfType<MatchController>();

        // Find all objects tagged as "Checkpoint" and add them to the checkpoint list
        foreach (var checkpoint in GameObject.FindGameObjectsWithTag("Checkpoint"))
        {
            _checkpoints.Add(checkpoint.GetComponent<Checkpoint>());
        }
    }

    private void Update()
    {
        // Allow the player to reset the car position by pressing 'R' if the key is active
        if (isLocalPlayer && Input.GetKeyDown(KeyCode.R) && _isKeyActive)
        {
            _isKeyActive = false;
            CmdHideWarningAlert();
            CmdResetCarPosition(_passedCheckpointNumber + 1);
        }
    }
    #endregion
    
    #region Methods
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
            // Ensure checkpoints are passed in the correct order
            if (_passedCheckpointNumber + 1 == checkpoint.checkPointNumber)
            {
                _passedCheckpointNumber = checkpoint.checkPointNumber;
                _numberOfPassedCheckpoints++;
                _timeAtLastPassedCheckpoint = Time.time;

                // Check if the checkpoint is the finish line
                if (checkpoint.isFinishLine)
                {
                    _passedCheckpointNumber = 0;
                    CmdUpdateLapsText();
                    
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
                // If the player skipped a checkpoint, display a warning
                CmdWrongCheckpointAlert();
                _isKeyActive = true;
            }
        }
    }
    
    public void Reset()
    {
        // Reset race progress and player position
        _passedCheckpointNumber = 0;
        _timeAtLastPassedCheckpoint = 0;
        _numberOfPassedCheckpoints = 0;
        _lapsCompleted = 0;
        _isRaceCompleted = false;
        _playerPlaceInRace = 0;
    }
    #endregion
}
