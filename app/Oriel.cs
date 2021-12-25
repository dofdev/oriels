using System;
using System.Runtime.InteropServices;
using StereoKit;

[StructLayout(LayoutKind.Sequential)]
struct BufferData {
  public Vec3 position;
  // public Vec3[] tri;
  public float time;
}

public class Oriel {
  public Bounds bounds;
  Material mat = new Material(Shader.FromFile("oriel.hlsl"));
  Material crown = new Material(Shader.FromFile("crown.hlsl"));
  Mesh mesh = Default.MeshCube;
  Mesh quad = Default.MeshQuad;
  Vec3 _dimensions;

  MaterialBuffer<BufferData> buffer;

  public void Start(int bufferIndex) {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));
    _dimensions = bounds.dimensions;
    buffer = new MaterialBuffer<BufferData>(bufferIndex);
  }

  BufferData data = new BufferData();
  public void Step(Vec3 p0) {
    data.position = p0;
    // data.a = new Color(1.0f, 0.5f, 0.5f);
    // data.b = new Color(0.5f, 1.0f, 0.5f);
    // data.c = new Color(0.5f, 0.5f, 1.0f);
    // data.tri = new Vec3[] {
    //   new Vec3(0, 0, 0),
    //   new Vec3(0, 0, 1),
    //   new Vec3(1, 0, 0),
    // };

    data.time = (float)Time.Total;
    buffer.Set(data);


    // circle around center
    // bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;
    // bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));


    mat.FaceCull = Cull.Front;
    mat.DepthTest = DepthTest.Always;
    mat.QueueOffset = 1000;
    
    mat.SetFloat("_distmax", 1000);
    mat.SetVector("_dimensions", bounds.dimensions);
    mat.SetVector("_center", bounds.center);
    // mat.Wireframe = true;

    Matrix m = Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions);
    Pose head = Input.Head;
    // Vec3 quadPos = head.position + head.Forward * 0.0021f;
    // if (bounds.Contains(head.position, head.position, 0.036f)) {
    //   mat.FaceCull = Cull.Front;
    //   m = Matrix.TRS(head.position, head.orientation, new Vec3(1.0f, 0.5f, 0.0088f * 2));
    //   Renderer.
    // }
    mesh.Draw(mat, m);

    // if (bounds.Contains(head.position, quadPos)) {
    //   quad.Draw(mat, Matrix.TRS(quadPos, Quat.LookAt(quadPos, head.position), Vec3.One * 0.5f));
    // }

    // instead of a quad, just slap the same mesh to the head


    // crown.SetVector("_center", bounds.center);


    // crown.SetFloat("_height", bounds.dimensions.y);
    // crown.SetFloat("_ypos", bounds.center.y);
    // crown.FaceCull = Cull.Front;
    // crown.Transparency = Transparency.Add;
    // crown.DepthTest = DepthTest.Always;

    // // crown.QueueOffset = 0;
    // // crown.DepthWrite = false;

    // mesh.Draw(crown, Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
  }
}