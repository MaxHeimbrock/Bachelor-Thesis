using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EthernetGloveController : MonoBehaviour, GloveConnectionInterface
{
    //public Glove glove;
    public const UInt16 NB_VALUES_GLOVE = 40;

    // "connection" things for Ping
    IPEndPoint remoteEndPointPing;
    UdpClient pingClient;
    public static string gloveIP = "192.168.131.59";
    public static int pingPort = 11159;
    Boolean autoconnect = true;

    // "connection" things for receiving
    IPEndPoint valuesRemoteEndPoint;
    IPEndPoint IMURemoteEndPoint;
    UdpClient valuesClient;
    UdpClient IMUClient;
    Boolean connected = false;
    public static string myIP = "192.168.131.1";
    public static int valuesPort = 64059; //65259 for IMU
    public static int IMUPort = 64159;
    
    StringBuilder sb = new StringBuilder();
    logging logStatus = logging.noLogging;

    enum logging {noLogging, logStarted, logfinished}
    long last_timestamp_microseconds;
    float elapsed_seconds_glove = 0;

    private readonly object imuLock = new object();
    private readonly object gloveLock = new object();
    private EthernetIMUPacket imu_packet;
    private EthernetGlovePacket glove_packet;

    public class EthernetGlovePacket
    {
        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        UInt16 cnt;
        UInt16 version;
        UInt32[] values;

        public EthernetGlovePacket(UInt16 cnt, UInt16 version, UInt32[] values)
        {
            this.cnt = cnt;
            this.version = version;
            this.values = values;
        }
    }

    public class EthernetIMUPacket
    {
        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;
        UInt16 cnt;
        UInt16 version;
        Int16[] acceleration;
        Int16[] gyroscope;
        UInt32 timestamp;

        public EthernetIMUPacket(UInt16 cnt, UInt16 version, Int16[] acceleration, Int16[] gyroscope, UInt32 timestamp)
        {
            this.cnt = cnt;
            this.version = version;
            this.acceleration = acceleration;
            this.gyroscope = gyroscope;
            this.timestamp = timestamp;
        }
    }    

    // Use this for initialization
    void Start()
    {
        // Testing
        initUDPReceiverValues();

        initUDPReceiverIMU();
    }

    // Update is called once per frame
    void Update()
    {
        if (autoconnect && connected == false)
            ping();        

        // IMU logging - start with 'k' - finish with 'l'
        if (Input.GetKey("k") && logStatus == logging.noLogging)
            logStatus = logging.logStarted;

        else if (Input.GetKey("l") && logStatus == logging.logStarted)
        {
            logStatus = logging.logfinished;
            WriteIMUToFile();
        }

        UpdateTimerUI();
    }

    public void ping()
    {
        remoteEndPointPing = new IPEndPoint(IPAddress.Parse(gloveIP), pingPort);
        pingClient = new UdpClient();
        //Debug.Log("Ping Glove on " + gloveIP + " : " + pingPort);
        sendPing();
    }

    public void initUDPReceiverValues()
    {
        valuesRemoteEndPoint = new IPEndPoint(IPAddress.Parse(myIP), valuesPort);
        valuesClient = new UdpClient(valuesRemoteEndPoint);

        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
    }

    public void initUDPReceiverIMU()
    {
        IMURemoteEndPoint = new IPEndPoint(IPAddress.Parse(myIP), IMUPort);
        IMUClient = new UdpClient(IMURemoteEndPoint);

        IMUClient.BeginReceive(new AsyncCallback(recvIMU), null);
    }

    //CallBack for joint values
    private void recvValues(IAsyncResult res)
    {
        byte[] data = valuesClient.EndReceive(res, ref valuesRemoteEndPoint);
        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
        //Debug.Log("Received Value package from glove");

        UInt16 cnt;
        UInt16 version;
        UInt32[] jointValues = new UInt32[40];

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        cnt = BitConverter.ToUInt16(data, 0);
        version = BitConverter.ToUInt16(data, sizeof(UInt16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), jointValues, 0, 40 * sizeof(UInt32));

        //Debug.Log(version);

        lock (gloveLock)
            glove_packet = new EthernetGlovePacket(cnt, version, jointValues);

        // Apply joint values to glove object
        //glove.apply_ethernetJointPacket(jointValues);

        // If first packet
        if (connected == false)
        {
            Debug.Log("Glove is active");
            connected = true;
        }
    }

    //CallBack for IMU
    private void recvIMU(IAsyncResult res)
    {
        byte[] data = IMUClient.EndReceive(res, ref IMURemoteEndPoint);
        IMUClient.BeginReceive(new AsyncCallback(recvIMU), null);
        //Debug.Log("Received IMU package from glove");
        connected = true;

        UInt16 cnt;

        UInt16 version;

        Int16[] acc = new Int16[3];
        Vector3 accVec;

        Int16[] gyro = new Int16[3];
        Vector3 gyroVec;

        UInt32 timestamp_in_ticks;

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;
        cnt = BitConverter.ToUInt16(data, 0);
        version = BitConverter.ToUInt16(data, sizeof(UInt16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), acc, 0, 3 * sizeof(Int16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16), gyro, 0, 3 * sizeof(Int16));
        timestamp_in_ticks = BitConverter.ToUInt32(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16) + 3 * sizeof(Int16));
        
        accVec = new Vector3(acc[0], acc[1], acc[2]);
        gyroVec = new Vector3(gyro[0], gyro[1], gyro[2]);

        if (logStatus == logging.logStarted)
        {
            elapsed_seconds_glove = GetTime((int)timestamp_in_ticks);
        }       

        // make stringbuilder threadsafe, since method is async
        if (logStatus == logging.logStarted)
        {
            lock (sb)
            {
                LogIMU((int)elapsed_seconds_glove, accVec, gyroVec);
            }
        }

        lock (imuLock)
        {
            imu_packet = new EthernetIMUPacket(cnt, version, acc, gyro, timestamp_in_ticks);
        }

        //glove.applyEthernetPacketIMU(accVec, gyroVec, timestamp_in_ticks);

        // 2000 degrees/sec default settings
        // max acceleration 2g 
    }

    // sendPing
    private void sendPing()
    {
        // Daten mit der UTF8-Kodierung in das Binärformat kodieren.
        byte[] message = Encoding.UTF8.GetBytes("Ping");

        // den Ping zum Remote-Client senden.
        pingClient.Send(message, message.Length, remoteEndPointPing);
    }

    // OnGUI
    void OnGUI()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.GetStyle("label"));
        labelStyle.fontSize = 40;

        if (!connected)
            GUI.Box(new Rect(100, 200, 800, 500), "Glove offline", labelStyle);
        else
            GUI.Box(new Rect(100, 200, 500, 500), "Glove active - getting data", labelStyle);

        if (logStatus == logging.logStarted)
        {
            GUI.Box(new Rect(100, 300, 500, 500), "seconds: " + secondsCountRounded, labelStyle);
            GUI.Box(new Rect(100, 350, 500, 500), "rep: " + repCount, labelStyle);

            if ((int)secondsCount >= 40)
                GUI.Box(new Rect(100, 400, 500, 500), "TURN", labelStyle);
        }
    }

    public void LogIMU (int timestamp, Vector3 accVec, Vector3 gyroVec)
    {
        float G_Gain = 0.07f;
        float Accel_Factor = 16384.0f;

        gyroVec *= G_Gain;
        accVec /= Accel_Factor;

        sb.Append(timestamp + ", ");
        sb.Append(accVec.x + ", " + accVec.y + ", " + accVec.z + ", ");
        sb.Append(gyroVec.x + ", " + gyroVec.y + ", " + gyroVec.z);
        sb.Append("\n");
    }

    public void WriteIMUToFile()
    {
        Debug.Log("IMU_Log");

        System.IO.File.WriteAllText("C:\\Users\\Max\\Documents\\GitHub\\Bachelor-Thesis\\Glove_Server_App\\IMU_Calibration\\IMU_log.txt", sb.ToString());
    }
    
    // initialized to discard first two seconds
    private float secondsCount = 40;
    private int secondsCountRounded;
    private int repCount = -1;

    public void UpdateTimerUI()
    {
        //set timer UI
        secondsCount += Time.deltaTime;
        secondsCountRounded = (int)secondsCount;
        if (secondsCount >= 42)
        {
            repCount++;
            secondsCount = 0;
        }
    }

    private float GetTime(int timestamp_in_ticks)
    {

        long this_timestamp_microseconds = timestamp_in_ticks * 39;

        long delta_time_microseconds = 0;

        if (last_timestamp_microseconds != 0)
            delta_time_microseconds = this_timestamp_microseconds - last_timestamp_microseconds;

        float delta_time_seconds = ((float)delta_time_microseconds / (1000 * 1000));

        if (delta_time_seconds < 0)
            delta_time_seconds = 0.0033f;

        elapsed_seconds_glove += delta_time_seconds;

        // this is it - just get delta time
        //Debug.Log(elapsed_seconds_glove);

        last_timestamp_microseconds = this_timestamp_microseconds;

        return delta_time_seconds;
    }

    public GloveConnector.ValuePacket GetValuePacket()
    {
        throw new NotImplementedException();
    }

    public GloveConnector.IMUPacket GetIMUPacket()
    {
        throw new NotImplementedException();
    }
}
