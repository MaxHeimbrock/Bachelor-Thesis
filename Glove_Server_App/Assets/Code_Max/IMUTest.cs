using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IMUTest : MonoBehaviour {

    public GameObject glove_controller;
    private Glove glove;

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
            //this.transform.position = glove.position;
            //this.transform.rotation = glove.q;

            this.transform.Rotate(glove.rotation);
        }
    }
}
