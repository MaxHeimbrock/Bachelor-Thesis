﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IMUTest : MonoBehaviour {

    public GameObject glove_controller;
    private Glove glove;
   
    public mode orientationMode = mode.acc;

    public Scrollbar bar;

    public enum mode {acc, gyro, filtered, madgwick, madgwickFiltered};

    // Use this for initialization
	void Start () {
        //glove = glove_controller.GetComponent<EthernetGloveController>().glove;
    }
	
	// Update is called once per frame
	void Update () {
        if (glove == null)
        {
            glove = glove_controller.GetComponent<EthernetGloveController>().glove;
        }
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
                Debug.Log(glove.q_madgwick.eulerAngles.x);
                bar.value = glove.q_madgwick.eulerAngles.z/360;
            }
            else if (orientationMode == mode.madgwickFiltered)
            {
                this.transform.rotation = Quaternion.Inverse(glove.q_madgwick_filtered);
            }
        }
    }
}
