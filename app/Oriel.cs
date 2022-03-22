using System;
using System.Collections;
using System.Collections.Generic;
using StereoKit;

public class Oriel {
  static Material matFrame = new Material(Shader.FromFile("wireframe.hlsl"));
  static Material matPanes = new Material(Shader.FromFile("panes.hlsl"));
  static Material matOriel = new Material(Shader.FromFile("oriel.hlsl"));
  static Model model = Model.FromFile("colorball.glb");
  Mesh meshCube, meshFrame;

  public Bounds bounds;
  public Quat ori;
  public Matrix matrix;
  public float crown = 0.0666f;
  public bool drawAxis = false;

  bool adjusting = false;
  // bool scalingOriel = false;
  // bool draggingOriel = false;
  // bool rotatingOriel = false;
  Quat qOffset = Quat.Identity;
  Vec3 vOffset = Vec3.Zero;
  Vec3 lOffset = Vec3.Zero;
  Vec3 anchor = Vec3.Zero;
  Matrix mOffset = Matrix.Identity;

  public Oriel() {
    bounds = new Bounds(
      Input.Head.position + new Vec3(-0.5f, 0, -1f),
      new Vec3(0.8f, 0.5f, 0.5f)
    );

    ori = Quat.Identity;

    matFrame.SetMat(102, Cull.None, true);
    matPanes.SetMat(100, Cull.Front, false);
    matOriel.SetMat(101, Cull.None, true);

    meshFrame = model.GetMesh("Wireframe");
    meshCube = Mesh.Cube;

    Gen();
  }

  public class Transform {
    public string name;
    public Pose pose;
    public float scale;

    public Transform() {

    }

    public Vec3 LocalPos() {
      return pose.position;
    }
  }

  Vec3 detect = Vec3.Zero;
  int detectCount = 0;
  public void Step(Monolith mono) {
    matrix = Matrix.TR(bounds.center, ori).Inverse;

    Vec3 rGlovePos = mono.rGlove.virtualGlove.position;
    Quat rGloveRot = mono.rGlove.virtualGlove.orientation;
    // Vec3 lGlovePos = mono.lGlove.virtualGlove.position;

    // face detection = (1 axis)
    // edge detection = (2 axis)
    // corner detection = (3 axis)
    // Pose pose = new Pose();
    

    Vec3 localPos = ori.Inverse * (rGlovePos - bounds.center);




    if (!mono.rCon.triggerBtn.held) {
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
      anchor  = bounds.center + ori * -(detect * bounds.dimensions / 2);

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

        // Quat q = Quat.FromAngles(up * Vec2.AngleBetween(dir.XZ, detect.XZ));

        // a quick reset function, as the rotation gets fucked

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



    // meet the user halfway, as there is a lot of context provided with where they grab the oriel
    // instead of a discrete handle and interaction


    // circle around center
    // bounds.center = Vec3.Forward * 3 + Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;
    // bounds.dimensions.y = _dimensions.y * (1f + (MathF.Sin(Time.Totalf * 3) * 0.6f));


    matrix = Matrix.TR(bounds.center, ori).Inverse;



    matFrame.Wireframe = true;
    matFrame.DepthTest = DepthTest.Always;
    matFrame.SetVector("_rGlovePos", rGlovePos);
    meshFrame.Draw(matFrame,
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
    matOriel["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrix);


    





    Matrix orielSimMatrix = Matrix.TRS(new Vec3(0, -bounds.dimensions.y / 2, 0), Quat.Identity, Vec3.One * 0.5f * bounds.dimensions.y).Inverse;


    if (drawAxis) {
      meshAxis.Draw(matOriel,
        Matrix.TRS(Vec3.Zero, Quat.Identity, Vec3.One * 1f) * orielSimMatrix.Inverse * matrix.Inverse,
        Color.White
      );
    }

    Mesh.Quad.Draw(matOriel,
      Matrix.TRS(Vec3.Zero, Quat.FromAngles(90, 0, 0), Vec3.One * 100f) * orielSimMatrix.Inverse * matrix.Inverse,
      new Color(1.0f, 1.0f, 1.0f) * 0.3f
    );

    meshCube.Draw(matOriel, 
      mono.rGlove.virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3 * 1.05f),
      new Color(0.3f, 0.3f, 0.6f)
    );

    // draw relative to oriel matrix
    // for (int i = 0; i < asteroids.Count; i++) {
    //   Asteroid asteroid = asteroids[i];
    //   asteroid.pose.orientation = Quat.FromAngles(Time.Elapsedf * 10 * asteroid.scale, 0, 0) * asteroid.pose.orientation;
    //   meshAsteroid.Draw(matOriel,
    //     asteroid.pose.ToMatrix(Vec3.One * asteroid.scale) * matrix.Inverse,
    //     Color.White * 0.32f
    //   );
    // }
    playerPos += new Vec3(-mono.lCon.device.stick.x, 0, -mono.lCon.device.stick.y) * Time.Elapsedf;
    player.Move(playerPos + simOffset, Quat.Identity);
    meshCube.Draw(matOriel,
      Matrix.TRS(player.GetPose().position + (player.GetPose().orientation * Vec3.Up * 0.5f) - simOffset, player.GetPose().orientation, new Vec3(0.4f, 1f, 0.2f)) * orielSimMatrix.Inverse * matrix.Inverse,
      new Color(1.0f, 0.0f, 0.05f)
    );


    // FULLSTICK
    // Vec3 Fullstick() {
    //   Controller con = mono.lCon.device;
    //   Quat rot = Quat.FromAngles(con.stick.y * -90, 0, con.stick.x * 90);
    //   Vec3 dir = Vec3.Up * (con.IsStickClicked ? -1 : 1);
    //   return con.aim.orientation * rot * dir;
    // }
    // Vec3 fullstick = Fullstick();
    // sword.Move(playerPos + simOffset + fullstick, Quat.LookAt(Vec3.Zero, fullstick, Vec3.Up));
    // meshCube.Draw(matOriel,
    //   Matrix.TRS(sword.GetPose().position + (Vec3.Up * 0.7f) + (sword.GetPose().orientation * Vec3.Forward * -0.5f) - simOffset, sword.GetPose().orientation, new Vec3(0.1f, 0.03f, 1f)) * orielSimMatrix.Inverse * matrix.Inverse,
    //   new Color(0.9f, 0.5f, 0.5f)
    // );


    for (int i = 0; i < enemies.Count; i++) {
      Solid enemy = enemies[i];
      Pose pose = enemy.GetPose();
      // move towards player
      enemy.Move(pose.position + (playerPos - pose.position).Normalized * Time.Elapsedf * 0.5f, pose.orientation);
      meshCube.Draw(matOriel,
        Matrix.TRS(pose.position - simOffset, pose.orientation, new Vec3(0.4f, 1f, 0.2f) * 0.99f) * orielSimMatrix.Inverse * matrix.Inverse,
        Color.White * 0.62f
      );
    }
  }

  Solid ground;
  Solid player; Vec3 playerPos;
  Solid sword;
  List<Solid> enemies = new List<Solid>();
  Vec3 simOffset = new Vec3(0, 100, 0);

  Mesh meshAsteroid, meshAxis;

  static Model modelAsteroids = Model.FromFile("asteroids.glb");
  class Asteroid {
    public Pose pose;
    public float scale;

    public Asteroid(Pose pose, float scale) {
      this.pose = pose;
      this.scale = scale;
    }
  }
  List<Asteroid> asteroids = new List<Asteroid>();
  void Gen() {
    meshAxis = model.GetMesh("Axis");
    meshAsteroid = modelAsteroids.GetMesh("1 Meteor");
    asteroids.Clear();
    Random random = new Random();

    // for (int i = 0; i < 128; i++) {
    //   Pose pose = new Pose(
    //     PullRequest.RandomInCube(Vec3.Up * bounds.dimensions.y * 1, bounds.dimensions.x * 2),
    //     Quat.FromAngles(
    //       random.Next(360),
    //       random.Next(360),
    //       random.Next(360)
    //     )
    //   );
    //   asteroids.Add(new Asteroid(pose, 0.1f + random.NextSingle() * 0.4f));
    // }

    ground = new Solid(simOffset, Quat.Identity, SolidType.Immovable);
    ground.AddBox(new Vec3(20, 1, 20), 1, new Vec3(0, -0.5f, 0));

    player = new Solid(simOffset + Vec3.Up, Quat.Identity, SolidType.Normal);
    player.AddBox(new Vec3(0.4f, 1f, 0.2f), 1, new Vec3(0, 0.5f, 0));

    sword = new Solid(simOffset + Vec3.Up, Quat.Identity, SolidType.Normal);
    sword.AddBox(new Vec3(0.1f, 0.03f, 1f), 1, new Vec3(0, 0, -0.5f));

    for (int i = 0; i < 32; i++) {
      Solid solid = new Solid(
        simOffset + Vec3.Up * i,
        Quat.Identity,
        SolidType.Normal
      );
      solid.AddBox(new Vec3(0.4f, 1f, 0.2f), 1);
      enemies.Add(solid);
    }
  }
}