using System;
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
    public enum Gesture {None, Clap};

    // Data Format: uint16_t cnt || uint16_t version/svn_revision || int16_t acceleration[3] || int16_t gyro[3] || uint32_t timestamp || uint32_t temperature;
    UInt16 cnt;
    UInt16 version;
    Vector3 acceleration;
    Vector3 gyroscope;
    UInt32 timestamp;
    Quaternion orientation;
    Gesture gesture;

    public IMUPacket(UInt16 cnt, UInt16 version, Vector3 acceleration, Vector3 gyroscope, UInt32 timestamp, Quaternion orientation, Gesture gesture)
    {
        this.cnt = cnt;
        this.version = version;
        this.acceleration = acceleration;
        this.gyroscope = gyroscope;
        this.timestamp = timestamp;
        this.orientation = orientation;
        this.gesture = gesture;
    }

    public IMUPacket()
    {
    }

    public Vector3 GetGyro()
    {
        return gyroscope;
    }

    public Vector3 GetAcceleration()
    {
        return acceleration;
    }

    public Quaternion GetOrientation()
    {
        return orientation;
    }    

    public UInt32 GetTimestamp()
    {
        return timestamp;
    }

    public Gesture GetGesture()
    {
        return gesture;
    }
}
public class TrackingData
{
    public float[] JointValues;
    public long timestamp;
    public Quaternion orientation;
    public IMUPacket.Gesture gesture;
    public Vector3 accel;
    public Vector3 gyro;

    // Mock
    public TrackingData()
    {
        JointValues = new float[40];

        for (int i = 0; i < JointValues.Length; i++)
            JointValues[i] = i;

        orientation = Quaternion.identity;

        timestamp = 1;
    }

    public TrackingData(float[] JointValues, Quaternion orientation, long timestamp, IMUPacket.Gesture gesture, Vector3 accel, Vector3 gyro)
    {
        this.JointValues = JointValues;
        this.orientation = orientation;
        this.timestamp = timestamp;
        this.gesture = gesture;
        this.accel = accel;
        this.gyro = gyro;
    }
}
