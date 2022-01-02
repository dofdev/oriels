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
  Mesh mesh = Default.MeshCube;
  public float crown = 0.0666f;

  bool scalingOriel = false;
  bool draggingOriel = false;
  Vec3 orielOffset = Vec3.Zero;

  MaterialBuffer<BufferData> buffer;

  public void Start(int bufferIndex) {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));
    buffer = new MaterialBuffer<BufferData>(bufferIndex);
  }

  BufferData data = new BufferData();
  public void Step(Vec3 cursor0, Vec3 cursor3) {
    Controller rCon = Input.Controller(Handed.Right);
    Controller lCon = Input.Controller(Handed.Left);

    if (rCon.trigger > 0.5f && lCon.trigger > 0.5f) {
      if (!scalingOriel) {
        if (bounds.Contains(cursor0) || bounds.Contains(cursor3)) {
          scalingOriel = true;
        }
      } else {
        bounds.center = Vec3.Lerp(cursor0, cursor3, 0.5f);
        float distX = Math.Abs(cursor0.x - cursor3.x);
        float distY = Math.Abs(cursor0.y - cursor3.y);
        float distZ = Math.Abs(cursor0.z - cursor3.z);
        bounds.dimensions = new Vec3(distX, distY, distZ);
      }
    } else {
      scalingOriel = false;
    }

    if (!scalingOriel && rCon.trigger > 0.5f) {
      if (!draggingOriel) {
        bool inCrown = cursor0.y > (bounds.center.y + bounds.dimensions.y / 2.0) - crown;
        if (bounds.Contains(cursor0) && inCrown) {
          orielOffset = cursor0 - bounds.center;
          draggingOriel = true;
        }
      } else {
        bounds.center = cursor0 - orielOffset;
      }
    } else {
      draggingOriel = false;
    }




    data.position = cursor0;

    data.time = (float)Time.Total;
    buffer.Set(data);

    Matrix matrix = Matrix.TR(bounds.center + Vec3.Up * (float)Math.Sin(Time.Elapsedf), Quat.FromAngles(0, Time.Elapsedf * 60, 0)).Inverse;
    // matrix. = (float)Math.Sin(Time.Elapsedf);
    mat.SetMatrix("_matrix", matrix);

    // circle around center
    // bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;
    // bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));

    // bounds.center.y = (float)Math.Sin(Time.Totalf * 3) * 0.3f;

    mat.FaceCull = Cull.Front;
    // mat.DepthTest = DepthTest.Always;
    // mat.QueueOffset = 1000;
    
    mat.SetFloat("_distmax", 1000);
    mat.SetVector("_dimensions", bounds.dimensions);
    mat.SetVector("_center", bounds.center);
    mat.SetFloat("_crown", crown);

    Matrix m = Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions);
    Pose head = Input.Head;
    
    mesh.Draw(mat, m);
  }
}