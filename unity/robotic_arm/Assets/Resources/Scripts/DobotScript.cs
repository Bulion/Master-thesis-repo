using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosSharp.RosBridgeClient;
using System.Diagnostics;

namespace RosSharp.RosBridgeClient
{

    public class DobotScript : MonoBehaviour {

        private ROSConnector RosConnector;

        public float PositionRefreshRate;
        private uint frequencyDivider;
        private const float screenRefreshRate = 60.0f;
        private uint counter;

        private float x;
        private float y;
        private float z;
        private float r;

        private ServiceResponseHandler<Messages.Dobot.DobotPose> robotPoseResponseHandler;
        // Use this for initialization
        void Start() {
            RosConnector = GameObject.Find("ROSConnector").GetComponent<ROSConnector>();
            robotPoseResponseHandler = new ServiceResponseHandler<Messages.Dobot.DobotPose>(RobotPoseCallback);

            counter = 0;
            if (0 < PositionRefreshRate)
            {
                frequencyDivider = (uint)(screenRefreshRate / PositionRefreshRate);
            }
            else
            {
                frequencyDivider = 0;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (true == RosConnector.isConnected)
            {
                if (frequencyDivider < counter)
                {
                    UpdateRobotPose();
                    counter = 0;
                }
                ++counter;
            }
        }

        private void UpdateRobotPose()
        {
            RosConnector.socket.CallService("/DobotServer/GetPose",
                                            robotPoseResponseHandler,
                                            new Messages.Dummy());
            transform.localPosition = new Vector3(y, z + 0.5f, x + 1.0f);
        }

        private void RobotPoseCallback(Messages.Dobot.DobotPose pose)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine("Pose x: " + pose.x.ToString() + 
                                                   " y: " + pose.y.ToString() + 
                                                   " z: " + pose.z.ToString() + 
                                                   " r: " + pose.r.ToString());
#endif
            x = pose.x / 100;
            y = pose.y / 100;
            z = pose.z / 100;
            r = pose.r;
        }
    }

}