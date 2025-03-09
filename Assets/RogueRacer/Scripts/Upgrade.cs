using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Upgrade
{
	public abstract void OnPickup(CarController carController);
	public abstract void OnFixedUpdate(CarController carController, ref CarSettings carSettings);
	public abstract void OnAttack(CarController carController, ref CarSettings carSettings, CarController enemyCarController);
	public abstract void OnEnemyDestroyed(CarController carController, ref CarSettings carSettings, Vector3 enemyPosition);
	public abstract void OnCoinCollected(CarController carController, ref CarSettings carSettings);
	
}
