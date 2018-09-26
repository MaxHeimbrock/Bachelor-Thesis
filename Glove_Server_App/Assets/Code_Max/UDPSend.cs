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

    // prefs
    private string IP;  // define in init
    public int port;  // define in init

    // "connection" things
    IPEndPoint remoteEndPoint;
    UdpClient client;

    // gui
    string strMessage = "";

    static uint seq = 0;
    static long currentTicks = 0;

    // for testing
    private TrackingData glove;

    // call it from shell (as program)
    private static void Main()
    {
        UDPSend sendObj = new UDPSend();
        sendObj.init();

        // testing via console
        // sendObj.inputFromConsole();

        // as server sending endless
        sendObj.sendEndless(" endless infos \n");

    }
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
        Rect rectObj = new Rect(40, 380, 200, 400);
        GUIStyle style = new GUIStyle();
        style.alignment = TextAnchor.UpperLeft;
        GUI.Box(rectObj, "# UDPSend-Data\n127.0.0.1 " + port + " #\n"
                    + "shell> nc -lu 127.0.0.1  " + port + " \n"
                , style);

        // ------------------------
        // send it
        // ------------------------
        strMessage = GUI.TextField(new Rect(40, 420, 140, 20), strMessage);
        if (GUI.Button(new Rect(190, 420, 40, 20), "send"))
        {
            sendString(strMessage + "\n");
            Debug.Log("Gesendet von Max");
        }
    }

    // init
    public void init()
    {
        init_glove();

        // Endpunkt definieren, von dem die Nachrichten gesendet werden.
        print("UDPSend.init()");

        // define
        IP = "127.0.0.1";
        port = 11110;

        // ----------------------------
        // Senden
        // ----------------------------
        //remoteEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
        client = new UdpClient(port);        

        // status
        print("Sending to " + IP + " : " + port);
        print("Testing: nc -lu " + IP + " : " + port);

        client.BeginReceive(new AsyncCallback(recv), null);
    }

    public void init_glove()
    {
        // random values for testing

        glove = new TrackingData();

        glove.JointValues = new float[40];
        for (int i = 0; i < 40; i++)
        {
            glove.JointValues[i] = i;
        }

        glove.pose = new Matrix4x4(new Vector4(1, 2, 3, 4), new Vector4(1, 2, 3, 4), new Vector4(1, 2, 3, 4), new Vector4(1, 2, 3, 4));

        glove.velocity = new Vector3(2, 4, 6);
        glove.acceleration = new Vector3(2, 4, 6);
    }

    // inputFromConsole
    private void inputFromConsole()
    {
        try
        {
            string text;
            do
            {
                text = Console.ReadLine();

                // Den Text zum Remote-Client senden.
                if (text != "")
                {

                    // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
                    byte[] data = Encoding.UTF8.GetBytes(text);

                    // Den Text zum Remote-Client senden.
                    client.Send(data, data.Length, remoteEndPoint);
                }
            } while (text != "");
        }
        catch (Exception err)
        {
            print(err.ToString());
        }

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


    // endless test
    private void sendEndless(string testStr)
    {
        do
        {
            sendString(testStr);


        }
        while (true);

    }

    //______________________________________ Code von Alex

    private void sendSinglePoseUpdate(TrackingData trD)
    {
        //StringBuilder s = new StringBuilder();
        //s.AppendFormat("{0,9:N2} {1,9:N2} {2,9:N2} {3,9:N2}\n{4,9:N2} {5,9:N2} {6,9:N2} {7,9:N2}\n{8,9:N2} {9,9:N2} {10,9:N2} {11,9:N2}\n{12,9:N2} {13,9:N2} {14,9:N2} {15,9:N2}", matrix[0][0], matrix[0][1], matrix[0][2], matrix[0][3], matrix[1][0], matrix[1][1], matrix[1][2], matrix[1][3], matrix[2][0], matrix[2][1], matrix[2][2], matrix[2][3], matrix[3][0], matrix[3][1], matrix[3][2], matrix[3][3]);
        //Console.WriteLine(s.ToString());
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
        //System.Threading.Thread.Sleep(rnd.Next(2, 15));
        
        client.Send(data, data.Length, remoteEndPoint);

        seq++;
    }

    
    //CallBack
    private void recv(IAsyncResult res)
    {
        Debug.Log("Callback started");
        remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        byte[] received = client.EndReceive(res, ref remoteEndPoint);
        client.BeginReceive(new AsyncCallback(recv), null);
        Debug.Log("Message Received");

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

