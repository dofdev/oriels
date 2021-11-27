using StereoKit;
using System;
using System.Net;
using System.Net.Sockets;
using FlatBuffers;
// using NetData;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

public class MonoNet {
  public int myID;
  public MonoNet(int myID) {
    this.myID = myID;
  }
  public Socket socket;
  // public NetworkStream stream;
  byte[] data;
  int head;

  public Peer[] peers;

  public Vec3 cursor; // are these stored here???
  public Pose headset;
  public Pose offHand;
  public Pose mainHand;

  public async void Start() {
    string publicIP, localIP;
    // GetIPs();
    void GetIPs()
    {
      using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
      {
        socket.Connect("8.8.8.8", 65530);
        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
        localIP = endPoint.Address.ToString();
      }
      // Console.WriteLine("Your local IP is: " + localIP);
      publicIP = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
      // Console.WriteLine("Your IP is: " + publicIP);
    }
    
    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    string ip = "192.168.1.70";
    // ip = "139.177.201.219";
    EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), 1234);
    socket.Connect(serverEndPoint);
    data = new byte[1024];
    peers = new Peer[4];

    Thread.Sleep(1000);

    bool running = true;
    while (running) {
      while (socket.Available > 0) {
        try  {socket.Receive(data, 0, data.Length, SocketFlags.None);}
        catch (Exception e) {
          Console.WriteLine("can't connect to the server");
          return;
        }
        // ByteBuffer bb = new ByteBuffer(data);
        
        // NetData.Peer peer = NetData.Peer.GetRootAsPeer(bb);
        // int id = peer.Id;
        head = 0;
        int id = ReadInt();
        if (id != 0) {
          for (int i = 0; i < peers.Length; i++) {
            if (peers[i] != null) {
              if (peers[i].id == id) {
                // peers[i].cursor = NetVec3(peer.Cursor.Value);
                peers[i].cursor = ReadVec3();
                break;
              }
            } else {
              // if (peer.Cursor.HasValue) {
              //   peers[i] = new Peer(id, NetVec3(peer.Cursor.Value));
              //   break;
              // }
              peers[i] = new Peer(id, ReadVec3());
              break;
            }
          }
        }
      }
      
      // FlatBufferBuilder fbb = new FlatBufferBuilder(1024);
      // NetData.Peer.StartPeer(fbb);
      // NetData.Peer.AddId(fbb, myID);
      // NetData.Peer.AddCursor(fbb, Vec3Net(cursor, ref fbb));
      // var p = NetData.Peer.EndPeer(fbb);
      // fbb.Finish(p.Value);
      // socket.Send(fbb.SizedByteArray());

      data = new byte[1024];
      head = 0;
      WriteInt(myID);
      WriteVec3(cursor);
      socket.Send(data);

      await Task.Delay(100);
    }
    socket.Close();
  }

  public Vec3 NetVec3(NetData.Vec3 v) {
    return new Vec3(v.X, v.Y, v.Z);
  } Offset<NetData.Vec3> Vec3Net(Vec3 v, ref FlatBufferBuilder fbb) {
    return NetData.Vec3.CreateVec3(fbb, v.x, v.y, v.z);
  }

  public Quat NetQuat(NetData.Quat q) {
    return new Quat(q.X, q.Y, q.Z, q.W);
  }

  public Pose NetPose(NetData.Pose p) {
    return new Pose(NetVec3(p.Pos), NetQuat(p.Rot));
  }

  int ReadInt() {
    int value = BitConverter.ToInt32(data, head);
    head += 4;
    return value;
  } void WriteInt(int value) {
    BitConverter.GetBytes(value).CopyTo(data, head);
    head += 4;
  }

  Vec3 ReadVec3() {
    Vec3 value = new Vec3(
      BitConverter.ToSingle(data, head),
      BitConverter.ToSingle(data, head + 4),
      BitConverter.ToSingle(data, head + 8)
    );
    head += 12;
    return value;
  } void WriteVec3(Vec3 vec) {
    BitConverter.GetBytes(vec.x).CopyTo(data, head);
    BitConverter.GetBytes(vec.y).CopyTo(data, head + 4);
    BitConverter.GetBytes(vec.z).CopyTo(data, head + 8);
    head += 12;
  }

  Quat ReadQuat() {
    Quat value = new Quat(
      BitConverter.ToSingle(data, head),
      BitConverter.ToSingle(data, head + 4),
      BitConverter.ToSingle(data, head + 8),
      BitConverter.ToSingle(data, head + 12)
    );
    head += 16;
    return value;
  } void WriteQuat(Quat quat) {
    BitConverter.GetBytes(quat.x).CopyTo(data, head);
    BitConverter.GetBytes(quat.y).CopyTo(data, head + 4);
    BitConverter.GetBytes(quat.z).CopyTo(data, head + 8);
    BitConverter.GetBytes(quat.w).CopyTo(data, head + 12);
    head += 16;
  }

  Pose ReadPose() {
    return new Pose(
      ReadVec3(),
      ReadQuat()
    );
  } void WritePose(Pose pose) {
    WriteVec3(pose.position);
    WriteQuat(pose.orientation);
  }

  Mesh meshCube = Default.MeshCube;
  Material matCube = Default.Material;
  public void Cubee(Matrix m) {
    meshCube.Draw(matCube, m);
  }

  public class Peer
  {
    public int id;
    public Vec3 cursor;

    public Peer(int id, Vec3 cursor) {
      this.id = id;
      this.cursor = cursor;
    }
  }
}
