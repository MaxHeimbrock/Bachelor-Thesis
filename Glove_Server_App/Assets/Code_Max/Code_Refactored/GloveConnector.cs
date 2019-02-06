using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveConnector : MonoBehaviour {

    public enum GloveVersion {USB_Glove, Ethernet_Glove, Wifi_Glove};

    public GloveVersion gloveVersion = GloveVersion.Wifi_Glove;

    private GloveConnectionInterface gloveConnectionInterface;

    public class ValuePacket
    {

    }

    public class IMUPacket
    {

    }

	// Use this for initialization
	void Start () {
		switch (gloveVersion)
        {
            case GloveVersion.Ethernet_Glove:
                gloveConnectionInterface = new EthernetGloveController();
                break;
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
