using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveConnector : MonoBehaviour {

    public enum GloveVersion {USB_Glove, Ethernet_Glove, Wifi_Glove};
    public GloveVersion gloveVersion = GloveVersion.Ethernet_Glove;
    private GloveConnectionInterface gloveConnectionInterface;
    bool connected = false;

    public enum IMU_FilterType { Madgwick, Mahony, Gyro, Accelerometer }
    public IMU_FilterType IMU_filterType = IMU_FilterType.Mahony;
    private IMU_Processor IMU_processor;

    // Use this for initialization
    void Awake () {

        switch (IMU_filterType)
        {
            // Mahony-Filter with default Preprocessor 
            case IMU_FilterType.Mahony:
                IMU_processor = new MahonyProcessor(new IMU_Preprocessor());
                break;
        }

		switch (gloveVersion)
        {
            case GloveVersion.Wifi_Glove:
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
    }

    public float[] GetAngles()
    {
        return gloveConnectionInterface.GetValuePacket().GetAngles();
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
        return gloveConnectionInterface.GetIMUPacket().GetOrientation();
    }
}


