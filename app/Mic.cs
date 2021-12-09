using System;
using StereoKit;

public class Mic {
  public float[] bufferRaw = new float[0];
  public int bufferRawSize = 0;

  public int comp = 8;
  public float[] buffer = new float[0];
  public int bufferSize = 0;

  FilterButterworth filter;
  public void Step() {
    if (Microphone.IsRecording) {
      // Ensure our buffer of samples is large enough to contain all the
      // data the mic has ready for us this frame
      if (Microphone.Sound.UnreadSamples > bufferRaw.Length) {
        bufferRaw = new float[Microphone.Sound.UnreadSamples];
        buffer = new float[Microphone.Sound.UnreadSamples / comp];
      }

      // Read data from the microphone stream into our buffer, and track 
      // how much was actually read. Since the mic data collection runs in
      // a separate thread, this will often be a little inconsistent. Some
      // frames will have nothing ready, and others may have a lot!
      bufferRawSize = Microphone.Sound.ReadSamples(ref bufferRaw);
      bufferSize = bufferRawSize / comp;

      if (bufferSize > 0) {
        // LowPassFilter lowpass = new LowPassFilter(48000 / comp / 2, 2, 48000);
        for (int i = 0; i < bufferRawSize; i++) {
          // bufferRaw[i] = (float)lowpass.compute(bufferRaw[i]);
          filter.Update(bufferRaw[i]);
          bufferRaw[i] = filter.Value;
        }
        // voice.WriteSamples(bufferRaw);

        buffer[0] = bufferRaw[0];
        for (int i = 1; i < bufferSize; i++) {
          buffer[i] = bufferRaw[i * comp - 1];
        }

        // upsample
        float[] upsampled = new float[bufferSize * comp];
        for (int i = 0; i < bufferSize - 1; i++) {
          upsampled[Math.Max(i * comp - 1, 0)] = buffer[i];
          for (int j = 1; j < comp; j++) {
            upsampled[i * comp - 1 + j] = SKMath.Lerp(buffer[i], buffer[i + 1], (float)j / (float)comp);
          }
        }
        voice.WriteSamples(upsampled);
      }
    } else {
      Microphone.Start();
      voice = Sound.CreateStream(0.5f);
      voiceInst = voice.Play(Vec3.Zero, 0.5f);
      filter = new FilterButterworth(48000 / comp / 2, 48000, FilterButterworth.PassType.Lowpass, (float)Math.Sqrt(2));
    }
  }
  public Sound voice;
  public SoundInst voiceInst; // update position

  public class FilterButterworth {
    /// <summary>
    /// rez amount, from sqrt(2) to ~ 0.1
    /// </summary>
    private readonly float resonance;

    private readonly float frequency;
    private readonly int sampleRate;
    private readonly PassType passType;

    private readonly float c, a1, a2, a3, b1, b2;

    /// <summary>
    /// Array of input values, latest are in front
    /// </summary>
    private float[] inputHistory = new float[2];

    /// <summary>
    /// Array of output values, latest are in front
    /// </summary>
    private float[] outputHistory = new float[3];

    public FilterButterworth(float frequency, int sampleRate, PassType passType, float resonance) {
      this.resonance = resonance;
      this.frequency = frequency;
      this.sampleRate = sampleRate;
      this.passType = passType;

      switch (passType) {
        case PassType.Lowpass:
          c = 1.0f / (float)Math.Tan(Math.PI * frequency / sampleRate);
          a1 = 1.0f / (1.0f + resonance * c + c * c);
          a2 = 2f * a1;
          a3 = a1;
          b1 = 2.0f * (1.0f - c * c) * a1;
          b2 = (1.0f - resonance * c + c * c) * a1;
          break;
        case PassType.Highpass:
          c = (float)Math.Tan(Math.PI * frequency / sampleRate);
          a1 = 1.0f / (1.0f + resonance * c + c * c);
          a2 = -2f * a1;
          a3 = a1;
          b1 = 2.0f * (c * c - 1.0f) * a1;
          b2 = (1.0f - resonance * c + c * c) * a1;
          break;
      }
    }

    public enum PassType {
      Highpass,
      Lowpass,
    }

    public void Update(float newInput) {
      float newOutput = a1 * newInput + a2 * this.inputHistory[0] + a3 * this.inputHistory[1] - b1 * this.outputHistory[0] - b2 * this.outputHistory[1];

      this.inputHistory[1] = this.inputHistory[0];
      this.inputHistory[0] = newInput;

      this.outputHistory[2] = this.outputHistory[1];
      this.outputHistory[1] = this.outputHistory[0];
      this.outputHistory[0] = newOutput;
    }

    public float Value {
      get { return this.outputHistory[0]; }
    }
  }

}