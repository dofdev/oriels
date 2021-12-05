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
      socket.Send(wData);

      Thread.Sleep(60);
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
  void WriteBlock(Cubic[] cubics) {
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
    publicIP = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
  }

  public class Peer {



    // to do this we need to assign fixed id's to each peer from the server
    // ++ make a peer timeout on the client side as well

    public float lastPing;

    public int id;
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
        new Block(type, color),
        new Block(type, color),
        new Block(type, color),
        new Block(type, color),
        new Block(type, color),
        new Block(type, color)
      };
      cubics = new Cubic[] {
        new Cubic(),
        new Cubic(),
        new Cubic(),
        new Cubic(),
        new Cubic(),
        new Cubic()
      };
      // voice = Sound.CreateStream(0.5f);
      // voiceInst = voice.Play(Vec3.Zero, 0.5f);
    }

    BlockCon dBlock = new BlockCon();
    BlockCon sBlock = new BlockCon();

    CubicCon cubicCon = new CubicCon();

    public void Step(Controller domCon, Controller subCon) {
      dBlock.Step(domCon, cursor0, ref sBlock, ref blocks);
      sBlock.Step(subCon, cursor3, ref dBlock, ref blocks);

      cubicCon.Step(domCon, subCon, this, ref cubics);

      Draw(false);
    }

    class BlockCon {
      public int index = -1;
      public Vec3 offset = Vec3.Zero;
      public Quat heldRot = Quat.Identity, spinRot = Quat.Identity, spinDelta = Quat.Identity;
      public Quat oldConRot = Quat.Identity, oldHeldRot = Quat.Identity;
      public Vec3 delta = Vec3.Zero, momentum = Vec3.Zero, angularMomentum = Vec3.Zero;

      public void Step(Controller con, Vec3 cursor, ref BlockCon otherBlockCon, ref Block[] blocks) {
        if (con.stickClick.IsJustActive()) {
          if (index < 0) {
            for (int i = 0; i < blocks.Length; i++) {
              if (!blocks[i].active) {
                blocks[i].Enable(cursor, Quat.Identity);
                break;
              }
            }
            blocks[PullRequest.RandomRange(0, blocks.Length)].Enable(cursor, Quat.Identity);
          } else {
            blocks[index].Disable();
            index = -1;
          }
        }

        Quat conRotDelta = (con.aim.orientation * oldConRot.Inverse).Normalized;

        if (con.grip > 0.5f) {
          if (index < 0) {
            // BLOCK EXCHANGE
            // loop over peer blocks as well
            // disable theirs ? (id of the peer, index of block)
            // wait for their block to be disabled
            // recycle one of yours to replace it

            for (int i = 0; i < blocks.Length; i++) {
              Pose blockPose = blocks[i].solid.GetPose();
              Bounds bounds = new Bounds(Vec3.Zero, Vec3.One);
              if (blocks[i].active && bounds.Contains(blockPose.orientation.Inverse * (cursor - blockPose.position))) {
                index = i;
                if (otherBlockCon.index == i) {
                  otherBlockCon.index = -1;
                }
                // block.color = colorCube.color;
                // clear
                spinRot = spinDelta = Quat.Identity;
                blocks[i].solid.SetAngularVelocity(Vec3.Zero);
                blocks[i].solid.SetVelocity(Vec3.Zero);
                // set
                heldRot = (con.aim.orientation.Inverse * blockPose.orientation).Normalized;
                offset = blockPose.orientation.Inverse * (blockPose.position - cursor);

                // 
                break;
              }
            }
          }

          if (index >= 0) {
            Quat newRot = (con.aim.orientation * heldRot * spinRot).Normalized;
            // trackballer
            if (con.IsX2Pressed) {
              spinDelta = Quat.Slerp(
                spinDelta.Normalized,
                (newRot.Inverse * conRotDelta * newRot).Normalized,
                Time.Elapsedf / 0.1f
              );
            }
            spinRot *= spinDelta;
            Quat toRot = (con.aim.orientation * heldRot * spinRot).Normalized;
            Vec3 toPos = cursor + (con.aim.orientation * heldRot * spinRot).Normalized * offset;
            // cursor - offset;
            blocks[index].solid.Move(toPos, toRot);

            Quat newHeldRot = blocks[index].solid.GetPose().orientation;
            angularMomentum = Vec3.Lerp(angularMomentum, PullRequest.AngularDisplacement((newHeldRot * oldHeldRot.Inverse).Normalized), Time.Elapsedf / 0.1f);
            oldHeldRot = newHeldRot;

            delta = (cursor + (con.aim.orientation * heldRot * spinRot).Normalized * offset) - blocks[index].solid.GetPose().position;
            momentum = Vec3.Lerp(momentum, delta, Time.Elapsedf / 0.1f);
          }
        } else {
          if (index >= 0) {
            blocks[index].solid.SetAngularVelocity(angularMomentum / Time.Elapsedf);
            blocks[index].solid.SetVelocity(momentum / Time.Elapsedf);
          }
          index = -1;
        }

        oldConRot = con.aim.orientation;
      }
    }

    class CubicCon {
      public void Step(Controller domCon, Controller subCon, Peer peer, ref Cubic[] cubics) {
        bool place = domCon.IsX2JustPressed;
        if (place) {
          for (int i = 0; i < cubics.Length; i++) {
            if (!cubics[i].active) {
              cubics[i].Enable(peer.cursor0, peer.cursor1, peer.cursor2, peer.cursor3, peer.color);
              break;
            }
          }
          cubics[PullRequest.RandomRange(0, cubics.Length)].Enable(peer.cursor0, peer.cursor1, peer.cursor2, peer.cursor3, peer.color);
        }
      }
    }

    public void Draw(bool body) {
      if (body){
        Cube(Matrix.TRS(cursor0, Quat.Identity, Vec3.One * 0.05f), color);
        Cube(headset.ToMatrix(Vec3.One * 0.3f), color);
        Cube(offHand.ToMatrix(Vec3.One * 0.1f), color);
        Cube(mainHand.ToMatrix(Vec3.One * 0.1f), color);

        Bezier.Draw(cursor0, cursor1, cursor2, cursor3, Color.White);
      }
      // cubicFlow.Draw(peer.cursorA, peer.cursorB, peer.cursorC, peer.cursorD);

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
      meshCube.Draw(matCube, m, color);
    }
  }
}

public class Block {
  public static Mesh mesh = Default.MeshCube;
  public static Material mat = Default.Material;

  public bool active = false;
  public Solid solid;

  public Color color;

  // if you grab someone else's it becomes your own
  // how to communicate to the other peer that you have grabbed it?
  // public int request; // request ownership
  // public int owner; // then if owner continue as usual
  // public bool busy; // marked as held so no fighting
  public Block(SolidType type, Color color) {
    this.solid = new Solid(Vec3.Zero, Quat.Identity, type);
    this.solid.AddBox(Vec3.One, 3);
    this.color = color;
    Disable();
  }

  // public Block(Vec3 pos, Quat rot, SolidType type, Color color) {
  //   this.solid = new Solid(pos, rot, type);
  //   this.solid.AddBox(Vec3.One, 1);
  //   this.color = color;
  // }

  public void Enable(Vec3 pos, Quat rot) {
    solid.SetAngularVelocity(Vec3.Zero);
    solid.SetVelocity(Vec3.Zero);
    solid.Teleport(pos, rot);
    solid.Enabled = active = true;
  }

  public void Disable() {
    solid.Enabled = active = false;
  }

  public void Draw() {
    if (active) {
      mesh.Draw(mat, solid.GetPose().ToMatrix(), color);
    }
  }
}

public class Cubic {
  public bool active;
  public Vec3 p0, p1, p2, p3;
  public Color color;

  public Cubic() {
    color = Color.White;
    active = false;
  }

  public void Enable(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, Color c) {
    this.p0 = p0;
    this.p1 = p1;
    this.p2 = p2;
    this.p3 = p3;
    color = c;
    active = true;
  }

  public void Disable() {
    active = false;
  }

  public void Draw() {
    if (active) {
      Bezier.Draw(p0, p1, p2, p3, color);
    }
  }
}