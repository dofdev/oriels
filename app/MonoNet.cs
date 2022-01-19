using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StereoKit;

public class MonoNet {
  public Monolith mono;
  public bool send;
  public MonoNet(Monolith mono) {
    this.mono = mono;
    this.send = false;
    Random rnd = new Random();
    me = new Peer(rnd.Next(1, 1024 * 8)); // let the server determine the id
    // me.block = new Block(new Vec3((float)rnd.NextDouble() * 0.5f, 10, -4), Quat.Identity, SolidType.Normal, Color.White);
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
        if (id != 0 && id != me.id) {
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
      if (send)
        continue;

      wHead = 0;
      me.Write();
      socket.Send(wData);
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
  public Vec3 cursor0, cursor1, cursor2, cursor3;
  public Pose headset;
  public Pose offHand;
  public Pose mainHand;
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

  public Vec3 vGlovePos;


  public void Step(Monolith mono) { // CLIENT SIDE
    
    // too much in this networking class
    // only contain a copy of network related data
    // and not be a weird pitstop for game logic

    // game logic driven for write, flexible for read (SolidType.Immovable)
    
    vGlovePos = mono.rGlove.virtualGlove.position;


    if (blocks.Length != mono.blocks.Length) { blocks = new NetBlock[mono.blocks.Length]; }
    for (int i = 0; i < blocks.Length; i++) {
      blocks[i].active = mono.blocks[i].active;
      blocks[i].pose = mono.blocks[i].solid.GetPose();
    }

    if (cubics.Length != mono.cubics.Length) { cubics = new NetCubic[mono.cubics.Length]; }
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

    Draw(false);
  }

  public void Write() {
    net.WriteInt(id);
    net.WriteColor(color);
    net.WriteVec3(cursor0);
    net.WriteVec3(cursor1);
    net.WriteVec3(cursor2);
    net.WriteVec3(cursor3);
    net.WritePose(headset);
    net.WritePose(offHand);
    net.WritePose(mainHand);
    WriteBlock();
    WriteCubic();
  }
  public void Read() {
    color = net.ReadColor();
    cursor0 = net.ReadVec3();
    cursor1 = net.ReadVec3();
    cursor2 = net.ReadVec3();
    cursor3 = net.ReadVec3();
    headset = net.ReadPose();
    offHand = net.ReadPose();
    mainHand = net.ReadPose();
    ReadBlock();
    ReadCubic();
  }

  struct NetBlock {
    public bool active;
    public Pose pose;
  }
  void ReadBlock() {
    int length = net.ReadInt();
    if (length != blocks.Length) { blocks = new NetBlock[length]; }
    for (int i = 0; i < length; i++) {
      NetBlock netBlock = blocks[i];
      netBlock.active = net.ReadBool();
      netBlock.pose = net.ReadPose();
    }
  }
  void WriteBlock() {
    net.WriteInt(blocks.Length);
    for (int i = 0; i < blocks.Length; i++) {
      NetBlock netBlock = blocks[i];
      net.WriteBool(netBlock.active);
      net.WritePose(netBlock.pose);
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
    if (body) {
      Cube(Matrix.TRS(headset.position + Input.Head.Forward * -0.15f, headset.orientation, Vec3.One * 0.3f), color);
    }

    Bezier.Draw(cursor0, cur Color.White); // overlap
    // Cube(offHand.ToMatrix(new Vec3(0.1f, 0.025f, 0.1f)), color);
    // Cube(mainHand.ToMatrix(new Vec3(0.1f, 0.025f, 0.1f)), color);
  
    Cube(Matrix.TRS(cursor0, mainHand.orientation, new Vec3(0.025f, 0.1f, 0.1f)), color);
    Cube(Matrix.TRS(cursor3, offHand.orientation, new Vec3(0.025f, 0.1f, 0.1f)), color);

    for (int i = 0; i < blocks.Length; i++) {
      if (blocks[i].solid.GetPose().position.y < -10) {
        blocks[i].Disable();
      } else {
        blocks[i].Draw();
      }
    }

    for (int i = 0; i < cubics.Length; i++) {
      cubics[i].Draw();
    }
  }

  static Mesh meshCube = Default.MeshCube;
  static Material matCube = Default.Material;
  public void Cube(Matrix m, Color color) {
    matCube.FaceCull = Cull.None;
    meshCube.Draw(matCube, m, color);
  }
}