using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
		{
			spring = 45000,
			damper = 4500,
			targetPosition = 0.85f
		},
		new WheelFrictionCurveSerializable
		{
			asymptoteSlip = 0.4f,
			asymptoteValue = 1,
			extremumSlip = 0.8f,
			extremumValue = 0.5f,
			stiffness = 1
		},
		new WheelFrictionCurveSerializable
		{
			asymptoteSlip = 0.2f,
			asymptoteValue = 1,
			extremumSlip = 0.5f,
			extremumValue = 0.75f,
			stiffness = 2
		}
	);
	#endregion
	public WheelSettings backWheelSettings = new WheelSettings(
	#region backWheelSettings default values
		10, 
		0.28f,
		1,
		0.15f, 
		0, 
		Vector3.zero, 
		new JointSpringSerializable
		{
			spring = 45000,
			damper = 4500,
			targetPosition = 0.85f
		},
		new WheelFrictionCurveSerializable
		{
			asymptoteSlip = 0.4f,
			asymptoteValue = 1,
			extremumSlip = 0.8f,
			extremumValue = 0.5f,
			stiffness = 1
		},
		new WheelFrictionCurveSerializable
		{
			asymptoteSlip = 0.2f,
			asymptoteValue = 1,
			extremumSlip = 0.5f,
			extremumValue = 0.75f,
			stiffness = 2
		}
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
    [SerializeField] private float brakePower = 5000;
    
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
}
