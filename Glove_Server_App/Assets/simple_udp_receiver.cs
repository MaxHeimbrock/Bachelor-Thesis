using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System;
using System.IO;

/*public class Packet
{
    public UInt32 packet_cnt;
    public UInt32 packet_size;
    public Vector3 position;

    public void deserialize(Byte[] data)
    {
        packet_cnt = System.BitConverter.ToUInt32(data, 0);
        packet_size = System.BitConverter.ToUInt32(data, 4);
        position.x = System.BitConverter.ToSingle(data, 8);
        //Debug.Log(packet_cnt + " "+  packet_size);
    }

    override public string ToString() {
        string text;
        text = "<Packet>[]" + packet_cnt + " " + packet_size+ ' ' + position;
        return text; 
    }
};


public class simple_udp_receiver : MonoBehaviour {
    UdpClient client;
    Thread thread;
    IPEndPoint source_address;
    public int port;
    Packet packet = new Packet();
    // Use this for initialization
    void Start () {
        client = new UdpClient(port);
        source_address = new IPEndPoint(IPAddress.Any, 0);
        thread = new Thread(new ThreadStart(ReceiveData));
        client.Client.ReceiveTimeout = 1000;
        thread.IsBackground = true;
        thread.Start();
    }


    // Unity Application Quit Function
    void OnApplicationQuit()
    {
        if (thread.IsAlive)
        {
            thread.Abort();
        }
        client.Close();
    }

    private void Update()
    {
        packet.position.y = Mathf.Cos(Time.fixedTime);
        transform.position = packet.position;
    }

    private void ReceiveData()
    {
        try
        {


            Debug.Log("starting receive thread");
            //  While thread is still alive.
            while (Thread.CurrentThread.IsAlive)
            {
                try
                {
                    //  Grab the data.
                    byte[] data = client.Receive(ref source_address);

                    //Debug.Log("got data from: " + source_address.Address + ' ' + data);
                    //Debug.Log(data.Length + data.ToString());
                    
                    packet.deserialize(data);
                    //Debug.Log(packet);

                }
                catch (SocketException e)
                {
                    Debug.Log(e.ToString());
                    continue;
                }
            }

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }
}*/
