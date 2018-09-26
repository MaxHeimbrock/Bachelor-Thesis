using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;

public class UDP_Sender : MonoBehaviour {

    static int UDPPORT = 11111;
    static UdpClient udpClient;

    // Use this for initialization
    void Start () {
        udpClient = new UdpClient(UDPPORT);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
