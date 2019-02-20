using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class EthernetGloveController : GloveConnectionInterface
{
    AngleProcessor angleProcessor;
    IMU_Processor IMU_processor;

    // "connection" things for Ping
    IPEndPoint remoteEndPointPing;
    UdpClient pingClient;
    public static string gloveIP = "192.168.131.59";
    public static int pingPort = 11159;

    // "connection" things for receiving
    IPEndPoint valuesRemoteEndPoint;
    IPEndPoint IMURemoteEndPoint;
    UdpClient valuesClient;
    UdpClient IMUClient;
    Boolean connected = false;
    public static string myIP = "192.168.131.1";
    public static int valuesPort = 64059; 
    public static int IMUPort = 64159;
    
    StringBuilder sb = new StringBuilder();
    logging logStatus = logging.noLogging;

    enum logging {noLogging, logStarted, logfinished}
    long last_timestamp_microseconds;
    float elapsed_seconds_glove = 0;

    private readonly object imuLock = new object();
    private readonly object gloveLock = new object();
    private IMUPacket imu_packet;
    private ValuePacket glove_packet;

    public EthernetGloveController(IMU_Processor IMU_processor)
    {
        Debug.Log("Ethernet Glove Controller started");

        imu_packet = new IMUPacket();
        glove_packet = new ValuePacket();

        angleProcessor = new EthernetAngleProcessor();
        this.IMU_processor = IMU_processor;

        initUDPReceiverValues();
        initUDPReceiverIMU();
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
        //--------------------------------------------------------------------------------------------------------------------
        //----------- RECEIVING DATA -----------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        byte[] data = valuesClient.EndReceive(res, ref valuesRemoteEndPoint);
        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
        //Debug.Log("Received Value package from glove");

        //--------------------------------------------------------------------------------------------------------------------
        //----------- COPYING DATA -------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        UInt16 cnt;
        UInt16 version;
        UInt32[] jointValues = new UInt32[40];

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        cnt = BitConverter.ToUInt16(data, 0);
        version = BitConverter.ToUInt16(data, sizeof(UInt16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), jointValues, 0, 40 * sizeof(UInt32));

        //Debug.Log(version);

        //--------------------------------------------------------------------------------------------------------------------
        //----------- COMPUTING DATA -----------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        lock (gloveLock)
        {
            angleProcessor.ProcessAngles(jointValues);

            glove_packet = new ValuePacket(cnt, version, jointValues, angleProcessor.GetAngles());
        }

        //--------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

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
        //--------------------------------------------------------------------------------------------------------------------
        //----------- RECEIVING DATA -----------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        byte[] data = IMUClient.EndReceive(res, ref IMURemoteEndPoint);
        IMUClient.BeginReceive(new AsyncCallback(recvIMU), null);
        //Debug.Log("Received IMU package from glove");
        
        // If first packet
        if (connected == false)
        {
            Debug.Log("Glove is active");
            connected = true;
        }

        //--------------------------------------------------------------------------------------------------------------------
        //----------- COPYING DATA -------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        Stream dataStream = new MemoryStream(data);
        BinaryReader binaryReader = new BinaryReader(dataStream);

        // in Ethernet: accel x = -z in real | accel y = -x in real | accel z = y in real - real for me left handed like unity
        // same for gyro

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;

        UInt16 cnt = binaryReader.ReadUInt16();
        UInt16 version = binaryReader.ReadUInt16(); 
        Vector3 accVec = new Vector3();
        accVec.x = binaryReader.ReadInt16();
        accVec.y = binaryReader.ReadInt16();
        accVec.z = binaryReader.ReadInt16();
        Vector3 gyroVec = new Vector3();
        gyroVec.x = binaryReader.ReadInt16();
        gyroVec.y = binaryReader.ReadInt16();
        gyroVec.z = binaryReader.ReadInt16();
        UInt32 timestamp_in_ticks = binaryReader.ReadUInt32();
        float delta_t_s = GetTime((int)timestamp_in_ticks);
        
        /*
        accVec.z = -binaryReader.ReadInt16();
        accVec.x = -binaryReader.ReadInt16();
        accVec.y = binaryReader.ReadInt16();
        Vector3 gyroVec = new Vector3();
        gyroVec.z = -binaryReader.ReadInt16();
        gyroVec.x = -binaryReader.ReadInt16();
        gyroVec.y = binaryReader.ReadInt16();
        UInt32 timestamp_in_ticks = binaryReader.ReadUInt32();
        float delta_t_s = GetTime((int)timestamp_in_ticks);
        */
        //--------------------------------------------------------------------------------------------------------------------
        //----------- COMPUTING DATA -----------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        IMUPacket.Gesture gesture = IMUPacket.Gesture.None;
        Quaternion orientation;

        lock (imuLock)
        {
            // Ethernet Glove doesnt have magnetometer - pass zeros - will not be used
            IMU_processor.ProcessIMU(delta_t_s, accVec, gyroVec, Vector3.zero, out orientation, out gesture);

            imu_packet = new IMUPacket(cnt, version, accVec, gyroVec, timestamp_in_ticks, orientation, gesture);
        }
        
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

        last_timestamp_microseconds = this_timestamp_microseconds;

        return delta_time_seconds;
    }

    public ValuePacket GetValuePacket()
    {
        lock (gloveLock)
            return glove_packet;
    }

    public IMUPacket GetIMUPacket()
    {
        lock (imuLock)
            return imu_packet;
    }

    public void SetZero()
    {
        angleProcessor.SetZero();
        IMU_processor.SetZero();
    }

    public void CheckGloveConnection(out bool connected)
    {
        remoteEndPointPing = new IPEndPoint(IPAddress.Parse(gloveIP), pingPort);
        pingClient = new UdpClient();
        Debug.Log("Ping Glove on " + gloveIP + " : " + pingPort);
        sendPing();

        connected = this.connected;

        if (connected)
            SetZero();
    }
}

