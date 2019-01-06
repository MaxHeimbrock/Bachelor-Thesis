using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SensorUDPCommunicator : SensorCommunicator
{
    // Debug.Log("hier");

    bool autoconnect;
    bool connected = false;
    
    IPEndPoint ping_endpoint;
    UdpClient ping_client;

    IPEndPoint sensor_endpoint;
    UdpClient sensor_client;

    public SensorUDPCommunicator(string PC_IP, string glove_IP, int ping_port, int sensor_port, bool autoconnect)
    {
        ping_endpoint = new IPEndPoint(IPAddress.Parse(glove_IP), ping_port);
        ping_client = new UdpClient();

        sensor_endpoint = new IPEndPoint(IPAddress.Parse(PC_IP), sensor_port);
        sensor_client = new UdpClient(sensor_endpoint);

        if (autoconnect)
            ConnectToGlove();
    }

    // Sends Ping and starts receiving
    public void ConnectToGlove()
    {
        Ping();

        // from here on try pinging non-stop
        autoconnect = true;

        StartReceiving();
    }

    // Message on ping_port starts UDP-Script in glove
    private void Ping()
    {
        // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
        byte[] message = Encoding.UTF8.GetBytes("Ping");

        // den Ping zum Remote-Client senden.
        ping_client.Send(message, message.Length, ping_endpoint);
    }

    // Starts callback-thread for receive
    private void StartReceiving()
    {
        sensor_client.BeginReceive(new AsyncCallback(ReceiveSensor), null);
    }

    // Callback for receive
    private void ReceiveSensor(IAsyncResult sensor_data)
    {
        // If first packet
        if (connected == false)
        {
            Debug.Log("Sensor communication is active");
            connected = true;
        }

        // get data and start listening again
        byte[] data = sensor_client.EndReceive(sensor_data, ref sensor_endpoint);
        sensor_client.BeginReceive(new AsyncCallback(ReceiveSensor), null);
        Debug.Log("Received Value package from glove");

        UInt32[] jointValues = new UInt32[40];

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), jointValues, 0, 40 * sizeof(UInt32));
        
        // Apply joint values to glove object
        //glove.apply_ethernetJointPacket(jointValues);
        
    }

    // Update is called once per frame
    void Update()
    {
        // Try to connect again
        if (autoconnect && connected == false)
            Ping();
    }
}
