using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class JointSpringSerializable
{
    public float spring;
    public float damper;
    public float targetPosition;
    
    public JointSpringSerializable(float _spring, float _damper, float _targetPosition)
    {
        spring = _spring;
        damper = _damper;
        targetPosition = _targetPosition;
    }
    
    // Copy constructor
    public JointSpringSerializable(JointSpringSerializable other)
    {
        spring = other.spring;
        damper = other.damper;
        targetPosition = other.targetPosition;
    }
}

[System.Serializable]
public class WheelFrictionCurveSerializable
{
    public float stiffness;
    public float asymptoteSlip;
    public float asymptoteValue;
    public float extremumSlip;
    public float extremumValue;
    
    public WheelFrictionCurveSerializable(float _stiffness, float _asymptoteSlip, float _asymptoteValue, float _extremumSlip, float _extremumValue)
    {
        stiffness = _stiffness;
        asymptoteSlip = _asymptoteSlip;
        asymptoteValue = _asymptoteValue;
        extremumSlip = _extremumSlip;
        extremumValue = _extremumValue;
    }
    
    // Copy constructor
    public WheelFrictionCurveSerializable(WheelFrictionCurveSerializable other)
    {
        stiffness = other.stiffness;
        asymptoteSlip = other.asymptoteSlip;
        asymptoteValue = other.asymptoteValue;
        extremumSlip = other.extremumSlip;
        extremumValue = other.extremumValue;
    }
}


[System.Serializable]
public class WheelSettings
{
    public float mass;
    public float radius;
    public float wheelDampingRate;
    public float suspensionDistance;
    public float forceAppPointDistance;
    public Vector3 center;
    public JointSpringSerializable suspensionSpring;
    public WheelFrictionCurveSerializable forwardFriction;
    public WheelFrictionCurveSerializable sidewaysFriction;

    public void SetWheelColliderSettings(ref WheelCollider _wheel)
    {
        _wheel.mass = mass;
        _wheel.radius = radius;
        _wheel.wheelDampingRate = wheelDampingRate;
        _wheel.suspensionDistance = suspensionDistance;
        _wheel.forceAppPointDistance = forceAppPointDistance;
        _wheel.center = center;
        _wheel.suspensionSpring = new JointSpring()
        {
            spring = suspensionSpring.spring,
            damper = suspensionSpring.damper,
            targetPosition = suspensionSpring.targetPosition
        };
        _wheel.forwardFriction = new WheelFrictionCurve()
        {
            stiffness = forwardFriction.stiffness,
            asymptoteSlip = forwardFriction.asymptoteSlip,
            asymptoteValue = forwardFriction.asymptoteValue,
            extremumSlip = forwardFriction.extremumSlip,
            extremumValue = forwardFriction.extremumValue
        };
        _wheel.sidewaysFriction = new WheelFrictionCurve()
        {
            stiffness = sidewaysFriction.stiffness,
            asymptoteSlip = sidewaysFriction.asymptoteSlip,
            asymptoteValue = sidewaysFriction.asymptoteValue,
            extremumSlip = sidewaysFriction.extremumSlip,
            extremumValue = sidewaysFriction.extremumValue
        };
    }

    public WheelSettings(float mass, float radius, float wheelDampingRate, float suspensionDistance, float forceAppPointDistance, Vector3 center, JointSpringSerializable suspensionSpring, WheelFrictionCurveSerializable forwardFriction, WheelFrictionCurveSerializable sidewaysFriction)
    {
        this.mass = mass;
        this.radius = radius;
        this.wheelDampingRate = wheelDampingRate;
        this.suspensionDistance = suspensionDistance;
        this.forceAppPointDistance = forceAppPointDistance;
        this.center = center;
        this.suspensionSpring = suspensionSpring;
        this.forwardFriction = forwardFriction;
        this.sidewaysFriction = sidewaysFriction;
    }
    
    // Copy constructor
    public WheelSettings(WheelSettings other)
    {
        mass = other.mass;
        radius = other.radius;
        wheelDampingRate = other.wheelDampingRate;
        suspensionDistance = other.suspensionDistance;
        forceAppPointDistance = other.forceAppPointDistance;
        center = other.center;
        suspensionSpring = new JointSpringSerializable(other.suspensionSpring);
        forwardFriction = new WheelFrictionCurveSerializable(other.forwardFriction);
        sidewaysFriction = new WheelFrictionCurveSerializable(other.sidewaysFriction);
    }
}

[System.Serializable]
public class CarSettings
{
	public WheelSettings frontWheelSettings = new WheelSettings(
	#region frontWheelSettings default values
		10, 
		0.28f,
		1,
		0.15f, 
		0, 
		Vector3.zero, 
		new JointSpringSerializable
		(
			45000,
			4500,
			0.85f
		),
		new WheelFrictionCurveSerializable
		(
			0.4f,
			1,
			0.8f,
			0.5f,
			1
		),
		new WheelFrictionCurveSerializable
		(
			0.2f,
			1,
			0.5f,
			0.75f,
			2
		)
	);
	#endregion
	#region backWheelSettings default values
	public WheelSettings backWheelSettings = new WheelSettings(
		10, 
		0.28f,
		1,
		0.15f, 
		0, 
		Vector3.zero, 
		new JointSpringSerializable
		(
			45000,
			4500,
			0.85f
		),
		new WheelFrictionCurveSerializable
		(
			0.4f,
			1,
			0.8f,
			0.5f,
			1
		),
		new WheelFrictionCurveSerializable
		(
			0.2f,
			1,
			0.5f,
			0.75f,
			2
		)
	);
	#endregion
	[Tooltip("The maximum amount of steering that can be visually shown")]
	public float visualMaxSteeringAngle = 45;
	[Tooltip("The amount of steering applied against the speed of the vehicle")]
    public AnimationCurve steeringCurve = new AnimationCurve(new Keyframe(0, 30, 0, 0), new Keyframe(60, 10, -.5f, -.5f));
    public float steerLerpSpeed = 10;
    [Tooltip("The normal amount of back wheel friction")]
    public float normalWheelFriction = 2;
    [Tooltip("The amount of back wheel friction while drifting")]
    public float driftWheelFriction = 0.75f;
    [Range(0, 180), Tooltip("The angle at which the counter-drift torque begins.")]
    public float maxDriftAngleStart = 60;
    [Range(0, 180), Tooltip("The angle at which the counter-drift torque is at maximum.")]
    public float maxDriftAngleStop = 90;
    [Tooltip("The Counter-Drift torque wil start to be applied above this speed")]
    public float counterDriftStartSpeed = 5;
    [Tooltip("The Counter-Drift torque will be fully applied above this speed")]
    public float counterDriftStopSpeed = 10;
    [Tooltip("The maximum amount of angular acceleration that will be applied to the vehicle to prevent it from spinning out while drifting.")]
    public float maxCounterDriftAngularAccel = 25;
    
    [Header("=== Boost ===")]
    public float boostForce = 10;
    [Tooltip("boost is in seconds")]
    public float maxBoost = 3f;
    public float boostRechargeRate = 0.5f;
    public float maxBoostRechargeCooldown = 2f;
    
    [Header("=== Breaking ===")]
    public float brakePower = 5000;
    
    [Header("=== Torque/Acceleration ===")]
    [Tooltip("This is a 1x1 graph of the % of max torque against % of max speed")]
    public AnimationCurve torqueCurve = new AnimationCurve(new Keyframe(0, 0.25f, 7, 3.5f), new Keyframe(0.5f, 1, 0, 0), new Keyframe(1, 0, 0, 0));
    [Tooltip("basically accelleration")]
    public float maxTorque = 1500;
    [Tooltip("A base acceleration that is applied when the car is traveling below Initial Acceleration Max Speed to get it going")]
    public float initialAccelleration = 10;
    public float initialAccellerationMaxSpeed = 15;
    [Tooltip("The speed at which torque becomes 0 in m/s")]
    public float maxSpeed = 20;
    
    public CarSettings(CarSettings other)
    {
	    frontWheelSettings = new WheelSettings(other.frontWheelSettings);
	    backWheelSettings = new WheelSettings(other.backWheelSettings);
	    visualMaxSteeringAngle = other.visualMaxSteeringAngle;
	    steeringCurve = new AnimationCurve(other.steeringCurve.keys);
	    steerLerpSpeed = other.steerLerpSpeed;
	    normalWheelFriction = other.normalWheelFriction;
	    driftWheelFriction = other.driftWheelFriction;
	    maxDriftAngleStart = other.maxDriftAngleStart;
	    maxDriftAngleStop = other.maxDriftAngleStop;
	    counterDriftStartSpeed = other.counterDriftStartSpeed;
	    counterDriftStopSpeed = other.counterDriftStopSpeed;
	    maxCounterDriftAngularAccel = other.maxCounterDriftAngularAccel;
	    boostForce = other.boostForce;
	    maxBoost = other.maxBoost;
	    boostRechargeRate = other.boostRechargeRate;
	    maxBoostRechargeCooldown = other.maxBoostRechargeCooldown;
	    brakePower = other.brakePower;
	    torqueCurve = new AnimationCurve(other.torqueCurve.keys);
	    maxTorque = other.maxTorque;
	    initialAccelleration = other.initialAccelleration;
	    initialAccellerationMaxSpeed = other.initialAccellerationMaxSpeed;
	    maxSpeed = other.maxSpeed;
    }
}
