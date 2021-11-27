using StereoKit;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

public class MonoNet {
  public MonoNet() {
    Random rnd = new Random();
    me = new Peer(rnd.Next(1, 256)); // temp, until unique usernames
  }
  public Socket socket;
  byte[] data;
  int head;

  public Peer me;
  public Peer[] peers;

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
    peers = new Peer[64];

    Thread.Sleep(1000); // useful?

    bool running = true;
    while (running) {
      while (socket.Available > 0) {
        try  {socket.Receive(data, 0, data.Length, SocketFlags.None);}
        catch (Exception e) {
          Console.WriteLine($"can't connect to the server: {e}");
          return;
        }
        
        head = 0;
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
          peers[index].cursor = ReadVec3();
          peers[index].headset = ReadPose();
          peers[index].offHand = ReadPose();
          peers[index].mainHand = ReadPose();
        }
      }

      data = new byte[1024];
      head = 0;
      WriteInt(me.id);
      WriteVec3(me.cursor);
      WritePose(me.headset);
      WritePose(me.offHand);
      WritePose(me.mainHand);
      socket.Send(data);

      await Task.Delay(1);
    }
    socket.Close();
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
    public Pose headset;
    public Pose offHand;
    public Pose mainHand;

    public Peer(int id) {
      this.id = id;
    }
  }
}
