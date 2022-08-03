using System.Collections.Generic;

namespace Space;
public class Mono {

  Vec3 playerPos;
  List<Vec3> enemies = new List<Vec3>();
  float spawnTime;

  Mesh meshCube;

  public Mono() {

  }

  public void Init() {
    meshCube = Mesh.Cube;
  }

  public void Frame() {

    Rig rig = Oriels.Mono.inst.rig;
    Oriel oriel = Oriels.Mono.inst.oriel;

    // stretch cursor move
    // trackballer spin
    // orbital view
    // dummy enemies
    // points (point of reference *all around)
    // roll dodge move






    // placeholder "app"
    // Vec3 playerWorldPos = playerPos * 0.5f * oriel.bounds.dimensions.y;
    Matrix orielSimMatrix = Matrix.TRS(
      new Vec3(0, 0, 0), //-oriel.bounds.dimensions.y / 2.01f, -playerWorldPos.z), 
      Quat.Identity,
      Vec3.One * 0.5f * oriel.bounds.dimensions.y
    );


    // meshCube.Draw(oriel.matOriel,
    //   rGlove.virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3 * 1.05f),
    //   new Color(0.3f, 0.3f, 0.6f)
    // );




    // go hacky for now, just to show that you are doing something

    // but communicate that:
    // there are a whole host of things that are taken for granted rn
    // that need to be implemented for the real thing
    // they aren't that hard, i just can't panic rush through them

    // because they are mundane and somewhat time consuming
    // we can show dedication and communicate/ensure importance by streaming and making videos again!

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
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(cursor, Quat.Identity, Vec3.One * 0.02f),
      new Color(1f, 1f, 1f)
    );

    Vec3 localCursor = orielSimMatrix.Inverse.Transform(oriel.matrix.Transform(cursor));
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(localCursor, Quat.Identity, Vec3.One * 0.02f) * orielSimMatrix * oriel.matrix.Inverse,
      new Color(0f, 0f, 0f)
    );



    // fly player towards cursor:
    playerPos += (localCursor - playerPos).Normalized * 1f * Time.Elapsedf;
    // use the gorilla tag equation to stop the stutter at when the player is close to the cursor
    meshCube.Draw(oriel.matOriel,
      Matrix.TRS(
        playerPos,
        Quat.LookDir((localCursor - playerPos).Normalized),
        new Vec3(0.4f, 0.2f, 0.4f)
      ) * orielSimMatrix * oriel.matrix.Inverse,
      new Color(1.0f, 0.0f, 0.05f)
    );






    // destroy enemies that are too close to the playerPos
    for (int i = 0; i < enemies.Count; i++) {
      if (Vec3.Distance(enemies[i], playerPos) < 0.5f) {
        // enemies.RemoveAt(i);
        // i--;
        enemies[i] = playerPos + Quat.FromAngles(0, Oriels.Mono.inst.noise.value * 360f, 0) * Vec3.Forward * 8;
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
    // meshCube.Draw(oriel.matOriel,
    //   Matrix.TRS(sword.GetPose().position + (Vec3.Up * 0.7f) + (sword.GetPose().orientation * Vec3.Forward * -0.5f) - simOffset, sword.GetPose().orientation, new Vec3(0.1f, 0.03f, 1f)) * orielSimMatrix * matrix.Inverse,
    //   new Color(0.9f, 0.5f, 0.5f)
    // );

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




      meshCube.Draw(oriel.matOriel,
        Matrix.TRS(enemies[i],
          Quat.LookAt(enemies[i], playerPos, Vec3.Up),
          new Vec3(0.4f, 1f, 0.2f)
        ) * orielSimMatrix * oriel.matrix.Inverse,
        Color.White * 0.62f
      );
    }

  }
}
