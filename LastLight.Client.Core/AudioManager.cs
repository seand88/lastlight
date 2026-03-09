using System;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace LastLight.Client.Core;

public static class AudioManager
{
    private static SoundEffect? _shootSound;
    private static SoundEffect? _hitSound;
    private static SoundEffect? _deathSound;
    private static SoundEffect? _levelUpSound;
    private static SoundEffect? _dropSound;
    private static SoundEffectInstance? _footstepInstance;

    public static void Initialize()
    {
        _shootSound = CreateTone(440, 0.05f, 0.1f);
        _hitSound = CreateTone(220, 0.05f, 0.2f);
        _deathSound = CreateTone(110, 0.2f, 0.5f);
        _levelUpSound = CreateTone(880, 0.3f, 0.8f);
    }

    public static void LoadContent(ContentManager content)
    {
        try {
            var footsteps = content.Load<SoundEffect>("Audio/Sound/footsteps");
            _footstepInstance = footsteps.CreateInstance();
            _footstepInstance.IsLooped = true;
        } catch { }

        try { _dropSound = content.Load<SoundEffect>("Audio/Sound/drop"); } catch { }
    }

    private static SoundEffect CreateTone(int frequency, float duration, float volume)
    {
        int sampleRate = 44100;
        int samplesCount = (int)(sampleRate * duration);
        short[] data = new short[samplesCount];

        for (int i = 0; i < samplesCount; i++)
        {
            double time = (double)i / sampleRate;
            data[i] = (short)(Math.Sin(2 * Math.PI * frequency * time) * short.MaxValue * volume * (1.0 - (double)i / samplesCount));
        }

        byte[] byteData = new byte[data.Length * 2];
        Buffer.BlockCopy(data, 0, byteData, 0, byteData.Length);

        return new SoundEffect(byteData, sampleRate, AudioChannels.Mono);
    }

    public static void PlayShoot() => _shootSound?.Play();
    public static void PlayHit() => _hitSound?.Play();
    public static void PlayDeath() => _deathSound?.Play();
    public static void PlayLevelUp() => _levelUpSound?.Play();
    public static void PlayDrop() => _dropSound?.Play(0.20f, 0f, 0f);

    public static void StartFootsteps()
    {
        if (_footstepInstance != null && _footstepInstance.State != SoundState.Playing)
            _footstepInstance.Play();
    }

    public static void StopFootsteps()
    {
        if (_footstepInstance != null && _footstepInstance.State == SoundState.Playing)
            _footstepInstance.Stop();
    }
}
