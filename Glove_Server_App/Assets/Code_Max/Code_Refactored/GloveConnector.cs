using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveConnector : MonoBehaviour {

    public enum GloveVersion {USB_Glove, Ethernet_Glove, Wifi_Glove};
    public GloveVersion gloveVersion = GloveVersion.Ethernet_Glove;
    private GloveConnectionInterface gloveConnectionInterface;
    bool connected = false;

    public enum IMU_FilterType {Madgwick, Mahony, Gyroscope, Accelerometer}
    public IMU_FilterType IMU_filterType = IMU_FilterType.Mahony;
    private IMU_Processor IMU_processor;

    // Use this for initialization
    void Awake () {

        switch (IMU_filterType)
        {
            // Mahony-Filter with default Preprocessor 
            case IMU_FilterType.Mahony:
                IMU_processor = new MahonyProcessorNoMagnet(new IMU_Preprocessor());
                break;

            // Madgwick-Filter with default Preprocessor 
            case IMU_FilterType.Madgwick:
                IMU_processor = new MadgwickProcessorNoMagnet(new IMU_Preprocessor());
                break;

            // GyroscopeProcessor with default Preprocessor 
            case IMU_FilterType.Gyroscope:
                IMU_processor = new GyroscopeProcessor(new IMU_Preprocessor());
                break;

            // AccelerometerProcessor with default Preprocessor 
            case IMU_FilterType.Accelerometer:
                IMU_processor = new AccelerometerProcessor(new IMU_Preprocessor());
                break;
        }

		switch (gloveVersion)
        {
            case GloveVersion.Wifi_Glove:
                gloveConnectionInterface = new WifiGloveConnection(IMU_processor);
                break;

            case GloveVersion.Ethernet_Glove:
                gloveConnectionInterface = new EthernetGloveController(IMU_processor);
                break;

            case GloveVersion.USB_Glove:
                break;
        }
	}

    void Update()
    {
        if (connected == false)
            gloveConnectionInterface.CheckGloveConnection(out connected);

        if (Input.GetKey("space"))
            gloveConnectionInterface.SetZero();

        //Debug.Log(gloveConnectionInterface.GetIMUPacket().GetGesture());
    }

    public float[] GetAngles()
    {
        if (connected)
            return gloveConnectionInterface.GetValuePacket().GetAngles();
        else
            return new float[40];
    }

    public Vector3 GetGyro()
    {    
        return gloveConnectionInterface.GetIMUPacket().GetGyro();
    }

    public Vector3 GetAcceleration()
    {
        return gloveConnectionInterface.GetIMUPacket().GetAcceleration();
    }

    public Quaternion GetOrientation()
    {
        if (connected)
            return gloveConnectionInterface.GetIMUPacket().GetOrientation();
        else
            return Quaternion.identity;
    }

    public TrackingData getTrackingData()
    {
        if (connected)
            return new TrackingData(GetAngles(), GetOrientation(), gloveConnectionInterface.GetIMUPacket().GetTimestamp(), gloveConnectionInterface.GetIMUPacket().GetGesture());
        throw new NotImplementedException();
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
    }
}


