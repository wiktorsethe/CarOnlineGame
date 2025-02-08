using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CarLayerHandler : NetworkBehaviour
{
    #region Variables
    // Reference to rendering layers
    public string sortingLayerAboveOverpass = "RacetrackOverpass";
    public string sortingLayerBelowOverpass = "Default";
    #endregion
    
    #region Properties
    public SpriteRenderer carOutline;
    private Collider2D _carCollider;
    private List<Collider2D> _overpassColliderList = new List<Collider2D>();
    private List<Collider2D> _underpassColliderList = new List<Collider2D>();
    #endregion
    
    #region Network Variables
    [SyncVar(hook = nameof(OnDrivingOnOverpassChanged))]
    private bool _isDrivingOnOverpass = false; // Initially set to false, as the player starts below the bridge
    #endregion

    #region Unity Callbacks
    private void Awake()
    {
        // Find all colliders for overpasses
        foreach (GameObject overpassColliderGameObject in GameObject.FindGameObjectsWithTag("OverpassCollider"))
        {
            _overpassColliderList.Add(overpassColliderGameObject.GetComponent<Collider2D>());
        }
        
        foreach (GameObject underpassColliderGameObject in GameObject.FindGameObjectsWithTag("UnderpassCollider"))
        {
            _underpassColliderList.Add(underpassColliderGameObject.GetComponent<Collider2D>());
        }

        _carCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // Set initial collisions, assuming the player starts on default layer
        SetCollisionOverpass(_isDrivingOnOverpass);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isLocalPlayer) return;

        if (collision.CompareTag("OverpassTrigger"))
        {
            // The player drives onto the overpass, change the rendering layer to OverpassTrigger
            CmdChangeSortingLayerAndCollision(true, sortingLayerAboveOverpass, false);
        }
        else if (collision.CompareTag("UnderpassTrigger"))
        {
            // The player drives under the overpass, change the rendering layer to UnderpassTrigger
            CmdChangeSortingLayerAndCollision(false, sortingLayerBelowOverpass, true);
        }
    }
    #endregion

    #region Networking
    // Command to change rendering layer and collision (executed on the server)
    [Command]
    void CmdChangeSortingLayerAndCollision(bool isDrivingOnOverpass, string newSortingLayer, bool isOutlineEnabled)
    {
        _isDrivingOnOverpass = isDrivingOnOverpass;
        TargetChangeSortingLayer(gameObject, newSortingLayer, isOutlineEnabled);
        RpcSetCollisionOverpass(_isDrivingOnOverpass);
    }

    // RPC that changes the rendering layer only for the specific player (executed on the client)
    [ClientRpc]
    void TargetChangeSortingLayer(GameObject player, string newSortingLayer, bool isOutlineEnabled)
    {
        carOutline.enabled = isOutlineEnabled;
        SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = newSortingLayer;
        }
    }

    // Synchronize collision on all clients
    [ClientRpc]
    void RpcSetCollisionOverpass(bool isDrivingOnOverpass)
    {
        SetCollisionOverpass(isDrivingOnOverpass);
    }
    #endregion
    
    #region Methods
    // Adjust collisions depending on whether the player is on or under the overpass
    private void SetCollisionOverpass(bool isDrivingOnOverpass)
    {
        foreach (Collider2D col in _overpassColliderList)
        {
            // Ignore collisions with the overpass if not on the overpass
            Physics2D.IgnoreCollision(_carCollider, col, !isDrivingOnOverpass);
        }

        foreach (Collider2D col in _underpassColliderList)
        {
            // Enable collisions with the lower overpass only when on the overpass
            Physics2D.IgnoreCollision(_carCollider, col, isDrivingOnOverpass);
        }
    }

    // Hook called when the SyncVar `_isDrivingOnOverpass` changes
    private void OnDrivingOnOverpassChanged(bool oldValue, bool newValue)
    {
        SetCollisionOverpass(newValue);
    }

    public bool IsDrivingOnOverpass()
    {
        return _isDrivingOnOverpass;
    }
    #endregion
}