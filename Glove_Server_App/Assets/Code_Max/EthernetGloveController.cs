using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EthernetGloveController : MonoBehaviour {

    Glove glove;
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
        glove = new Glove();
        TrD = new TrackingData();

        if (autoconnect)
            ping();

        initUDPReceiver();
	}
	
	// Update is called once per frame
	void Update () {

        // von mir hier hin verschoben
        if (Input.GetKey("space"))
        {
            glove.set_zero();
            Debug.Log("set_zero");
        }
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
    }

    //CallBack
    private void recv(IAsyncResult res)
    {
        byte[] data = client.EndReceive(res, ref remoteEndPoint);
        client.BeginReceive(new AsyncCallback(recv), null);
        Debug.Log("Received package from glove");
        connected = true;
        TrD = createTrackingData(data);
    }

    // sendPing
    private void sendPing()
    { 
        // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
        byte[] message = Encoding.UTF8.GetBytes("Ping");

        // Den message zum Remote-Client senden.
        pingClient.Send(message, message.Length, remoteEndPointPing);
    }

    private TrackingData createTrackingData(byte[] data)
    {
        float[] jointValues = new float[40];

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), jointValues, 0, 40 * sizeof(float));
        glove.applyEthernetPacket(jointValues);

        return glove.GetTrackingData();
    }
}


