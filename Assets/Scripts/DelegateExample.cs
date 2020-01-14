using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelegateExample : MonoBehaviour
{
    delegate float DoMath(float a, float b);

    float Add(float a, float b)
    {
        return a + b;
    }

    float Subtract(float a, float b)
    {
        return a - b;
    }

    // Use this for initialization
    void Start ()
    {
        DoMath dm = new DoMath(Add);
        Debug.Log(dm(6, 4));

        dm += Subtract;
        Debug.Log(dm(6, 4));

        // lambda expression for multiplication function
        dm += (a, b) => a * b;
        Debug.Log(dm(6, 4));
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
