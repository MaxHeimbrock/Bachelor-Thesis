﻿using System;
using UnityEngine;

public interface GloveConnectionInterface {    

    ValuePacket GetValuePacket();

    IMUPacket GetIMUPacket();

    void SetZero();

    void CheckGloveConnection(out bool connected);
}

public class ValuePacket
{
    // Data Format: uint16_t cnt || uint16_t version/svn_revision || uint32_t values[NB_VALUES_GLOVE]
    UInt16 cnt;
    UInt16 version;
    UInt32[] values;
    float[] angles;

    public ValuePacket()
    {
    }

    public ValuePacket(UInt16 cnt, UInt16 version, UInt32[] values, float[] angles)
    {
        this.cnt = cnt;
        this.version = version;
        this.values = values;
        this.angles = angles;
    }

    public float[] GetAngles()
    {
        return angles;
    }
}
public class IMUPacket
{
    // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;
    UInt16 cnt;
    UInt16 version;
    Int16[] acceleration;
    Int16[] gyroscope;
    UInt32 timestamp;
    Quaternion orientation;

    public IMUPacket(UInt16 cnt, UInt16 version, Int16[] acceleration, Int16[] gyroscope, UInt32 timestamp, Quaternion orientation)
    {
        this.cnt = cnt;
        this.version = version;
        this.acceleration = acceleration;
        this.gyroscope = gyroscope;
        this.timestamp = timestamp;
        this.orientation = orientation;
    }

    public IMUPacket()
    {
    }

    public Vector3 GetGyro()
    {
        return new Vector3(gyroscope[0], gyroscope[1], gyroscope[2]);
    }

    public Vector3 GetAcceleration()
    {
        return new Vector3(acceleration[0], acceleration[1], acceleration[2]);
    }

    public Quaternion GetOrientation()
    {
        return orientation;
    }
}
