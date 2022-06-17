using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using Unity.Collections;
using Unity.Networking.Transport;
using MLAPI.Puncher.Client;
using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.ABI.Model;
using Nethereum.Contracts;
using Nethereum.JsonRpc.UnityClient;
using TMPro;
using NaughtyAttributes;
using Shapes;

using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Quaternion = UnityEngine.Quaternion;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Random = UnityEngine.Random;

public class Monolith : ImmediateModeShapeDrawer
{
  public GOManager goManager;

  public Contract contract;
  public Peer peer;//, toPeer;
  public Rig rig;
  Oriel[] oriels = new Oriel[32];

  // wrap these in a dof class?
  public StretchCursor stretchCursor;
  public TwistCursor leftTwistCursor, rightTwistCursor;
  public Grabber leftGrabber, rightGrabber;
  // public Latent latent;
  // public Alpha alpha;

  public Render render;


  void Start()
  {
    goManager.Start("Prefabs/", this.transform);

    rig.Start(this);
    contract.Start(this);
    // toPeer.Start(this, "peer 2");
    peer.Start(this, "peer 1");

    noise = new Noise(9000);
    plane = goManager.GetPrefab("plane");

    stretchCursor.Start(this);
    leftTwistCursor.Start(this, rig.offHand);
    rightTwistCursor.Start(this, rig.mainHand);
    leftGrabber.Start(this, rig.offHand);
    rightGrabber.Start(this, rig.mainHand);
    // latent.Start(this);
    // alpha.Start(this);
    render.Start(this);
  }

  Noise noise;
  GameObject plane;
  public Vector3 planePos;

  void OnDisable()
  {
    // alpha.Stop();
    // peer.Stop();
  }

  void Update()
  {
    rig.Update();
    contract.Update();
    peer.Update();
    // toPeer.Update();

    Mesh meshPlane = plane.GetComponent<MeshFilter>().mesh;
    // set vertex colors to green
    List<Color> colors = new List<Color>();
    meshPlane.GetColors(colors);
    if (colors.Count != meshPlane.vertexCount)
    {
      colors.Clear();
      colors.AddRange(new Color[meshPlane.vertexCount]);
    }

    Color c = new Color(46 / 256f, 67 / 256f, 34 / 256f);
    for (int i = 0; i < colors.Count; i++)
    {
      colors[i] = c;
    }
    meshPlane.SetColors(colors);


    List<Vector3> verts = new List<Vector3>();
    meshPlane.GetVertices(verts);
    for (int i = 0; i < verts.Count; i++)
    {
      Vector3 meshV = verts[i];
      Vector3 noiseV = verts[i] + planePos;

      float x = noiseV.x / 3;
      float z = noiseV.z / 3;
      int xi = Mathf.FloorToInt(x);
      int zi = Mathf.FloorToInt(z);

      float c00 = noise.D2(xi, zi);
      float c10 = noise.D2(xi + 1, zi);
      float c01 = noise.D2(xi, zi + 1);
      float c11 = noise.D2(xi + 1, zi + 1);

      meshV.y = Mathf.LerpUnclamped(
        Mathf.LerpUnclamped(c00, c10, ut(x - xi)),
        Mathf.LerpUnclamped(c01, c11, ut(x - xi)),
        ut(z - zi)
      ) * 1.5f; // scale height

      float ut(float t)
      {
        // if negative, then flip
        return t < 0 ? 1 - Mathf.Abs(t) : t;
      }

      verts[i] = meshV;
    }
    meshPlane.SetVertices(verts);
    meshPlane.RecalculateNormals();
    meshPlane.RecalculateBounds();
    MeshCollider colPlane = plane.GetComponent<MeshCollider>();
    colPlane.sharedMesh = meshPlane;


    stretchCursor.Update();
    leftGrabber.Update();
    rightGrabber.Update();
    // leftTwistCursor.Update(leftGrabber.cursor);
    // rightTwistCursor.Update(rightGrabber.cursor);
    leftTwistCursor.sensitivity += rig.rHand.joystick.y * Time.deltaTime;
    rightTwistCursor.sensitivity = leftTwistCursor.sensitivity;
    leftTwistCursor.Update(rig.lHand.pos);
    rightTwistCursor.Update(rig.rHand.pos);
    // latent.Update();
    // alpha.Update();
    render.Update();
  }

  public override void DrawShapes(Camera cam)
  {
    render.DrawShapes(cam);
  }

  string log = "oriels 0.0.1\n(c) 2018-21 dofdev\n\n";
  void OnGUI()
  {
    // change gui font
    GUI.skin.font = (Font)Resources.Load("Fonts/DM-Mono");
    GUI.skin.label.fontSize = 14;
    GUI.skin.label.alignment = TextAnchor.UpperLeft;
    GUI.skin.label.normal.textColor = Color.white;
    GUI.skin.label.fontStyle = FontStyle.Normal;

    GUI.Label(new Rect(10, 10, 512, 256), log);
    // how to render censored *****

    // peer.newPeerIP = GUI.PasswordField(CenterRect(new Rect(0, Screen.height - 120, 256, 28)), peer.newPeerIP, '*');

    // // Debug.Log(peer.newPeerIP);

    // peer.connect = GUI.Button(CenterRect(new Rect(0, Screen.height - 80, 128, 28)), "connect");
    // peer.disconnect = GUI.Button(CenterRect(new Rect(0, Screen.height - 50, 128, 28)), "disconnect");

    // Rect CenterRect(Rect rect)
    // {
    //   rect.x = (Screen.width / 2) - (rect.width / 2);
    //   return rect;
    // }
  }
  public void Log(string s)
  {
    log += s + "\n";

    // if number of lines > 10 then remove the oldest line
    int lines = log.Split('\n').Length;
    if (lines > 10)
    {
      log = log.Substring(log.IndexOf('\n') + 1);
    }
  }

  [Button]
  public void Test()
  {

  }
}

[Serializable]
public class GOManager
{
  [HideInInspector] public GameObject[] prefabs;

  public void Start(string folderName, Transform parent)
  {
    prefabs = Resources.LoadAll<GameObject>(folderName + "/");
    for (int i = 0; i < prefabs.Length; i++)
    {
      string name = prefabs[i].name;
      prefabs[i] = GameObject.Instantiate(prefabs[i], parent);
      prefabs[i].name = name;
    }
  }

  public GameObject GetPrefab(string name)
  {
    for (int i = 0; i < prefabs.Length; i++)
    {
      if (prefabs[i].name.ToLower() == name.ToLower())
      {
        return prefabs[i];
      }
    }
    Debug.LogWarning("Prefab not found: " + name);
    return null;
  }
}

[Serializable]
public class Contract
{
  Monolith mono;

  string url = "http://localhost:7545"; // set your rpc url here
  string privateKey = "d838218fe7f406ad746a3eb70d5f8095f73f0d6f0e254f824323e0ef6fcbdd01";
  string[] accounts = {
    "0x03b60D1303328a2a1dae344D4A317ee26a2aaf04",
    "0x8250C318c76a336fC9FE6Eb39c115bBad62ed8a0",
    "0x81B6e80037FDa721aC96d6D38DD65d24463559C2"
  };

  string contractAddress = "0x58c58B56EC30628005823062A2238266e477f9e4";

  public void Start(Monolith mono)
  {
    this.mono = mono;

    mono.StartCoroutine(GetAccountBalance());
  }

  public void Update()
  {

  }

  public IEnumerator GetAccountBalance()
  {
    // var getBalanceRequest = new EthGetBalanceUnityRequest(url);

    // yield return getBalanceRequest.SendRequest(account, Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());

    // if (getBalanceRequest.Exception == null)
    // {
    //   var balance = getBalanceRequest.Result.Value;

    //   var n = Nethereum.Util.UnitConversion.Convert.FromWei(balance, 18).ToString("n8");
    //   mono.Log(n);
    // }
    // else
    // {
    //   mono.Log(getBalanceRequest.Exception.Message);
    // }

    // var newExpense = new QueryUnityRequest<NewExpenseFunction, NewExpenseFunctionOutput>(url, accounts[0]);
    // yield return newExpense.Query(new NewExpenseFunction()
    // {
    //   PayTo = accounts[2],
    //   Description = "test expense",
    //   Amount = 1000000000
    // }, contractAddress);
    // mono.Log(newExpense.Result.Index);

    var signExpense = new QueryUnityRequest<SignExpenseFunction, SignExpenseFunctionOutput>(url, accounts[1]);
    yield return signExpense.Query(new SignExpenseFunction()
    {
      Index = 0,
      Amount = 1000000000
    }, contractAddress);
    mono.Log(signExpense.Result.Message);
  }

  [Function("newExpense", "uint256")]
  public class NewExpenseFunction : FunctionMessage
  {
    [Parameter("address", "payTo", 1)]
    public string PayTo { get; set; }
    [Parameter("string", "description", 2)]
    public string Description { get; set; }
    [Parameter("uint256", "amount", 3)]
    public int Amount { get; set; }
  }

  [FunctionOutput]
  public class NewExpenseFunctionOutput : IFunctionOutputDTO
  {
    [Parameter("uint256", 1)]
    public int Index { get; set; }
  }

  [Function("signExpense", "string")]
  public class SignExpenseFunction : FunctionMessage
  {
    [Parameter("uint256", "index", 1)]
    public int Index { get; set; }
    [Parameter("uint256", "amount", 2)]
    public int Amount { get; set; }
  }

  [FunctionOutput]
  public class SignExpenseFunctionOutput : IFunctionOutputDTO
  {
    [Parameter("string", 1)]
    public string Message { get; set; }
  }
}

[Serializable]
public class Peer
{
  Monolith mono;
  string name;

  public bool connect;
  public bool disconnect;

  public NetworkDriver m_Driver;
  private List<Connection> connections = new List<Connection>();
  [Serializable]
  public class Connection
  {
    NetworkConnection connection;
    public IPAddress ip;

    public Connection(NetworkConnection connection, IPAddress ip)
    {
      this.connection = connection;
      this.ip = ip;
    }
  }

  private string puncherHost
  {
    get { return "139.177.201.219"; }
  }
  private ushort puncherPort = 6776;
  private List<Punch> punchList = new List<Punch>();
  [Serializable]
  public class Punch
  {
    public bool ping;
    public string ip;

    public Punch(bool ping, string ip)
    {
      this.ping = ping;
      this.ip = ip;
    }
  }

  [HideInInspector] public string newPeerIP;
  private string publicIP;
  private ushort port = 9000;

  public void Start(Monolith mono, string name)
  {
    this.mono = mono;
    this.name = name;

    // publicIP = new WebClient().DownloadString("https://ipv4.icanhazip.com/").TrimEnd();
    // Task.Factory.StartNew(() =>
    // {
    //   try
    //   {
    //     mono.Log(name + " is listening for a Punch");
    //     using (PuncherClient listenPunch = new PuncherClient(puncherHost, puncherPort))
    //     {
    //       while (true)
    //       {
    //         IPEndPoint endpoint = listenPunch.ListenForSinglePunch(new IPEndPoint(IPAddress.Any, port));
    //         mono.Log(name + " was punched");
    //       }
    //     }
    //   }
    //   catch (Exception e)
    //   {
    //     mono.Log(e.Message);
    //   }
    // });

    // m_Driver = NetworkDriver.Create();
    // mono.Log(name + " is allocated");

    // var endpoint = NetworkEndPoint.AnyIpv4;
    // endpoint.Port = port;
    // if (m_Driver.Bind(endpoint) != 0)
    // {
    //   mono.Log(name + " failed to bind to port " + endpoint.Port);
    // }
    // else
    // {
    //   m_Driver.Listen();
    //   mono.Log(name + " is listening on port " + endpoint.Port);
    // }
  }


  public void Connect()
  {
    // // check for validity
    // // check for duplicates
    // mono.Log(name + " is winding up...");
    // Task.Factory.StartNew(() =>
    // {
    //   Punch punch = new Punch(true, newPeerIP);
    //   punchList.Add(punch);

    //   using (PuncherClient punchPeer = new PuncherClient(puncherHost, puncherPort))
    //   {
    //     while (true)
    //     {
    //       if (!punch.ping) { continue; }
    //       if (punchPeer.TryPunch(IPAddress.Parse(punch.ip), out IPEndPoint punchResult))
    //       {
    //         mono.Log(name + " punched through to port " + punchResult.Port);

    //         var endpoint = NetworkEndPoint.LoopbackIpv4;
    //         endpoint.Port = port;
    //         if (newPeerIP != publicIP)
    //         {
    //           endpoint = NetworkEndPoint.Parse(punchResult.Address.ToString(), (ushort)punchResult.Port);
    //         }
    //         connections.Add(m_Driver.Connect(endpoint));

    //         mono.Log(name + " attempting to connect to the punched");
    //       }
    //       else
    //       {
    //         mono.Log(name + " failed to punch");
    //       }
    //     }
    //   }
    // });
  }

  public void Disconnect()
  {
    // for (int i = 0; i < connections.Length; i++)
    // {
    //   if (connections[i].IsCreated)
    //   {
    //     connections[i].Disconnect(m_Driver);
    //     connections[i] = default(NetworkConnection);
    //   }
    // }
    // mono.Log(name + " is disconnecting");
  }

  public void Stop()
  {
    // m_Driver.Dispose();
    // mono.Log(name + " is disposed of");
  }

  public void Update()
  {
    // if (connect)
    // {
    //   Connect();
    //   connect = false;
    // }

    // if (disconnect)
    // {
    //   Disconnect();
    //   disconnect = false;
    // }

    // m_Driver.ScheduleUpdate().Complete();

    // AcceptNewConnections();
    // void AcceptNewConnections()
    // {
    //   NetworkConnection c;
    //   while ((c = m_Driver.Accept()) != default(NetworkConnection))
    //   {
    //     // check NetworkConnection ip address
    //     IPAddress ip = c.RemoteEndPoint.Ipv4Address;
    //     System.Net.Sockets.UdpReceiveResult result;




    //     connections.Add(new Connection(c, ip));
    //     mono.Log(name + " accepted a connection");

    //     // what if both peers are punching

    //   }
    // }

    // DataStreamReader reader;
    // for (int i = 0; i < connections.Length; i++)
    // {
    //   if (!connections[i].IsCreated)
    //   {
    //     connections.RemoveAtSwapBack(i);
    //     --i;
    //     continue;
    //   }

    //   NetworkEvent.Type cmd;
    //   while ((cmd = m_Driver.PopEventForConnection(connections[i], out reader)) != 0)
    //   {
    //     switch (cmd)
    //     {
    //       case NetworkEvent.Type.Connect:
    //         mono.Log(name + " connected to peer");
    //         m_Driver.BeginSend(NetworkPipeline.Null, connections[i], out var writer);
    //         writer.WriteUInt(1);
    //         m_Driver.EndSend(writer);
    //         mono.Log(name + " sent 1 to peer");
    //         break;
    //       case NetworkEvent.Type.Data:
    //         mono.Log(name + " received data from peer");
    //         uint value = reader.ReadUInt();
    //         mono.Log(name + " received " + value);
    //         break;
    //       case NetworkEvent.Type.Disconnect:
    //         mono.Log(name + " disconnected from a peer");
    //         connections[i] = default(NetworkConnection);
    //         break;
    //       default:
    //         mono.Log(name + " received an unknown network event");
    //         break;
    //     }
    //   }
    // }
  }
}

[Serializable]
public class Rig
{
  Monolith mono;

  [HideInInspector]
  public Transform headset;
  public PhysicalInput lHand, rHand;
  public PhysicalInput offHand, mainHand;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    headset = mono.goManager.GetPrefab("headset").transform;
    // convert to goManager

    lHand.Start();
    rHand.Start();

    bool lefty = false;
    if (lefty)
    {
      offHand = rHand;
      mainHand = lHand;
    }
    else
    {
      offHand = lHand;
      mainHand = rHand;
    }
  }

  XRHMD hmd;
  XRController lCon, rCon;
  public void Update()
  {
    Vector3 height = new Vector3(0, 0.67f, 0);
    RaycastHit hit;
    // raycast down to find the floor
    if (Physics.Raycast(headset.position, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
    {
      height += hit.point;
    }

    if (hmd != null && hmd.wasUpdatedThisFrame)
    {
      headset.position = height + hmd.centerEyePosition.ReadValue();
      headset.rotation = hmd.centerEyeRotation.ReadValue();
    }
    else
    {
      hmd = InputSystem.GetDevice<XRHMD>();
    }

    if (lCon != null && lCon.wasUpdatedThisFrame)
    {
      lHand.pos = height + (Vector3)lCon.TryGetChildControl("pointerPosition").ReadValueAsObject();
      lHand.rot = (Quaternion)lCon.TryGetChildControl("pointerRotation").ReadValueAsObject();

      lHand.faceBtn.Set(lCon.TryGetChildControl("primarybutton").IsPressed());
      lHand.triggerBtn.Set(lCon.TryGetChildControl("triggerpressed").IsPressed());
      lHand.joystick = (Vector2)lCon.TryGetChildControl("joystick").ReadValueAsObject();

      // Debug.Log all the controls
      // UnityEngine.InputSystem.Utilities.ReadOnlyArray<InputControl> controls = lCon.allControls;
      // foreach (InputControl control in controls)
      // {
      //   Debug.Log(control.name + ": " + control.ReadValueAsObject());
      // }
    }
    else
    {
      lCon = XRController.leftHand;
    }

    if (rCon != null && rCon.wasUpdatedThisFrame)
    {
      rHand.pos = height + (Vector3)rCon.TryGetChildControl("pointerPosition").ReadValueAsObject();
      rHand.rot = (Quaternion)rCon.TryGetChildControl("pointerRotation").ReadValueAsObject();

      rHand.faceBtn.Set(rCon.TryGetChildControl("primarybutton").IsPressed());
      rHand.triggerBtn.Set(rCon.TryGetChildControl("triggerpressed").IsPressed());
      rHand.joystick = (Vector2)rCon.TryGetChildControl("joystick").ReadValueAsObject();
    }
    else
    {
      rCon = XRController.rightHand;
    }
  }
}

[Serializable]
public class PhysicalInput
{
  public Vector3 pos;
  public Quaternion rot;

  public void Start()
  {
    this.pos = Vector3.zero;
    this.rot = Quaternion.identity;
  }

  // trigger
  // triggerTouch
  public Btn triggerBtn;
  public Btn faceBtn;
  public Vector2 joystick;
}

[Serializable]
public class Btn
{
  public bool down;
  public bool held;
  public bool up;

  public void Set(bool held)
  {
    down = up = false;
    if (this.held)
    {
      if (!held)
      {
        up = true;
      }
    }
    else
    {
      if (held)
      {
        down = true;
      }
    }
    this.held = held;
  }
}

[Serializable]
public class Oriel
{
  Monolith mono;

  public Vector3 pos;
  public Quaternion rot;
  public Vector3 size;

  // oriels do not exist on their own
  // an project creates one to be seen by the user
  // its a way to manage multiple spatial apps simultaneously
  // without conflicts

  // the fullscreen equivalent is either
  // a orbital view oriel
  // or a maximized oriel

  public void Start(Monolith mono)
  {
    this.mono = mono;
    size = new Vector3(0.3f, 0.2f, 0.2f);
  }

  public void Update()
  {

  }

  public void Render()
  {

  }
}

[Serializable]
public class StretchCursor
{
  Monolith mono;

  public Vector3 cursor;
  public float stretch;

  float deadZone = 0.1f;

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public void Update()
  {
    PhysicalInput offHand = mono.rig.offHand;
    PhysicalInput mainHand = mono.rig.mainHand;

    stretch = Mathf.Max((offHand.pos - mainHand.pos).magnitude - deadZone, 0);
    cursor = mainHand.pos + mainHand.rot * Vector3.forward * stretch * 3;
  }
}

[Serializable]
public class TwistCursor
{
  Monolith mono;
  PhysicalInput hand;

  public Vector3 cursor;
  public float sensitivity;
  public float twist;
  public bool outty;

  public void Start(Monolith mono, PhysicalInput hand)
  {
    this.mono = mono;
    this.hand = hand;
    this.sensitivity = 3.5f;
  }

  public void Update(Vector3 origin)
  {
    // the z twist of the mainHand relative to where its pointing
    Quaternion rel = Quaternion.LookRotation(hand.rot * Vector3.forward);
    twist = Vector3.Angle(rel * Vector3.up, hand.rot * Vector3.up) / 180;
    // detect if twist is left or right
    outty = (Vector3.Dot(Vector3.up, hand.rot * Vector3.left) > 0);
    // twist = Mathf.Max(twist - 0.05f, 0);
    cursor = origin + hand.rot * Vector3.forward * twist * sensitivity;
  }
}

[Serializable]
public class Grabber
{
  Monolith mono;
  PhysicalInput hand;

  public Vector3 cursor;
  public Vector3 origin;
  public Vector3 originWorldPos;
  public float stretch;

  public void Start(Monolith mono, PhysicalInput hand)
  {
    this.mono = mono;
    this.hand = hand;
  }

  public void Update()
  {
    Transform head = mono.rig.headset;
    Quaternion headTurn = Quaternion.Euler(0, head.rotation.eulerAngles.y, 0);
    if (mono.rig.mainHand.triggerBtn.down)
    {
      origin = hand.pos - head.position;

      origin = Quaternion.Inverse(headTurn) * origin;
    }
    originWorldPos = head.position + headTurn * origin;
    Vector3 dir = (hand.pos - originWorldPos).normalized;
    stretch = Vector3.Distance(hand.pos, originWorldPos);
    cursor = hand.pos + dir * stretch * 3;
  }
}

[Serializable]
public class Latent
{
  Monolith mono;
  // space.add(relative possible position)
  // direction = (space[x] - from).normalized
  // position += direction * (forward - back)

  // every couple months
  public string[] questions = new string[] {
    "what are the same problems that im running into repeatedly",
    "if i take a leveraged risk, what is the worst thing that could happen",
    "what are the most critical things im missing",
    "how do we 10x our results",
    "what do i need to increase the value of what im doing 10x",
    "how do we take our 1-3-5 year plans and turn them into 1-3-5 month plans",
    "if i only worked 2 hours a week on my business/venture/goal, what would i do",
    "am i delegating appropriately",
    "am i investing a certain % of my income (the highest possible) back into making more / my business",
    "what if i only could subtract things from my life"
  };

  // monthly
  public string[] directions = new string[] {
    "what has had the greatest negative emotional impact on my life in the last month",
    "what 20% of activities / people are taking 80% of my time",
    "what are 20% of the activities and people that are producting 80% of my problems",
    "what has had the greatest positive emotional impact on my life in the last month",
    "what 20% of activities / people are making the most 80% of my time",
    "what are 20% of the activities / people that are producting 80% of what I want"
  };

  // weekly
  public string[] todo = new string[5];

  public string[] nodo = new string[5];

  // respond to all these prompts in a pseudo code fashion

  public void Start(Monolith mono)
  {
    this.mono = mono;
  }

  public void Update()
  {
    // I want the higher level visualization to be very spatial
    // as you respond to the prompts, you'll place the responses in a space relative to your position
    // the todo nodo are just vectors drawn from your position toward the responses *not always connected*
    // when you review
    // the actions you made can be immediately visualized by having your position change 
    // by the sum of vectors acted upon

    // its all persistent!
    // drag the space around to move
    // zoomed out = points
    // zoomed in =  text


    // dofs
    // vien systems
  }
}

[Serializable]
public class Alpha
{
  Monolith mono;

  // GOAL: to layout the patterns of the alphabet to were I can better optimize them
  // for a lower key count input device for VR / mobile
  // making the alphabet more phoenetically consistant would be cool as a thing to tackle after

  public GameObject textPrefab;
  public Material textMaterial;
  public Transform[,,] textMeshes = new Transform[3, 3, 3];

  Data data = null;
  public void Start(Monolith mono)
  {
    this.mono = mono;
    data = DataManager.Deserialize();
    if (data == null)
    {
      data = new Data();
      data.map = new char[3, 3, 3] {
        {
          {'a', 'b', 'c'},
          {'d', 'e', 'f'},
          {'g', 'h', 'i'}
        },
        {
          {'j', 'k', 'l'},
          {'m', 'n', 'o'},
          {'p', 'q', 'r'}
        },
        {
          {'s', 't', 'u'},
          {'v', 'w', 'x'},
          {'y', 'z', ' '}
        }
      };
    }

    // setup text meshes in 3d space
    for (int i = 0; i < data.map.GetLength(0); i++)
    {
      for (int j = 0; j < data.map.GetLength(1); j++)
      {
        for (int k = 0; k < data.map.GetLength(2); k++)
        {
          GameObject textObj = GameObject.Instantiate(textPrefab);
          textObj.transform.position = new Vector3(i, j, k);
          TextMeshPro textMesh = textObj.transform.GetComponent<TextMeshPro>();
          // set color based on position xyz -> rgb
          textMesh.color = new Color(
            i / ((float)data.map.GetLength(0) - 1),
            j / ((float)data.map.GetLength(1) - 1),
            k / ((float)data.map.GetLength(2) - 1)
          );
          // set material
          textMesh.fontMaterial = textMaterial;
          textMeshes[i, j, k] = textObj.transform;
        }
      }
    }
  }

  public void Stop()
  {
    DataManager.Serialize(data);
  }

  Vector3Int cursor = new Vector3Int(0, 0, 0);
  Quaternion viewRot = Quaternion.identity;
  public void Update()
  {
    // if (SceneView.HasOpenInstances<SceneView>())
    // {
    //   SceneView view = SceneView.lastActiveSceneView;
    //   if (view != null)
    //   {
    //     viewRot = view.rotation;
    //   }
    // }

    Vector3Int oldCursor = cursor;
    // move cursor WASD
    // and wrap around
    // increment relative to viewRot
    // by rotating the vector by viewRot
    Keyboard keeb = Keyboard.current;
    if (keeb.aKey.wasPressedThisFrame)
    {
      Vector3 v = viewRot * Vector3.left;
      cursor += new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
    if (keeb.dKey.wasPressedThisFrame)
    {
      Vector3 v = viewRot * Vector3.right;
      cursor += new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
    if (keeb.wKey.wasPressedThisFrame)
    {
      Vector3 v = viewRot * Vector3.up;
      cursor += new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
    if (keeb.sKey.wasPressedThisFrame)
    {
      Vector3 v = viewRot * Vector3.down;
      cursor += new Vector3Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
    // wrap around
    if (cursor.x >= data.map.GetLength(0)) { cursor.x = 0; }
    if (cursor.x < 0) { cursor.x = data.map.GetLength(0) - 1; }
    if (cursor.y >= data.map.GetLength(1)) { cursor.y = 0; }
    if (cursor.y < 0) { cursor.y = data.map.GetLength(1) - 1; }
    if (cursor.z >= data.map.GetLength(2)) { cursor.z = 0; }
    if (cursor.z < 0) { cursor.z = data.map.GetLength(2) - 1; }


    // shift character
    // by moving the character in the data.map
    // based on oldCursor and cursor
    if (oldCursor != cursor && keeb.leftShiftKey.isPressed)
    {
      char oldChar = data.map[oldCursor.x, oldCursor.y, oldCursor.z];
      char newChar = data.map[cursor.x, cursor.y, cursor.z];
      data.map[oldCursor.x, oldCursor.y, oldCursor.z] = newChar;
      data.map[cursor.x, cursor.y, cursor.z] = oldChar;
    }

    // this is 3d shit, use your VR shit ^-^

    // textMeshes face camera
    for (int i = 0; i < textMeshes.GetLength(0); i++)
    {
      for (int j = 0; j < textMeshes.GetLength(1); j++)
      {
        for (int k = 0; k < textMeshes.GetLength(2); k++)
        {
          TextMeshPro textMesh = textMeshes[i, j, k].GetComponent<TextMeshPro>();
          // set text to char from data.map
          textMesh.text = data.map[i, j, k].ToString();
          textMeshes[i, j, k].transform.rotation = viewRot;

          if (i == cursor.x && j == cursor.y && k == cursor.z)
          {
            // bold
            textMesh.fontStyle = FontStyles.Bold;
            textMesh.outlineWidth = 0.1f;
            // inverted color
            textMesh.outlineColor = new Color(1 - textMesh.color.r, 1 - textMesh.color.g, 1 - textMesh.color.b);
          }
          else
          {
            // normal
            textMesh.fontStyle = FontStyles.Normal;
            textMesh.outlineWidth = 0;
          }
        }
      }
    }
  }
}

[Serializable]
public class Render
{
  Monolith mono;

  Material[] materials;
  Mesh[] meshes;

  public void Start(Monolith mono)
  {
    this.mono = mono;

    materials = Resources.LoadAll<Material>("Materials/");
    meshes = Resources.LoadAll<Mesh>("Meshes/");

    spectator = mono.goManager.GetPrefab("spectator").transform;
  }
  Transform spectator;

  public void DrawShapes(Camera cam)
  {
    using (Draw.Command(cam))
    {
      Draw.LineGeometry = LineGeometry.Volumetric3D;
      Draw.ThicknessSpace = ThicknessSpace.Meters;
      // Draw.ZTest = UnityEngine.Rendering.CompareFunction.Less;
      // Draw.ZOffsetFactor = 0

      Draw.Matrix = mono.transform.localToWorldMatrix;
      // put it in camera space? to fix order issues?

      // draw lines
      // Draw.Sphere(mono.leftTwistCursor.cursor, 0.1f, Color.black);
      // float stretch = (1 - mono.leftGrabber.stretch) * (1 - mono.leftTwistCursor.twist);
      // Draw.Thickness = 0.2f * stretch;
      // Draw.Line(mono.rig.lHand.pos, mono.leftGrabber.cursor, Color.white);
      // Draw.Line(mono.leftGrabber.cursor, mono.leftTwistCursor.cursor, Color.white);
      Draw.Thickness = 0.01f;
      // Draw.Line(mono.rig.rHand.pos, mono.rightTwistCursor.cursor, Color.white);

      // Draw.Sphere(mono.rightTwistCursor.cursor, 0.1f, Color.black);
      // stretch = (1 - mono.rightGrabber.stretch) * (1 - mono.rightTwistCursor.twist);
      // Draw.Thickness = 0.2f * stretch;
      // Draw.Line(mono.rig.rHand.pos, mono.rightGrabber.cursor, Color.white);
      // Draw.Line(mono.rightGrabber.cursor, mono.rightTwistCursor.cursor, Color.white);

      // Draw.Line(mosno.rig.lHand.pos, mono.leftTwistCursor.cursor, Color.white);

      // just hands with twist cursors
      // deadzone 0)

      if (mono.rig.rHand.triggerBtn.down)
      {
        toggle = !toggle;
      }

      Vector3 p0 = mono.rig.lHand.pos;
      Vector3 p1 = mono.leftTwistCursor.cursor;
      Vector3 p2 = mono.rightTwistCursor.cursor;
      Vector3 p3 = mono.rig.rHand.pos;
      if (toggle && !mono.leftTwistCursor.outty)
      {
        Vector3 pp = p0;
        p0 = p1;
        p1 = pp;
      }
      if (toggle && mono.rightTwistCursor.outty)
      {
        Vector3 pp = p2;
        p2 = p3;
        p3 = pp;
      }
      Vector3 lightPos = p0;
      Vector3 pastPos = p0;
      for (int i = 0; i < 64; i++)
      {
        float t = i / 63.0f;
        Vector3 a = Vector3.Lerp(p0, p1, t);
        Vector3 b = Vector3.Lerp(p1, p2, t);
        Vector3 c = Vector3.Lerp(p2, p3, t);

        Vector3 pos = Vector3.Lerp(Vector3.Lerp(a, b, t), Vector3.Lerp(b, c, t), t);
        Draw.Line(pastPos, pos, Color.blue);
        pastPos = pos;

        if (i == lightIndex)
        {
          lightPos = pos;
        }
      }

      Draw.Sphere(lightPos, 0.02f, Color.white);
      lightStep += Time.deltaTime;
      if (lightStep > 0.0333f)
      {
        if (lightLeft)
        {
          if (lightIndex < 63)
          {
            lightIndex++;
            lightStep = 0;
          }
        }
        else
        {
          if (lightIndex > 0)
          {
            lightIndex--;
            lightStep = 0;
          }
        }
      }
      if (mono.rig.lHand.faceBtn.down)
      {
        lightLeft = true;
      }
      if (mono.rig.rHand.faceBtn.down)
      {
        lightLeft = false;
      }
    }
  }
  bool toggle = false;
  int lightIndex = 0;
  float lightStep = 0;
  bool lightLeft;

  public void Update()
  {
    // draw out the hands
    DrawMesh("controller", "default", mono.rig.lHand.pos, mono.rig.lHand.rot, 2);
    DrawMesh("controller", "default", mono.rig.rHand.pos, mono.rig.rHand.rot, 2);

    // draw out the cursor
    // DrawMesh("controller", "default", mono.stretchCursor.cursor, Quaternion.identity, 0.5f);

    DrawMesh("cursor", "default", mono.leftTwistCursor.cursor, Quaternion.identity, 0.02f);
    DrawMesh("cursor", "default", mono.rightTwistCursor.cursor, Quaternion.identity, 0.02f);

    // DrawMesh("cursor", "default", mono.leftGrabber.cursor, Quaternion.identity, 0.02f);
    // DrawMesh("cursor", "default", mono.rightGrabber.cursor, Quaternion.identity, 0.02f);

    spectator.position = mono.rig.headset.position + mono.rig.headset.rotation * Vector3.back * 0.666f;
    spectator.rotation = mono.rig.headset.rotation;
  }

  Matrix4x4 m4 = new Matrix4x4();
  void DrawMesh(string mesh, string mat, Vector3 pos, Quaternion rot, float scale)
  {
    m4.SetTRS(pos, rot, Vector3.one * scale);
    Graphics.DrawMesh(Mesh(mesh), m4, Mat(mat), 0);
  }

  public Material Mat(string name)
  {
    for (int i = 0; i < materials.Length; i++)
    {
      if (name.ToLower() == materials[i].name.ToLower())
      {
        return materials[i];
      }
    }
    Debug.LogWarning("Material not found: " + name);
    return null;
  }

  public Mesh Mesh(string name)
  {
    for (int i = 0; i < meshes.Length; i++)
    {
      if (meshes[i].name.ToLower() == name.ToLower())
      {
        return meshes[i];
      }
    }
    Debug.LogWarning("Mesh not found: " + name);
    return null;
  }
}

[Serializable]
public class Noise
{
  const uint CAP = 4294967295;
  const uint BIT_NOISE1 = 0xB5297A4D;
  const uint BIT_NOISE2 = 0x68E31DA4;
  const uint BIT_NOISE3 = 0x1B56C4E9;

  public uint seed;

  public Noise(uint seed)
  {
    this.seed = seed;
  }

  int position;
  public float value
  {
    get
    {
      float v = RNG(position, seed) / (float)CAP;
      position++;
      return v;
    }
  }

  public float D1(int position)
  {
    return RNG(position, seed) / (float)CAP;
  }

  public float D2(int x, int y)
  {
    // large prime number with non-boring bits
    const int PRIME = 198491317;
    return RNG(x + (PRIME * y), seed) / (float)CAP;
  }

  public float D3(int x, int y, int z)
  {
    // large prime number with non-boring bits
    const int PRIME1 = 198491317;
    const int PRIME2 = 6542989;
    return RNG(x + (PRIME1 * y) + (PRIME2 * z), seed) / (float)CAP;
  }

  public uint RNG(int position, uint seed)
  {
    uint mangled = (uint)position;
    mangled *= BIT_NOISE1;
    mangled += seed;
    mangled ^= mangled >> 8;
    mangled += BIT_NOISE2;
    mangled ^= mangled << 8;
    mangled *= BIT_NOISE3;
    mangled ^= mangled >> 8;
    return mangled;
  }
}