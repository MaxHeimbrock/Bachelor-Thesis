using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class WifiGloveConnection : GloveConnectionInterface
{
    AngleProcessor angleProcessor;
    IMU_Processor IMU_processor;

    // "connection" things for receiving
    IPEndPoint valuesRemoteEndPoint;
    IPEndPoint IMURemoteEndPoint;
    UdpClient valuesClient;
    UdpClient IMUClient;
    Boolean connected = false;
    public static string defaultIP = "0.0.0.0";
    public static string myIP = "192.168.137.1";
    public static int valuesPort = 64000; 
    public static int IMUPort = 64200;
    public static int IMUPort2 = 64400;

    public static string myIP_PhoneAccessPoint = "192.168.43.154";
    public static string gloveIP_PhoneAccessPoint = "192.168.43.188";

    StringBuilder sb = new StringBuilder();
    logging logStatus = logging.noLogging;

    enum logging { noLogging, logStarted, logfinished }
    long last_timestamp_microseconds;
    float elapsed_seconds_glove = 0;

    private readonly object imuLock = new object();
    private readonly object gloveLock = new object();
    private IMUPacket imu_packet;
    private ValuePacket glove_packet;

    public WifiGloveConnection(IMU_Processor IMU_processor)
    {
        Debug.Log("Wifi Glove Controller started");

        imu_packet = new IMUPacket();
        glove_packet = new ValuePacket();

        angleProcessor = new EthernetAngleProcessor();
        this.IMU_processor = IMU_processor;

        initUDPReceiverValues();
        initUDPReceiverIMU();
    }

    public void initUDPReceiverValues()
    {
        valuesRemoteEndPoint = new IPEndPoint(IPAddress.Parse(defaultIP), valuesPort);
        valuesClient = new UdpClient(valuesRemoteEndPoint);

        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
    }

    public void initUDPReceiverIMU()
    {
        IMURemoteEndPoint = new IPEndPoint(IPAddress.Parse(defaultIP), IMUPort2);
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
        Debug.Log("Received Value package from glove");

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
        Debug.Log("Received IMU package from glove");

        // If first packet
        if (connected == false)
        {
            Debug.Log("Glove is active");
            connected = true;
        }

        //--------------------------------------------------------------------------------------------------------------------
        //----------- COPYING DATA -------------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        UInt16 cnt;

        UInt16 version;

        Int16[] acc = new Int16[3];
        Vector3 accVec;

        Int16[] gyro = new Int16[3];
        Vector3 gyroVec;

        Int16[] mag = new Int16[3];
        Vector3 magVec;

        UInt32 timestamp_in_ticks;

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || int16 mag[3] || int16 hall || uint32_t timestamp || uint32_t temperature;
        cnt = BitConverter.ToUInt16(data, 0);
        version = BitConverter.ToUInt16(data, sizeof(UInt16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), acc, 0, 3 * sizeof(Int16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16), gyro, 0, 3 * sizeof(Int16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16) + 3 * sizeof(Int16), mag, 0, 3 * sizeof(Int16));
        timestamp_in_ticks = BitConverter.ToUInt32(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16) + 3 * sizeof(Int16) + 3 * sizeof(Int16) + sizeof(Int16));
        float delta_t_s = GetTime((int)timestamp_in_ticks);

        accVec = new Vector3(acc[0], acc[1], acc[2]);
        gyroVec = new Vector3(gyro[0], gyro[1], gyro[2]);
        magVec = new Vector3(mag[0], mag[1], mag[2]);

        //--------------------------------------------------------------------------------------------------------------------
        //----------- COMPUTING DATA -----------------------------------------------------------------------------------------
        //--------------------------------------------------------------------------------------------------------------------

        lock (imuLock)
        {
            // Ethernet Glove doesnt have magnetometer - pass zeros - will not be used
            //Quaternion orientation = IMU_processor.GetOrientation(delta_t_s, accVec, gyroVec, magVec);
            Quaternion orientation = IMU_processor.GetOrientation(delta_t_s, accVec, gyroVec, Vector3.zero);

            imu_packet = new IMUPacket(cnt, version, acc, gyro, timestamp_in_ticks, orientation);
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

    public void LogIMU(int timestamp, Vector3 accVec, Vector3 gyroVec)
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
    }

    public void CheckGloveConnection(out bool connected)
    {
        connected = this.connected;
    }
}
