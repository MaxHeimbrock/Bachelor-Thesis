using UnityEngine;
using System.Collections.Generic;
using System;
using System.Text;

#if !WINDOWS_UWP
using System.Net;
using System.Net.Sockets;
using System.Threading;


#else
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
using System.Linq;
using System.IO;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Storage;
#endif


// Kopiert aus serial_port_receiver.cs, welche ersetzt werden soll
static class Constants
{
    public const int NB_SENSORS = 40;
    public const bool IS_BLUETOOTH = false;
}

public class Glove
    {
        public UInt16 NB_SENSORS = 40;
        public UInt32 cnt;
        public UInt16 version;

        public Int64[] raw_values;
        //private Int64[] raw_values;
        private Int64[] offsets;

        public float[] values;
        public double timestamp;
        public Quaternion orientation;

    // 0 for nothing, 1 for clap detected
    public enum Gesture { Clap, Fist };

    public Glove()
    {
        cnt = 0;
        version = 0;
        raw_values = new Int64[Constants.NB_SENSORS];
        offsets = new Int64[Constants.NB_SENSORS];
        values = new float[Constants.NB_SENSORS];

        orientation = new Quaternion();
    }

    public void set_zero()
    {
        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            offsets[i] = raw_values[i];
        }
    }    
}

    //----------------------------------------------------------------------------------------

public class UDPReceive : MonoBehaviour {
    
    public Dictionary<string, TrackingData> trackedPoints = new Dictionary<string, TrackingData>();

    // Narvis
    //public static string IPAddress = "192.168.1.210";

    // zu hause
    public static string IPAddress = "192.168.178.33";
    public static int port = 11110;
    
    private bool initialized = false;
    private bool connected = false;

    public bool autoConnect = false;

    public bool showDebug = false;

    private uint prevSEQ = 0;
    private string lastReceivedUDPString = "";

    public long rtt = 0;
    public long remoteTime = 0;
    public long remoteTimeOffset = 0;
    public long ownFirstTick = 0;

    public Glove glove;
    public GameObject hand;
    private hand_controller handController;
    private long before = 0;

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
        Windows.Networking.HostName hnip = new Windows.Networking.HostName(IPAddress);
        Debug.Log("Send message to IPAddress " + hnip.DisplayName + " on Port " + port.ToString());
        using(var stream = await socket.GetOutputStreamAsync(hnip, port.ToString())) {
            using(var writer = new Windows.Storage.Streams.DataWriter(stream)) {
                writer.WriteBytes(message);
                await writer.StoreAsync();
            }
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
                rtt = ( System.DateTime.Now.Ticks - before ) / System.TimeSpan.TicksPerMillisecond;
                Debug.Log("rtt: " + rtt);
                connected = true;
            }
        }
        
        else
            updateTrackingData(messageBytes);
    }    
    
    //reads the tracking data, either in old human readable UTF8 position/quaternion format, or new binary matrix format
    void updateTrackingData(byte[] trackingMessage) {

        //Debug.Log("New TrackingData arrived");

        if(showDebug) {
            lastReceivedUDPString = BitConverter.ToString(trackingMessage);
        }
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
                float[] jointValues = new float[40];
                float[] orientationArray = new float[4];

                // jointValues
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint), jointValues, 0, 40 * sizeof(float));

                // orientationArray
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float), orientationArray, 0, 4 * sizeof(float));
                
                int gesture = BitConverter.ToInt32(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * sizeof(float));
              
                long currentTick = BitConverter.ToInt64(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 4 * sizeof(float) + sizeof(int));

                if(remoteTime == 0) {
                    ownFirstTick = System.DateTime.Now.Ticks;
                    remoteTime = currentTick + ( (long)( ( rtt / 2.0 ) * TimeSpan.TicksPerMillisecond ) );
                    remoteTimeOffset = remoteTime - ownFirstTick;
                }        
    
                glove.orientation.w = orientationArray[0];
                glove.orientation.x = orientationArray[1];
                glove.orientation.y = orientationArray[2];
                glove.orientation.z = orientationArray[3];
                
                glove.values = jointValues;                

                prevSEQ = seq;
            }
        }

        // gesture packet
        else if(type == 2)
        {
            int gesture;

            //System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte), gesture, 0, sizeof(int));
            gesture = BitConverter.ToInt32(trackingMessage, sizeof(int) + sizeof(byte));

            if (gesture == 0)
                handController.ClapDetected();

            else if (gesture == 1)
                handController.FistDetected();

            else
                Debug.Log("wrong gesture");
        }
    }   

    public void Start() {

        glove = new Glove();

        handController = hand.GetComponent<hand_controller>();

        initUDPReceiver();
    }
    
    void Update() {


        // TODO test this without init in update
        /*

        if(!initialized) {
            initUDPReceiver();
        }

        */

        if (autoConnect && !connected)
        {
            //UDPPing
            before = System.DateTime.Now.Ticks;
            sendUDPMessage(Encoding.UTF8.GetBytes("UDPPing"));
        }
    }
#endif
}