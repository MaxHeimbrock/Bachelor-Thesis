/*
 
    -----------------------
    UDP-Send
    -----------------------
    // [url]http://msdn.microsoft.com/de-de/library/bb979228.aspx#ID0E3BAC[/url]
   
    // > gesendetes unter
    // 127.0.0.1 : 8050 empfangen
   
    // nc -lu 127.0.0.1 8050
 
        // todo: shutdown thread at the end
*/
using UnityEngine;
using System.Collections;

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPSend : MonoBehaviour
{
    private static int localPort;
    
    public int port = 11110;  // define in init

    // "connection" things
    IPEndPoint remoteEndPoint;
    UdpClient client;
    Boolean connected = false;

    // gui
    string strMessage = "";

    // Sequenznumber of the packet send
    static uint seq = 0;

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
    }

    // OnGUI
    void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
        labelStyle.fontSize = 50;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
        buttonStyle.fontSize = 50;

        Rect rectObj = new Rect(40, 380, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        if (!connected)
            GUI.Box(new Rect(100, 100, 800, 500), "No Client connected", labelStyle);
        else
            GUI.Box(new Rect(100, 100, 800, 500), "Getting Data from " + remoteEndPoint.Address + "\non Port " + remoteEndPoint.Port, labelStyle);

        // ------------------------
        // send it
        // ------------------------
        if (connected)
            if (GUI.Button(new Rect(100, 300, 300, 100), "send pose", buttonStyle))
                sendSinglePoseUpdate(glove);
    }

    // init
    public void init()
    {
        init_glove();
        
        client = new UdpClient(port);  

        client.BeginReceive(new AsyncCallback(recv), null);
    }

    public void init_glove()
    {
        // random values for testing

        glove = new TrackingData();

        glove.JointValues = new float[40];
        for (int i = 0; i < 40; i++)
        {
            glove.JointValues[i] = (float)i;
        }

        glove.pose = new Matrix4x4(new Vector4(1, 2, 3, 4), new Vector4(1, 2, 3, 4), new Vector4(1, 2, 3, 4), new Vector4(1, 2, 3, 4));

        glove.velocity = new Vector3(2, 4, 6);
        glove.acceleration = new Vector3(2, 4, 6);
    }       

    // sendData
    private void sendString(string message)
    {
        try
        {
            //if (message != "")
            //{

            // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
            byte[] data = Encoding.UTF8.GetBytes(message);

            // Den message zum Remote-Client senden.
            client.Send(data, data.Length, remoteEndPoint);
            //}
        }
        catch (Exception err)
        {
            print(err.ToString());
        }
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

        Debug.Log("pose " + seq + " send!");
        seq++;
        
    }

    
    //CallBack
    private void recv(IAsyncResult res)
    {
        if (!connected)
        {
            remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] received = client.EndReceive(res, ref remoteEndPoint);
            client.BeginReceive(new AsyncCallback(recv), null);
            Debug.Log("Client connected");
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
                Debug.Log("Ping received");
            }
        }
    }
}

