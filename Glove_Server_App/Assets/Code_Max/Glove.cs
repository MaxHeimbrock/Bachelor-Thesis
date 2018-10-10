using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    }

    public void applyEthernetPacket(float[] newValues)
    {
        cnt++;

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            raw_values[i] = (Int64)(raw_values[i] + newValues[i]);
        }

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            values[i] = 0.001f * (raw_values[i] - offsets[i]);
        }
    }

    public TrackingData GetTrackingData()
    {
        Vector3 vel = new Vector3(1, 2, 3);
        Vector3 acc = new Vector3(2, 4, 6);
        Matrix4x4 pose = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

        return new TrackingData(values, pose, vel, acc, 2.0);
    }

    }

static class Constants
{
    public const int NB_SENSORS = 40;
    public const bool IS_BLUETOOTH = false;
}

public class TrackingData
{

    public float[] JointValues;
    public Matrix4x4 pose;
    public Vector3 velocity;
    public Vector3 acceleration;
    public double timestamp;

    // Dummy for testing
    public TrackingData()
    {
        JointValues = new float[40];

        for (int i = 0; i < JointValues.Length; i++)
            JointValues[i] = i;

        pose = Matrix4x4.identity;

        velocity = new Vector3(1, 1, 1);
        acceleration = new Vector3(2, 3, 4);

        timestamp = 1;
    }

    // Dummy for testing
    public TrackingData(float[] values)
    {
        JointValues = values;

        pose = Matrix4x4.identity;

        velocity = new Vector3(1, 1, 1);
        acceleration = new Vector3(2, 3, 4);

        timestamp = 1;
    }

    public TrackingData(float[] JointValues, Matrix4x4 pose, Vector3 velocity, Vector3 acceleration, double timestamp)
    {
        this.JointValues = JointValues;
        this.pose = pose;
        this.velocity = velocity;
        this.acceleration = acceleration;
        this.timestamp = timestamp;
    }

    public TrackingData Copy()
    {
        return (TrackingData)this.MemberwiseClone();
    }

}