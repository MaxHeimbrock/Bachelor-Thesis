using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMU_Orientation : MonoBehaviour {

    public GloveConnector gloveConnector;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.rotation = gloveConnector.GetOrientation();
    }
}
