using System.Collections;
using UnityEngine;
using Mirror;
using Cinemachine;

public class CarController : NetworkBehaviour
{
    [Header("Car Settings")]
    [Tooltip("Coefficient of drift applied to lateral velocity")]
    public float driftFactor = 0.95f;
    [Tooltip("Acceleration multiplier")]
    public float accelerationFactor = 30.0f;
    [Tooltip("Steering sensitivity")]
    public float turnFactor = 3.5f;
    [Tooltip("Maximum car speed")]
    public float maxSpeed = 20.0f;

    private float _accelerationInput = 0f;
    private float _steeringInput = 0f;
    private float _rotationAngle = 0f;
    private float _velocityVsUp = 0f; // Velocity component in the direction of the car's front

    private Rigidbody2D rb;

    public CinemachineVirtualCamera virtualCamera;

    public TrailRenderer[] trailRenderers;
    public TrailRenderer[] overpassTrailRenderers;

    public ParticleSystem[] particleSystems;

    // Store whether the tires are screeching
    [SyncVar]
    public bool isTireScreeching;

    // Store the emission rate
    [SyncVar]
    public float particleEmissionRate = 0f;

    // Store whether the player is on overpass
    [SyncVar]
    public bool isOverpassEmitter = false;

    private CarLayerHandler _carLayerHandler;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
        _carLayerHandler = GetComponent<CarLayerHandler>();
    }

    public override void OnStartLocalPlayer()
    {
        // Assign the virtual camera to follow the local player's car
        if (virtualCamera != null)
        {
            virtualCamera.Follow = transform;
        }

        SetTrailRenderers(false);
        isTireScreeching = false;
        CmdSetTireScreeching(false);

        foreach (var particle in particleSystems)
        {
            if (particle != null)
            {
                var particleEmission = particle.emission;
                particleEmission.rateOverTime = 0;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!isLocalPlayer) return;

        _accelerationInput = Input.GetAxis("Vertical");
        _steeringInput = Input.GetAxis("Horizontal");

        // Apply forces and steering based on player input
        ApplyEngineForce();
        KillOrthogonalVelocity();
        ApplySteering();

        // Check if the tires are screeching and notify the server
        CheckTireScreeching();
        
        // Check if the smoke is emitting and notify the server
        CheckSmokeEmission();
        
        // Check if the car is driving on an overpass and update trail renderers accordingly
        UpdateOverpassState();
    }

    private void UpdateOverpassState()
    {
        if (_carLayerHandler != null)
        {
            bool isOnOverpass = _carLayerHandler.IsDrivingOnOverpass();

            if (isOverpassEmitter != isOnOverpass)
            {
                isOverpassEmitter = isOnOverpass;
                
                // Notify the server about the overpass state change
                CmdSetOverpassEmitter(isOverpassEmitter);
            }
        }
    }

    private void CheckTireScreeching()
    {
        // Calculate if the tires are screeching locally
        if (IsTireScreeching(out float lateralVelocity, out bool isBraking))
        {
            if (!isTireScreeching)
            {
                isTireScreeching = true;
                CmdSetTireScreeching(true);
            }
        }
        else
        {
            if (isTireScreeching)
            {
                isTireScreeching = false;
                CmdSetTireScreeching(false);
            }
        }
    }

    [Command]
    private void CmdSetTireScreeching(bool screeching)
    {
        // Update the state on the server
        isTireScreeching = screeching;

        // Notify all clients about the change
        RpcSetTireScreeching(screeching);
    }

    [ClientRpc]
    private void RpcSetTireScreeching(bool screeching)
    {
        // Update the trail renderers for all clients
        SetTrailRenderers(screeching);
    }

    [Command]
    private void CmdSetOverpassEmitter(bool overpassEmitter)
    {
        // Propagate overpass emitter state to all clients
        isOverpassEmitter = overpassEmitter;
        RpcSetOverpassEmitter(overpassEmitter);
    }

    [ClientRpc]
    private void RpcSetOverpassEmitter(bool overpassEmitter)
    {
        // Update trail renderers based on overpass state and tire screeching
        isOverpassEmitter = overpassEmitter;
        SetTrailRenderers(isTireScreeching);
    }

    private void SetTrailRenderers(bool screeching)
    {
        // Choose the correct set of trail renderers based on the overpass state
        TrailRenderer[] activeRenderers = isOverpassEmitter ? overpassTrailRenderers : trailRenderers;

        // Disable all trail renderers first
        foreach (var trail in trailRenderers)
        {
            if (trail != null)
            {
                trail.emitting = false;
            }
        }

        foreach (var trail in overpassTrailRenderers)
        {
            if (trail != null)
            {
                trail.emitting = false;
            }
        }

        // Enable the selected trail renderers
        foreach (var trail in activeRenderers)
        {
            if (trail != null)
            {
                trail.emitting = screeching;
            }
        }
    }

    private void CheckSmokeEmission()
    {
        // Calculate smoke emission rate
        if (isTireScreeching)
        {
            particleEmissionRate = Mathf.Lerp(particleEmissionRate, 30, Time.deltaTime * 5);
        }
        else
        {
            particleEmissionRate = Mathf.Lerp(particleEmissionRate, 0, Time.deltaTime * 5);
        }

        // Update the emission rate on the server
        CmdUpdateParticleEmissionRate(particleEmissionRate); // TU COS JEST NIE TAK
    }

    [Command]
    private void CmdUpdateParticleEmissionRate(float rate)
    {
        // Update the state on the server
        particleEmissionRate = rate;
        
        // Notify all clients about the change
        RpcUpdateParticleEmissionRate(rate);
    }

    [ClientRpc]
    private void RpcUpdateParticleEmissionRate(float rate)
    {
        // Update the particle systems for all clients  
        foreach (var particle in particleSystems)
        {
            if (particle != null)
            {
                var particleEmission = particle.emission;
                particleEmission.rateOverTime = rate;
            }
        }
    }

    // Method to apply engine force based on player input
    private void ApplyEngineForce()
    {
        // Calculate the car's velocity relative to its forward direction
        _velocityVsUp = Vector2.Dot(transform.up, rb.velocity);

        // Prevent further acceleration if the car is already at its max speed
        if (_velocityVsUp > maxSpeed && _accelerationInput > 0f) return;

        // Prevent further reverse acceleration if the car is reversing too fast
        if (_velocityVsUp < -maxSpeed * 0.5f && _accelerationInput < 0f) return;

        // Limit the car's speed in any direction
        if (rb.velocity.sqrMagnitude > maxSpeed * maxSpeed && _accelerationInput > 0f) return;

        // Apply drag when no acceleration is input
        if (_accelerationInput == 0f)
        {
            rb.drag = Mathf.Lerp(rb.drag, 3.0f, Time.fixedDeltaTime * 3);
        }
        else
        {
            rb.drag = 0f;
        }

        // Apply the engine force in the forward direction
        Vector2 engineForceVector = transform.up * _accelerationInput * accelerationFactor;
        rb.AddForce(engineForceVector, ForceMode2D.Force);
    }

    // Method to apply steering based on player input
    private void ApplySteering()
    {
        // Adjust the steering sensitivity based on the car's speed
        float minSpeedBeforeAllowTurningFactor = (rb.velocity.magnitude / 8);
        minSpeedBeforeAllowTurningFactor = Mathf.Clamp01(minSpeedBeforeAllowTurningFactor);

        // Rotate the car based on the steering input and speed
        _rotationAngle -= _steeringInput * turnFactor * minSpeedBeforeAllowTurningFactor;
        rb.MoveRotation(_rotationAngle);
    }

    // Method to reduce the car's lateral velocity (simulate drifting)
    private void KillOrthogonalVelocity()
    {
        Vector2 forwardVelocity = transform.up * Vector2.Dot(rb.velocity, transform.up);
        Vector2 rightVelocity = transform.right * Vector2.Dot(rb.velocity, transform.right);

        // Apply drift factor to the lateral velocity
        rb.velocity = forwardVelocity + rightVelocity * driftFactor;
    }

    // Helper method to get the lateral velocity of the car
    private float GetLateralVelocity()
    {
        return Vector2.Dot(transform.right, rb.velocity);
    }

    // Method to check if the tires are screeching (e.g., during drifting or braking)
    public bool IsTireScreeching(out float lateralVelocity, out bool isBraking)
    {
        lateralVelocity = GetLateralVelocity();
        isBraking = false;

        // Check if the player is braking (reverse input while moving forward)
        if (_accelerationInput < 0 && _velocityVsUp > 0)
        {
            isBraking = true;
            return true;
        }

        // Check if the lateral velocity is high enough to cause tire screeching
        if (Mathf.Abs(GetLateralVelocity()) > 4.0f)
        {
            return true;
        }

        return false;
    }
    
    public void StopCar()
    {
       StartCoroutine(StopCarCoroutine());
    }

    IEnumerator StopCarCoroutine()
    {
        yield return new WaitForSeconds(1.5f);
        
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
}