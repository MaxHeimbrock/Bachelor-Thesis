using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class WifiGloveConnection : MonoBehaviour
{
    //public Glove glove;   

    // "connection" things for receiving
    IPEndPoint valuesRemoteEndPoint;
    IPEndPoint IMURemoteEndPoint;
    UdpClient valuesClient;
    UdpClient IMUClient;
    Boolean connected = false;
    public static string myIP = "192.168.137.1";
    public static int valuesPort = 64000; //65259 for IMU
    public static int IMUPort = 64200;
    public static int IMUPort2 = 64400;

    private Int16 imu_cnt = 0;
    private long time0 = DateTime.Now.Ticks;
    int timestamp0 = -1;
    int elapsedTime = 0;
    StringBuilder sb = new StringBuilder();
    logging logStatus = logging.noLogging;

    enum logging {noLogging, logStarted, logfinished}

    // Use this for initialization
    void Start()
    {
        //glove = new Glove();

        // Testing
        initUDPReceiverValues();

        initUDPReceiverIMU();
    }

    // Update is called once per frame
    void Update()
    {
        // von mir hier hin verschoben
        if (Input.GetKey("space"))
            //glove.set_zero();

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

    public void initUDPReceiverValues()
    {
        valuesRemoteEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), valuesPort);
        valuesClient = new UdpClient(valuesRemoteEndPoint);

        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
    }

    public void initUDPReceiverIMU()
    {
        IMURemoteEndPoint = new IPEndPoint(IPAddress.Parse("0.0.0.0"), IMUPort);
        IMUClient = new UdpClient(IMURemoteEndPoint);

        IMUClient.BeginReceive(new AsyncCallback(recvIMU), null);
    }

    //CallBack for joint values
    private void recvValues(IAsyncResult res)
    {
        byte[] data = valuesClient.EndReceive(res, ref valuesRemoteEndPoint);
        valuesClient.BeginReceive(new AsyncCallback(recvValues), null);
        Debug.Log("Received Value package from glove");

        UInt32[] jointValues = new UInt32[40];

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), jointValues, 0, 40 * sizeof(UInt32));

        //Debug.Log(jointValues[1]);

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
        Debug.Log("Received IMU package from glove");
        connected = true;

        Int16[] acc = new Int16[3];
        Vector3 accVec;

        Int16[] gyro = new Int16[3];
        Vector3 gyroVec;

        // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16), acc, 0, 3 * sizeof(Int16));
        System.Buffer.BlockCopy(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16), gyro, 0, 3 * sizeof(Int16));
        int timestamp = BitConverter.ToInt32(data, sizeof(UInt16) + sizeof(UInt16) + 3 * sizeof(Int16) + 3 * sizeof(Int16));

        /*
        if (imu_cnt == 10000)
        {
            TimeSpan elapsedSpan = new TimeSpan(DateTime.Now.Ticks - time0);
            long delta_t_ms = elapsedSpan.Milliseconds;
            long delta_t_s = elapsedSpan.Seconds;
            delta_t_ms += delta_t_s * 1000;
            Debug.Log(delta_t_ms);
            Debug.Log(timestamp - timestamp0);
        }
        */

        accVec = new Vector3(acc[0], acc[1], acc[2]);
        gyroVec = new Vector3(gyro[0], gyro[1], gyro[2]);

        if (logStatus == logging.logStarted)
        {
            int delta_timestamp = timestamp - timestamp0;

            if (timestamp0 == -1)
            {
                timestamp0 = timestamp;
                delta_timestamp = 0;
                elapsedTime = 0;
            }

            timestamp0 = timestamp;

            elapsedTime += delta_timestamp;
        }

        if (elapsedTime < 0)
            elapsedTime = 85;

        // TODO setter
        //glove.timestamp1 = timestamp;
        imu_cnt++;

        // make stringbuilder threadsafe, since method is async
        if (logStatus == logging.logStarted)
        {
            lock (sb)
            {
                LogIMU(elapsedTime, accVec, gyroVec);
            }
        }

        //glove.applyEthernetPacketIMU(accVec, gyroVec, timestamp);

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

}
