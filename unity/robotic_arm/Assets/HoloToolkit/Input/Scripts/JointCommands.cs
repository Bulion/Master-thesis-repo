using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JointCommands : MonoBehaviour {

    private DobotConnectionScript dobot;
    private GameObject jointJ1;
    private GameObject jointJ2;

    private void Start()
    {
        dobot = GameObject.Find("DobotConnection").GetComponent<DobotConnectionScript>();
        jointJ1 = GameObject.Find("J1Rotation");
        jointJ2 = GameObject.Find("J2Rotation");
    }

    private float lastJ1 = 0.0F;
    private float lastJ2 = 0.0F;

    void Update()
    {
        float J1Angle = 180 - jointJ1.transform.rotation.eulerAngles.y;
        float J2Angle = 180 - (jointJ2.transform.rotation.eulerAngles.y + 180);
        if ((J1Angle > lastJ1 + 0.5 || J1Angle < lastJ1 - 0.5) || (J2Angle > lastJ2 + 0.5 || J2Angle < lastJ2 - 0.5)) 
        {
            dobot.Go(J1Angle, 0, 0);
            Debug.LogFormat("Dobot Go command send: J1 = {0}, J2 = {1}", J1Angle, J2Angle);
            lastJ1 = J1Angle;
            lastJ2 = J2Angle;
        }
    }
}
