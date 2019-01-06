using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class IMU_UDPCommunicator : IMUCommunicator
{
    // Debug.Log("hier");

    bool autoconnect;
    bool connected = false;

    IPEndPoint ping_endpoint;
    UdpClient ping_client;

    IPEndPoint IMU_endpoint;
    UdpClient IMU_client;

    public IMU_UDPCommunicator(string PC_IP, string glove_IP, int ping_port, int IMU_port, bool autoconnect)
    {
        ping_endpoint = new IPEndPoint(IPAddress.Parse(glove_IP), ping_port);
        ping_client = new UdpClient();

        IMU_endpoint = new IPEndPoint(IPAddress.Parse(PC_IP), IMU_port);
        IMU_client = new UdpClient(IMU_endpoint);

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
        IMU_client.BeginReceive(new AsyncCallback(ReceiveIMU), null);
    }

    // Callback for receive
    private void ReceiveIMU(IAsyncResult IMU_data)
    {
        // If first packet
        if (connected == false)
        {
            Debug.Log("IMU communication is active");
            connected = true;
        }

        // get data and start listening again
        byte[] data = IMU_client.EndReceive(IMU_data, ref IMU_endpoint);
        IMU_client.BeginReceive(new AsyncCallback(ReceiveIMU), null);
        Debug.Log("Received IMU package from glove");

        Int16[] acc = new Int16[3];
        Vector3 accVec;

        Int16[] gyro = new Int16[3];
        Vector3 gyroVec;

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), acc, 0, 3 * sizeof(Int16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16), gyro, 0, 3 * sizeof(Int16));
        int timestamp = BitConverter.ToInt32(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16) + 3 * sizeof(Int16));

        accVec = new Vector3(acc[0], acc[1], acc[2]);
        gyroVec = new Vector3(gyro[0], gyro[1], gyro[2]);

        //glove.applyEthernetPacketIMU(accVec, gyroVec);
    }

    // Update is called once per frame
    void Update()
    {
        // Try to connect again
        if (autoconnect && connected == false)
            Ping();
    }
}
