using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GloveManager : MonoBehaviour {


    // Glove-Connection
    string PC_IP = "192.168.131.1";
    string glove_IP = "192.168.131.59";
    int ping_port = 11159;
    int sensor_port = 64059;
    int IMU_port = 64159;
    bool autoconnect_glove = true;

    SensorCommunicator sensor_communicator;
    IMUCommunicator IMU_communicator;

	// Use this for initialization
	void Start () {
        sensor_communicator = new SensorUDPCommunicator(PC_IP, glove_IP, ping_port, sensor_port, autoconnect_glove);
        IMU_communicator = new IMU_UDPCommunicator(PC_IP, glove_IP, ping_port, IMU_port, autoconnect_glove);
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
