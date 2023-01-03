using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Mathematics;

using UnityEngine;

public class CarController : MonoBehaviour
{
    private Rigidbody playerRB;
    public Vector3 centerOfMass;
    public WheelColliders colliders;
    public VisualWheels visualWheels;
    public WheelParticles wheelParticles;
    public GameObject smokePrefab;
    public float motorPower;
    public float brakePower;
    public float visualMaxSteeringAngle = 45;
    public AnimationCurve steeringCurve;
    public float normalWheelFriction;
    public float driftWheelFriction;
    [Tooltip("This is a 1x1 graph of the % of max torque against % of max speed")]
    public AnimationCurve torqueCurve;

    [Tooltip("basically accelleration")]
    public float maxTorque;
    [Tooltip("The speed at which torque becomes 0 in m/s")]
    public float maxSpeed;
    
    [Header("=== For Debugging ===")]
    public float slipAngle;
    public float gasInput;
    public float brakeInput;
    public float steeringInput;
    private bool boostInput;
    private bool driftInput;
    private float steeringAngle;
    
    private float speed;
    
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
        wheelParticles.FRWheel = Instantiate(smokePrefab, colliders.FRWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.FRWheel.transform)
            .GetComponent<ParticleSystem>();
        wheelParticles.FLWheel = Instantiate(smokePrefab, colliders.FLWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.FLWheel.transform)
            .GetComponent<ParticleSystem>();
        wheelParticles.RRWheel = Instantiate(smokePrefab, colliders.RRWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.RRWheel.transform)
            .GetComponent<ParticleSystem>();
        wheelParticles.RLWheel = Instantiate(smokePrefab, colliders.RLWheel.transform.position - Vector3.up * colliders.FRWheel.radius, Quaternion.identity, colliders.RLWheel.transform)
            .GetComponent<ParticleSystem>();
    }

    // Update is called once per frame
    void Update()
    {
        speed = playerRB.velocity.magnitude;
        CheckInput();
        ApplyDrift();
        ApplyMotor();
        ApplySteering();
        ApplyBrake();
        if(wheelParticles.ParticlesExist)
        {
            CheckParticles();
        }
        ApplyWheelPositions();
    }

    void CheckInput()
    {
        gasInput = Input.GetAxisRaw("Vertical");
        //TODO: change this to getaxis raw
        steeringInput = Input.GetAxis("Horizontal");
        slipAngle = Vector3.Angle(transform.forward, playerRB.velocity-transform.forward);

        driftInput = Input.GetButton("Drift");

        //fixed code to brake even after going on reverse by Andrew Alex 
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
        colliders.FRWheel.brakeTorque = brakeInput * brakePower* 0.7f ;
        colliders.FLWheel.brakeTorque = brakeInput * brakePower * 0.7f;

        colliders.RRWheel.brakeTorque = brakeInput * brakePower * 0.3f;
        colliders.RLWheel.brakeTorque = brakeInput * brakePower *0.3f;
    }

    void ApplyDrift()
    {
        var rrFriction = colliders.RRWheel.sidewaysFriction;
        var rlFriction = colliders.RLWheel.sidewaysFriction;
        if(driftInput)
        {
            rrFriction.stiffness = driftWheelFriction;
            rlFriction.stiffness = driftWheelFriction;
            colliders.RRWheel.sidewaysFriction = rrFriction;
            colliders.RLWheel.sidewaysFriction = rlFriction;
        }
        else
        {
            rrFriction.stiffness = normalWheelFriction;
            rlFriction.stiffness = normalWheelFriction;
            colliders.RRWheel.sidewaysFriction = rrFriction;
            colliders.RLWheel.sidewaysFriction = rlFriction;
        }
    }
    
    void ApplyMotor() 
    {
        colliders.RRWheel.motorTorque = torqueCurve.Evaluate(speed/maxSpeed) * maxTorque * gasInput;
        colliders.RLWheel.motorTorque = torqueCurve.Evaluate(speed/maxSpeed) * maxTorque * gasInput;
        //Debug.Log(speed);
        Debug.Log(torqueCurve.Evaluate(speed/maxSpeed) * maxTorque * gasInput);
    }
    
    void ApplySteering()
    {
        steeringAngle = steeringInput * steeringCurve.Evaluate(speed);
        if (slipAngle < 120f)
        {
            steeringAngle += Vector3.SignedAngle(transform.forward, playerRB.velocity + transform.forward, Vector3.up);
        }
        steeringAngle = Mathf.Clamp(steeringAngle, -90f, 90f);
        colliders.FRWheel.steerAngle = steeringAngle;
        colliders.FLWheel.steerAngle = steeringAngle;
    }

    void ApplyWheelPositions()
    {
        float visualSteeringAngle = Mathf.Clamp(steeringAngle, -visualMaxSteeringAngle, visualMaxSteeringAngle);
        UpdateWheel(colliders.FRWheel, visualWheels.FRWheel, visualSteeringAngle);
        UpdateWheel(colliders.FLWheel, visualWheels.FLWheel, visualSteeringAngle);
        UpdateWheel(colliders.RRWheel, visualWheels.RRWheel, 0);
        UpdateWheel(colliders.RLWheel, visualWheels.RLWheel, 0);
    }
    
    void CheckParticles() {
        WheelHit[] wheelHits = new WheelHit[4];
        colliders.FRWheel.GetGroundHit(out wheelHits[0]);
        colliders.FLWheel.GetGroundHit(out wheelHits[1]);

        colliders.RRWheel.GetGroundHit(out wheelHits[2]);
        colliders.RLWheel.GetGroundHit(out wheelHits[3]);

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
        _visualWheelTransform.rotation = quat;
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
