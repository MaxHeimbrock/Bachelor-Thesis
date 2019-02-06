using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

#if !UNITY_EDITOR
//using System.Net;  
//using System.Net.Sockets;  

using Windows.Storage.Streams;
using Windows.Networking.Sockets;
using Windows.Networking.Connectivity;
using Windows.Networking;
#endif


//============================================================================
//
//============================================================================
public class WiFiGlovePacket{
    
    public UInt16 NB_VALUES_GLOVE = 40;
    public UInt16 cnt;
    public UInt16 protocol_version;
    public float[] values;
    public Int16[] values_raw;
    public Int16[] values_offsets;
    
    public WiFiGlovePacket() {
        values          = new float[Constants.NB_VALUES_GLOVE];
        values_raw      = new Int16[Constants.NB_VALUES_GLOVE];
        values_offsets  = new Int16[Constants.NB_VALUES_GLOVE];
    }
}

//============================================================================
//
//============================================================================
public class WiFiGesturePacket{
    public UInt16 cnt;
    public UInt16 protocol_version;
    public UInt32 gesture;
    
    public WiFiGesturePacket() {
        cnt              = 0;
        protocol_version = 0;
        gesture          = 0;
    }
}

//============================================================================
//
//============================================================================
public class WiFiImuPacket{
    public UInt16 cnt;
    public UInt16 protocol_version;
    public Int16[] acc;
    public Int16[] rot;
    
    public WiFiImuPacket() {
        cnt              = 0;
        protocol_version = 0;
        acc          = new Int16[3];
        rot          = new Int16[3];
    }
}

//============================================================================
//
//============================================================================
public class UDPCommunication : MonoBehaviour{
    private readonly object gestureLock  =  new object();
    private readonly object imuLock      =  new object();
    private readonly object gloveLock    =  new object();
    private WiFiGesturePacket gesture_packet;
    private WiFiImuPacket     imu_packet;
    private WiFiGlovePacket   glove_packet;

    
#if UNITY_EDITOR
        public void start(){
            Debug.Log("UNITY: start from UDP Communication");
        }
        public void update(){
            Debug.Log("UNITY: update from UDP Communication");
        }
#else
        DataWriter writer;
        //private readonly Queue<WiFiGesturePacket> ReceivePacketsQueue= new Queue<WiFiGesturePacket>();
        private DatagramSocket socket_gesture;
        private DatagramSocket socket_imu;
        private DatagramSocket socket_glove;
    //============================================================================
    void Start(){
        gesture_packet  = new WiFiGesturePacket();
        imu_packet      = new WiFiImuPacket();
        glove_packet    = new WiFiGlovePacket();

        Initialize_network();
    }

    //============================================================================
    async void Initialize_network(){
        //HostName HtargetIp    = new HostName("192.168.137.1");
        //HostName HlocalhostIp = new HostName("192.168.137.131");
        //HostName HtargetIp    = new HostName("10.60.177.190");
        //HostName HlocalhostIp = new HostName("192.168.137.131");

        socket_gesture = new DatagramSocket();
        if (socket_gesture==null){
            Debug.Log("failed at creating a socket_gesture");
            return;    
        }
        socket_gesture.MessageReceived += Socket_MessageGestureReceived; // attach receive event
        

        socket_imu = new DatagramSocket();
        if (socket_imu==null){
            Debug.Log("failed at creating a socket_imu");
            return;    
        }
        socket_imu.MessageReceived += Socket_MessageImuReceived; // attach receive event
        

        socket_glove = new DatagramSocket();
        if (socket_glove==null){
            Debug.Log("failed at creating a socket_glove");
            return;    
        }
        socket_glove.MessageReceived += Socket_MessageGloveReceived; // attach receive event
        
        string localPortGesture = "64003";
        await socket_gesture.BindEndpointAsync(null, localPortGesture); // bind to any interface from this machine
        
        string localPortImu = "64004";
        await socket_imu.BindEndpointAsync(null, localPortImu); // bind to any interface from this machine
        
        string localPortGlove = "64005";
        await socket_glove.BindEndpointAsync(null, localPortGlove); // bind to any interface from this machine
       
        /*
        DatagramSocket socket_out = new DatagramSocket();
        if (socket_out==null){
            Debug.Log("failed at creating a socket_out");
            return;    
        }
        //await socket_out.ConnectAsync(HtargetIp, "64123");
        var stream = await socket_out.GetOutputStreamAsync(HtargetIp, "64123");
        if (stream == null){
            return;
        }
        writer = new Windows.Storage.Streams.DataWriter(stream);
        if (writer == null){
            return;
        }
         */
    }

        //============================================================================
        // Update is called once per frame
        async void Update(){

            /*
            // placed the lock only on the dequeue so the receive count
            // might no be correct anymore, still positive though.
            // so we are good
            WiFiGesturePacket packet = null;
            while (ReceivePacketsQueue.Count > 0) { 
                lock(balanceLock){
                    packet = ReceivePacketsQueue.Dequeue();
                    //UnityEngine.UI.Text t = text_area.GetComponent<UnityEngine.UI.Text>();
                    //t.text = "received: " + packet.cnt + "\\" + packet.values[9] + "\\" + packet.values[10];
                }
            }
            
            if(packet==null){
                return;
            }
            */
            if(writer==null){
                return;
            }            
            //  send the ...
            string message = "got packet\r\n";
            writer.WriteBytes( System.Text.Encoding.UTF8.GetBytes(message) );
            await writer.StoreAsync();

        }

        //============================================================================
        async void Socket_MessageGestureReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args){

            MemoryStream streamIn = new MemoryStream();
            await args.GetDataStream().AsStreamForRead().CopyToAsync(streamIn);
            BinaryReader reader = new BinaryReader(streamIn);
            if (streamIn.Length != 8 ){
                return;
            }
            
            reader.BaseStream.Position = 0;
            // depack the stream in the packet
            WiFiGesturePacket packet       = new WiFiGesturePacket();
            packet.cnt                     = reader.ReadUInt16();
            packet.protocol_version        = reader.ReadUInt16();
            packet.gesture                 = reader.ReadUInt32();

            // insert the packet in the queue while being sure we are allowed to
           
            lock(gestureLock){
              gesture_packet = packet;
            }
         }

        //============================================================================
        async void Socket_MessageImuReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args){

            MemoryStream streamIn = new MemoryStream();
            await args.GetDataStream().AsStreamForRead().CopyToAsync(streamIn);
            BinaryReader reader = new BinaryReader(streamIn);
            if (streamIn.Length != 16 ){
                return;
            }
            
            reader.BaseStream.Position = 0;
            // depack the stream in the packet
            WiFiImuPacket packet          = new WiFiImuPacket();
            packet.cnt                    = reader.ReadUInt16();
            packet.protocol_version       = reader.ReadUInt16();
            packet.acc[0]                 = reader.ReadInt16();
            packet.acc[1]                 = reader.ReadInt16();
            packet.acc[2]                 = reader.ReadInt16();
            packet.rot[0]                 = reader.ReadInt16();
            packet.rot[1]                 = reader.ReadInt16();
            packet.rot[2]                 = reader.ReadInt16();

            // insert the packet in the queue while being sure we are allowed to
            lock(imuLock){
              imu_packet = packet;
            }
         }

        //============================================================================
        async void Socket_MessageGloveReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args){

            MemoryStream streamIn = new MemoryStream();
            await args.GetDataStream().AsStreamForRead().CopyToAsync(streamIn);
            BinaryReader reader = new BinaryReader(streamIn);
            if (streamIn.Length != 84 ){
                return;
            }
            
            reader.BaseStream.Position = 0;
            // depack the stream in the packet
            WiFiGlovePacket packet       = new WiFiGlovePacket();
            packet.cnt                     = reader.ReadUInt16();
            packet.protocol_version        = reader.ReadUInt16();
            for (int i=0;i< Constants.NB_VALUES_GLOVE; i++){
                packet.values_raw[i]           = reader.ReadInt16();
                packet.values[i]               = ((float) packet.values_raw[i])/1000.0f;
            }

            // insert the packet in the queue while being sure we are allowed to
            lock(gloveLock){
              glove_packet = packet;
            }
         }
#endif

        // ==============================================================
        public void GetGesturePacket(ref WiFiGesturePacket packet){
            lock(gestureLock){
                packet = gesture_packet;
            }
            return;
        }

        // ==============================================================
        public void GetImuPacket(ref WiFiImuPacket packet){
            lock(imuLock){
                packet = imu_packet;
            }
            return;
        }

        // ==============================================================
        public void GetGlovePacket(ref WiFiGlovePacket packet){
            lock(gloveLock){
                packet = glove_packet;
            }
        }


}