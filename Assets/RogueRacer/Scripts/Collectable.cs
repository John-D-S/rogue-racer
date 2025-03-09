using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public abstract class Collectable : MonoBehaviour
{
    const float accelerationRate = 10;
    private float speed = 0;
    
    public void CollectObject(CarController carController)
    {
        StartCoroutine(AttractTowardsCar(carController));
        Destroy(gameObject);
    }
    
    private IEnumerator AttractTowardsCar(CarController controller)
    {
        while (controller != null)
        {
            float distanceToCar = Vector3.Distance(controller.transform.position, transform.position);
            if(distanceToCar < 0.1f)
            {
                AddToCar(controller);
                Destroy(this.gameObject);
                break;
            }
            Vector3 direction = controller.transform.position - transform.position;
            direction.Normalize();
            speed += accelerationRate * Time.deltaTime;
            float travelDistance = Mathf.Clamp(Time.deltaTime * speed, 0, distanceToCar); 
            transform.position +=  direction * travelDistance;
            yield return null;
        }
    }

    protected abstract void AddToCar(CarController carController);
}
