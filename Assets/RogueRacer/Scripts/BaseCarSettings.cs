using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "CarSettings", menuName = "RogueRacer/CarSettings")]
public class BaseCarSettings : ScriptableObject
{
	[SerializeField] private CarSettings carSettings;
	
	public CarSettings CarSettings => carSettings;
    
	/*
	public WheelSettings FrontWheelSettings => frontWheelSettings;
	public WheelSettings BackWheelSettings => backWheelSettings;
	public float VisualMaxSteeringAngle => visualMaxSteeringAngle;
	public AnimationCurve SteeringCurve => steeringCurve;
	public float SteerLerpSpeed => steerLerpSpeed;
	public float NormalWheelFriction => normalWheelFriction;
	public float DriftWheelFriction => driftWheelFriction;
	public float MaxDriftAngleStart => maxDriftAngleStart;
	public float MaxDriftAngleStop => maxDriftAngleStop;
	public float CounterDriftStartSpeed => counterDriftStartSpeed;
	public float CounterDriftStopSpeed => counterDriftStopSpeed;
	public float MaxCounterDriftAngularAccel => maxCounterDriftAngularAccel;
	public float BoostForce => boostForce;
	public float MaxBoost => maxBoost;
	public float BoostRechargeRate => boostRechargeRate;
	public float MaxBoostRechargeCooldown => maxBoostRechargeCooldown;
	public float BrakePower => brakePower;
	public AnimationCurve TorqueCurve => torqueCurve;
	public float MaxTorque => maxTorque;
	public float InitialAccelleration => initialAccelleration;
	public float InitialAccellerationMaxSpeed => initialAccellerationMaxSpeed;
	public float MaxSpeed => maxSpeed;
	*/
}
