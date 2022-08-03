using System.Collections.Generic;

// stretch cursor move
// trackballer spin
// orbital view
// dummy enemies
// points (point of reference *all around)
// roll dodge move

namespace Space;
public class Mono {

  Vec3 playerPos;
  List<Vec3> enemies = new List<Vec3>();
  float spawnTime;

  Oriels.PullRequest.PID pidX = new Oriels.PullRequest.PID();
  Oriels.PullRequest.PID pidY = new Oriels.PullRequest.PID();
  Oriels.PullRequest.PID pidZ = new Oriels.PullRequest.PID();

  Mesh meshCube;

  public Mono() {

  }

  public void Init() {
    meshCube = Mesh.Cube;
  }

  public void Frame() {
    Rig rig = Oriels.Mono.inst.rig;
    Oriel oriel = Oriels.Mono.inst.oriel;

    Matrix orielSimMatrix = Matrix.TRS(
      new Vec3(0, 0, 0), //-oriel.bounds.dimensions.y / 2.01f, -playerWorldPos.z), 
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
    Vec3 localCursor = orielSimMatrix.Inverse.Transform(oriel.matrix.Transform(cursor));

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
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(cursor, Quat.Identity, Vec3.One * 0.02f),
      new Color(1f, 1f, 1f)
    );
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(localCursor, Quat.Identity, Vec3.One * 0.02f) * orielSimMatrix * oriel.matrix.Inverse,
      new Color(0f, 0f, 0f)
    );
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(
        playerPos,
        Quat.LookDir((localCursor - playerPos).Normalized),
        new Vec3(0.4f, 0.2f, 0.4f)
      ) * orielSimMatrix * oriel.matrix.Inverse,
      new Color(1.0f, 0.0f, 0.05f)
    );

    // meshCube.Draw(oriel.matOriel,
    //   rGlove.virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3 * 1.05f),
    //   new Color(0.3f, 0.3f, 0.6f)
    // );





    return;
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
        ) * orielSimMatrix * oriel.matrix.Inverse,
        Color.White * 0.62f
      );
    }
  }

  // design variables
  float moveP = 8f;
  float moveI = 0.2f;
}
