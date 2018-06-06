using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using System.IO;
using UnityEngine;
using System.Text;
using System.Linq;
using HoloToolkit.Unity;
using UnityEngine.Events;

#if !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking;
#endif

public class Message
{
    public List<byte> header { get; set; }
    public byte id { get; set; }
    public byte len { get; set; }
    public byte ctrl { get; set; }
    public List<byte> parameters { get; set; }
    public byte checksum { get; set; }

    public Message()
    {
        header = new List<byte>{ 0xAA, 0xAA };
        len = 0x00;
        ctrl = 0x00;
        parameters = new List<byte> { };
        checksum = 0;
    }

    public Message(byte[] msg)
    {
        header = new List<byte> { msg[0], msg[1] };
        id = msg[2];
        len = msg[3];
        ctrl = msg[4];
        parameters = new List<byte> { };
        for (int i = 5; i < (msg.Length - 1); ++i)
        {
            parameters.Add(msg[i]);
        }
        checksum = msg[msg.Length - 1];
    }

    private void Refresh()
    {
        if (0 == checksum)
        {
            checksum = (byte)(id + ctrl);
            foreach (byte p in parameters)
            {
                checksum += p;
            }
        }
        checksum = (byte)(checksum % 256);
        checksum = (byte)(Mathf.Pow(2, 8) - checksum);
        checksum = (byte)(checksum % 256);
        len = (byte)(0x02 + parameters.Count);
    }

    public byte[] Bytes()
    {
        List<byte> command;
        Refresh();
        if (0 < parameters.Count)
        {
            command = new List<byte> { 0xAA, 0xAA, len, id, ctrl };
            foreach (byte p in parameters)
            {
                command.Add(p);
            }
            command.Add(checksum);
        }
        else
        {
            command = new List<byte> { 0xAA, 0xAA, len, id, ctrl, checksum };
        }
        return command.ToArray();
    }

    public int Length()
    {
        return 6 + parameters.Count;
    }
}

public class DobotConnectionScript : MonoBehaviour {

    public enum MoveMode{
        MODE_PTP_JUMP_XYZ = 0x00,
        MODE_PTP_MOVJ_XYZ = 0x01,
        MODE_PTP_MOVL_XYZ = 0x02,
        MODE_PTP_JUMP_ANGLE = 0x03,
        MODE_PTP_MOVJ_ANGLE = 0x04,
        MODE_PTP_MOVL_ANGLE = 0x05,
        MODE_PTP_MOVJ_INC = 0x06,
        MODE_PTP_MOVL_INC = 0x07,
        MODE_PTP_MOVJ_XYZ_INC = 0x08,
        MODE_PTP_JUMP_MOVL_XYZ = 0x09
    }

    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public float r { get; set; }
    public float j1 { get; set; }
    public float j2 { get; set; }
    public float j3 { get; set; }
    public float j4 { get; set; }

    private int myChannelId;
    private int hostId;
    private int connectionId;

    private byte error;

    private int port;
    private string ip;

    private bool verbose;

    static MemoryStream ToMemoryStream(Stream input)
    {
        try
        {                                         // Read and write in
            byte[] block = new byte[0x1000];       // blocks of 4K.
            MemoryStream ms = new MemoryStream();
            while (true)
            {
                int bytesRead = input.Read(block, 0, block.Length);
                if (bytesRead == 0) return ms;
                ms.Write(block, 0, bytesRead);
            }
        }
        finally { }
    }

#if UNITY_EDITOR
    private UdpClient client;
    private IPEndPoint ep;

    private void Start()
    {
        ConnectTo("192.168.1.24", 8899, true);
    }
#else
    DatagramSocket socket;
    DataWriter writer;
    async void Start()
    {
        socket = new DatagramSocket();
        socket.MessageReceived += SocketMessageReceived;
        try
        {
            await socket.BindServiceNameAsync("8899");
            var outputStream = await socket.GetOutputStreamAsync(new HostName("192.168.1.24"), "8899");
            writer = new DataWriter(outputStream);
        }
        catch (Exception e)
        {
            return;
        }
    }

    private async void SocketMessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        try
        {
            Stream streamIn = args.GetDataStream().AsStreamForRead();
            MemoryStream ms = ToMemoryStream(streamIn);
            OnMessageReceivedEvent(new Message(ms.ToArray()));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    private void OnMessageReceivedEvent(Message msg)
    {
        switch(msg.id)
        {
        case 10:
            PoseResponseParser(msg);
            break;
        }
    }
#endif

#if UNITY_EDITOR
    public void ConnectTo(string serverIp, int serverPort, bool verb = false) {
        client = new UdpClient();
        ep = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        client.Client.SendTimeout = 50;
        client.Client.ReceiveTimeout = 50;
        client.Connect(ep);
        verbose = verb;
        SetPtpCoordinateParams(200.0F, 200.0F);
        SetPtpCommonParams(200.0F, 200.0F);
    }
#endif

    void Update()
    {
        //Run();
    }

    public void Run()
    {
        GetPose();
    }

    private Message SendCommand(Message msg)
    {
        SendMessage(msg);
        Message response = new Message();//ReadMessage();
        return response;
    }

    private void SendMessage(Message msg)
    {
        if (verbose)
        {
            Debug.Log("pydobot: << Send Message");
        }
#if UNITY_EDITOR
        client.Send(msg.Bytes(), msg.Length());
#else
        if (writer != null)
        {
            writer.WriteBytes(msg.Bytes());
            writer.StoreAsync();
        }
#endif
    }

    private Message ReadMessage()
    {
        Message msg = new Message();
#if UNITY_EDITOR
        byte[] recBuffer = client.Receive(ref ep);
        if (recBuffer.Length > 0)
        {
            msg = new Message(recBuffer);
            if (verbose)
            {
                Debug.Log("pydobot: >> Received data");
            }
        }
#endif
        return msg;
    }

    private Message GetPose()
    {
        Message msg = new Message();
        msg.id = 10;
        Message response = SendCommand(msg);
        return response;
    }

    private void PoseResponseParser(Message msg)
    {
        if (32 >= msg.parameters.Count)
        {
            x = BitConverter.ToSingle(msg.parameters.ToArray(), 0);
            y = BitConverter.ToSingle(msg.parameters.ToArray(), 4);
            z = BitConverter.ToSingle(msg.parameters.ToArray(), 8);
            r = BitConverter.ToSingle(msg.parameters.ToArray(), 12);
            j1 = BitConverter.ToSingle(msg.parameters.ToArray(), 16);
            j2 = BitConverter.ToSingle(msg.parameters.ToArray(), 20);
            j3 = BitConverter.ToSingle(msg.parameters.ToArray(), 24);
            j4 = BitConverter.ToSingle(msg.parameters.ToArray(), 28);
        }
        if (verbose)
        {
            Debug.LogFormat("pydobot: x: {0} y: {1} z: {2} r: {3} j1: {4} j2: {5} j3: {6} j4: {7}",
                             x, y, z, r, j1, j2, j3, j4);
        }
    }

    private Message SetCpCmd(float x, float y, float z)
    {
        Message msg = new Message();
        msg.id = 91;
        msg.ctrl = 0x03;
        msg.parameters = new List<byte> { 0x01 };
        msg.parameters.AddRange(BitConverter.GetBytes(x));
        msg.parameters.AddRange(BitConverter.GetBytes(y));
        msg.parameters.AddRange(BitConverter.GetBytes(z));
        msg.parameters.Add(0x00);
        return SendCommand(msg);
    }

    private Message SetPtpCoordinateParams(float velocity, float acceleration)
    {
        Message msg = new Message();
        msg.id = 81;
        msg.ctrl = 0x03;
        msg.parameters = new List<byte> { };
        msg.parameters.AddRange(BitConverter.GetBytes(velocity));
        msg.parameters.AddRange(BitConverter.GetBytes(velocity));
        msg.parameters.AddRange(BitConverter.GetBytes(acceleration));
        msg.parameters.AddRange(BitConverter.GetBytes(acceleration));
        return SendCommand(msg);
    }

    private Message SetPtpCommonParams(float velocity, float acceleration)
    {
        Message msg = new Message();
        msg.id = 83;
        msg.ctrl = 0x03;
        msg.parameters = new List<byte> { };
        msg.parameters.AddRange(BitConverter.GetBytes(velocity));
        msg.parameters.AddRange(BitConverter.GetBytes(acceleration));
        return SendCommand(msg);
    }

    private Message SetPtpCmd(float x, float y, float z, float r, MoveMode mode)
    {
        Message msg = new Message();
        msg.id = 84;
        msg.ctrl = 0x03;
        msg.parameters = new List<byte> { };
        msg.parameters.Add((byte)mode);
        msg.parameters.AddRange(BitConverter.GetBytes(x));
        msg.parameters.AddRange(BitConverter.GetBytes(y));
        msg.parameters.AddRange(BitConverter.GetBytes(z));
        msg.parameters.AddRange(BitConverter.GetBytes(r));
        return SendCommand(msg);
    }

    private Message SetEndEffectorSuctionCup(bool suck = false)
    {
        Message msg = new Message();
        msg.id = 62;
        msg.ctrl = 0x03;
        msg.parameters = new List<byte> { };
        msg.parameters.Add(0x01);
        if (true == suck)
        {
            msg.parameters.Add(0x01);
        }
        else
        {
            msg.parameters.Add(0x00);
        }
        return SendCommand(msg);
    }

    public void Go(float x, float y, float z, float r = 0.0F, MoveMode mode = MoveMode.MODE_PTP_MOVJ_ANGLE)
    {
        SetPtpCmd(x, y, z, r, mode);
    }

    public void Suck(bool suck)
    {
        SetEndEffectorSuctionCup(suck);
    }

    public void speed(float velocity = 100.0F, float acceleration = 100.0F)
    {
        SetPtpCommonParams(velocity, acceleration);
        SetPtpCoordinateParams(velocity, acceleration);
    }



}
