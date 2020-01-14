using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCam : MonoBehaviour
{
    public float zDistance;
    public float yDistance;
    public GameObject Player;

	// Use this for initialization
	void Start ()
    {
        Player = GameObject.Find("Sphere"); // alternatively, drag in Sphere in the inspector
	}
	
	void LateUpdate ()
    {
        if (Input.GetKey(KeyCode.A))
        {
            transform.Rotate(Vector3.up, Time.deltaTime * 180);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Rotate(Vector3.up, Time.deltaTime * -180);
        }

        transform.position = Player.transform.position - transform.forward * zDistance;
        transform.Translate(Vector3.up * yDistance);
	}
}
