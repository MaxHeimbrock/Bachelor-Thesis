using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveConnectionInterface : MonoBehaviour {

    public enum GloveVersion {USB_Glove, Ethernet_Glove, Wifi_Glove};
    public GloveVersion gloveVersion = GloveVersion.Wifi_Glove;

    const int SENSOR_NUMBER = 40;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
