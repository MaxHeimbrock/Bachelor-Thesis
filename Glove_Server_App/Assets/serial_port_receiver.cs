

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

public class Packet {

	public  Char marker;
	public  Char marker2;
	public UInt16 version;
	public Int16[] values;
	public UInt32 value;
	public Byte key;
	public Byte crc8;
	public Byte padding1;
	public Byte padding2;

	public Packet(){
		values = new Int16[Constants.NB_SENSORS];
	}
}

//=======================================================
//
//=======================================================
public class serial_port_receiver : MonoBehaviour {

	private  SerialPort _serialPort;
	private Thread readThread;
	private int MESSAGE_SIZE;
	public Glove glove;


	//=======================================================
	// Use this for initialization
	void Start () {
		MESSAGE_SIZE = Constants.NB_SENSORS * 2 + 12;
		glove = new Glove ();
		readThread = new Thread(Read);
		 
		// Allow the user to set the appropriate properties.

		//_serialPort.Parity = SetPortParity(_serialPort.Parity);
		//_serialPort.DataBits = SetPortDataBits(_serialPort.DataBits);
		//_serialPort.StopBits = SetPortStopBits(_serialPort.StopBits);
		//_serialPort.Handshake = SetPortHandshake(_serialPort.Handshake);

		name = "\\\\.\\COM3";//a+i;
		//string name = "\\\\.\\COM"+port_number;
		int baudrate = 460800;
		// Create a new SerialPort object with default settings.
		_serialPort = new SerialPort(name, baudrate);
		_serialPort.Open ();

		/*
		bool found = false;
		for (UInt16 i = 0; i < 32; i++) {
		try{
				name = "\\\\.\\COM4";//a+i;
				//string name = "\\\\.\\COM"+port_number;
				int baudrate = 460800;
				// Create a new SerialPort object with default settings.
				_serialPort = new SerialPort(name, baudrate);
				_serialPort.Open ();
				Debug.Log("managed to open port " + i);
				found = true;
				break;
		}catch(Exception){
				
		}
		}
		*

		if (found == false) {
			Debug.Log("could not open any port ");//throw Exception ();
		}
		*/

		// Set the read/write timeouts
		_serialPort.ReadTimeout = 500;
		_serialPort.WriteTimeout = 500;

		readThread.Start();

	}

	//=======================================================
	// Update is called once per frame
	void Update () {
		if(glove.cnt%1000==0){
			Debug.Log ("got 1000 packet");
		}
        //Debug.Log(glove.cnt + " " + glove.version + " " + glove.values[1] + "\t" + glove.values[2] + "\t" + glove.values[2] + "\t" + glove.values[3] );

        // von mir hier hin verschoben
        if (Input.GetKey("space"))
        {
            //glove.set_zero();
            Debug.Log("set_zero");
        }
    }

	//=======================================================
	void OnDisable() 
	{ 
		readThread.Abort(); 
		_serialPort.Close();
	} 

	//=======================================================
	void Read(){
		byte[] buf = new byte[MESSAGE_SIZE];
		Packet packet = new Packet ();
		int gotlen;
		MemoryStream stream = new MemoryStream ();
		BinaryReader reader = new BinaryReader(stream);


		while (true) {
			int received = 0;
			//Debug.Log ("reading "+ MESSAGE_SIZE);
			while (received < MESSAGE_SIZE) {
				gotlen = _serialPort.Read (buf, received, MESSAGE_SIZE - received);
				received += gotlen;
				//Debug.Log ("got " + received  + "bytes ");
			}
			stream.Position = 0; 
			stream.Write (buf, 0, MESSAGE_SIZE);
			stream.Position = 0; 
			//Debug.Log ( stream.Length);
			packet.marker = reader.ReadChar ();
			//Debug.Log ( packet.marker + " " + packet.marker2 );
			if (Convert.ToChar(packet.marker) != '#') {
				_serialPort.Read (buf, 0, 1);
				//Debug.Log ("got  bad marker " + Convert.ToByte(packet.marker));
				continue;
			}
			packet.marker2 = reader.ReadChar ();
			if (Convert.ToChar(packet.marker2) != '#') {
				_serialPort.Read (buf, 0, 1);
				//Debug.Log ("got  bad marker2");
				continue;
			}
			packet.version = reader.ReadUInt16 ();

			for(UInt16 i=0;i<Constants.NB_SENSORS;i++){
				packet.values[i] = reader.ReadInt16 ();
			}

			packet.value = reader.ReadUInt32 ();
			packet.key = reader.ReadByte ();
			packet.crc8 = reader.ReadByte ();
			reader.ReadByte ();
			reader.ReadByte ();

			glove.apply_packet (packet);

		}
	}
}


