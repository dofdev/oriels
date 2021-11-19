using System;
using StereoKit;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

class Program {
	static void Main(string[] args) {
    SKSettings settings = new SKSettings {
      appName = "oriels",
      assetsFolder = "Assets",
    };
		if (!SK.Initialize(settings))
      Environment.Exit(1);

    // TextStyle style = Text.MakeStyle(Font.FromFile("DMMono-Regular.ttf"), 0.1f, Color.White);

    Mono.Run();
	}
}

public static class Mono {

  public static Controller offHand, mainHand;

  public static Model model = Model.FromFile("cursor.glb", Shader.Default);

  public static void Run() {
    void GetIPs()
    {
      string localIP;
      using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
      {
        socket.Connect("8.8.8.8", 65530);
        IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
        localIP = endPoint.Address.ToString();
      }
      // Console.WriteLine("Your local IP is: " + localIP);
      string publicIP = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
      // Console.WriteLine("Your IP is: " + publicIP);
    }

    Peer peer0 = new Peer("peer0");
    Peer peer1 = new Peer("peer1");
    peer0.Start(false);
    peer1.Start(false);

    ColorCube cube = new ColorCube();
    OrbitalView.strength = 4;
    OrbitalView.distance = 0.4f;
    cube.thickness = 0.01f;

    ReachCursor reachCursor = new ReachCursor();
    SupineCursor supineCursor = new SupineCursor();
    ClawCursor clawCursor = new ClawCursor();

    Oriel oriel = new Oriel();

    oriel.Start();

    Lerper lerper = new Lerper();

    while (SK.Step(() => {
      offHand = Input.Controller(Handed.Left);
      mainHand = Input.Controller(Handed.Right);

      peer0.cursor = offHand.aim.position * new Vec3(1,1,-1);
      peer1.cursor = mainHand.aim.position * new Vec3(1,1,-1);

      model.Draw(Matrix.TS(peer0.peerCursor, 0.1f));
      model.Draw(Matrix.TS(peer1.peerCursor, 0.1f));

      // if (offHand.trigger.) {
      //   lerper.t = 0;
      // }
      // lerper.Step(1, false);
      // Console.WriteLine(lerper.t);

      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      // reachCursor.Step();
      // supineCursor.Step(
      //   new Pose(Vec3.Zero, offHand.aim.orientation),
      //   new Pose(mainHand.aim.position, mainHand.aim.orientation),
      //   mainHand.IsStickClicked
      // );
      // clawCursor.Step(
      //   Input.Head.position - Vec3.Up * 0.2f,
      //   new Pose(offHand.aim.position, offHand.aim.orientation),
      //   new Pose(mainHand.aim.position, mainHand.aim.orientation),
      //   mainHand.IsStickClicked
      // );
      
      // oriel.Step();

      // cursor.Draw(Matrix.S(0.1f));
    }));
    SK.Shutdown();
  }
}

public class Lerper
{
  public float t = 0;
  public float spring = 1;
  public float dampen = 1;
  float vel;

  public void Step(float to = 1, bool bounce = false)
  {
    float dir = to - t;
    vel += dir * spring * Time.Elapsedf;

    if (Math.Sign(vel) != Math.Sign(dir))
    {
      vel *= 1 - (dampen * Time.Elapsedf);
    }
    else
    {
      vel *= 1 - (dampen * 0.33f * Time.Elapsedf);
    }

    float newt = t + vel * Time.Elapsedf;
    if (bounce && (newt < 0 || newt > 1))
    {
      vel *= -0.5f;
      newt = Math.Clamp(newt, 0, 1);
    }

    t = newt;
  }

  public void Reset()
  {
    t = vel = 0;
  }
}

public class Peer {
  public string name;
  public Peer(string name) {
    this.name = name;
  }

  public Vec3 cursor; // is this stored here???
  public Vec3 peerCursor;

  public async void Start(bool log) {
    int port = 1234;
    string serverIP = "139.177.201.219";
    serverIP = "192.168.1.70";
    // try connecting to the server
    if (log) Console.WriteLine($"{name} attempting to connect to server...");

    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    EndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), port);
    socket.Connect(serverEndPoint);
    
    // send every 0.1 seconds
    while (true) {
      byte[] data = new byte[1024];
      int dataPos;

      // send a message to the server
      dataPos = 0;
      BitConverter.GetBytes(cursor.x).CopyTo(data, dataPos); dataPos += 4;
      BitConverter.GetBytes(cursor.y).CopyTo(data, dataPos); dataPos += 4;
      BitConverter.GetBytes(cursor.z).CopyTo(data, dataPos); dataPos += 4;
      socket.SendTo(data, serverEndPoint);

      // receive a message from the server
      while (socket.Available > 0){
        dataPos = 0;
        socket.ReceiveFrom(data, ref serverEndPoint);
        peerCursor.x = BitConverter.ToSingle(data, dataPos); dataPos += 4;
        peerCursor.y = BitConverter.ToSingle(data, dataPos); dataPos += 4;
        peerCursor.z = BitConverter.ToSingle(data, dataPos); dataPos += 4;
      }

      // sleep for 0.1 seconds
      await Task.Delay(100);
    }
  }
}

public class Oriel {
  public Bounds bounds;

  // render
  Model model = Model.FromFile("oriel.glb", Shader.FromFile("oriel.hlsl"));
  Vec3 _dimensions;
  public void Start() {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));
    _dimensions = bounds.dimensions;
  }
  
  public void Step() {
    // circle around center
    bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;


    bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));

    model.GetMaterial(0).Transparency = Transparency.Blend;
    model.GetMaterial(0).SetFloat("_height", bounds.dimensions.y);
    model.GetMaterial(0).SetFloat("_ypos", bounds.center.y);
    model.Draw(Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
  }
}

public static class PullRequest {
  public static void BoundsDraw(Bounds b, float thickness, Color color) {
    Vec3 c = Vec3.One / 2;
    Vec3 ds = b.dimensions;
    for (int i = 0; i < 4; i++) {
      Quat q = Quat.FromAngles(i * 90, 0, 0);
      Lines.Add(q * (new Vec3(0, 0, 0) - c) *  ds, q * (new Vec3(0, 1, 0) - c) *  ds, color, color, thickness);
      Lines.Add(q * (new Vec3(0, 1, 0) - c) *  ds, q * (new Vec3(1, 1, 0) - c) *  ds, color, color, thickness);
      Lines.Add(q * (new Vec3(1, 1, 0) - c) *  ds, q * (new Vec3(1, 0, 0) - c) *  ds, color, color, thickness);

      // convert to linepoints
    }
  }

  // amplify quaternions (q * q * lerp(q.i, q, %))
}
