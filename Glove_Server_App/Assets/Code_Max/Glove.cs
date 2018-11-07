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

    private UInt32[] offsets;
    private UInt32[] raw_values;
    
    // for imu testing
    private Vector3 acceleration;
    private Vector3 velocity;
    public Vector3 position;
    public Quaternion q;
    public Quaternion q2;
    public Quaternion q3;
    public float AccXangle;
    public float AccYangle;
    public Vector3 rotation;
    public Vector3 rotation_filtered;

    float G_Gain = 0.07f; // to get degrees per second with 2000dps http://ozzmaker.com/berryimu/

    private Vector3 acceleration_bias;
    private Vector3 gyro_bias;
    private int bias_counter = 0;
    private int bias_length = 100;

    long time0;

    public Glove()
    {
        cnt = 0;
        version = 0;
        raw_values = new UInt32[Constants.NB_SENSORS];
        offsets = new UInt32[Constants.NB_SENSORS];
        values = new float[Constants.NB_SENSORS];

        acceleration = new Vector3(0, 0, 0);
        velocity = new Vector3(0, 0, 0);
        position = new Vector3(0, 0, 5);

        time0 = 0;
}

    public void set_zero()
    {
        Debug.Log("set_zero");

        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            offsets[i] = raw_values[i];
        }        
    }

    public void apply_packet(Packet packet)
    {
     //  version = packet.version;
     //  cnt++;
     //
     //  for (int i = 0; i < Constants.NB_SENSORS; i++)
     //      raw_values[i] = (Int64)(raw_values[i] + packet.values[i]);
     //
     //  raw_values[packet.key] = packet.value;
     //
     //  for (int i = 0; i < Constants.NB_SENSORS; i++)
     //      values[i] = 0.001f * (raw_values[i] - offsets[i]);
    }


    public void apply_ethernetJointPacket(UInt32[] newValues)
    {
        cnt++;
 
        float filter = 0.9f;

        raw_values = newValues;
 
        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            Int64 tmp = ((Int64)newValues[i]) - ((Int64)offsets[i]);
            double tmpd = (double)tmp; // I use double here to avoid loosing to much precision
            tmpd = 0.001f * tmpd; // That should be the same scale as for the serial glove
            double filtered_value = (1.0f - filter) * tmpd + filter * values[i];
            values[i] = (float)filtered_value; // finally cut it to float, the precision should be fine at that point
            //Debug.Log(values[1]);
        }
    }

    /* taken from apply packet but didnt work
    public void apply_ethernetJointPacket(UInt32[] newValues)
    {
        cnt++;
                
        for (int i = 0; i < Constants.NB_SENSORS; i++)
        {
            raw_values[i] = (Int64)(raw_values[i] + newValues[i]);
            values[i] = 0.001f * (raw_values[i] - offsets[i]);
        }
    }
    */

    public void applyEthernetPacketIMU(Vector3 acceleration1, Vector3 gyroscope)
    {
        // testing double integration, just playing around

        long time1 = DateTime.Now.Ticks;
        Vector3 velocity1;
        Vector3 position1;

        if (bias_counter > bias_length)
        {
            acceleration1 -= acceleration_bias;
            gyroscope -= gyro_bias;

            acceleration1 /= 16384;
            
            TimeSpan elapsedSpan = new TimeSpan(time1 - time0);
            long delta_t_ms = elapsedSpan.Milliseconds;
            float delta_t_s = delta_t_ms / 1000f;
            
            //velocity1 = velocity + acceleration + (acceleration1 - acceleration) / 2;
            //position1 = position + velocity + (velocity1 - velocity) / 2;

            velocity1 = velocity + acceleration1 * delta_t_s;
            position1 = position + velocity1 * delta_t_s;

            position = position1;
            velocity = velocity1;
            acceleration = acceleration1;

            // Orientation Tests

            // X-axis
            AccXangle = (float)((Math.Atan2(acceleration1.x, acceleration1.z) + Math.PI) * (180/Math.PI)); // andere Rechnung http://ozzmaker.com/berryimu/

            // diese Rechnung korrigiert Orientierung zu -180 bis 180 grad
            if (AccXangle > 180)
                AccXangle -= (float)360;

            // Y-axis
            AccYangle = (float)((Math.Atan2(acceleration1.y, acceleration1.z) + Math.PI) * (180 / Math.PI)); // andere Rechnung
            
            // diese Rechnung korrigiert Orientierung zu -180 bis 180 grad
            if (AccYangle > 180)
                AccYangle -= (float)360;            
            
            rotation += gyroscope * delta_t_s * G_Gain;

            q = Quaternion.Euler(AccXangle, 0, -AccYangle);
            q2 = Quaternion.Euler(-rotation.y, rotation.z, -rotation.x);

            // complementary filter
            float filter = 0.98f;

            //rotation_filtered.x = filter * (rotation_filtered.x + -gyroscope.y * delta_t_s * G_Gain) + (1 - filter) * AccXangle;
            //rotation_filtered.z = filter * (rotation_filtered.z + -gyroscope.x * delta_t_s * G_Gain) + (1 - filter) * -AccYangle;
            //rotation_filtered.y = gyroscope.z * delta_t_s * G_Gain;

            //rotation.x = filter * rotation.x + (1 - filter) * AccXangle;
            //rotation.y = filter * rotation.y + (1 - filter) * AccYangle;

            // in one equation
            //rotation.x = filter * (rotation.x + gyroscope.x * delta_t_s * G_Gain) + (1 - filter) * AccXangle;
            //rotation.y = filter * (rotation.y + gyroscope.y * delta_t_s * G_Gain) + (1 - filter) * AccYangle;

            // So sind die einzelnen Achsen richtig

            //q = Quaternion.Euler(0, 0, -AccYangle);
            //q2 = Quaternion.Euler(0, 0, -rotation.x);

            //q = Quaternion.Euler(AccXangle, 0, 0);
            //q2 = Quaternion.Euler(-rotation.y, 0, 0);

            //q doesn't give z rotation
            //q2 = Quaternion.Euler(0, rotation.z, 0);

            q3 = Quaternion.Euler(rotation_filtered);
        }

        // Die ersten 1000 Messungen setzen den Bias, also die Gravitation
        else if (bias_counter == bias_length)
        {
            acceleration_bias /= 1000;
            gyro_bias /= 1000;
            bias_counter++;
            Debug.Log("Acceleration bias is " + acceleration_bias);
            Debug.Log("Gyroscope bias is " + gyro_bias);
        }
        else
        {
            // trying to get a bias for flat on table
            acceleration_bias += acceleration1;
            gyro_bias += gyroscope;

            bias_counter++;
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