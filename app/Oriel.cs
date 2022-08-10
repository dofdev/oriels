namespace Oriels;

public class Oriel {
  Material matFrame = new Material(Shader.FromFile("shaders/frame.hlsl"));
  Material matPanes = new Material(Shader.FromFile("shaders/panes.hlsl"));
  public Material matOriel = new Material(Shader.FromFile("shaders/oriel.hlsl"));
  Model model = Model.FromFile("oriel.glb");
  Mesh meshCube;

  public Bounds bounds;
  public Quat ori;
  public Matrix matrix;
  public float crown = 0.0666f;

  bool adjusting = false;
  Quat qOffset = Quat.Identity;
  Vec3 vOffset = Vec3.Zero;
  Vec3 lOffset = Vec3.Zero;
  Vec3 anchor = Vec3.Zero;
  Matrix mOffset = Matrix.Identity;

  public Oriel() {
    bounds = new Bounds(
      new Vec3(-1.0f, -0.5f, 0.0f),
      new Vec3(0.8f, 0.5f, 0.5f)
    );

    ori = Quat.Identity;
    matrix = Matrix.TR(bounds.center, ori).Inverse;

    matFrame.SetMat(102, Cull.Back, true);
    matFrame.Transparency = Transparency.Blend;
    matFrame.SetTexture("dither", Tex.FromFile("dither.png"));
    matPanes.SetMat(100, Cull.Front, false);
    matOriel.SetMat(101, Cull.None, true);

    meshCube = Mesh.Cube;
    // meshCube = model.GetMesh("oriel");
  }

  Vec3 detect = Vec3.Zero;
  int detectCount = 0;
  public void Step() {
    matrix = Matrix.TR(bounds.center, ori).Inverse;

    Rig rig = Mono.inst.rig;
    Glove rGlove = Mono.inst.rGlove;
    Vec3 rGlovePos = rGlove.virtualGlove.position;
    Quat rGloveRot = rGlove.virtualGlove.orientation;
    // Vec3 lGlovePos = rig.lGlove.virtualGlove.position;

    // face detection = (1 axis)
    // edge detection = (2 axis)
    // corner detection = (3 axis)
    // Pose pose = new Pose();


    Vec3 localPos = ori.Inverse * (rGlovePos - bounds.center);




    if (!rig.rCon.triggerBtn.held) {
      float margin = PullRequest.Lerp(0.03f, 0.5f, bounds.dimensions.y / 2);
      Vec3 newDetect = Vec3.Zero;
      if ((bounds.dimensions.x / 2) - MathF.Abs(localPos.x) < 0) newDetect.x = 1 * MathF.Sign(localPos.x);
      if ((bounds.dimensions.y / 2) - MathF.Abs(localPos.y) < 0) newDetect.y = 1 * MathF.Sign(localPos.y);
      if ((bounds.dimensions.z / 2) - MathF.Abs(localPos.z) < 0) newDetect.z = 1 * MathF.Sign(localPos.z);

      if (newDetect.x != detect.x || newDetect.y != detect.y || newDetect.z != detect.z) {
        detect = newDetect;
        detectCount = (int)(MathF.Abs(detect.x) + MathF.Abs(detect.y) + MathF.Abs(detect.z));
        // Console.WriteLine(detectCount + ": " + detect);
      }

      Vec3 dim = new Vec3(
        bounds.dimensions.x + 0.1f,
        bounds.dimensions.y + 0.1f,
        bounds.dimensions.z + 0.1f
      );
      Bounds arounds = new Bounds(Vec3.Zero, dim);
      if (!arounds.Contains(localPos) || bounds.Contains(bounds.center + localPos)) {
        detect = Vec3.Zero;
        detectCount = 0;
      }

      vOffset = rGlovePos - bounds.center;
      lOffset = ori.Inverse * vOffset;
      qOffset = (ori.Inverse * rGloveRot).Normalized;
      mOffset = matrix;
      anchor = bounds.center + ori * -(detect * bounds.dimensions / 2);

      adjusting = false;
    } else {
      if (detectCount == 1) { // Move
        ori = (rGloveRot * qOffset.Inverse).Normalized;
        bounds.center = rGlovePos - ori * lOffset;
      } else if (detectCount == 2) { // Rotate
        localPos = mOffset.Transform(rGlovePos);
        Vec3 dir = new Vec3(
          detect.x == 0 ? 0 : localPos.x,
          detect.y == 0 ? 0 : localPos.y,
          detect.z == 0 ? 0 : localPos.z
        );

        Vec3 up = new Vec3(
          detect.x == 0 ? 1 : 0,
          detect.y == 0 ? 1 : 0,
          detect.z == 0 ? 1 : 0
        );

        Quat q = Quat.LookAt(Vec3.Zero, dir, up);

        if (!adjusting) {
          qOffset = (q.Inverse * ori).Normalized;
          adjusting = true;
        } else {
          ori = (q * qOffset).Normalized;
        }
      } else if (detectCount == 3) { // Scale
        Vec3 localAnchor = matrix.Transform(anchor);
        float distX = Math.Abs(localAnchor.x - localPos.x);
        float distY = Math.Abs(localAnchor.y - localPos.y);
        float distZ = Math.Abs(localAnchor.z - localPos.z);
        bounds.dimensions = new Vec3(distX, distY, distZ);
        bounds.center = Vec3.Lerp(anchor, rGlovePos, 0.5f);
      }
    }


    matrix = Matrix.TR(bounds.center, ori).Inverse;


    // matFrame.Wireframe = true;
    matFrame.DepthTest = DepthTest.Always;
    matFrame.SetVector("_rGlovePos", rGlovePos);
    matFrame.SetFloat("_time", Time.Totalf);
    meshCube.Draw(matFrame,
      Matrix.TRS(bounds.center, ori, bounds.dimensions),
      new Color(0.1f, 0.1f, 0.1f)
    );
    if (detectCount > 0) {
      meshCube.Draw(Material.Default,
        Matrix.TS(detect * (bounds.dimensions / 2), Vec3.One * 0.01f) * matrix.Inverse
      );
    }

    // matPanes.DepthTest = DepthTest.Greater;
    matPanes["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrix);
    meshCube.Draw(matPanes,
      Matrix.TRS(bounds.center, ori, bounds.dimensions),
      new Color(0.0f, 0.0f, 0.5f)
    );

    matOriel.SetVector("_center", bounds.center);
    matOriel.SetVector("_dimensions", bounds.dimensions);
    matOriel.SetVector("_light", ori * new Vec3(0.6f, -0.9f, 0.3f));
    matOriel.SetFloat("_lit", 1);
    matOriel["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrix);
  }
}