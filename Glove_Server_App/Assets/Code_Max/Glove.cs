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

    // for imu testing
    private Vector3 acceleration;
    private Vector3 velocity;
    public Vector3 position;

    private Vector3 acceleration_bias;

    long time0;

    public Glove()
    {
        cnt = 0;
        version = 0;
        raw_values = new Int64[Constants.NB_SENSORS];
        offsets = new Int64[Constants.NB_SENSORS];
        values = new float[Constants.NB_SENSORS];

        acceleration = new Vector3(0, 0, 0);
        velocity = new Vector3(0, 0, 0);
        position = new Vector3(0, 0, 5);

        time0 = 0;
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
            raw_values[i] = (Int64)(raw_values[i] + packet.values[i]);

        raw_values[packet.key] = packet.value;

        for (int i = 0; i < Constants.NB_SENSORS; i++)
            values[i] = 0.001f * (raw_values[i] - offsets[i]);
    }

    public void applyEthernetPacketValues(UInt32[] newValues)
    {
        cnt++;

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            //raw_values[i] = (Int64)(raw_values[i] + (UInt32)(newValues[i] / 4000000));
            raw_values[i] = (raw_values[i] + (Int64)newValues[i]);
            //raw_values[i] = (Int64)(raw_values[i] + (UInt16)(newValues[i] / 1000000));
            //raw_values[i] = (Int64)(raw_values[i] + (UInt16)(newValues[i] / 10000000));
            //raw_values[i] = (Int64)(raw_values[i] + newValues[i]);

           // Debug.Log((Int64)newValues[1]);
        }

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            // Wenn values = 0 --> Hand flach
            values[i] = 0.001f * (raw_values[i] - offsets[i]);
        }
    }

    public void applyEthernetPacketIMU(Vector3 acceleration1)
    {
        // testing double integration, just playing around

        long time1 = DateTime.Now.Ticks;
        Vector3 velocity1;
        Vector3 position1;        

        if (time0 != 0 && Math.Abs(acceleration_bias.x) > 1000)
        {
            acceleration1 -= acceleration_bias;

            acceleration1 /= 1000000000;

            TimeSpan elapsedSpan = new TimeSpan(time1 - time0);
            long delta_t = elapsedSpan.Seconds;

            velocity1 = velocity + acceleration + (acceleration1 - acceleration)/2;
            position1 = position + velocity + (velocity1 - velocity) / 2;

            position = position1;
            velocity = velocity1;
            acceleration = acceleration1;
        }
        else
        {
            // trying to get a bias for flat on table
            acceleration_bias = acceleration1;

            Debug.Log(acceleration);
        }
        time0 = time1;
    }

    public TrackingData GetTrackingData()
    {
        // All Dummy Values but acc for testing
        //float[] JointValues = new float[40];

        //for (int i = 0; i < JointValues.Length; i++)
            //JointValues[i] = i;

        Vector3 vel = new Vector3(1, 2, 3);
        //Vector3 acc = new Vector3(2, 4, 6);
        Matrix4x4 pose = new Matrix4x4(new Vector4(1, 0, 0, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 1, 0), new Vector4(0, 0, 0, 1));

        return new TrackingData(values, pose, vel, acceleration, 2.0);
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