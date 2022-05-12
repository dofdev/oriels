using System;
using System.Collections;
using System.Collections.Generic;
using StereoKit;

public class Oriel {
  static Material matFrame = new Material(Shader.FromFile("wireframe.hlsl"));
  static Material matPanes = new Material(Shader.FromFile("panes.hlsl"));
  static Material matOriel = new Material(Shader.FromFile("oriel.hlsl"));
  static Model modelArena = Model.FromFile("megaman/scene.gltf");
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
      new Vec3(-1.0f, -0.5f, 0.0f),
      new Vec3(0.8f, 0.5f, 0.5f)
    );

    ori = Quat.Identity;
    matrix = Matrix.TR(bounds.center, ori).Inverse;

    matFrame.SetMat(102, Cull.None, true);
    matPanes.SetMat(100, Cull.Front, false);
    matOriel.SetMat(101, Cull.None, true);

    meshFrame = model.GetMesh("Wireframe");
    meshCube = Mesh.Cube;














    meshCube = Mesh.Sphere;















    Gen();
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
    // meshFrame.Draw(matFrame,
    //   Matrix.TRS(bounds.center, ori, bounds.dimensions),
    //   new Color(0.1f, 0.1f, 0.1f)
    // );
    if (detectCount > 0) {
      meshCube.Draw(Material.Default,
        Matrix.TS(detect * (bounds.dimensions / 2), Vec3.One * 0.01f) * matrix.Inverse
      );
    }

    // matPanes.DepthTest = DepthTest.Greater;
    matPanes["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrix);
    // meshCube.Draw(matPanes,
    //   Matrix.TRS(bounds.center, ori, bounds.dimensions),
    //   new Color(0.0f, 0.0f, 0.5f)
    // );

    matOriel.SetVector("_center", bounds.center);
    matOriel.SetVector("_dimensions", bounds.dimensions);
    matOriel.SetVector("_light", ori * new Vec3(0.6f, -0.9f, 0.3f));
    matOriel["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrix);




    // modelArena.Draw(Matrix.TRS(
    //   new Vec3(-1, -0.6f, 1.8f), 
    //   Quat.FromAngles(0, -90, 0),
    //   Vec3.One * 0.002f
    // ));








    // APP
    Vec3 playerWorldPos = playerPos * 0.5f * bounds.dimensions.y;
    Matrix orielSimMatrix = Matrix.TRS(



      // new Vec3(0, -bounds.dimensions.y / 2, -playerWorldPos.z), 
      new Vec3(0, -bounds.dimensions.y / 3, -playerWorldPos.z), 




      Quat.Identity, 
      Vec3.One * 0.5f * bounds.dimensions.y
    );


    if (drawAxis) {
      meshAxis.Draw(matOriel,
        Matrix.TRS(Vec3.Zero, Quat.Identity, Vec3.One * 1f) * orielSimMatrix * matrix.Inverse,
        Color.White
      );
    }

    Mesh.Quad.Draw(matOriel,
      Matrix.TRS(Vec3.Zero, Quat.FromAngles(90, 0, 0), Vec3.One * 100f) * orielSimMatrix * matrix.Inverse,
      new Color(1.0f, 1.0f, 1.0f) * 0.3f
    );

    meshCube.Draw(matOriel,
      rGlove.virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3 * 1.05f),
      new Color(0.3f, 0.3f, 0.6f)
    );




    float fwd = Input.Key(Key.W).IsActive() ? 1 : 0;
    playerPos += new Vec3(-rig.lCon.device.stick.x, 0, -rig.lCon.device.stick.y + fwd) * Time.Elapsedf;
    meshCube.Draw(matOriel,
      Matrix.TRS(playerPos, Quat.Identity, new Vec3(0.4f, 1f, 0.2f)) * orielSimMatrix * matrix.Inverse,
      new Color(1.0f, 0.0f, 0.05f)
    );

    // destroy enemies that are too close to the playerPos
    for (int i = 0; i < enemies.Count; i++) {
      if (Vec3.Distance(enemies[i], playerPos) < 0.5f) {
        // enemies.RemoveAt(i);
        // i--;
        enemies[i] = playerPos + Quat.FromAngles(0, Mono.inst.noise.value * 360f, 0) * Vec3.Forward * 8;
      }
    }

    // FULLSTICK
    // Vec3 Fullstick() {
    //   Controller con = rig.lCon.device;
    //   Quat rot = Quat.FromAngles(con.stick.y * -90, 0, con.stick.x * 90);
    //   Vec3 dir = Vec3.Up * (con.IsStickClicked ? -1 : 1);
    //   return con.aim.orientation * rot * dir;
    // }
    // Vec3 fullstick = Fullstick();
    // sword.Move(playerPos + simOffset + fullstick, Quat.LookAt(Vec3.Zero, fullstick, Vec3.Up));
    // meshCube.Draw(matOriel,
    //   Matrix.TRS(sword.GetPose().position + (Vec3.Up * 0.7f) + (sword.GetPose().orientation * Vec3.Forward * -0.5f) - simOffset, sword.GetPose().orientation, new Vec3(0.1f, 0.03f, 1f)) * orielSimMatrix * matrix.Inverse,
    //   new Color(0.9f, 0.5f, 0.5f)
    // );

    if (enemies.Count < 100 && Time.Totalf > spawnTime) {
      enemies.Add(playerPos + Quat.FromAngles(0, Mono.inst.noise.value * 360f, 0) * Vec3.Forward * 8);
      spawnTime = Time.Totalf + 0.05f;
    }

    for (int i = 0; i < enemies.Count; i++) {

      // move towards player
      Vec3 toPlayer = (playerPos - enemies[i]).Normalized;
      float variation = Mono.inst.noise.D1(i);
      toPlayer *= Quat.FromAngles(0, MathF.Sin(Time.Totalf * variation) * 90 * variation, 0);
      Vec3 newPos = enemies[i] + toPlayer * Time.Elapsedf * 0.5f;

      // if far enough away from other enemies than set new pos
      bool setNewPos = true;
      int iteration = 0;
      while (iteration < 6) {
        for (int j = 0; j < enemies.Count; j++) {
          if (i == j) continue;
          // intersection depth
          float radius = 0.5f;
          // (newPos - enemies[j]).Length
          float depth = (newPos - enemies[j]).Length - radius;
          if (depth < 0) {
            // pull back
            Vec3 toEnemy = (enemies[j] - newPos).Normalized;
            newPos = enemies[j] - toEnemy * radius * 1.01f;

            // bump
            // enemies[j] += toEnemy * Time.Elapsedf * 0.5f;

            // setNewPos = false;
            // break;
            // break;
          }
        }

        iteration++;
      }

      if (setNewPos) {
        enemies[i] = newPos;
      }




      meshCube.Draw(matOriel,
        Matrix.TRS(enemies[i],
          Quat.LookAt(enemies[i], playerPos, Vec3.Up),
          new Vec3(0.4f, 1f, 0.2f)
        ) * orielSimMatrix * matrix.Inverse,
        Color.White * 0.62f
      );
    }

  }

  // Custom > Physics
  // issue that we are having is we don't have enough access to the physics sim
  // and because of that we run into issues where solutions we've learned of in Unity
  // will not carry over simply due to what we have current access to.

  // getting over these physics hurdles is worthwhile, but not when we have a viable alternate solution
  Vec3 playerPos;
  List<Vec3> enemies = new List<Vec3>();
  float spawnTime;

  Mesh meshAxis;
  void Gen() {
    meshAxis = model.GetMesh("Axis");
  }
}