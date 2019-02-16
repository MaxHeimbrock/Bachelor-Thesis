using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPSend : MonoBehaviour
{
    public Boolean SerialPortUsed = false;
    public GameObject glove_controller;
    private GloveConnector gloveConnector;
    
    public int port = 11110;  // define in init

    //public string myIP = "192.168.1.210";

    // zu hause
    //public string myIP = "192.168.178.33";

    // default klappt
    string myIP = "0.0.0.0";

    // "connection" things
    IPEndPoint remoteEndPoint;
    UdpClient client;
    Boolean connected = false;

    // Send constant data stream
    public Boolean autosend = true;    

    // Sequenznumber of the packet send
    static uint seq = 1;

    // For synchronizing clocks
    static long currentTicks = 0;
        
    // start from unity3d
    public void Start()
    {
        gloveConnector = glove_controller.GetComponent<GloveConnector>();
        init();
        Debug.Log("ready");
    }

    public void Update()
    {
        currentTicks = DateTime.Now.Ticks;
        //Hier Packet senden

        if (connected)
            sendSinglePoseUpdate(gloveConnector.getTrackingData());
    }       

    // init
    public void init()    {
        
        IPEndPoint listeningEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
        client = new UdpClient(listeningEndPoint);

        client.BeginReceive(new AsyncCallback(recv), null);
    }    

    //______________________________________ Code von Alex

    private void sendSinglePoseUpdate(TrackingData trD)
    {
        float[] orientation = new float[4];
        orientation[0] = trD.orientation.w;
        orientation[1] = trD.orientation.x;
        orientation[2] = trD.orientation.y;
        orientation[3] = trD.orientation.z;

        // data format = length (int) | Type (byte) | SEQ (uint) |  jointValues (float[40]) | orientation (float[4]) | gesture (int) | time (long)

        byte[] data = new byte[sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * sizeof(float) + sizeof(int) + sizeof(long)];
        
        // lenght
        Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, data, 0, BitConverter.GetBytes(data.Length).Length);

        // type
        data[sizeof(int)] = (byte)1;//Type: 1 = default format

        // SEQ
        Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, data, sizeof(int) + sizeof(byte), BitConverter.GetBytes(seq).Length);

        // Maybe inefficient

        // jointValues
        for (int i = 0; i < 40; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(trD.JointValues[i]), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + i * sizeof(float), sizeof(float));
        }

        // orientation
        for (int i = 0; i < 4; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(orientation[i]), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + i * sizeof(float), sizeof(float));
        }

        // gesture
        //Buffer.BlockCopy(BitConverter.GetBytes(trD.gesture), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * sizeof(float), sizeof(int));
        Buffer.BlockCopy(BitConverter.GetBytes((int)trD.gesture), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * sizeof(float), sizeof(int));

        // time
        Buffer.BlockCopy(BitConverter.GetBytes(currentTicks), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * sizeof(float) + sizeof(int), sizeof(long));
                
        client.Send(data, data.Length, remoteEndPoint);

        //Debug.Log("pose " + seq + " send!");        

        seq++;        
    }

    public void sendGesture(Glove.Gesture gesture)
    {
        // data format = length (int) | Type (byte) | gesture (int)

        byte[] data = new byte[sizeof(int) + sizeof(byte) + sizeof(int)];

        // length
        Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, data, 0, BitConverter.GetBytes(data.Length).Length);

        // type 2 for gesture
        data[sizeof(int)] = (byte)2;

        Buffer.BlockCopy(BitConverter.GetBytes((int)gesture), 0, data, sizeof(int) + sizeof(byte), sizeof(int));
        if (connected)
            client.Send(data, data.Length, remoteEndPoint);

        Debug.Log("Send " + gesture);
    }

    //CallBack
    private void recv(IAsyncResult res)
    {
        // Got a Ping from the Hololens --> Create remoteEndPoint to send to
        if (!connected)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] received = client.EndReceive(res, ref remoteEndPoint);
            client.BeginReceive(new AsyncCallback(recv), null);
            Debug.Log("Hololens connected");
            connected = true;

            //Ping
            if (Encoding.UTF8.GetString(received, 0, received.Length).Equals("UDPPing"))
            {
                Console.WriteLine("UDPPing received from {0}:", remoteEndPoint.ToString());
                byte[] udpPing = Encoding.UTF8.GetBytes("UDPPingReply");
                byte[] data = new byte[sizeof(int) + udpPing.Length];
                Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, data, 0, BitConverter.GetBytes(data.Length).Length);
                Buffer.BlockCopy(udpPing, 0, data, sizeof(int), udpPing.Length);
                client.Send(data, data.Length, remoteEndPoint);
                Debug.Log("Ping from Hololens received");
            }
        }
    }

    // OnGUI
    void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
        labelStyle.fontSize = 40;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
        buttonStyle.fontSize = 40;
                
        if (!connected)
            GUI.Box(new Rect(100, 100, 800, 500), "Hololens not connected", labelStyle);
        else
            GUI.Box(new Rect(100, 100, 800, 800), "Hololens connected - sending data", labelStyle);       
    }
}

