using UnityEngine;
using System.Text;
using System.Linq;
using System;
using System.IO;

#if !WINDOWS_UWP
using System.Net;
using System.Net.Sockets;
using System.Threading;


#else
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
#endif

public class UDPReceive : MonoBehaviour {

    private const int numberOfSensors = 40;

    // Narvis
    //public static string IPAddress = "192.168.1.210";

    public static int port = 11110;

    private bool initialized = false;
    private bool connected = false;

    uint prevSEQ = 0;

    private readonly object dataLock = new object();
    GloveData gloveData;

    public UI_Manager UImanager;

#if WINDOWS_UWP

    private DatagramSocket socket;

    private int UDPPingReplyLength = Encoding.UTF8.GetBytes("UDPPingReply").Length + 4;


    public static string GetLocalIp(HostNameType hostNameType = HostNameType.Ipv4)
    {
        var icp = NetworkInformation.GetInternetConnectionProfile();

        if (icp?.NetworkAdapter == null) return null;
        var hostname =
            NetworkInformation.GetHostNames()
                .FirstOrDefault(
                    hn =>
                        hn.Type == hostNameType &&
                        hn.IPInformation?.NetworkAdapter != null &&
                        hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId);

        // the ip address
        return hostname?.CanonicalName;
    }

    void initUDPReceiver() {
        Debug.Log("Waiting for a connection...");

        socket = new DatagramSocket();
        socket.MessageReceived += Socket_MessageReceived;
        HostNameType hostNameType = HostNameType.Ipv4;
        HostName IP = null;
        try {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            IP = Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
             .FirstOrDefault(
                    hn =>
                        hn.Type == hostNameType &&
                        hn.IPInformation?.NetworkAdapter != null &&
                        hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId);

            _ = socket.BindEndpointAsync(IP, port.ToString());

            initialized = true;
            Debug.Log("Hololens IP is " + IP.CanonicalName);
        }
        catch(Exception e) {
            Debug.Log("Hier gecrasht");
            Debug.Log(e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }

        Debug.Log("exit start");
    }

    /*
    public async void sendUDPMessage(byte[] message) {
        try {
            Windows.Networking.HostName hnip = new Windows.Networking.HostName(IPAddress);
            //Debug.Log("Send message to IPAddress " + hnip.DisplayName + " on Port " + port.ToString());
            using(var stream = await socket.GetOutputStreamAsync(hnip, port.ToString())) {
                using(var writer = new Windows.Storage.Streams.DataWriter(stream)) {
                    writer.WriteBytes(message);
                    await writer.StoreAsync();
                }
            }
        } catch (Exception e)
        {
            Debug.Log("cant send from Hololens");
        }
    }
    */

    private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
    Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args) {
        connected = true;
        //Debug.Log("message received");
        Stream streamIn = args.GetDataStream().AsStreamForRead();
        byte[] byteLength = new byte[4];
        await streamIn.ReadAsync(byteLength, 0, 4);
        int length = BitConverter.ToInt32(byteLength, 0);
        byte[] messageBytes = new byte[length];
        System.Buffer.BlockCopy(byteLength, 0, messageBytes, 0, 4);
        await streamIn.ReadAsync(messageBytes, 4, length-4);
        //Debug.Log(Encoding.UTF8.GetString(messageBytes, sizeof(int), length-4));

        if(length == UDPPingReplyLength) {
            if(Encoding.UTF8.GetString(messageBytes, sizeof(int), length - 4).Equals("UDPPingReply")) {
                Debug.Log("UDPPingReply received.");
            }
        }        
        else
            updateTrackingData(messageBytes);
    }    
    
    //reads the tracking data, either in old human readable UTF8 position/quaternion format, or new binary matrix format
    void updateTrackingData(byte[] trackingMessage) {

        //Debug.Log("New TrackingData arrived");

        if(trackingMessage.Length < 5) {
            Debug.Log("Too short Packet");
            return;
        }

        byte type = trackingMessage[sizeof(int)];
        if(type == 1) 
        {
            //This is tracking data in binary format
            // data format = length (int) | Type (byte) | SEQ (uint) |  jointValues (float[40]) | orientation (float[4]) | gesture (int) | time (long)
            int length = BitConverter.ToInt32(trackingMessage, 0);
            if(trackingMessage.Length != length) {
                Debug.Log("Malformed Packet");
                return;
            }
            if(trackingMessage.Length <= 157) {
                Debug.Log("Strange message length");
                return;
            }
            
            uint seq = BitConverter.ToUInt32(trackingMessage, sizeof(int) + sizeof(byte));
            if(seq > prevSEQ || ( seq < 10000 && prevSEQ > UInt32.MaxValue * 0.75 )) { //tracking data is newer than what we already have
                float[] jointValues = new float[numberOfSensors];
                float[] orientationArray = new float[4];
                float[] accel = new float[3];
                float[] gyro = new float[3];

                // jointValues
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint), jointValues, 0, numberOfSensors * sizeof(float));

                // orientationArray
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float), orientationArray, 0, 4 * sizeof(float));

                Quaternion orientationQuaternion = new Quaternion(orientationArray[0], orientationArray[1], orientationArray[2], orientationArray[3]);
                
                int gesture = BitConverter.ToInt32(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float) + 4 * sizeof(float));
              
                long currentTick = BitConverter.ToInt64(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float) + 4 * sizeof(float) + sizeof(int));                       

                // accel
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float) + 4 * sizeof(float) + sizeof(int) + sizeof(long), accel, 0, 3 * sizeof(float));

                // gyro
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float) + 4 * sizeof(float) + sizeof(int) + sizeof(long) + 3 * sizeof(float), gyro, 0, 3 * sizeof(float));
                
                prevSEQ = seq;

                lock(dataLock)
                    gloveData = new GloveData(orientationQuaternion, jointValues, gesture, accel, gyro);

                if (gesture == 1)
                    UImanager.Clap();

                // in Ethernet: accel x = -z in real | accel y = -x in real | accel z = y in real - real for me left handed like unity

                //Debug.Log(gyro[2]);
            }
        }

    }   

    public void Start() {

        float[] dummyJoints = new float[40];
        float[] dummyAccel = new float[3];
        float[] dummyGyro = new float[3];

        for (int i = 0; i < 40; i++)
            dummyJoints[i] = 0;
        for (int i = 0; i < 3; i++)
            dummyAccel[i] = 0;
        for (int i = 0; i < 3; i++)
            dummyGyro[i] = 0;
        gloveData = new GloveData(Quaternion.identity, dummyJoints, 0, new float[3], new float[3]);

        initUDPReceiver();
    }
    
    void Update() {
        //if (connected == false)
        //    initUDPReceiver();
    }


#endif

    public Quaternion GetOrientation()
    {
        lock (dataLock)
            return gloveData.GetOrientation();
    }

    public float[] GetJointAngles()
    {
        lock (dataLock)
            return gloveData.GetJointAngles();
    }

    public float[] GetAccel()
    {
        lock (dataLock)
            return gloveData.GetAccel();
    }
}

public class GloveData
{
    public enum Gesture {None, Clap};

    private Quaternion orientation;
    private float[] jointAngles;
    Gesture gesture;
    float[] accel;
    float[] gyro;

    public GloveData(Quaternion orientation, float[] jointAngles, int gesture, float[] accel, float[] gyro)
    {
        this.orientation = orientation;
        this.jointAngles = jointAngles;
        this.gesture = (Gesture)gesture;
        this.accel = accel;
        this.gyro = gyro;
    }

    public Quaternion GetOrientation()
    {
        return orientation;
    }

    public float[] GetJointAngles()
    {
        return jointAngles;
    }

    public Gesture GetGesture()
    {
        return gesture;
    }

    public float[] GetAccel()
    {
        return accel;
    }
}