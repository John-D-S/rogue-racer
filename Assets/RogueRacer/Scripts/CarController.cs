using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEditor.Rendering;

using UnityEngine;

using Debug = System.Diagnostics.Debug;

public class CarController : MonoBehaviour
{
    private Rigidbody playerRB;
    public Vector3 centerOfMass;
    public WheelColliders wheelColliders;
    public VisualWheels visualWheels;
    public WheelParticles wheelParticles;
    public GameObject smokePrefab;
    public float visualMaxSteeringAngle = 45;
    public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0, 30, 0, 0), new Keyframe(60, 10));
    public float steerLerpSpeed;
    public float normalWheelFriction;
    [Range(0, 180)]
    public float maxDriftAngleStart = 80;
    [Range(0, 180)]
    public float maxDriftAngleStop = 125;
    public float counterDriftStartSpeed = 5;
    public float counterDriftStopSpeed = 10;
    public float maxCounterDriftAngularAccel = 50;
    public float driftWheelFriction;
    [Header("=== Boost ===")]
    public float boostForce = 10;
    [Tooltip("boost is in seconds")]
    public float maxBoost = 3f;
    public float boostRechargeRate = 0.5f;
    public float boostRechargeDelay = 2f;

    private bool FullyGrounded => wheelColliders.FLWheel.isGrounded && wheelColliders.FRWheel.isGrounded && wheelColliders.RLWheel.isGrounded && wheelColliders.RRWheel.isGrounded;
    
    public float brakePower;
    [Tooltip("This is a 1x1 graph of the % of max torque against % of max speed")]
    public AnimationCurve torqueCurve;
    [Tooltip("basically accelleration")]
    public float maxTorque;
    public float initialAccelleration = 5;
    public float initialAccellerationMaxSpeed = 5;
    [Tooltip("The speed at which torque becomes 0 in m/s")]
    public float maxSpeed;
    
    [Header("=== For Debugging ===")]
    public float slipAngle;
    public float gasInput;
    public float brakeInput;
    public float steeringInput;
    public float remainingBoost;
    
    private bool boostInput;
    private bool driftInput;
    private float targetSteeringAngle;
    private float steeringAngle;

    private Dictionary<WheelCollider, float> visualWheelRotations = new Dictionary<WheelCollider, float>();
    
    private float speed;

    public float MetPerSecToKilPerHour(float input) => input * 3.6f;
    public float KilPerHourToMetPerSec(float input) => input / 3.6f;
    
    // Start is called before the first frame update
    void Start()
    {
        playerRB = gameObject.GetComponent<Rigidbody>();
        playerRB.centerOfMass = centerOfMass;
        if(wheelParticles.ParticlesExist)
            InstantiateSmoke();
    }

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

    // Update is called once per frame
    void Update()
    {
        speed = playerRB.velocity.magnitude;
        CheckInput();
        ApplyDrift();
        ApplySteering();
        ApplyBrake();
        if(wheelParticles.ParticlesExist)
        {
            CheckParticles();
        }
        ApplyWheelPositions();
    }

    
    private void FixedUpdate()
    {
        ApplyMotor();
        
        //ApplyBoost();
        float counterDriftTorque = 0;
        if(slipAngle > maxDriftAngleStart && driftInput && gasInput != 0 && FullyGrounded && speed > counterDriftStartSpeed)
        {
            float speedModifier = Mathf.Lerp(0, 1, speed - counterDriftStartSpeed / counterDriftStopSpeed - counterDriftStartSpeed);
            counterDriftTorque = Mathf.Pow(Mathf.Lerp(0, 1, (slipAngle - maxDriftAngleStart) / (maxDriftAngleStop - maxDriftAngleStart)), 2) * maxCounterDriftAngularAccel * speedModifier;
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
    
    void CheckInput()
    {
        gasInput = Input.GetAxisRaw("Vertical");
        //TODO: change this to getaxis raw
        steeringInput = Input.GetAxis("Horizontal");
        slipAngle = Vector3.Angle(transform.forward, playerRB.velocity-transform.forward);

        driftInput = Input.GetButton("Drift");
        boostInput = Input.GetButton("Boost");

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
    }
    
    void ApplyBrake()
    {
        wheelColliders.FRWheel.brakeTorque = brakeInput * brakePower* 0.7f ;
        wheelColliders.FLWheel.brakeTorque = brakeInput * brakePower * 0.7f;

        wheelColliders.RRWheel.brakeTorque = brakeInput * brakePower * 0.3f;
        wheelColliders.RLWheel.brakeTorque = brakeInput * brakePower * 0.3f;
    }

    void ApplyDrift()
    {
        var rrFriction = wheelColliders.RRWheel.sidewaysFriction;
        var rlFriction = wheelColliders.RLWheel.sidewaysFriction;
        if(driftInput)
        {
            
            /*
            rrFriction.stiffness = driftWheelFriction;
            rlFriction.stiffness = driftWheelFriction;
            */
            rrFriction.stiffness = driftWheelFriction;
            rlFriction.stiffness = driftWheelFriction;
            wheelColliders.RRWheel.sidewaysFriction = rrFriction;
            wheelColliders.RLWheel.sidewaysFriction = rlFriction;
        }
        else
        {
            rrFriction.stiffness = normalWheelFriction;
            rlFriction.stiffness = normalWheelFriction;
            wheelColliders.RRWheel.sidewaysFriction = rrFriction;
            wheelColliders.RLWheel.sidewaysFriction = rlFriction;
        }
    }
    /*
    void ApplyBoost()
    {
        if(boostInput && )
        {
            playerRB.AddForce(transform.forward * boostForce, ForceMode.Force);
        }
    }
  */  
    void ApplyMotor() 
    {
        wheelColliders.RRWheel.motorTorque = torqueCurve.Evaluate(speed/maxSpeed) * maxTorque * gasInput;
        wheelColliders.RLWheel.motorTorque = torqueCurve.Evaluate(speed/maxSpeed) * maxTorque * gasInput;
        
        //this code will add
        if(FullyGrounded && speed < initialAccellerationMaxSpeed)
        {
            Vector3 forcePosL = wheelColliders.RLWheel.transform.position - wheelColliders.RLWheel.transform.up * wheelColliders.RLWheel.forceAppPointDistance;
            Vector3 forcePosR = wheelColliders.RRWheel.transform.position - wheelColliders.RRWheel.transform.up * wheelColliders.RRWheel.forceAppPointDistance;
            float forceAmount = Mathf.Lerp(initialAccelleration, 0, Mathf.InverseLerp(0, initialAccellerationMaxSpeed, speed)) * gasInput * 0.5f;
            playerRB.AddForceAtPosition(transform.forward * forceAmount, forcePosL, ForceMode.Acceleration);
            playerRB.AddForceAtPosition(transform.forward * forceAmount, forcePosR, ForceMode.Acceleration);
        }
        //Debug.Log(speed);
        //Debug.Log(torqueCurve.Evaluate(speed/maxSpeed) * maxTorque * gasInput);
    }
    
    void ApplySteering()
    {
        targetSteeringAngle = steeringInput * steeringCurve.Evaluate(speed);
        if (slipAngle < 120f)
        {
            targetSteeringAngle += Vector3.SignedAngle(transform.forward, playerRB.velocity + transform.forward, transform.up);
        }
        targetSteeringAngle = Mathf.Clamp(targetSteeringAngle, -90f, 90f);
        steeringAngle = Mathf.Lerp(steeringAngle, targetSteeringAngle, Time.deltaTime * steerLerpSpeed);
        wheelColliders.FRWheel.steerAngle = steeringAngle;
        wheelColliders.FLWheel.steerAngle = steeringAngle;
    }

    void ApplyWheelPositions()
    {
        float visualSteeringAngle = Mathf.Clamp(targetSteeringAngle, -visualMaxSteeringAngle, visualMaxSteeringAngle);
        UpdateWheel(wheelColliders.FRWheel, visualWheels.FRWheel, visualSteeringAngle);
        UpdateWheel(wheelColliders.FLWheel, visualWheels.FLWheel, visualSteeringAngle);
        UpdateWheel(wheelColliders.RRWheel, visualWheels.RRWheel, 0);
        UpdateWheel(wheelColliders.RLWheel, visualWheels.RLWheel, 0);
    }
    
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
    
    void UpdateWheel(WheelCollider _wheelCollider, Transform _visualWheelTransform, float _steeringAngle)
    {
        Quaternion quat;
        Vector3 position;
        _wheelCollider.GetWorldPose(out position, out quat);
        //Vector3 visualWheelRot = new Vector3(0, _steeringAngle, 0);
        //_visualWheelTransform.rotation = transform.rotation * Quaternion.Euler(visualWheelRot);
        _visualWheelTransform.position = position;
        float angle = _wheelCollider.steerAngle;
        if(!visualWheelRotations.ContainsKey(_wheelCollider))
            visualWheelRotations[_wheelCollider] = 0;
        visualWheelRotations[_wheelCollider] = (visualWheelRotations[_wheelCollider] + _wheelCollider.rpm * 0.016666f * 360 * Time.deltaTime) % 360;
        _visualWheelTransform.rotation = transform.rotation * Quaternion.Euler(visualWheelRotations[_wheelCollider], angle, 0);
    }

    public void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position + transform.rotation * centerOfMass, 0.25f);
    }
}

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
public class WheelParticles{
    public ParticleSystem FLWheel;
    public ParticleSystem FRWheel;
    public ParticleSystem RLWheel;
    public ParticleSystem RRWheel;

    public bool ParticlesExist => (FLWheel != null) && (FRWheel != null) && (RLWheel != null) && (RRWheel != null);
}
