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

    // zu hause
    public static string IPAddress = "192.168.178.33";
    public static int port = 11110;

    private bool initialized = false;
    private bool connected = false;

    uint prevSEQ = 0;

    private readonly object dataLock = new object();
    GloveData gloveData = new GloveData(Quaternion.identity, new float[40]);

#if WINDOWS_UWP

    private DatagramSocket socket;

    private int UDPPingReplyLength = Encoding.UTF8.GetBytes("UDPPingReply").Length + 4;

    void initUDPReceiver() {
        Debug.Log("Waiting for a connection...");

        socket = new DatagramSocket();
        socket.MessageReceived += Socket_MessageReceived;

        HostName IP = null;
        try {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            IP = Windows.Networking.Connectivity.NetworkInformation.GetHostNames()
            .SingleOrDefault(
                hn =>
                    hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                    == icp.NetworkAdapter.NetworkAdapterId);

            _ = socket.BindEndpointAsync(IP, port.ToString());

            initialized = true;
        }
        catch(Exception e) {
            Debug.Log("Hier gecrasht");
            Debug.Log(e.ToString());
            Debug.Log(SocketError.GetStatus(e.HResult).ToString());
            return;
        }

        Debug.Log("exit start");
    }

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

    private async void Socket_MessageReceived(Windows.Networking.Sockets.DatagramSocket sender,
    Windows.Networking.Sockets.DatagramSocketMessageReceivedEventArgs args) {
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
                connected = true;
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

                // jointValues
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint), jointValues, 0, numberOfSensors * sizeof(float));

                // orientationArray
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float), orientationArray, 0, 4 * sizeof(float));

                Quaternion orientationQuaternion = new Quaternion(orientationArray[0], orientationArray[1], orientationArray[2], orientationArray[3]);
                
                int gesture = BitConverter.ToInt32(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float) + 4 * sizeof(float));
              
                long currentTick = BitConverter.ToInt64(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + numberOfSensors * sizeof(float) + 4 * sizeof(float) + sizeof(int));                       
                
                prevSEQ = seq;

                lock(dataLock)
                    gloveData = new GloveData(orientationQuaternion, jointValues);
            }
        }

    }   

    public void Start() {        
        initUDPReceiver();
    }
    
    void Update() {

        if (!connected)
        {
            sendUDPMessage(Encoding.UTF8.GetBytes("UDPPing"));
        }
    }
#endif

    public Quaternion GetOrientation()
    {
        if (connected)
            lock (dataLock)
                return gloveData.GetOrientation();

        else
            return Quaternion.identity;
    }

    public float[] GetJointAngles()
    {
        if (connected)
            lock (dataLock)
                return gloveData.GetJointAngles();

        else
            return new float[numberOfSensors];
    }
}

public class GloveData
{
    private Quaternion orientation;
    private float[] jointAngles;

    public GloveData(Quaternion orientation, float[] jointAngles)
    {
        this.orientation = orientation;
        this.jointAngles = jointAngles;
    }

    public Quaternion GetOrientation()
    {
        return orientation;
    }

    public float[] GetJointAngles()
    {
        return jointAngles;
    }
}