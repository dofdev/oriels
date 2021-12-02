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
          peers[index].cursorA = ReadVec3();
          peers[index].cursorB = ReadVec3();
          peers[index].cursorC = ReadVec3();
          peers[index].cursorD = ReadVec3();
          peers[index].headset = ReadPose();
          peers[index].offHand = ReadPose();
          peers[index].mainHand = ReadPose();
          ReadBlock(ref peers[index].blocks);
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

  string localIP, publicIP;
  void GetIPs() {
    using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0)) {
      socket.Connect("8.8.8.8", 65530);
      IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
      localIP = endPoint.Address.ToString();
    }
    publicIP = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
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
      this.solid.AddBox(Vec3.One, 1);
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

  public class Peer {



    // to do this we need to assign fixed id's to each peer from the server
    // ++ make a peer timeout on the client side as well



    public int id;
    public Vec3 cursorA, cursorB, cursorC, cursorD;
    public Pose headset;
    public Pose offHand;
    public Pose mainHand;
    public Block[] blocks;
    // public Sound voice;
    // public SoundInst voiceInst; // update position

    public Peer(int id, SolidType type, Color color) {
      this.id = id;
      blocks = new Block[] {
        new Block(type, color),
        new Block(type, color),
        new Block(type, color)
      };
      // voice = Sound.CreateStream(0.5f);
      // voiceInst = voice.Play(Vec3.Zero, 0.5f);
    }

    int blockIndex = -1;
    Vec3 blockOffset = Vec3.Zero;
    public void Step(Controller domCon) {
      if (domCon.IsX2JustPressed) {
        if (blockIndex < 0) {
          for (int i = 0; i < blocks.Length; i++) {
            if (!blocks[i].active) {
              blockIndex = i;
              blocks[i].Enable(cursorA, Quat.Identity);
              // blockOffset = blocks[i].solid.GetPose().position;
              break;
            }
          }
        } else {
          blocks[blockIndex].Disable();
          blockIndex = -1;
        }
      }

      if (domCon.grip > 0.5f) {
        if (blockIndex < 0) {
          for (int i = 0; i < blocks.Length; i++) {
            Pose blockPose = blocks[i].solid.GetPose();
            Bounds bounds = new Bounds(Vec3.Zero, Vec3.One);
            if (blocks[i].active && bounds.Contains(blockPose.orientation.Inverse * (cursorA - blockPose.position))) {
              blockOffset = cursorA - blockPose.position;
              // block.color = colorCube.color;
              blockIndex = i;
              break;
            }
          }
        } 
        
        if (blockIndex >= 0) {
          // trackballer
          blocks[blockIndex].solid.Move(cursorA - blockOffset, blocks[blockIndex].solid.GetPose().orientation);
        }
      } else {
        blockIndex = -1;
      }
      Draw(false);
    }

    public void Draw(bool body) {
      if (body){
        Cube(Matrix.TRS(cursorA, Quat.Identity, Vec3.One * 0.05f));
        Cube(headset.ToMatrix(Vec3.One * 0.3f));
        Cube(offHand.ToMatrix(Vec3.One * 0.1f));
        Cube(mainHand.ToMatrix(Vec3.One * 0.1f));
      }
      // cubicFlow.Draw(peer.cursorA, peer.cursorB, peer.cursorC, peer.cursorD);

      for (int i = 0; i < blocks.Length; i++) {
        blocks[i].Draw();
      }
    }

    static Mesh meshCube = Default.MeshCube;
    static Material matCube = Default.Material;
    public void Cube(Matrix m) {
      meshCube.Draw(matCube, m);
    }
  }
}
