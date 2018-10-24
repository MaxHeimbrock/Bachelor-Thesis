using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EthernetGloveController : MonoBehaviour
{
    public Glove glove;

    // "connection" things for Ping
    IPEndPoint remoteEndPointPing;
    UdpClient pingClient;
    public static string gloveIP = "192.168.131.59";
    public static int pingPort = 11159;
    Boolean autoconnect = true;

    // "connection" things for receiving
    IPEndPoint valuesRemoteEndPoint;
    IPEndPoint IMURemoteEndPoint;
    UdpClient valuesClient;
    UdpClient IMUClient;
    Boolean connected = false;
    public static string myIP = "192.168.131.1";
    public static int valuesPort = 64059; //65259 for IMU
    public static int IMUPort = 64159;

    // Use this for initialization
    void Start()
    {
        glove = new Glove();

        // Testing
        initUDPReceiverValues();

        initUDPReceiverIMU();
    }

    // Update is called once per frame
    void Update()
    {
        if (autoconnect && connected == false)
            ping();

        // von mir hier hin verschoben
        if (Input.GetKey("space"))
        {
            glove.set_zero();
            Debug.Log("set_zero");
        }

        //Debug.Log(glove.acceleration);
    }

    public void ping()
    {
        remoteEndPointPing = new IPEndPoint(IPAddress.Parse(gloveIP), pingPort);
        pingClient = new UdpClient();
        Debug.Log("Ping Glove on " + gloveIP + " : " + pingPort);
        sendPing();
    }

    public void initUDPReceiverValues()
    {
        valuesRemoteEndPoint = new IPEndPoint(IPAddress.Parse(myIP), valuesPort);
        valuesClient = new UdpClient(valuesRemoteEndPoint);

        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
    }

    public void initUDPReceiverIMU()
    {
        IMURemoteEndPoint = new IPEndPoint(IPAddress.Parse(myIP), IMUPort);
        IMUClient = new UdpClient(IMURemoteEndPoint);

        IMUClient.BeginReceive(new AsyncCallback(recvIMU), null);
    }

    //CallBack fuer Values
    private void recvValues(IAsyncResult res)
    {
        if (connected == false)
            Debug.Log("Glove is active");
        byte[] data = valuesClient.EndReceive(res, ref valuesRemoteEndPoint);
        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
        //Debug.Log("Received Value package from glove");
        connected = true;
        applyValuePacket(data);
    }

    //CallBack fuer IMU
    private void recvIMU(IAsyncResult res)
    {
        byte[] data = IMUClient.EndReceive(res, ref IMURemoteEndPoint);
        IMUClient.BeginReceive(new AsyncCallback(recvIMU), null);
        //Debug.Log("Received IMU package from glove");
        connected = true;
        applyIMUPacket(data);
    }

    // sendPing
    private void sendPing()
    {
        // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
        byte[] message = Encoding.UTF8.GetBytes("Ping");

        // den Ping zum Remote-Client senden.
        pingClient.Send(message, message.Length, remoteEndPointPing);
    }

    // Change Glove Object according to new joint Data
    private void applyValuePacket(byte[] data)
    {
        UInt32[] jointValues = new UInt32[40];

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), jointValues, 0, 40 * sizeof(UInt32));

        Debug.Log(jointValues[1]);

        glove.applyEthernetPacketValues(jointValues);
    }

    // Change Glove Object according to new IMU Data
    private void applyIMUPacket(byte[] data)
    {
        Int16[] acc = new Int16[3];
        Vector3 accVec;

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3]
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), acc, 0, 3 * sizeof(Int16));

        accVec = new Vector3(acc[0], acc[1], acc[2]);

        glove.applyEthernetPacketIMU(accVec);
    }

    // OnGUI
    void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
        labelStyle.fontSize = 40;

        if (!connected)
            GUI.Box(new Rect(100, 200, 800, 500), "Glove offline", labelStyle);
        else
            GUI.Box(new Rect(100, 200, 500, 500), "Glove active - getting data", labelStyle);
    }
}

