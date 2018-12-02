using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMUTest : MonoBehaviour {

    public GameObject glove_controller;
    private Glove glove;
   
    public mode orientationMode = mode.acc;

    public enum mode {acc, gyro, filtered, madgwick};

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
            if (orientationMode == mode.acc)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_acc);
            }
            else if (orientationMode == mode.gyro)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_gyro);
            }
            else if (orientationMode == mode.filtered)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_filtered);
            }
            else if (orientationMode == mode.madgwick)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_madgwick);
                this.transform.Rotate(180,0,0);
            }
            // this.transform.rotation = Quaternion.Inverse(glove.q3);
        }
    }
}
