using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarPlayer : MonoBehaviour
{
    private CarController _carController;
    
    private void Start()
    {
        _carController = GetComponent<CarController>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
    }
    
    void CheckInput()
    {
        CarInput input = new CarInput();
        input.gasInput = Input.GetAxisRaw("Vertical");
        input.steeringInput = Input.GetAxisRaw("Horizontal");
        input.driftInput = Input.GetButton("Drift");
        input.boostInput = Input.GetButton("Boost");
        _carController.SetInput(input);
    }
}
