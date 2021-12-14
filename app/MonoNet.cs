using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using StereoKit;

class MonoNet {
  public Monolith mono;
  public MonoNet(Monolith mono) {
    this.mono = mono;
    Random rnd = new Random();
    me = new Peer(rnd.Next(1, 1024 * 8), SolidType.Normal, Color.White); // let the server determine the id
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
              peers[i] = new Peer(id, SolidType.Immovable, Color.White * 0.5f);
              index = i;
              break;
            }
          }
          if (index == -1) {
            Console.WriteLine("too many peers");
            return;
          }
          peers[index].color = ReadColor();
          peers[index].cursor0 = ReadVec3();
          peers[index].cursor1 = ReadVec3();
          peers[index].cursor2 = ReadVec3();
          peers[index].cursor3 = ReadVec3();
          peers[index].headset = ReadPose();
          peers[index].offHand = ReadPose();
          peers[index].mainHand = ReadPose();
          ReadBlock(ref peers[index].blocks);
          ReadCubic(ref peers[index].cubics);

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
      if (Input.Controller(Handed.Right).IsTracked)
        continue;

      wHead = 0;
      WriteInt(me.id);
      WriteColor(me.color);
      WriteVec3(me.cursor0);
      WriteVec3(me.cursor1);
      WriteVec3(me.cursor2);
      WriteVec3(me.cursor3);
      WritePose(me.headset);
      WritePose(me.offHand);
      WritePose(me.mainHand);
      WriteBlock(me.blocks);
      WriteCubic(me.cubics);
      socket.Send(wData);
    }
  }

  bool ReadBool() {
    bool result = rData[rHead] == 1;
    rHead++;
    return result;
  }
  void WriteBool(bool value) {
    wData[wHead] = (byte)(value ? 1 : 0);
    wHead++;
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

  Color ReadColor() {
    Color color = new Color(
      BitConverter.ToSingle(rData, rHead),
      BitConverter.ToSingle(rData, rHead + 4),
      BitConverter.ToSingle(rData, rHead + 8),
      BitConverter.ToSingle(rData, rHead + 12)
    );
    rHead += 16;
    return color;
  }
  void WriteColor(Color color) {
    BitConverter.GetBytes(color.r).CopyTo(wData, wHead);
    BitConverter.GetBytes(color.g).CopyTo(wData, wHead + 4);
    BitConverter.GetBytes(color.b).CopyTo(wData, wHead + 8);
    BitConverter.GetBytes(color.a).CopyTo(wData, wHead + 12);
    wHead += 16;
  }

  void ReadBlock(ref Block[] blocks) {
    for (int i = 0; i < blocks.Length; i++) {
      bool bActive = ReadBool();
      Pose pose = ReadPose();
      if (bActive) {
        blocks[i].Enable(pose.position, pose.orientation);
      } else {
        blocks[i].Disable();
      }
    }
  }
  void WriteBlock(Block[] blocks) {
    for (int i = 0; i < blocks.Length; i++) {
      WriteBool(blocks[i].active);
      WritePose(blocks[i].solid.GetPose());
    }
  }

  void ReadCubic(ref Cubic[] cubics) {
    for (int i = 0; i < cubics.Length; i++) {
      bool bActive = ReadBool();
      Color color = ReadColor();
      Vec3 p0 = ReadVec3();
      Vec3 p1 = ReadVec3();
      Vec3 p2 = ReadVec3();
      Vec3 p3 = ReadVec3();
      if (bActive) {
        cubics[i].Enable(p0, p1, p2, p3, color);
      } else {
        cubics[i].Disable();
      }
    }
  }
  void WriteCubic(Cubic[] cubics) {
    for (int i = 0; i < cubics.Length; i++) {
      WriteBool(cubics[i].active);
      WriteColor(cubics[i].color);
      WriteVec3(cubics[i].p0);
      WriteVec3(cubics[i].p1);
      WriteVec3(cubics[i].p2);
      WriteVec3(cubics[i].p3);
    }
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

class Peer {
  public float lastPing;

  public int id; // on connect: wait on server sending your peer id
  public Color color;
  public Vec3 cursor0, cursor1, cursor2, cursor3;
  public Pose headset;
  public Pose offHand;
  public Pose mainHand;
  public Block[] blocks;
  public Cubic[] cubics;
  // public Sound voice;
  // public SoundInst voiceInst; // update position

  public Peer(int id, SolidType type, Color color) {
    this.id = id;
    blocks = new Block[] {
      new Block(type, color), new Block(type, color), new Block(type, color),
      new Block(type, color), new Block(type, color), new Block(type, color)
    };
    cubics = new Cubic[] {
      new Cubic(), new Cubic(), new Cubic(),
      new Cubic(), new Cubic(), new Cubic()
    };
    // voice = Sound.CreateStream(0.5f);
    // voiceInst = voice.Play(Vec3.Zero, 0.5f);
  }

  BlockCon dBlock = new BlockCon();
  BlockCon sBlock = new BlockCon();

  CubicCon cubicCon = new CubicCon();

  public void Step(Controller domCon, Controller subCon) {
    dBlock.Step(domCon, subCon, cursor0, ref sBlock, ref blocks);
    sBlock.Step(subCon, domCon, cursor3, ref dBlock, ref blocks);

    cubicCon.Step(domCon, subCon, this, ref cubics);

    Draw(false);
  }

  public void Draw(bool body) {
    if (body){
      // Cube(Matrix.TRS(cursor0, Quat.Identity, Vec3.One * 0.05f), color);
      Cube(Matrix.TRS(headset.position + Input.Head.Forward * -0.15f, headset.orientation, Vec3.One * 0.3f), color);
      // Cube(offHand.ToMatrix(new Vec3(0.1f, 0.025f, 0.1f)), color);
      // Cube(mainHand.ToMatrix(new Vec3(0.1f, 0.025f, 0.1f)), color);

      Bezier.Draw(cursor0, cursor1, cursor2, cursor3, Color.White);
    }
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