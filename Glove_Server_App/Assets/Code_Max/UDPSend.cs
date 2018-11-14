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
    
    public int port = 11110;  // define in init

    public string myIP = "192.168.1.210";

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

    // for testing
    private TrackingData glove;
        
    // start from unity3d
    public void Start()
    {
        init();
    }

    public void Update()
    {
        currentTicks = DateTime.Now.Ticks;
        
        if (SerialPortUsed)
            glove = glove_controller.GetComponent<serial_port_receiver>().glove.GetTrackingData();
        else
            glove = glove_controller.GetComponent<EthernetGloveController>().glove.GetTrackingData();

        if (connected && autosend)
            sendSinglePoseUpdate(glove);
    }       

    // init
    public void init()    {
        
        IPEndPoint listeningEndPoint = new IPEndPoint(IPAddress.Parse(myIP), port);
        client = new UdpClient(listeningEndPoint);

        client.BeginReceive(new AsyncCallback(recv), null);
    }    

    //______________________________________ Code von Alex

    private void sendSinglePoseUpdate(TrackingData trD)
    {       
        // data format = length (int) | Type (byte) | SEQ (uint) |  jointValues (float[40]) | pose (4*4 floats) | velocity (3 floats) | acceleration (3 floats) | time (long)
       
        byte[] data = new byte[sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * 4 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float) + sizeof(long)];
        
        Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, data, 0, BitConverter.GetBytes(data.Length).Length);
        data[sizeof(int)] = (byte)1;//Type: 1 = default format
        Buffer.BlockCopy(BitConverter.GetBytes(seq), 0, data, sizeof(int) + sizeof(byte), BitConverter.GetBytes(seq).Length);

        // Maybe inefficient
        for (int i = 0; i < 40; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(trD.JointValues[i]), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + i * sizeof(float), sizeof(float));
        }

        for (int i = 0; i < 16; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(trD.pose[i]), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + i * sizeof(float), sizeof(float));
        }

        for (int i = 0; i < 3; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(trD.velocity[i]), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 16 * sizeof(float) + i * sizeof(float), sizeof(float));
        }

        for (int i = 0; i < 3; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(trD.acceleration[i]), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 16 * sizeof(float) + 3 * sizeof(float) + i * sizeof(float), sizeof(float));
        }
                
        Buffer.BlockCopy(BitConverter.GetBytes(currentTicks), 0, data, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 16 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float), sizeof(long));
                
        client.Send(data, data.Length, remoteEndPoint);
        
        //Debug.Log("pose " + seq + " send!");
        seq++;
        
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
        else
        {
            byte[] received = client.EndReceive(res, ref remoteEndPoint);
            client.BeginReceive(new AsyncCallback(recv), null);
            Debug.Log("Packet from Hololens");
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

        // ------------------------
        // send it
        // ------------------------
        if (connected && !autosend)
            if (GUI.Button(new Rect(100, 200, 300, 100), "send pose", buttonStyle))
                sendSinglePoseUpdate(glove);        
    }
}

