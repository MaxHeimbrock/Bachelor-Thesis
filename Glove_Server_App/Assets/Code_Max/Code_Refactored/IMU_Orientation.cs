﻿using System.Collections;
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

        //this.transform.position = Vector3.Lerp(this.transform.position, new Vector3(3, 3, 3), 0.01f);

        //Debug.Log(transform.rotation.eulerAngles);
    }
}
