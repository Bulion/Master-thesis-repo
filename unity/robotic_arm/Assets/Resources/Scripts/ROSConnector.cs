using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using RosSharp.RosBridgeClient;
using RosSharp.RosBridgeClient.Protocols;

namespace RosSharp.RosBridgeClient {

    public class ROSConnector : MonoBehaviour {

        public RosSocket socket;
        public string url;

        public bool isConnected { get; set; }

        // Use this for initialization
        void Start() {
            isConnected = false;
        }

        public void Connect()
        {
            if (isConnected)
            {

            }
            else
            {
                IProtocol uwpProtocol = new WebSocketUWPProtocol(url);
                socket = new RosSocket(uwpProtocol);
                if (socket != null)
                {
                    isConnected = true;
                }
            }
        }

        private void ROSResponse(Messages.Standard.String response)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine(response.data);
#endif
        }

        // Update is called once per frame
        void Update() {

        }
    }
}