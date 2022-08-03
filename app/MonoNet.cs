using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;

namespace Oriels;

public class MonoNet {
  public bool send;

  public Socket socket;
  int bufferSize = 1024;
  byte[] rData; int rHead;
  byte[] wData; int wHead;

  public Peer me;
  public Peer[] peers;

  public MonoNet() {
    this.send = false;
    Random rnd = new Random();
    me = new Peer(this, rnd.Next(1, 1024 * 8)); // let the server determine the id
    // me.block = new Block(new Vec3((float)rnd.NextDouble() * 0.5f, 10, -4), Quat.Identity, SolidType.Normal, Color.White);


    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    string ip = "192.168.1.70";
    ip = "139.177.201.219";


    EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), 1234);
    socket.Connect(serverEndPoint);
    rData = new byte[bufferSize];
    wData = new byte[bufferSize];
    peers = new Peer[64];

    Thread.Sleep(1000); // useful?

    Thread readThread = new Thread(Read);
    readThread.Start();
    Thread writeThread = new Thread(Write);
    writeThread.Start();
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
        if (id != 0 && id != me.id) {
          int index = -1;
          for (int i = 0; i < peers.Length; i++) {
            if (peers[i] != null) {
              if (peers[i].id == id) {
                index = i;
                break;
              }
            } else {
              peers[i] = new Peer(this, id);
              index = i;
              break;
            }
          }
          if (index == -1) {
            Console.WriteLine("too many peers");
            return;
          }
          peers[index].Read();

          peers[index].lastPing = Time.Totalf;
        }
      }

      for (int i = 0; i < peers.Length; i++) {
        if (peers[i] != null) {
          if (Time.Totalf - peers[i].lastPing > 6) {
            peers[i] = null;
          }
        }
      }
    }
  }

  void Write() {
    bool running = true;
    while (running) {
      Thread.Sleep(60);
      if (send) {
        wHead = 0;
        me.Write();
        socket.Send(wData);
      }
    }
  }

  public bool ReadBool() {
    bool result = rData[rHead] == 1;
    rHead++;
    return result;
  }
  public void WriteBool(bool value) {
    wData[wHead] = (byte)(value ? 1 : 0);
    wHead++;
  }

  public int ReadInt() {
    int value = BitConverter.ToInt32(rData, rHead);
    rHead += 4;
    return value;
  }
  public void WriteInt(int value) {
    BitConverter.GetBytes(value).CopyTo(wData, wHead);
    wHead += 4;
  }

  public float ReadFloat() {
    float value = BitConverter.ToSingle(rData, rHead);
    rHead += 4;
    return value;
  }
  public void WriteFloat(float value) {
    BitConverter.GetBytes(value).CopyTo(wData, wHead);
    wHead += 4;
  }

  public Vec3 ReadVec3() {
    Vec3 value = new Vec3(
      BitConverter.ToSingle(rData, rHead),
      BitConverter.ToSingle(rData, rHead + 4),
      BitConverter.ToSingle(rData, rHead + 8)
    );
    rHead += 12;
    return value;
  }
  public void WriteVec3(Vec3 vec) {
    BitConverter.GetBytes(vec.x).CopyTo(wData, wHead);
    BitConverter.GetBytes(vec.y).CopyTo(wData, wHead + 4);
    BitConverter.GetBytes(vec.z).CopyTo(wData, wHead + 8);
    wHead += 12;
  }

  public Quat ReadQuat() {
    Quat value = new Quat(
      BitConverter.ToSingle(rData, rHead),
      BitConverter.ToSingle(rData, rHead + 4),
      BitConverter.ToSingle(rData, rHead + 8),
      BitConverter.ToSingle(rData, rHead + 12)
    );
    rHead += 16;
    return value;
  }
  public void WriteQuat(Quat quat) {
    BitConverter.GetBytes(quat.x).CopyTo(wData, wHead);
    BitConverter.GetBytes(quat.y).CopyTo(wData, wHead + 4);
    BitConverter.GetBytes(quat.z).CopyTo(wData, wHead + 8);
    BitConverter.GetBytes(quat.w).CopyTo(wData, wHead + 12);
    wHead += 16;
  }

  public Pose ReadPose() {
    return new Pose(
      ReadVec3(),
      ReadQuat()
    );
  }
  public void WritePose(Pose pose) {
    WriteVec3(pose.position);
    WriteQuat(pose.orientation);
  }

  public Color ReadColor() {
    Color color = new Color(
      BitConverter.ToSingle(rData, rHead),
      BitConverter.ToSingle(rData, rHead + 4),
      BitConverter.ToSingle(rData, rHead + 8),
      BitConverter.ToSingle(rData, rHead + 12)
    );
    rHead += 16;
    return color;
  }
  public void WriteColor(Color color) {
    BitConverter.GetBytes(color.r).CopyTo(wData, wHead);
    BitConverter.GetBytes(color.g).CopyTo(wData, wHead + 4);
    BitConverter.GetBytes(color.b).CopyTo(wData, wHead + 8);
    BitConverter.GetBytes(color.a).CopyTo(wData, wHead + 12);
    wHead += 16;
  }

  string localIP, publicIP;
  void GetIPs() {
    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
      socket.Connect("8.8.8.8", 65530);
      IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
      localIP = endPoint.Address.ToString();
    }
    publicIP = new HttpClient().GetStringAsync("https://ipv4.icanhazip.com/").Result.TrimEnd();
  }
}

public class Peer {
  public float lastPing;

  MonoNet net;
  public int id; // on connect: wait on server sending your peer id
  public Color color;
  public Pose headset;
  public Pose rHand, lHand;
  public Pose rCursor, lCursor;
  NetBlock[] blocks;
  NetCubic[] cubics;
  // public Sound voice;
  // public SoundInst voiceInst; // update position

  public Peer(MonoNet net, int id) {
    this.net = net;
    this.id = id;
    // voice = Sound.CreateStream(0.5f);
    // voiceInst = voice.Play(Vec3.Zero, 0.5f);
  }

  public void Step() { // CLIENT SIDE
    Mono mono = Mono.inst;
    color = mono.colorCube.color;
    headset = Input.Head;
    rHand = mono.rig.rCon.Pose();
    lHand = mono.rig.lCon.Pose();
    rCursor = mono.rGlove.virtualGlove;
    lCursor = mono.lGlove.virtualGlove;

    if (blocks == null || blocks.Length != mono.blocks.Length) {
      blocks = new NetBlock[mono.blocks.Length];
    }
    for (int i = 0; i < blocks.Length; i++) {
      blocks[i].active = mono.blocks[i].active;
      blocks[i].color = mono.blocks[i].color;
      blocks[i].pose = mono.blocks[i].solid.GetPose();
      blocks[i].scale = mono.blocks[i].scale;
    }

    if (cubics == null || cubics.Length != mono.cubics.Length) { cubics = new NetCubic[mono.cubics.Length]; }
    for (int i = 0; i < cubics.Length; i++) {
      cubics[i].active = mono.cubics[i].active;
      cubics[i].color = mono.cubics[i].color;
      cubics[i].p0 = mono.cubics[i].p0;
      cubics[i].p1 = mono.cubics[i].p1;
      cubics[i].p2 = mono.cubics[i].p2;
      cubics[i].p3 = mono.cubics[i].p3;
    }

    // for (int i = 0; i < blocks.Length; i++) {
    //   Pose blockPose = blocks[i].solid.GetPose();
    //   Bounds bounds = new Bounds(Vec3.Zero, Vec3.One * blocks[i].size);
    //   if (blocks[i].active && (
    //     bounds.Contains(blockPose.orientation.Inverse * (cursor - blockPose.position)) ||
    //     bounds.Contains(blockPose.orientation.Inverse * (cursor3 - blockPose.position))
    //   )) {
    //     blocks[i].color = new Color(0.8f, 1, 1);
    //   } else {
    //     blocks[i].color = new Color(1, 1, 1);
    //   }
    // }

    for (int i = 0; i < net.peers.Length; i++) {
      Peer peer = net.peers[i];
      if (peer != null) { peer.Draw(true); }
    }
    Draw(false);
  }

  public void Write() {
    net.WriteInt(id);
    net.WriteColor(color);
    net.WritePose(headset);
    net.WritePose(rHand);
    net.WritePose(lHand);
    net.WritePose(rCursor);
    net.WritePose(lCursor);
    WriteBlock();
    WriteCubic();
  }
  public void Read() {
    color = net.ReadColor();
    headset = net.ReadPose();
    rHand = net.ReadPose();
    lHand = net.ReadPose();
    rCursor = net.ReadPose();
    lCursor = net.ReadPose();
    ReadBlock();
    ReadCubic();
  }

  struct NetBlock {
    public bool active;
    public Color color;
    public Pose pose;
    public Vec3 scale;
  }
  void ReadBlock() {
    int length = net.ReadInt();
    if (length != blocks.Length) { blocks = new NetBlock[length]; }
    for (int i = 0; i < length; i++) {
      NetBlock netBlock = blocks[i];
      netBlock.active = net.ReadBool();
      netBlock.color = net.ReadColor();
      netBlock.pose = net.ReadPose();
      netBlock.scale = net.ReadVec3();
    }
  }
  void WriteBlock() {
    net.WriteInt(blocks.Length);
    for (int i = 0; i < blocks.Length; i++) {
      NetBlock netBlock = blocks[i];
      net.WriteBool(netBlock.active);
      net.WriteColor(netBlock.color);
      net.WritePose(netBlock.pose);
      net.WriteVec3(netBlock.scale);
    }
  }

  public struct NetCubic {
    public bool active;
    public Color color;
    public Vec3 p0, p1, p2, p3;
  }
  void ReadCubic() {
    int length = net.ReadInt();
    if (length != cubics.Length) { cubics = new NetCubic[length]; }
    for (int i = 0; i < cubics.Length; i++) {
      NetCubic cubic = cubics[i];
      cubic.active = net.ReadBool();
      cubic.color = net.ReadColor();
      cubic.p0 = net.ReadVec3();
      cubic.p1 = net.ReadVec3();
      cubic.p2 = net.ReadVec3();
      cubic.p3 = net.ReadVec3();
    }
  }
  void WriteCubic() {
    net.WriteInt(cubics.Length);
    for (int i = 0; i < cubics.Length; i++) {
      net.WriteBool(cubics[i].active);
      net.WriteColor(cubics[i].color);
      net.WriteVec3(cubics[i].p0);
      net.WriteVec3(cubics[i].p1);
      net.WriteVec3(cubics[i].p2);
      net.WriteVec3(cubics[i].p3);
    }
  }

  public void Draw(bool body) {
    Mono mono = Mono.inst;
    if (body) {
      PullRequest.BlockOut(Matrix.TRS(headset.position + Input.Head.Forward * -0.15f, headset.orientation, Vec3.One * 0.3f), color);
    }

    // Bezier.Draw(
    //   mono.rGlove.virtualGlove.position,
    //   mono.rig.rCon.pos,
    //   mono.rig.lCon.pos,
    //   mono.lGlove.virtualGlove.position,
    //   new Color(1, 1, 1, 0.1f)
    // );

    for (int i = 0; i < blocks.Length; i++) {
      NetBlock block = blocks[i];
      if (block.active) {
        PullRequest.BlockOut(block.pose.ToMatrix(block.scale), block.color);
      }
    }

    for (int i = 0; i < cubics.Length; i++) {
      NetCubic cubic = cubics[i];
      if (cubic.active) {
        Bezier.Draw(cubic.p0, cubic.p1, cubic.p2, cubic.p3, color);
      }
    }
  }
}