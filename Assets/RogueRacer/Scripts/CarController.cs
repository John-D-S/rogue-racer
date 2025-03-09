using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
public class WheelColliders
{
    public WheelCollider FLWheel;
    public WheelCollider FRWheel;
    public WheelCollider RLWheel;
    public WheelCollider RRWheel;
}
[System.Serializable]
public class VisualWheels
{
    public Transform FLWheel;
    public Transform FRWheel;
    public Transform RLWheel;
    public Transform RRWheel;
}
[System.Serializable]
public class WheelParticles
{
    public ParticleSystem FLWheel;
    public ParticleSystem FRWheel;
    public ParticleSystem RLWheel;
    public ParticleSystem RRWheel;

    public bool ParticlesExist => (FLWheel != null) && (FRWheel != null) && (RLWheel != null) && (RRWheel != null);
}

public struct CarInput
{
    public float gasInput;
    public float steeringInput;
    public bool boostInput;
    public bool driftInput;
}

public class CarController : MonoBehaviour
{
    private Rigidbody playerRB;
    [SerializeField, Tooltip("The center of mass of the car.")] 
    private Vector3 centerOfMass;
    [SerializeField, Tooltip("The wheel colliders for the car.")] 
    private WheelColliders wheelColliders;
    [SerializeField, Tooltip("The visual wheel transforms.")] 
    private VisualWheels visualWheels;
    [SerializeField, Tooltip("The wheel particles for smoke effects.")] 
    private WheelParticles wheelParticles;
    [SerializeField, Tooltip("The smoke particle prefab.")] 
    private GameObject smokePrefab;
    [SerializeField, Tooltip("The base car settings.")] 
    private BaseCarSettings carSettings;

    private float boostRechargeCooldown = 2f;
    
    /// <summary>
    /// Checks if all wheels are grounded.
    /// </summary>
    private bool FullyGrounded => wheelColliders.FLWheel.isGrounded && wheelColliders.FRWheel.isGrounded && wheelColliders.RLWheel.isGrounded && wheelColliders.RRWheel.isGrounded;
    
    [Header("=== For Debugging ===")]
    public float slipAngle;
    public float gasInput;
    public float brakeInput;
    public float steeringInput;
    public bool boostInput;
    public bool driftInput;
    [Header("=== Input Debugging ===")]
    public float remainingBoost;
    public float targetSteeringAngle;
    public float steeringAngle;

    private Dictionary<WheelCollider, float> visualWheelRotations = new Dictionary<WheelCollider, float>();
    
    private float speed;

    public float MetPerSecToKilPerHour(float input) => input * 3.6f;
    public float KilPerHourToMetPerSec(float input) => input / 3.6f;
    
    private List<Upgrade> collectedUpgrades = new List<Upgrade>();

    /// <summary>
    /// Initializes the car controller.
    /// </summary>
    void Start()
    {
        playerRB = gameObject.GetComponent<Rigidbody>();
        playerRB.centerOfMass = centerOfMass;
        if(wheelParticles.ParticlesExist)
            InstantiateSmoke();
    }
    
    /// <summary>
    /// Instantiates smoke particle systems for each wheel.
    /// </summary>
    void InstantiateSmoke()
    {
        wheelParticles.FRWheel = Instantiate(smokePrefab, wheelColliders.FRWheel.transform.position - Vector3.up * wheelColliders.FRWheel.radius, Quaternion.identity, wheelColliders.FRWheel.transform)
            .GetComponent<ParticleSystem>();
        wheelParticles.FLWheel = Instantiate(smokePrefab, wheelColliders.FLWheel.transform.position - Vector3.up * wheelColliders.FRWheel.radius, Quaternion.identity, wheelColliders.FLWheel.transform)
            .GetComponent<ParticleSystem>();
        wheelParticles.RRWheel = Instantiate(smokePrefab, wheelColliders.RRWheel.transform.position - Vector3.up * wheelColliders.FRWheel.radius, Quaternion.identity, wheelColliders.RRWheel.transform)
            .GetComponent<ParticleSystem>();
        wheelParticles.RLWheel = Instantiate(smokePrefab, wheelColliders.RLWheel.transform.position - Vector3.up * wheelColliders.FRWheel.radius, Quaternion.identity, wheelColliders.RLWheel.transform)
            .GetComponent<ParticleSystem>();
    }

    /// <summary>
    /// Updates the speed of the car and applies wheel positions and particles in every frame.
    /// </summary>
    void Update()
    {
        speed = playerRB.velocity.magnitude;
        if(wheelParticles.ParticlesExist)
        {
            CheckParticles();
        }
        ApplyWheelPositions();
    }
    
    /// <summary>
    /// FixedUpdate function handles physics-based operations like applying drift, steering, brake, motor, boost, and counter drift torque.
    /// </summary>
    private void FixedUpdate()
    {
        CarSettings usedCarSettings = new(carSettings.CarSettings);
        foreach(Upgrade collectedUpgrade in collectedUpgrades)
        {
            collectedUpgrade.OnFixedUpdate(this, ref usedCarSettings);
        }
        ApplyDrift(usedCarSettings);
        ApplySteering(usedCarSettings);
        ApplyBrake(usedCarSettings);
        ApplyMotor(usedCarSettings);
        ApplyBoost(usedCarSettings);
        ApplyCounterDriftTorque(usedCarSettings);
    }

    /// <summary>
    /// Applies counter drift torque to the car based on the car's slip angle, drift input, and speed.
    /// </summary>
    /// <param name="usedCarSettings">The car settings to be used for calculations.</param>
    private void ApplyCounterDriftTorque(CarSettings usedCarSettings)
    {
        float counterDriftTorque = 0;
        if(slipAngle > usedCarSettings.maxDriftAngleStart && driftInput && gasInput != 0 && FullyGrounded && speed > usedCarSettings.counterDriftStartSpeed)
        {
            float speedModifier = Mathf.Lerp(0, 1, speed - usedCarSettings.counterDriftStartSpeed / usedCarSettings.counterDriftStopSpeed - usedCarSettings.counterDriftStartSpeed);
            counterDriftTorque = Mathf.Pow(Mathf.Lerp(0, 1, (slipAngle - usedCarSettings.maxDriftAngleStart) / (usedCarSettings.maxDriftAngleStop - usedCarSettings.maxDriftAngleStart)), 2) * usedCarSettings.maxCounterDriftAngularAccel * speedModifier;
            int direction = Vector3.Dot(transform.right, playerRB.velocity) > 1
                ? 1
                : -1;
            if(Mathf.RoundToInt(Mathf.Sign(playerRB.angularVelocity.y)) == direction && Mathf.Sign(Mathf.RoundToInt(Vector3.Dot(playerRB.velocity, transform.forward))) != -1)
            {
                direction = 0;
            }
            playerRB.AddTorque(transform.up * (counterDriftTorque * direction), ForceMode.Acceleration);
        }
    }

    /// <summary>
    /// Sets the car's input based on the given CarInput object.
    /// </summary>
    /// <param name="input">The CarInput object containing gas, steering, drift, and boost inputs.</param>
    public void SetInput(CarInput input)
    {
        gasInput = input.gasInput;
        steeringInput = input.steeringInput;
        driftInput = input.driftInput;
        boostInput = input.boostInput;
    }

    /// <summary>
    /// Applies brake to the car based on the car's moving direction and gas input.
    /// </summary>
    /// <param name="usedCarSettings">The car settings to be used for calculations.</param>
    void ApplyBrake(CarSettings usedCarSettings)
    {
        //fixed code to brake even after going on reverse 
        float movingDirection = Vector3.Dot(transform.forward, playerRB.velocity);
        if (movingDirection < -0.5f && gasInput > 0)
        {
            brakeInput = Mathf.Abs(gasInput);
        }
        else if (movingDirection > 0.5f && gasInput < 0)
        {
            brakeInput = Mathf.Abs(gasInput);
        }
        else if (Mathf.Abs(speed) < 1 && Mathf.Abs(gasInput) < 0.25f)
        {
            brakeInput = 1;
        }
        else
        {
            brakeInput = 0;
        }
        
        wheelColliders.FRWheel.brakeTorque = brakeInput * usedCarSettings.brakePower* 0.7f ;
        wheelColliders.FLWheel.brakeTorque = brakeInput * usedCarSettings.brakePower * 0.7f;

        wheelColliders.RRWheel.brakeTorque = brakeInput * usedCarSettings.brakePower * 0.3f;
        wheelColliders.RLWheel.brakeTorque = brakeInput * usedCarSettings.brakePower * 0.3f;
    }

    /// <summary>
    /// Applies drift to the car based on the drift input.
    /// </summary>
    /// <param name="usedCarSettings">The car settings to be used for calculations.</param>
    void ApplyDrift(CarSettings usedCarSettings)
    {
        var rrFriction = wheelColliders.RRWheel.sidewaysFriction;
        var rlFriction = wheelColliders.RLWheel.sidewaysFriction;
        if(driftInput)
        {
            rrFriction.stiffness = usedCarSettings.driftWheelFriction;
            rlFriction.stiffness = usedCarSettings.driftWheelFriction;
            wheelColliders.RRWheel.sidewaysFriction = rrFriction;
            wheelColliders.RLWheel.sidewaysFriction = rlFriction;
        }
        else
        {
            rrFriction.stiffness = usedCarSettings.normalWheelFriction;
            rlFriction.stiffness = usedCarSettings.normalWheelFriction;
            wheelColliders.RRWheel.sidewaysFriction = rrFriction;
            wheelColliders.RLWheel.sidewaysFriction = rlFriction;
        }
    }
    
    /// <summary>
    /// Applies boost to the car based on the boost input, remaining boost, and recharge cooldown.
    /// </summary>
    /// <param name="usedCarSettings">The car settings to be used for calculations.</param>
    void ApplyBoost(CarSettings usedCarSettings)
    {
        if(boostInput && remainingBoost > 0)
        {
            playerRB.AddForce(transform.forward * usedCarSettings.boostForce, ForceMode.Acceleration);
            remainingBoost = Mathf.Clamp(remainingBoost - Time.fixedDeltaTime, 0, usedCarSettings.maxBoost);
            boostRechargeCooldown = usedCarSettings.maxBoostRechargeCooldown;
        }
        else if(remainingBoost < usedCarSettings.maxBoost && boostRechargeCooldown <= 0)
        {
            remainingBoost = Mathf.Clamp(remainingBoost + usedCarSettings.boostRechargeRate * Time.fixedDeltaTime, 0, usedCarSettings.maxBoost);
        }
        else if(boostRechargeCooldown > 0)
        {
            boostRechargeCooldown = Mathf.Clamp(boostRechargeCooldown -= Time.fixedDeltaTime, 0, usedCarSettings.maxBoostRechargeCooldown);
        }
    }
  
    /// <summary>
    /// ApplyMotor adjusts the torque of the rear wheels based on the car's settings and speed, and adds an initial acceleration force to get the car moving.
    /// </summary>
    /// <param name="usedCarSettings">A CarSettings object containing the car's settings.</param>
    void ApplyMotor(CarSettings usedCarSettings)
    {
        wheelColliders.RRWheel.motorTorque = usedCarSettings.torqueCurve.Evaluate(speed/usedCarSettings.maxSpeed) * usedCarSettings.maxTorque * gasInput;
        wheelColliders.RLWheel.motorTorque = usedCarSettings.torqueCurve.Evaluate(speed/usedCarSettings.maxSpeed) * usedCarSettings.maxTorque * gasInput;
        
        //this code will add an additinal accelleration to get the car started moving
        float forceAmount = Mathf.Lerp(usedCarSettings.initialAccelleration, 0, Mathf.InverseLerp(0, usedCarSettings.initialAccellerationMaxSpeed, speed)) * gasInput * 0.5f;
        if(speed < usedCarSettings.initialAccellerationMaxSpeed)
        {
            if(wheelColliders.RRWheel.isGrounded)
            {
                Vector3 forcePosR = wheelColliders.RRWheel.transform.position - wheelColliders.RRWheel.transform.up * wheelColliders.RRWheel.forceAppPointDistance;
                playerRB.AddForceAtPosition(transform.forward * forceAmount, forcePosR, ForceMode.Acceleration);
            }

            if(wheelColliders.RLWheel.isGrounded)
            {
                Vector3 forcePosL = wheelColliders.RLWheel.transform.position - wheelColliders.RLWheel.transform.up * wheelColliders.RLWheel.forceAppPointDistance;
                playerRB.AddForceAtPosition(transform.forward * forceAmount, forcePosL, ForceMode.Acceleration);
            }
        }
    }
    
    /// <summary>
    /// ApplySteering calculates and applies the steering angle for the front wheels based on the car's settings, speed, and slip angle.
    /// </summary>
    /// <param name="usedCarSettings">A CarSettings object containing the car's settings.</param>
    void ApplySteering(CarSettings usedCarSettings)
    {
        slipAngle = Vector3.Angle(transform.forward, playerRB.velocity-transform.forward);
        targetSteeringAngle = steeringInput * usedCarSettings.steeringCurve.Evaluate(speed);
        if (slipAngle < 120f)
        {
            targetSteeringAngle += Vector3.SignedAngle(transform.forward, playerRB.velocity + transform.forward, transform.up);
        }
        targetSteeringAngle = Mathf.Clamp(targetSteeringAngle, -90f, 90f);
        steeringAngle = Mathf.Lerp(steeringAngle, targetSteeringAngle, Time.fixedDeltaTime * usedCarSettings.steerLerpSpeed);
        wheelColliders.FRWheel.steerAngle = steeringAngle;
        wheelColliders.FLWheel.steerAngle = steeringAngle;
    }

    /// <summary>
    /// ApplyWheelPositions updates the position and rotation of the visual wheels based on the corresponding wheel colliders.
    /// </summary>
    void ApplyWheelPositions()
    {
        float visualSteeringAngle = Mathf.Clamp(targetSteeringAngle, -carSettings.CarSettings.visualMaxSteeringAngle, carSettings.CarSettings.visualMaxSteeringAngle);
        Debug.Log(visualSteeringAngle);
        UpdateWheel(wheelColliders.FRWheel, visualWheels.FRWheel, visualSteeringAngle, true);
        UpdateWheel(wheelColliders.FLWheel, visualWheels.FLWheel, visualSteeringAngle);
        UpdateWheel(wheelColliders.RRWheel, visualWheels.RRWheel, 0);
        UpdateWheel(wheelColliders.RLWheel, visualWheels.RLWheel, 0);
    }
    
    /// <summary>
    /// CheckParticles checks the slip conditions of each wheel and starts or stops the corresponding particle systems to simulate tire smoke.
    /// </summary>
    void CheckParticles() {
        WheelHit[] wheelHits = new WheelHit[4];
        wheelColliders.FRWheel.GetGroundHit(out wheelHits[0]);
        wheelColliders.FLWheel.GetGroundHit(out wheelHits[1]);

        wheelColliders.RRWheel.GetGroundHit(out wheelHits[2]);
        wheelColliders.RLWheel.GetGroundHit(out wheelHits[3]);

        float slipAllowance = 0.5f;
        if ((Mathf.Abs(wheelHits[0].sidewaysSlip) + Mathf.Abs(wheelHits[0].forwardSlip) > slipAllowance)){
            wheelParticles.FRWheel.Play();
        }
        else
        {
            wheelParticles.FRWheel.Stop();
        }
        if ((Mathf.Abs(wheelHits[1].sidewaysSlip) + Mathf.Abs(wheelHits[1].forwardSlip) > slipAllowance)){
            wheelParticles.FLWheel.Play();
        }
        else
        {
            wheelParticles.FLWheel.Stop();
        }
        if ((Mathf.Abs(wheelHits[2].sidewaysSlip) + Mathf.Abs(wheelHits[2].forwardSlip) > slipAllowance)){
            wheelParticles.RRWheel.Play();
        }
        else
        {
            wheelParticles.RRWheel.Stop();
        }
        if ((Mathf.Abs(wheelHits[3].sidewaysSlip) + Mathf.Abs(wheelHits[3].forwardSlip) > slipAllowance)){
            wheelParticles.RLWheel.Play();
        }
        else
        {
            wheelParticles.RLWheel.Stop();
        }
    }
    
    /// <summary>
    /// UpdateWheel updates the position and rotation of a visual wheel based on its corresponding WheelCollider and an optional steering angle.
    /// </summary>
    /// <param name="_wheelCollider">The WheelCollider to update the visual wheel from.</param>
    /// <param name="_visualWheelTransform">The Transform of the visual wheel to be updated.</param>
    /// <param name="_visualSteeringAngle">The optional steering angle to apply to the visual wheel, defaults to 0.</param>
    void UpdateWheel(WheelCollider _wheelCollider, Transform _visualWheelTransform, float _visualSteeringAngle, bool DEBUG = false)
    {
        Quaternion quat;
        Vector3 position;
        _wheelCollider.GetWorldPose(out position, out quat);
        _visualWheelTransform.position = position;
        float absoulteSteeringAngle = Mathf.Abs(_wheelCollider.steerAngle);
        float clampedVisualSteeringAngle = Mathf.Clamp(_visualSteeringAngle, -absoulteSteeringAngle, absoulteSteeringAngle);
        if(!visualWheelRotations.ContainsKey(_wheelCollider))
            visualWheelRotations[_wheelCollider] = 0;
        visualWheelRotations[_wheelCollider] = (visualWheelRotations[_wheelCollider] + _wheelCollider.rpm * 0.016666f * 360 * Time.deltaTime) % 360;
        if(_wheelCollider.isGrounded)
        {
            _visualWheelTransform.rotation = transform.rotation * Quaternion.Euler(visualWheelRotations[_wheelCollider], clampedVisualSteeringAngle, 0);
        }
        else
        {
            _visualWheelTransform.rotation = transform.rotation * Quaternion.Euler(visualWheelRotations[_wheelCollider], 0, 0);
        }
    }

    private void CheckForCollectables()
    {
        //check within a radius of 5 units for upgrades
        Collider[] colliders = Physics.OverlapSphere(transform.position, attractionRadius, pickupLayer);

        foreach (Collider collider in colliders)
        {
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance <= pickupRadius)
            {
                Upgrade upgrade = collider.GetComponent<Upgrade>();
                if (upgrade != null)
                {
                    CollectUpgrade(upgrade);
                    continue;
                }

                Coin coin = collider.GetComponent<Coin>();
                if (coin != null)
                {
                    CollectCoin(coin);
                }
            }
            else
            {
                Vector3 attractionDirection = (transform.position - collider.transform.position).normalized;
                float speedMultiplier = (1 - (distance / attractionRadius));
                collider.transform.position += attractionDirection * attractionSpeed * speedMultiplier * Time.deltaTime;
            }
        }

        
    }
    
    public void AddUpgrade(Upgrade upgrade)
    {
        collectedUpgrades.Add(upgrade);
        upgrade.OnPickup(this);
    }
    
    public void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position + transform.rotation * centerOfMass, 0.25f);
    }
}

