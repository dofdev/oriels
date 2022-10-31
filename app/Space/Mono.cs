using System.Collections.Generic;
using Oriels;

// [X] stretch cursor move
// [X] nodes *point of reference rather than interest for now
// [X] follow player *cam? matrix? name?
// [ ] orbital view
// [ ] dummy enemies
// [ ] trackballer spin
// [ ] roll dodge move

namespace Space;
public class Mono {
  public Oriel oriel = new Oriel(
		new Vec3(1.0f, -0.5f, 0.5f),
		Quat.Identity,
		new Vec3(0.8f, 0.5f, 0.5f)
	);
  Node[] nodes = new Node[18];
  Vec3 playerPos;
  List<Vec3> enemies = new List<Vec3>();
  float spawnTime;

  Oriels.PullRequest.PID pidX = new Oriels.PullRequest.PID();
  Oriels.PullRequest.PID pidY = new Oriels.PullRequest.PID();
  Oriels.PullRequest.PID pidZ = new Oriels.PullRequest.PID();

  Mesh meshCube;
  // Model skyboxModel = Model.FromFile("fantasy_skybox.glb");
  // Mesh skybox;
  // Material skyboxMat = new Material(Shader.FromFile("/shaders/oriel.hlsl"));

  public Mono() {

  }

  public void Init() {
    Oriels.PullRequest.Noise noise = Oriels.Mono.inst.noise;

    // place nodes around a 10x4x10 cube
    float scalar = 3f;
    for (int i = 0; i < nodes.Length; i++) {
      nodes[i] = new Node(
        new Vec3(
          noise.value * 5f * scalar,
          noise.value * 2f * scalar,
          noise.value * 5f * scalar
        ),
        noise.uvalue
      );
    }

    meshCube = Mesh.Cube;
    // skybox = skyboxModel.GetMesh("sky");
    // skyboxMat.SetMat(101, Cull.None, true);
    // skyboxMat.SetTexture("diffuse", Tex.FromFile("fantasy_skybox.jpeg"));
  }

  public void Frame() {
    Oriels.Rig rig = Oriels.Mono.inst.rig;
    
    Matrix simMatrix = Matrix.TRS(
      -playerPos * 0.5f * oriel.bounds.dimensions.y, //-oriel.bounds.dimensions.y / 2.01f, -playerWorldPos.z), 
      Quat.Identity,
      Vec3.One * 0.5f * oriel.bounds.dimensions.y
    );


    // stretch cursor pattern:
    // stretch = dist(offHand, mainHand)
    // max(stretch - deadzone, 0)
    // dir = mainHand.fwd
    // cursor = mainHand.pos + dir * stretch * 3

    // stretch cursor code:
    float deadzone = 0.1f;
    float stretch = Vec3.Distance(rig.lCon.pos, rig.rCon.pos);
    stretch = Math.Max(stretch - deadzone, 0);
    Vec3 cursor = rig.rCon.pos + rig.rCon.ori * Vec3.Forward * stretch * 3;
    Vec3 localCursor = simMatrix.Inverse.Transform(oriel.matrixInv.Transform(cursor));

    localCursor = new Vec3(
      MathF.Sin(Time.Totalf * 2f) * 3f,
      MathF.Sin(Time.Totalf * 0.5f) * 3f,
      MathF.Sin(Time.Totalf * 1f) * 3f
    );

    // fly player towards cursor:
    // playerPos += (localCursor - playerPos).Normalized * 1f * Time.Elapsedf;
    pidX.p = moveP; pidY.p = moveP; pidZ.p = moveP; 
    pidX.i = moveI; pidY.i = moveI; pidZ.i = moveI;
    playerPos = new Vec3(
      pidX.Update(localCursor.x), 
      pidY.Update(localCursor.y), 
      pidZ.Update(localCursor.z)
    );




    // RENDER
    for (int i = 0; i < nodes.Length; i++) {
      meshCube.Draw(oriel.matOriel,
        Matrix.TRS(nodes[i].pos, Quat.Identity, Vec3.One * 1f) * simMatrix * oriel.matrix,
        Color.HSV(nodes[i].hue, 1f, 1f)
      );
    }

    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(cursor, Quat.Identity, Vec3.One * 0.02f),
      new Color(1f, 1f, 1f)
    );
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(localCursor, Quat.Identity, Vec3.One * 0.02f) * simMatrix * oriel.matrix,
      new Color(0f, 0f, 0f)
    );
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(
        playerPos,
        Quat.LookDir((localCursor - playerPos).Normalized),
        new Vec3(0.4f, 0.2f, 0.4f)
      ) * simMatrix * oriel.matrix,
      new Color(1.0f, 0.0f, 0.05f)
    );


    // skyboxMat.SetVector("_center", oriel.bounds.center);
    // skyboxMat.SetVector("_dimensions", oriel.bounds.dimensions);
    // skyboxMat.SetVector("_light", oriel.ori * new Vec3(0.6f, -0.9f, 0.3f));
    // skyboxMat.SetFloat("_lit", 0);
    // skyboxMat["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(oriel.matrix);
    // skybox.Draw(skyboxMat,
    //   Matrix.TRS(
    //     playerPos,
    //     Quat.Identity,
    //     new Vec3(10f, 10f, 10f)
    //   ) * simMatrix * oriel.matrix,
    //   Color.White
    // );

    

    // meshCube.Draw(oriel.matOriel,
    //   rGlove.virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3 * 1.05f),
    //   new Color(0.3f, 0.3f, 0.6f)
    // );





    // return;
    // ENEMIES

    // destroy enemies that are too close to the playerPos
    for (int i = 0; i < enemies.Count; i++) {
      if (Vec3.Distance(enemies[i], playerPos) < 0.5f) {
        // enemies.RemoveAt(i);
        // i--;
        enemies[i] = playerPos + Quat.FromAngles(0, Oriels.Mono.inst.noise.value * 360f, 0) * Vec3.Forward * 8;
      }
    }

    if (enemies.Count < 100 && Time.Totalf > spawnTime) {
      // enemies.Add(playerPos + Quat.FromAngles(0, Oriels.Mono.inst.noise.value * 360f, 0) * Vec3.Forward * 8);
      spawnTime = Time.Totalf + 0.05f;
    }

    for (int i = 0; i < enemies.Count; i++) {

      // move towards player
      Vec3 toPlayer = (playerPos - enemies[i]).Normalized;
      float variation = Oriels.Mono.inst.noise.D1(i);
      toPlayer *= Quat.FromAngles(0, MathF.Sin(Time.Totalf * variation) * 90 * variation, 0);
      Vec3 newPos = enemies[i] + toPlayer * Time.Elapsedf * 0.5f;

      // if far enough away from other enemies than set new pos
      bool setNewPos = true;
      int iteration = 0;
      while (iteration < 6) {
        for (int j = 0; j < enemies.Count; j++) {
          if (i == j) continue;
          float radius = 0.5f;
          float depth = (newPos - enemies[j]).Length - radius;
          if (depth < 0) {
            Vec3 toEnemy = (enemies[j] - newPos).Normalized;
            newPos = enemies[j] - toEnemy * radius * 1.01f;
          }
        }

        iteration++;
      }

      if (setNewPos) {
        enemies[i] = newPos;
      }

      meshCube.Draw(oriel.matOriel,
        Matrix.TRS(enemies[i],
          Quat.LookAt(enemies[i], playerPos, Vec3.Up),
          new Vec3(0.4f, 1f, 0.2f)
        ) * simMatrix * oriel.matrix,
        Color.White * 0.62f
      );
    }
  }

  // design variables
  float moveP = 8f;
  float moveI = 0.2f;
}

public class Node {
  public Vec3 pos;
  public float hue;

  public Node(Vec3 pos, float hue) {
    this.pos = pos;
    this.hue = hue;
  }
}