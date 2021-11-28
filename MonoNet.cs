using StereoKit;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Speech.Synthesis;
using System.Speech.Recognition;
using System.Speech.AudioFormat;

public class MonoNet {
  public Mono mono;
  public MonoNet(Mono mono) {
    this.mono = mono;
    Random rnd = new Random();
    me = new Peer(rnd.Next(1, 256)); // let the server determine these
  }
  public Socket socket;
  int bufferSize = 1024;
  byte[] rData; int rHead;
  byte[] wData; int wHead;

  public Peer me;
  public Peer[] peers;

  public void Start() {
    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    string ip = "192.168.1.70";
    ip = "139.177.201.219";
    EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), 1234);
    socket.Connect(serverEndPoint);
    rData = new byte[bufferSize];
    wData = new byte[bufferSize];
    peers = new Peer[64];

    // SpeechSynthesizer synth = new SpeechSynthesizer();
    // synth.Speak("oriels!");

    // SpeechRecognitionEngine reco = new SpeechRecognitionEngine();
    
    // System.IO.Stream s;
    // // s.Write();

    // SpeechAudioFormatInfo info = new SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, WaveFormatTag.Pcm, 1, 1);
    // reco.SetInputToAudioStream(s, info);

    Thread.Sleep(1000); // useful?

    Thread readThread = new Thread(Read);
    readThread.Start();
    Thread writeThread = new Thread(Write);
    writeThread.Start();

    // socket.Close();
  }

  void Read() {
    bool running = true;
    while (running) {
      while (socket.Available > 0) {
        try { socket.Receive(rData, 0, bufferSize, SocketFlags.None); } catch (Exception e) {
          Console.WriteLine($"can't connect to the server: {e}");
          return;
        }

        rHead = 0;
        int id = ReadInt();
        if (id != 0) {
          int index = -1;
          for (int i = 0; i < peers.Length; i++) {
            if (peers[i] != null) {
              if (peers[i].id == id) {
                index = i;
                break;
              }
            } else {
              peers[i] = new Peer(id);
              index = i;
              break;
            }
          }
          if (index == -1) {
            Console.WriteLine("too many peers");
            return;
          }
          peers[index].cursorA = ReadVec3();
          peers[index].cursorB = ReadVec3();
          peers[index].cursorC = ReadVec3();
          peers[index].cursorD = ReadVec3();
          peers[index].headset = ReadPose();
          peers[index].offHand = ReadPose();
          peers[index].mainHand = ReadPose();
        }
      }
    }
  }

  void Write() {
    bool running = true;
    while (running) {
      wHead = 0;
      WriteInt(me.id);
      WriteVec3(me.cursorA);
      WriteVec3(me.cursorB);
      WriteVec3(me.cursorC);
      WriteVec3(me.cursorD);
      WritePose(me.headset);
      WritePose(me.offHand);
      WritePose(me.mainHand);
      socket.Send(wData);

      Thread.Sleep(60);
    }
  }

  int ReadInt() {
    int value = BitConverter.ToInt32(rData, rHead);
    rHead += 4;
    return value;
  }
  void WriteInt(int value) {
    BitConverter.GetBytes(value).CopyTo(wData, wHead);
    wHead += 4;
  }

  float ReadFloat() {
    float value = BitConverter.ToSingle(rData, rHead);
    rHead += 4;
    return value;
  }
  void WriteFloat(float value) {
    BitConverter.GetBytes(value).CopyTo(wData, wHead);
    wHead += 4;
  }

  Vec3 ReadVec3() {
    Vec3 value = new Vec3(
      BitConverter.ToSingle(rData, rHead),
      BitConverter.ToSingle(rData, rHead + 4),
      BitConverter.ToSingle(rData, rHead + 8)
    );
    rHead += 12;
    return value;
  }
  void WriteVec3(Vec3 vec) {
    BitConverter.GetBytes(vec.x).CopyTo(wData, wHead);
    BitConverter.GetBytes(vec.y).CopyTo(wData, wHead + 4);
    BitConverter.GetBytes(vec.z).CopyTo(wData, wHead + 8);
    wHead += 12;
  }

  Quat ReadQuat() {
    Quat value = new Quat(
      BitConverter.ToSingle(rData, rHead),
      BitConverter.ToSingle(rData, rHead + 4),
      BitConverter.ToSingle(rData, rHead + 8),
      BitConverter.ToSingle(rData, rHead + 12)
    );
    rHead += 16;
    return value;
  }
  void WriteQuat(Quat quat) {
    BitConverter.GetBytes(quat.x).CopyTo(wData, wHead);
    BitConverter.GetBytes(quat.y).CopyTo(wData, wHead + 4);
    BitConverter.GetBytes(quat.z).CopyTo(wData, wHead + 8);
    BitConverter.GetBytes(quat.w).CopyTo(wData, wHead + 12);
    wHead += 16;
  }

  Pose ReadPose() {
    return new Pose(
      ReadVec3(),
      ReadQuat()
    );
  }
  void WritePose(Pose pose) {
    WriteVec3(pose.position);
    WriteQuat(pose.orientation);
  }

  string localIP, publicIP;
  void GetIPs() {
    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
      socket.Connect("8.8.8.8", 65530);
      IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
      localIP = endPoint.Address.ToString();
    }
    publicIP = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
  }

  Mesh meshCube = Default.MeshCube;
  Material matCube = Default.Material;
  public void Cubee(Matrix m) {
    meshCube.Draw(matCube, m);
  }

  public class Peer {
    public int id;
    public Vec3 cursorA, cursorB, cursorC, cursorD;
    public Pose headset;
    public Pose offHand;
    public Pose mainHand;
    // public Sound voice;
    // public SoundInst voiceInst; // update position

    public Peer(int id) {
      this.id = id;

      // voice = Sound.CreateStream(0.5f);
      // voiceInst = voice.Play(Vec3.Zero, 0.5f);
    }
  }
}
