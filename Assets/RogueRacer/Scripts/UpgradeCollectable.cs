using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeCollectable : Collectable
{
	public Upgrade upgrade;
	public int cost;
	
	protected override void AddToCar(CarController carController)
	{
		carController.AddUpgrade(upgrade);
	}
}
