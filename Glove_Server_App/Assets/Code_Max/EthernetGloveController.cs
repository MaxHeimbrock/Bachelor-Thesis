using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EthernetGloveController : MonoBehaviour {

    public TrackingData TrD;

    // "connection" things for Ping
    IPEndPoint remoteEndPointPing;
    UdpClient pingClient;
    public static string gloveIP = "192.168.131.59";
    public static int pingPort = 11159;
    Boolean autoconnect = true;

    // "connection" things for receiving
    IPEndPoint remoteEndPoint;
    UdpClient client;
    Boolean connected = false;
    public static string myIP = "192.168.131.1";
    public static int port = 64059; //65259 for IMU

    // Use this for initialization
    void Start () {
        TrD = new TrackingData();

        if (autoconnect)
            ping();

        initUDPReceiver();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void ping()
    {
        remoteEndPointPing = new IPEndPoint(IPAddress.Parse(gloveIP), pingPort);
        pingClient = new UdpClient();
        Debug.Log("Ping Glove on " + gloveIP + " : " + pingPort);
        sendPing();
    }

    public void initUDPReceiver()
    {
        remoteEndPoint = new IPEndPoint(IPAddress.Parse(myIP), port);
        client = new UdpClient(remoteEndPoint);
        
        client.BeginReceive(new AsyncCallback(recv), null);
        Debug.Log("Listening with " + myIP + " on Port: " + port);
    }

    //CallBack
    private void recv(IAsyncResult res)
    {
        Debug.Log("Got something");
        byte[] received = client.EndReceive(res, ref remoteEndPoint);
        client.BeginReceive(new AsyncCallback(recv), null);
        Debug.Log("package received");
        connected = true;
    }

    // sendPing
    private void sendPing()
    { 
        // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
        byte[] data = Encoding.UTF8.GetBytes("Ping");

        // Den message zum Remote-Client senden.
        pingClient.Send(data, data.Length, remoteEndPointPing);
    }
}
