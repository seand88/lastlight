using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace LastLight.Client.Core;

public interface IAssetManager
{
    void LoadAll();

    SoundEffect GetSound(string key);
    Song GetMusic(string key);
    Texture2D GetStaticImage(string key);
    SpriteFont GetFont(string key);

    Texture2D GetAtlasTexture(string atlasKey);
    Rectangle GetIconSourceRect(string atlasKey, string iconKey);

    Texture2D GetAnimationTexture(string sheetKey);
    int GetAnimationFrameCount(string sheetKey, string clipKey);
    Rectangle GetAnimationFrameSourceRect(string sheetKey, string clipKey, int frameIndex);
    float GetAnimationFrameDurationMs(string sheetKey, string clipKey, int frameIndex);
    bool IsAnimationLooping(string sheetKey, string clipKey);
}
