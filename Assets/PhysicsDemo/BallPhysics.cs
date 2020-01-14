using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    public float speed = 5;
    Rigidbody rb;
    Transform tCam;
    bool leftMouseClick;

	// Use this for initialization
	void Start ()
    {
        rb = GetComponent<Rigidbody>();
        tCam = GameObject.Find("Main Camera").transform;

        StartCoroutine(GetInput());
	}

    IEnumerator GetInput()
    {
        while (true)
        {
            leftMouseClick = Input.GetMouseButtonDown(0);

            yield return new WaitForFixedUpdate();
        }
    }
	
	void FixedUpdate ()
    {
        Vector3 forward = tCam.forward;
        Vector3 right = tCam.right;

        float boost = 1f;

        if (Input.GetKey(KeyCode.Space))
        {
            boost = 2f;
        }
        if (leftMouseClick)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // reset y velocity
            rb.AddForce(Vector3.up * rb.mass * 500f);
        }
		if (Input.GetKey(KeyCode.UpArrow))
        {
            rb.AddForce(forward * speed * boost);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            rb.AddForce(-forward * speed * boost);
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rb.AddForce(-right * speed * boost);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rb.AddForce(right * speed * boost);
        }
	}
}
