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
        public float[] values;
        public UInt16 version;

        private Int64[] raw_values;
        private Int64[] offsets;

        public Glove()
        {
            cnt = 0;
            version = 0;
            raw_values = new Int64[Constants.NB_SENSORS];
            offsets = new Int64[Constants.NB_SENSORS];
            values = new float[Constants.NB_SENSORS];        
        }

        public void set_zero()
        {
            for (int i = 0; i < Constants.NB_SENSORS; i++)
            {
                offsets[i] = raw_values[i];
            }
        }

    /*
    public void apply_packet(Packet packet)
    {
        version = packet.version;
        cnt++;

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            raw_values[i] = (Int64)(raw_values[i] + packet.values[i]);
        }
        raw_values[packet.key] = packet.value;

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            values[i] = 0.001f * (raw_values[i] - offsets[i]);
        }
        //Debug.Log ("cnt " + cnt);
    }
    */
    public void apply_packet(float[] jointValues)
    {
        for (int i = 0; i < 40; i++)
        {
            raw_values[i] = (Int64)jointValues[i];
        }
    }

    }

    //----------------------------------------------------------------------------------------

public class UDPReceive : MonoBehaviour {
    
    public Dictionary<string, TrackingData> trackedPoints = new Dictionary<string, TrackingData>();

    public string IPAddress = "192.168.1.210";
    public int port = 11110;

    public bool readIPAddressAndPortFromFile = false;
    private string IPAddressFromFile;
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
    private long before = 0;

#if WINDOWS_UWP

    ////////////////////
    // READ FROM FILE //
    ////////////////////

    public void ReadIPAddress() {
        try {
            using( Stream stream = OpenFileForRead( ApplicationData.Current.RoamingFolder.Path, "ipaddress.txt" ) ) {
                if( stream != null ) {
                    byte[] data = new byte[stream.Length];
                    stream.Read( data, 0, data.Length );
                    string readFromFile = Encoding.ASCII.GetString( data );
                    string[] splitString = readFromFile.Split(':');        
                    IPAddressFromFile = splitString[0].Trim();
                    if(splitString.Length == 2) {
                        port = int.Parse(splitString[1].Trim());
                    }
                }
            }
        }
        catch( Exception e ) {
            Debug.Log("Error thrown when reading IP address.\n" + e );
        }
    }

    private static Stream OpenFileForRead(string folderName, string fileName) {
        Stream stream = null;
        bool taskFinish = false;

        Task task = new Task(
            async () => {
                try {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync( folderName );
                    var item = await folder.TryGetItemAsync( fileName );
                    if( item != null ) {
                        StorageFile file = await folder.GetFileAsync( fileName );
                        if( file != null ) {
                            stream = await file.OpenStreamForReadAsync();
                        }
                    }
                }
                catch( Exception ) { }
                finally { taskFinish = true; }

            } );
        task.Start();
        while( !taskFinish ) {
            task.Wait();
        }

        return stream;
    }

    /////////////////////////////////////////////////////////////////////////    

    private DatagramSocket socket;
    public Queue<Action> ExecuteOnMainThread;

    private int UDPPingReplyLength = Encoding.UTF8.GetBytes("UDPPingReply").Length + 4;

    void initUDPReceiver() {
        if(!readIPAddressAndPortFromFile || (readIPAddressAndPortFromFile && !string.IsNullOrEmpty(IPAddressFromFile) )) {
            ExecuteOnMainThread = new Queue<Action>();
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

                if(autoConnect) {
                    //UDPPing
                    before = System.DateTime.Now.Ticks;
                    sendUDPMessage(Encoding.UTF8.GetBytes("UDPPing"));
                }
                initialized = true;

            }
            catch(Exception e) {
                Debug.Log("Hier gecrasht");
                Debug.Log(e.ToString());
                Debug.Log(SocketError.GetStatus(e.HResult).ToString());
                return;
            }
        }
        Debug.Log("exit start");
    }

    private async void sendUDPMessage(byte[] message) {
        Windows.Networking.HostName hnip = readIPAddressAndPortFromFile ? new Windows.Networking.HostName(IPAddressFromFile) : new Windows.Networking.HostName(IPAddress);
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


#else

    Thread receiveThread;
    UdpClient client;
    private volatile bool shutdown = false;

    public void ReadIPAddress() {
        try {
            string readFromFile = System.IO.File.ReadAllText(Application.persistentDataPath + "\\ipaddress.txt");
            string[] splitString = readFromFile.Split(':');
            IPAddressFromFile = splitString[0].Trim();
            if(splitString.Length == 2) {
                port = int.Parse(splitString[1].Trim());
            }
        }
        catch(Exception e) {
            Debug.Log("Error thrown when reading IP address.\n" + e);
        }
    }

    private void sendUDPMessage(byte[] message) {
        client.Send(message, message.Length);
    }

    private void initUDPReceiver() {
        if(!readIPAddressAndPortFromFile || (readIPAddressAndPortFromFile && !string.IsNullOrEmpty(IPAddressFromFile) )) {
            receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.IsBackground = true;
            receiveThread.Start();
            initialized = true;
        }

    }

    // receive thread
    private void ReceiveData() {

        client = new UdpClient();
        System.Net.IPAddress ip = readIPAddressAndPortFromFile ? System.Net.IPAddress.Parse(IPAddressFromFile) : System.Net.IPAddress.Parse(IPAddress);
        IPEndPoint EPIP = new IPEndPoint(ip, port);
        client.Connect(EPIP);
        if(autoConnect) {
#if !UNITY_ANDROID
            System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
            try {
                System.Net.NetworkInformation.PingReply pingreply = ping.Send(ip);
                Debug.Log("Sending ping");
                Debug.Log(pingreply.Status);
                if(pingreply.Status == System.Net.NetworkInformation.IPStatus.Success) {
                    Debug.Log(string.Format("Address: {0}", pingreply.Address));
                    Debug.Log(string.Format("status: {0}", pingreply.Status));
                    Debug.Log(string.Format("Round trip time: {0}", pingreply.RoundtripTime));
                    rtt = pingreply.RoundtripTime;
                }
            }
            catch(System.Net.NetworkInformation.PingException ex) {
                Debug.Log(ex);
            }
#endif


            byte[] helloMessage = Encoding.UTF8.GetBytes("Hello");
            sendUDPMessage(helloMessage);
        }
        //first packet handed differently
        if(!shutdown) {
            try {
                
                byte [] lastReceivedUDPBytes = client.Receive(ref EPIP);
                updateTrackingData(lastReceivedUDPBytes);

            }
            catch(Exception err) {
                Debug.Log(err.ToString());
            }
        }

        while(!shutdown) {
            try {
                byte [] lastReceivedUDPBytes = client.Receive(ref EPIP);
                //string trackerName = Encoding.UTF8.GetString(lastReceivedUDPBytes, sizeof(int) + sizeof(byte) + sizeof(uint), lastReceivedUDPBytes.Length-145);
                //Debug.Log(trackerName);
                updateTrackingData(lastReceivedUDPBytes);

            }
            catch(Exception err) {
                Debug.Log(err.ToString());
            }
        }
        client.Close();
    }


#endif

    void OnDisable() {
#if WINDOWS_UWP

#else
        shutdown = true;
#endif
    }


    //reads the tracking data, either in old human readable UTF8 position/quaternion format, or new binary matrix format
    void updateTrackingData(byte[] trackingMessage) {
        if(showDebug) {
            lastReceivedUDPString = BitConverter.ToString(trackingMessage);
        }
        if(trackingMessage.Length < 5) {
            Debug.Log("Too short Packet");
            return;
        }

        byte type = trackingMessage[sizeof(int)];
        if(type == 1) {
            //This is tracking data in binary format
            // data format = length (int) | Type (byte) | SEQ (uint) |  jointValues (float[40]) | pose (4*4 floats) | velocity (3 floats) | acceleration (3 floats) | time (long)
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
                float[] poseMatrixIntermediate = new float[16];
                float[] velocityIntermediate = new float[3];
                float[] accelerationIntermediate = new float[3];

                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint), jointValues, 0, 40 * sizeof(float));
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float), poseMatrixIntermediate, 0, 16 * sizeof(float));
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 16 * sizeof(float), velocityIntermediate, 0, 3 * sizeof(float));
                System.Buffer.BlockCopy(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 16 * sizeof(float) + 3 * sizeof(float), accelerationIntermediate, 0, 3 * sizeof(float));
              
                long currentTick = BitConverter.ToInt64(trackingMessage, sizeof(int) + sizeof(byte) + sizeof(uint) + 40 * sizeof(float) + 16 * sizeof(float) + 3 * sizeof(float) + 3 * sizeof(float));

                if(remoteTime == 0) {
                    ownFirstTick = System.DateTime.Now.Ticks;
                    remoteTime = currentTick + ( (long)( ( rtt / 2.0 ) * TimeSpan.TicksPerMillisecond ) );
                    remoteTimeOffset = remoteTime - ownFirstTick;
                }

                Matrix4x4 poseMatrix = new Matrix4x4();
                //column Major - row major switcheroo and right left handed conversion
                poseMatrix[0, 0] = (float)poseMatrixIntermediate[0 * 4 + 0];
                poseMatrix[0, 1] = (float)poseMatrixIntermediate[2 * 4 + 0];
                poseMatrix[0, 2] = (float)poseMatrixIntermediate[1 * 4 + 0];
                poseMatrix[0, 3] = (float)poseMatrixIntermediate[3 * 4 + 0];

                poseMatrix[1, 0] = (float)poseMatrixIntermediate[0 * 4 + 2];
                poseMatrix[1, 1] = (float)poseMatrixIntermediate[2 * 4 + 2];
                poseMatrix[1, 2] = (float)poseMatrixIntermediate[1 * 4 + 2];
                poseMatrix[1, 3] = (float)poseMatrixIntermediate[3 * 4 + 2];

                poseMatrix[2, 0] = (float)poseMatrixIntermediate[0 * 4 + 1];
                poseMatrix[2, 1] = (float)poseMatrixIntermediate[2 * 4 + 1];
                poseMatrix[2, 2] = (float)poseMatrixIntermediate[1 * 4 + 1];
                poseMatrix[2, 3] = (float)poseMatrixIntermediate[3 * 4 + 1];

                poseMatrix[3, 0] = (float)poseMatrixIntermediate[0 * 4 + 3];
                poseMatrix[3, 1] = (float)poseMatrixIntermediate[2 * 4 + 3];
                poseMatrix[3, 2] = (float)poseMatrixIntermediate[1 * 4 + 3];
                poseMatrix[3, 3] = (float)poseMatrixIntermediate[3 * 4 + 3];

                // Alex magic ausgeklammert
                //poseMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 180)) * poseMatrix;

                Vector3 pos = new Vector3(poseMatrix[0, 3] / 1000.0f, poseMatrix[1, 3] / 1000.0f, poseMatrix[2, 3] / 1000.0f);
                Quaternion rotation = poseMatrix.rotation;

                Vector3 velocity = new Vector3(velocityIntermediate[0], velocityIntermediate[1], velocityIntermediate[2]);
                Vector3 acceleration = new Vector3(accelerationIntermediate[0], accelerationIntermediate[1], accelerationIntermediate[2]);

                glove.apply_packet(jointValues);


                /*
                Ich habe keine trackerName, also direkt den glove updaten
                if(trackedPoints.ContainsKey(trackerName)) {
                    trackedPoints[trackerName].position = pos;
                    trackedPoints[trackerName].rotation = rotation;
                    trackedPoints[trackerName].buttonPress = (TrackedObject.ButtonState)buttonPress;
                    trackedPoints[trackerName].quality = quality;
                    trackedPoints[trackerName].timestamp = ( (double)( currentTick - ownFirstTick ) / 1000.0f ) / System.TimeSpan.TicksPerMillisecond;
                }
                else {
                    TrackingData temp = new TrackingData();
                    temp.position = pos;
                    temp.rotation = rotation;
                    temp.buttonPress = (TrackedObject.ButtonState)buttonPress;
                    temp.quality = quality;
                    temp.timestamp = ( (double)( currentTick - ownFirstTick ) / 1000.0f ) / System.TimeSpan.TicksPerMillisecond;
                    trackedPoints.Add(trackerName, temp);
                }

                */

                prevSEQ = seq;
            }
        }

        /*
        else if(type == 2) {
            //This is tracking data in binary format, multiple poses in one message
            // data format = length (int) | Type (byte) | SEQ (uint) | number of poses (byte) | 
            // plus multiple of:
            // pose length(int) | trackerName(variable) | matrix(4 * 4 * double) | buttonPress (int) | quality(double) | time(long)
            int length = BitConverter.ToInt32(trackingMessage, 0);
            if(trackingMessage.Length != length) {
                Debug.Log("Malformed Packet");
                return;
            }
            if(trackingMessage.Length <= 162) {
                Debug.Log("Strange message length");
                return;
            }

            uint seq = BitConverter.ToUInt32(trackingMessage, sizeof(int) + sizeof(byte));
            if(seq > prevSEQ || ( seq < 10000 && prevSEQ > UInt32.MaxValue * 0.75 )) { //tracking data is newer than what we already have
                byte numberOfPoses = trackingMessage[sizeof(int) + sizeof(byte) + sizeof(uint)];
                int offset = 0;
                double [] matrix;
                int trackerNameByteLength;
                string trackerName;
                int buttonPress = 0;
                double quality = 0.0f;
                long currentTick = 0;
                for(int i = 0; i < numberOfPoses; i++) {
                    matrix = new double[16];
                    trackerNameByteLength = BitConverter.ToInt32(trackingMessage, offset + sizeof(int) + sizeof(byte) + sizeof(uint) + sizeof(byte)) - 16 * sizeof(double) - sizeof(double) - sizeof(int) - sizeof(long);
                    trackerName = Encoding.UTF8.GetString(trackingMessage, offset + sizeof(int) + sizeof(byte) + sizeof(uint) + sizeof(byte) + sizeof(int), trackerNameByteLength);
                    System.Buffer.BlockCopy(trackingMessage, offset + sizeof(int) + sizeof(byte) + sizeof(uint) + sizeof(byte) + sizeof(int) + trackerNameByteLength, matrix, 0, 16 * sizeof(double));
                    buttonPress = BitConverter.ToInt32(trackingMessage, offset + sizeof(int) + sizeof(byte) + sizeof(uint) + sizeof(byte) + sizeof(int) + trackerNameByteLength + 16 * sizeof(double));
                    quality = BitConverter.ToDouble(trackingMessage, offset + sizeof(int) + sizeof(byte) + sizeof(uint) + sizeof(byte) + sizeof(int) + trackerNameByteLength + 16 * sizeof(double) + sizeof(int));
                    currentTick = BitConverter.ToInt64(trackingMessage, offset + sizeof(int) + sizeof(byte) + sizeof(uint) + sizeof(byte) + sizeof(int) + trackerNameByteLength + 16 * sizeof(double) + sizeof(int) + sizeof(long));
                    if(remoteTime == 0) {
                        ownFirstTick = System.DateTime.Now.Ticks;
                        remoteTime = currentTick + ( (long)( ( rtt / 2.0 ) * TimeSpan.TicksPerMillisecond ) );
                        remoteTimeOffset = remoteTime - ownFirstTick;
                    }


                    Matrix4x4 transformationMatrix = new Matrix4x4();
                    //column Major - row major switcheroo and right left handed conversion
                    transformationMatrix[0, 0] = (float)matrix[0 * 4 + 0];
                    transformationMatrix[0, 1] = (float)matrix[2 * 4 + 0];
                    transformationMatrix[0, 2] = (float)matrix[1 * 4 + 0];
                    transformationMatrix[0, 3] = (float)matrix[3 * 4 + 0];

                    transformationMatrix[1, 0] = (float)matrix[0 * 4 + 2];
                    transformationMatrix[1, 1] = (float)matrix[2 * 4 + 2];
                    transformationMatrix[1, 2] = (float)matrix[1 * 4 + 2];
                    transformationMatrix[1, 3] = (float)matrix[3 * 4 + 2];

                    transformationMatrix[2, 0] = (float)matrix[0 * 4 + 1];
                    transformationMatrix[2, 1] = (float)matrix[2 * 4 + 1];
                    transformationMatrix[2, 2] = (float)matrix[1 * 4 + 1];
                    transformationMatrix[2, 3] = (float)matrix[3 * 4 + 1];

                    transformationMatrix[3, 0] = (float)matrix[0 * 4 + 3];
                    transformationMatrix[3, 1] = (float)matrix[2 * 4 + 3];
                    transformationMatrix[3, 2] = (float)matrix[1 * 4 + 3];
                    transformationMatrix[3, 3] = (float)matrix[3 * 4 + 3];

                    transformationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 180)) * transformationMatrix;

                    Vector3 pos = new Vector3(transformationMatrix[0, 3] / 1000.0f, transformationMatrix[1, 3] / 1000.0f, transformationMatrix[2, 3] / 1000.0f);
                    Quaternion rotation = transformationMatrix.rotation;

                    if(trackedPoints.ContainsKey(trackerName)) {
                        trackedPoints[trackerName].position = pos;
                        trackedPoints[trackerName].rotation = rotation;
                        trackedPoints[trackerName].buttonPress = (TrackedObject.ButtonState)buttonPress;
                        trackedPoints[trackerName].quality = quality;
                        trackedPoints[trackerName].timestamp = ( (double)( currentTick - ownFirstTick ) / 1000.0f ) / System.TimeSpan.TicksPerMillisecond;
                    }
                    else {
                        TrackingData temp = new TrackingData();
                        temp.position = pos;
                        temp.rotation = rotation;
                        temp.buttonPress = (TrackedObject.ButtonState)buttonPress;
                        temp.quality = quality;
                        temp.timestamp = ( (double)( currentTick - ownFirstTick ) / 1000.0f ) / System.TimeSpan.TicksPerMillisecond;
                        trackedPoints.Add(trackerName, temp);
                    }
                    offset += trackerNameByteLength + 16 * sizeof(double) + sizeof(double) + sizeof(int) + sizeof(long);
                }
                prevSEQ = seq;
            }
           
        } */
    }


    public void sayHello() {
#if WINDOWS_UWP
        //UDPPing
        before = System.DateTime.Now.Ticks;
        sendUDPMessage(Encoding.UTF8.GetBytes("UDPPing"));
#else
#if !UNITY_ANDROID
        System.Net.IPAddress ip = readIPAddressAndPortFromFile ? System.Net.IPAddress.Parse(IPAddressFromFile) : System.Net.IPAddress.Parse(IPAddress);
        System.Net.NetworkInformation.Ping ping = new System.Net.NetworkInformation.Ping();
        try {
            System.Net.NetworkInformation.PingReply pingreply = ping.Send(ip);
            Debug.Log("Sending ping");
            Debug.Log(pingreply.Status);
            if(pingreply.Status == System.Net.NetworkInformation.IPStatus.Success) {
                Debug.Log(string.Format("Address: {0}", pingreply.Address));
                Debug.Log(string.Format("status: {0}", pingreply.Status));
                Debug.Log(string.Format("Round trip time: {0}", pingreply.RoundtripTime));
                rtt = pingreply.RoundtripTime;
            }
        }
        catch(System.Net.NetworkInformation.PingException ex) {
            Debug.Log(ex);
        }
#endif
#endif

        byte[] data = Encoding.UTF8.GetBytes("Hello");
        sendUDPMessage(data); 
    }

    public void Start() {
        if(readIPAddressAndPortFromFile) {
            ReadIPAddress();
        }

        glove = new Glove();

        initUDPReceiver();
    }

    void Update() {
        if(!initialized) {
            ReadIPAddress();
            initUDPReceiver();
        }
    }
    
    void OnGUI() {
        if(showDebug) {
            /*
            Rect rectObj = new Rect(40, 10, 200, 400);
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            GUI.Box(rectObj, "# UDPReceive\n " + IPAddress + "  port" + port + " #\n"
                        + "\nLast Packet: \n" + lastReceivedUDPString
                    , style);
                    */


            GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
            labelStyle.fontSize = 30;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.GetStyle("button"));
            buttonStyle.fontSize = 30;

            Rect rectObj = new Rect(40, 380, 200, 400);
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.UpperLeft;
            if (!connected)
                GUI.Box(new Rect(100, 100, 800, 500), "Not connected to Server", labelStyle);
            else
                GUI.Box(new Rect(100, 100, 800, 500), "Getting Data from " + IPAddress + "\non Port " + port, labelStyle);

            // ------------------------
            // connect to host
            // ------------------------
            if (!autoConnect)
                if (GUI.Button(new Rect(100, 300, 300, 100), "send Ping", buttonStyle)) { 
                    //UDPPing
                    before = System.DateTime.Now.Ticks;
                    sendUDPMessage(Encoding.UTF8.GetBytes("UDPPing"));
        }
        initialized = true;
    }
    }
}