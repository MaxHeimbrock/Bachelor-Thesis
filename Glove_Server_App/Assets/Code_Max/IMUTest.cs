using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMUTest : MonoBehaviour {

    public GameObject glove_controller;
    private Glove glove;
    
    public bool acc_rotation = true;
    public bool gyro_rotation = true;
    public bool translate = false;

    public int axis;

    // Use this for initialization
	void Start () {
        //glove = glove_controller.GetComponent<EthernetGloveController>().glove;
    }
	
	// Update is called once per frame
	void Update () {
        if (glove == null)
            glove = glove_controller.GetComponent<EthernetGloveController>().glove;
        else
        {                       
            if (translate)
                this.transform.position = glove.position;
            if (acc_rotation && !gyro_rotation)
                this.transform.rotation = glove.q;
            else if (gyro_rotation && !acc_rotation)
            {
                if (axis == 1)
                    this.transform.localRotation = glove.x;
                else if (axis == 2)
                    this.transform.localRotation = glove.y;
                else if (axis == 3)
                    this.transform.localRotation = glove.z;
            }
            else if (acc_rotation && gyro_rotation)
                this.transform.rotation = glove.q3;
            
        }
    }
}
