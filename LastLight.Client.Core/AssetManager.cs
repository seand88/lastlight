using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace LastLight.Client.Core;

// --- Internal JSON DTOs ---
internal class AssetManifestDto {
    public Dictionary<string, string> sounds { get; set; } = new();
    public Dictionary<string, string> music { get; set; } = new();
    public Dictionary<string, string> staticImages { get; set; } = new();
    public Dictionary<string, string> fonts { get; set; } = new();
    public Dictionary<string, AtlasManifestEntryDto> atlases { get; set; } = new();
    public Dictionary<string, AnimationManifestEntryDto> animations { get; set; } = new();
}
internal class AtlasManifestEntryDto { public string texture { get; set; } = ""; public string map { get; set; } = ""; }
internal class AnimationManifestEntryDto { public string texture { get; set; } = ""; public string map { get; set; } = ""; }

internal class AtlasMapDto { public Dictionary<string, AtlasSpriteEntryDto> sprites { get; set; } = new(); }
internal class AtlasSpriteEntryDto { public int x { get; set; } public int y { get; set; } public int w { get; set; } public int h { get; set; } }

internal class AnimationMapDto { public Dictionary<string, AnimationClipEntryDto> clips { get; set; } = new(); }
internal class AnimationClipEntryDto { public bool loop { get; set; } public AnimationFrameEntryDto[] frames { get; set; } = Array.Empty<AnimationFrameEntryDto>(); }
internal class AnimationFrameEntryDto { public int x { get; set; } public int y { get; set; } public int w { get; set; } public int h { get; set; } public float durationMs { get; set; } }

public sealed class AssetManager : IAssetManager
{
    private readonly ContentManager _content;
    
    // Runtime Caches
    private readonly Dictionary<string, SoundEffect> _sounds = new();
    private readonly Dictionary<string, Song> _music = new();
    private readonly Dictionary<string, Texture2D> _staticImages = new();
    private readonly Dictionary<string, SpriteFont> _fonts = new();
    
    private readonly Dictionary<string, Texture2D> _atlasTextures = new();
    private readonly Dictionary<(string AtlasKey, string IconKey), Rectangle> _atlasSourceRects = new();
    
    private readonly Dictionary<string, Texture2D> _animationTextures = new();
    private readonly Dictionary<(string SheetKey, string ClipKey), Rectangle[]> _animationSourceRects = new();
    private readonly Dictionary<(string SheetKey, string ClipKey), float[]> _animationDurationsMs = new();
    private readonly Dictionary<(string SheetKey, string ClipKey), bool> _animationLoops = new();

    public AssetManager(ContentManager content)
    {
        _content = content;
    }

    private string ReadContentText(string path)
    {
        using (var stream = TitleContainer.OpenStream(path))
        using (var reader = new StreamReader(stream))
        {
            return reader.ReadToEnd();
        }
    }

    public void LoadAll()
    {
        string manifestPath = Path.Combine(_content.RootDirectory, "asset_manifest.json");
        AssetManifestDto? manifest;
        try {
            var json = ReadContentText(manifestPath);
            manifest = JsonSerializer.Deserialize<AssetManifestDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        } catch (Exception ex) {
            throw new FileNotFoundException($"Failed to load {manifestPath}. Did you run pack-assets?", ex);
        }
        
        if (manifest == null) throw new InvalidDataException("Malformed asset_manifest.json");

        // Load Sounds
        foreach (var kvp in manifest.sounds) {
            _sounds[kvp.Key] = _content.Load<SoundEffect>(kvp.Value);
        }

        // Load Music
        foreach (var kvp in manifest.music) {
            // Note: MonoGame MediaPlayer uses URI or ContentManager for Song. Content.Load works if compiled via MGCB.
            _music[kvp.Key] = _content.Load<Song>(kvp.Value); 
        }

        // Load Static Images
        foreach (var kvp in manifest.staticImages) {
            _staticImages[kvp.Key] = _content.Load<Texture2D>(kvp.Value);
        }

        // Load Fonts
        foreach (var kvp in manifest.fonts) {
            _fonts[kvp.Key] = _content.Load<SpriteFont>(kvp.Value);
        }

        // Load Atlases
        foreach (var kvp in manifest.atlases) {
            string atlasKey = kvp.Key;
            _atlasTextures[atlasKey] = _content.Load<Texture2D>(kvp.Value.texture);

            var map = JsonSerializer.Deserialize<AtlasMapDto>(ReadContentText(kvp.Value.map), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (map != null) {
                foreach (var sprite in map.sprites) {
                    _atlasSourceRects[(atlasKey, sprite.Key)] = new Rectangle(sprite.Value.x, sprite.Value.y, sprite.Value.w, sprite.Value.h);
                }
            }
        }

        // Load Animations
        foreach (var kvp in manifest.animations) {
            string sheetKey = kvp.Key;
            _animationTextures[sheetKey] = _content.Load<Texture2D>(kvp.Value.texture);

            var map = JsonSerializer.Deserialize<AnimationMapDto>(ReadContentText(kvp.Value.map), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (map != null) {
                foreach (var clip in map.clips) {
                    string clipKey = clip.Key;
                    _animationLoops[(sheetKey, clipKey)] = clip.Value.loop;
                    
                    int frameCount = clip.Value.frames.Length;
                    var rects = new Rectangle[frameCount];
                    var durations = new float[frameCount];

                    for (int i = 0; i < frameCount; i++) {
                        var f = clip.Value.frames[i];
                        rects[i] = new Rectangle(f.x, f.y, f.w, f.h);
                        durations[i] = f.durationMs;
                    }

                    _animationSourceRects[(sheetKey, clipKey)] = rects;
                    _animationDurationsMs[(sheetKey, clipKey)] = durations;
                }
            }
        }
    }

    public SoundEffect GetSound(string key) {
        if (!_sounds.TryGetValue(key, out var val)) throw new KeyNotFoundException($"Sound '{key}' not found.");
        return val;
    }

    public Song GetMusic(string key) {
        if (!_music.TryGetValue(key, out var val)) throw new KeyNotFoundException($"Music '{key}' not found.");
        return val;
    }

    public Texture2D GetStaticImage(string key) {
        if (!_staticImages.TryGetValue(key, out var val)) throw new KeyNotFoundException($"Static image '{key}' not found.");
        return val;
    }

    public SpriteFont GetFont(string key) {
        if (!_fonts.TryGetValue(key, out var val)) throw new KeyNotFoundException($"Font '{key}' not found.");
        return val;
    }

    public Texture2D GetAtlasTexture(string atlasKey) {
        if (!_atlasTextures.TryGetValue(atlasKey, out var val)) throw new KeyNotFoundException($"Atlas '{atlasKey}' not found.");
        return val;
    }

    public Rectangle GetIconSourceRect(string atlasKey, string iconKey) {
        if (!_atlasSourceRects.TryGetValue((atlasKey, iconKey), out var val)) throw new KeyNotFoundException($"Icon '{iconKey}' not found in atlas '{atlasKey}'.");
        return val;
    }

    public Texture2D GetAnimationTexture(string sheetKey) {
        if (!_animationTextures.TryGetValue(sheetKey, out var val)) throw new KeyNotFoundException($"Animation sheet '{sheetKey}' not found.");
        return val;
    }

    public int GetAnimationFrameCount(string sheetKey, string clipKey) {
        if (!_animationSourceRects.TryGetValue((sheetKey, clipKey), out var val)) throw new KeyNotFoundException($"Clip '{clipKey}' not found in sheet '{sheetKey}'.");
        return val.Length;
    }

    public Rectangle GetAnimationFrameSourceRect(string sheetKey, string clipKey, int frameIndex) {
        var rects = _animationSourceRects[(sheetKey, clipKey)];
        if (frameIndex < 0 || frameIndex >= rects.Length) throw new ArgumentOutOfRangeException(nameof(frameIndex));
        return rects[frameIndex];
    }

    public float GetAnimationFrameDurationMs(string sheetKey, string clipKey, int frameIndex) {
        var durations = _animationDurationsMs[(sheetKey, clipKey)];
        if (frameIndex < 0 || frameIndex >= durations.Length) throw new ArgumentOutOfRangeException(nameof(frameIndex));
        return durations[frameIndex];
    }

    public bool IsAnimationLooping(string sheetKey, string clipKey) {
        if (!_animationLoops.TryGetValue((sheetKey, clipKey), out var val)) throw new KeyNotFoundException($"Clip '{clipKey}' not found in sheet '{sheetKey}'.");
        return val;
    }
}
