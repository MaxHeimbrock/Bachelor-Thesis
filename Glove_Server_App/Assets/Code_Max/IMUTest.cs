using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMUTest : MonoBehaviour {

    public GameObject glove_controller;
    private Glove glove;

    public int mode = 1;

    public bool acc_rotation = true;
    public bool gyro_rotation = true;
    public bool translate = false;

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
            switch(mode)
            {
                case 1:
                    this.transform.rotation = glove.x * glove.y * glove.z;
                    break;
                case 2:
                    this.transform.rotation = glove.x * glove.z * glove.y;
                    break;
                case 3:
                    this.transform.rotation = glove.y * glove.x * glove.z;
                    break;
                case 4:
                    this.transform.rotation = glove.y * glove.z * glove.x;
                    break;
                case 5:
                    this.transform.rotation = glove.z * glove.y * glove.x;
                    break;
                case 6:
                    this.transform.rotation = glove.z * glove.x * glove.y;
                    break;
            }

            //if (translate)
            //    this.transform.position = glove.position;
            //if (acc_rotation && !gyro_rotation)
            //    this.transform.rotation = glove.q;
            //else if (gyro_rotation && !acc_rotation)
            //    this.transform.rotation = glove.q2;
            //else if (acc_rotation && gyro_rotation)
            //    this.transform.rotation = glove.q3;
        }
    }
}
