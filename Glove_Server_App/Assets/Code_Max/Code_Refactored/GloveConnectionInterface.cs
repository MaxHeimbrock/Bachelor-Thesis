using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GloveConnector;

public interface GloveConnectionInterface {    

    ValuePacket GetValuePacket();

    IMUPacket GetIMUPacket();

}
