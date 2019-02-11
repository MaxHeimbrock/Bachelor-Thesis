using AHRS;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class IMU_Processor {
    
    protected IMU_Preprocessor preprocessor;

    public IMU_Processor(IMU_Preprocessor IMU_preprocessor)
    {
        this.preprocessor = IMU_preprocessor;
    }    

    public abstract Quaternion GetOrientation(float delta_t_s, Vector3 accel, Vector3 gyro, Vector3 magnet);

    // Local space IMU -> left handed, z up
    protected Quaternion CalcAngleFromAcc(Vector3 accel)
    {
        float rot_x = 0;
        float rot_y = 0;

        // X-axis - http://ozzmaker.com/berryimu/ // https://stackoverflow.com/questions/3755059/3d-accelerometer-calculate-the-orientation
        rot_x = (float)((Math.Atan2(accel.y, accel.z) + Math.PI) * (180 / Math.PI));
        //rot_x = -(float)(Math.Atan2(acc.y, Math.Sqrt(acc.x * acc.x + acc.z * acc.z)) * (180 / Math.PI));

        // diese Rechnung korrigiert Orientierung zu -180 bis 180 grad
        if (rot_x > 180)
            rot_x -= (float)360;

        // Y-axis - http://ozzmaker.com/berryimu/ // https://stackoverflow.com/questions/3755059/3d-accelerometer-calculate-the-orientation
        //rot_y = (float)(Math.Atan2(-acc.x, Math.Sqrt(acc.y * acc.y + acc.z * acc.z)) * (180 / Math.PI));
        rot_y = (float)((Math.Atan2(accel.z, accel.x) + Math.PI * 1 / 2) * (180 / Math.PI));
        //rot_y = 0;

        // diese Rechnung korrigiert Orientierung zu -180 bis 180 grad
        if (rot_y > 180)
            rot_y -= (float)360;
        
        //return new Vector3(rot_x, rot_y, 0);
        return Quaternion.Euler(new Vector3(rot_y, 0, rot_x));
    }
}

public class GyroscopeProcessor : IMU_Processor
{
    Vector3 currentRotation = Vector3.zero;
    int count = 2;
    Quaternion firstPose;

    public GyroscopeProcessor(IMU_Preprocessor IMU_preprocessor) : base(IMU_preprocessor)
    {
    }
    
    public override Quaternion GetOrientation(float delta_t_s, Vector3 accel, Vector3 gyro, Vector3 magnet)
    {
        gyro = preprocessor.CorrectGyro(gyro);
        accel = preprocessor.CorrectAccel(accel);
        accel = preprocessor.LowPassFilter(accel);

        currentRotation += gyro * delta_t_s;

        // since the Mahony-Filter integrates, we need to find the starting orientation. Disregard the first pose because it is always wrong
        if (count > 0)
        {
            firstPose = CalcAngleFromAcc(accel);
            count--;
        }

        Quaternion currentRotationQuaternion = Quaternion.Euler(new Vector3(currentRotation.y, -currentRotation.z, currentRotation.x));

        return Quaternion.Inverse(currentRotationQuaternion * firstPose);
    }
}

public class MahonyProcessorNoMagnet : IMU_Processor
{
    private MahonyAHRS mahonyARHS = new MahonyAHRS(0.00005f);
    int count = 2;
    Quaternion firstPose;

    public MahonyProcessorNoMagnet(IMU_Preprocessor IMU_preprocessor) : base(IMU_preprocessor)
    {
    }

    public override Quaternion GetOrientation(float delta_t_s, Vector3 accel, Vector3 gyro, Vector3 magnet)
    {
        Quaternion orientation;

        gyro = preprocessor.CorrectGyro(gyro);
        accel = preprocessor.CorrectAccel(accel);
        accel = preprocessor.LowPassFilter(accel);

        // since the Mahony-Filter integrates, we need to find the starting orientation. Disregard the first pose because it is always wrong
        if (count > 0)
        {
            firstPose = CalcAngleFromAcc(accel);
            count--;
        }

        if (magnet == Vector3.zero)
        {
            mahonyARHS.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z);
            orientation = new Quaternion(mahonyARHS.Quaternion[0], mahonyARHS.Quaternion[1], mahonyARHS.Quaternion[3], -mahonyARHS.Quaternion[2]);
            // zusätzlich noch um 180° zur x-Achse rotiert
            orientation *= Quaternion.AngleAxis(180, Vector3.right);
            // zusätzliche Rotationen für die Hololens
            //orientation *= Quaternion.AngleAxis(180, Vector3.forward);
            orientation *= firstPose;
        }
        else
        {
            mahonyARHS.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z, magnet.x, magnet.y, magnet.z);
            orientation = new Quaternion(mahonyARHS.Quaternion[0], mahonyARHS.Quaternion[1], mahonyARHS.Quaternion[3], -mahonyARHS.Quaternion[2]);
            // zusätzlich noch um 180° zur x-Achse rotiert
            orientation *= Quaternion.AngleAxis(180, Vector3.right);
            // zusätzliche Rotationen für die Hololens
            //orientation *= Quaternion.AngleAxis(180, Vector3.forward);
            orientation *= firstPose;
        }

        // Hier das inverse
        return Quaternion.Inverse(orientation);
    }
}

public class MadgwickProcessorNoMagnet : IMU_Processor
{
    private MadgwickAHRS madgwickARHS = new MadgwickAHRS(0.00005f);
    int count = 2;
    Quaternion firstPose;

    public MadgwickProcessorNoMagnet(IMU_Preprocessor IMU_preprocessor) : base(IMU_preprocessor)
    {
    }

    public override Quaternion GetOrientation(float delta_t_s, Vector3 accel, Vector3 gyro, Vector3 magnet)
    {
        Quaternion orientation;

        gyro = preprocessor.CorrectGyro(gyro);
        accel = preprocessor.CorrectAccel(accel);
        accel = preprocessor.LowPassFilter(accel);

        // since the Mahony-Filter integrates, we need to find the starting orientation. Disregard the first pose because it is always wrong
        if (count > 0)
        {
            firstPose = CalcAngleFromAcc(accel);
            count--;
        }

        if (magnet == Vector3.zero)
        {
            madgwickARHS.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z);
            orientation = new Quaternion(madgwickARHS.Quaternion[0], madgwickARHS.Quaternion[1], madgwickARHS.Quaternion[3], -madgwickARHS.Quaternion[2]);
            // zusätzlich noch um 180° zur x-Achse rotiert
            orientation *= Quaternion.AngleAxis(180, Vector3.right);
            // zusätzliche Rotationen für die Hololens
            //orientation *= Quaternion.AngleAxis(180, Vector3.forward);
            orientation *= firstPose;
        }
        else
        {
            madgwickARHS.Update(gyro.x, gyro.y, gyro.z, accel.x, accel.y, accel.z, magnet.x, magnet.y, magnet.z);
            orientation = new Quaternion(madgwickARHS.Quaternion[0], madgwickARHS.Quaternion[1], madgwickARHS.Quaternion[3], -madgwickARHS.Quaternion[2]);
            // zusätzlich noch um 180° zur x-Achse rotiert
            orientation *= Quaternion.AngleAxis(180, Vector3.right);
            // zusätzliche Rotationen für die Hololens
            //orientation *= Quaternion.AngleAxis(180, Vector3.forward);
            orientation *= firstPose;
        }


        // Hier das inverse
        return Quaternion.Inverse(orientation);
    }
}

/*
public class GyroAccelFilteredProcessor : IMU_Processor
{
    private float filter = 0.98f;

    private Vector3 filtered_rotation = Vector3.zero;

    public GyroAccelFilteredProcessor(IMU_Preprocessor IMU_preprocessor) : base(IMU_preprocessor)
    {
    }

    public override Quaternion GetOrientation(float delta_t_s, Vector3 accel, Vector3 gyro, Vector3 magnet)
    {
        gyro = preprocessor.CorrectGyro(gyro);
        accel = preprocessor.CorrectAccel(accel);
        accel = preprocessor.LowPassFilter(accel);

        Vector3 integrated_gyro = gyro * delta_t_s;

        Vector3 acc_angles = CalcAngleFromAcc(accel).eulerAngles;

        // Acc is only trustable till around 45° right now - still a flip happens
        if (acc_angles.x < 20)
            filtered_rotation.x = filter * (filtered_rotation.x + integrated_gyro.x) + (1 - filter) * acc_angles.z;
        else
            filtered_rotation.x = filtered_rotation.x + integrated_gyro.x;
        if (acc_angles.y < 20)
            filtered_rotation.y = filter * (filtered_rotation.y + integrated_gyro.y) + (1 - filter) * acc_angles.x;
        else
            filtered_rotation.y = filtered_rotation.y + integrated_gyro.y;

        filtered_rotation.z = filtered_rotation.z + integrated_gyro.z;

        return Quaternion.Inverse(Quaternion.Euler(filtered_rotation));
    }
}
*/

public class AccelerometerProcessor : IMU_Processor
{
    public AccelerometerProcessor(IMU_Preprocessor IMU_preprocessor) : base(IMU_preprocessor)
    {
    }

    public override Quaternion GetOrientation(float delta_t_s, Vector3 accel, Vector3 gyro, Vector3 magnet)
    {
        accel = preprocessor.CorrectAccel(accel);
        accel = preprocessor.LowPassFilter(accel);

        return Quaternion.Inverse(CalcAngleFromAcc(accel));
    }
}

public class IMU_Preprocessor
{
    private float G_Gain = 0.07f;
    private float Accel_Factor = 16384.0f;
    private Vector3 accel_bias = new Vector3(0.0397429f, -0.0665699f, -0.024349f);
    private Vector3 gyro_bias = new Vector3(-0.903895f, 0.44357f, -0.429229f);

    private float accel_threshold;
    private float LPF_filter;
    public int LPF_filter_size;
    private Vector3[] filter_array;

    public IMU_Preprocessor()
    {
        accel_threshold = 0.005f;
        LPF_filter = 0.25f;
        LPF_filter_size = 50;
        filter_array = new Vector3[LPF_filter_size];
    }

    public IMU_Preprocessor(float acc_threshold, float LPF_filter, int LPF_filter_size, Vector3[] filter_array)
    {
        this.accel_threshold = acc_threshold;
        this.LPF_filter = LPF_filter;
        this.LPF_filter_size = LPF_filter_size;
        filter_array = new Vector3[LPF_filter_size];
    }

    public Vector3 CorrectGyro(Vector3 gyro)
    {
        gyro *= G_Gain;
        gyro -= gyro_bias;
        return gyro;
    }

    public Vector3 CorrectAccel(Vector3 accel)
    {
        accel /= Accel_Factor;
        accel -= accel_bias;
        return accel;
    }

    public Vector3 ThresholdAcc(Vector3 accel1, Vector3 accel0)
    {
        if (Math.Abs(accel1.x - accel0.x) < accel_threshold)
            accel1.x = accel0.x;
        if (Math.Abs(accel1.y - accel0.y) < accel_threshold)
            accel1.y = accel0.y;
        if (Math.Abs(accel1.z - accel0.z) < accel_threshold)
            accel1.z = accel0.z;

        return accel1;
    }

    public Vector3 LowPassFilter(Vector3 accel)
    {
        // initialize filter_array
        if (filter_array == null)
        {
            filter_array = new Vector3[LPF_filter_size];
            for (int i = 0; i < LPF_filter_size; i++)
                filter_array[i] = accel;
        }

        // use threshold filter first
        accel = ThresholdAcc(accel, filter_array[LPF_filter_size - 1]);

        // get mean of all last accel vectors 
        Vector3 sum = Vector3.zero;

        for (int i = 0; i < LPF_filter_size; i++)
        {
            sum += filter_array[i];
        }

        sum /= LPF_filter_size;

        // push new value in filter_array
        for (int i = 0; i < LPF_filter_size - 1; i++)
            filter_array[i] = filter_array[i + 1];

        filter_array[LPF_filter_size - 1] = accel;

        return sum;
    }
}
