using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using LastLight.Common.Abilities;

namespace LastLight.Client.Core;

public sealed class WorldRenderer
{
    private struct AnimationState
    {
        public string SheetKey;
        public string ClipKey;
        public int FrameIndex;
        public float ElapsedMs;
        public bool Playing;
    }

    private readonly IAssetManager _assetManager;
    private readonly Dictionary<IEntity, AnimationState> _entityAnimations = new();

    public WorldRenderer(IAssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public void PlayAnimation(IEntity entity, string sheetKey, string clipKey, bool restart = false)
    {
        if (_entityAnimations.TryGetValue(entity, out var state))
        {
            if (!restart && state.SheetKey == sheetKey && state.ClipKey == clipKey && state.Playing)
            {
                return; // Already playing this exact animation
            }
        }

        _entityAnimations[entity] = new AnimationState
        {
            SheetKey = sheetKey,
            ClipKey = clipKey,
            FrameIndex = 0,
            ElapsedMs = 0f,
            Playing = true
        };
    }

    public void StopAnimation(IEntity entity)
    {
        if (_entityAnimations.TryGetValue(entity, out var state))
        {
            state.Playing = false;
            _entityAnimations[entity] = state; // Reassign struct
        }
    }

    public void Update(GameTime gameTime)
    {
        float dtMs = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        
        // Use a list of keys to allow modifying the dictionary
        var entities = new List<IEntity>(_entityAnimations.Keys);

        foreach (var entity in entities)
        {
            // Stop tracking dead entities
            if (entity is ClientEntity { Active: false } || entity is Player { CurrentHealth: <= 0 })
            {
                _entityAnimations.Remove(entity);
                continue;
            }

            var state = _entityAnimations[entity];
            if (!state.Playing) continue;

            state.ElapsedMs += dtMs;

            try 
            {
                float frameDuration = _assetManager.GetAnimationFrameDurationMs(state.SheetKey, state.ClipKey, state.FrameIndex);

                while (state.ElapsedMs >= frameDuration)
                {
                    state.ElapsedMs -= frameDuration;
                    state.FrameIndex++;

                    int frameCount = _assetManager.GetAnimationFrameCount(state.SheetKey, state.ClipKey);

                    if (state.FrameIndex >= frameCount)
                    {
                        bool loops = _assetManager.IsAnimationLooping(state.SheetKey, state.ClipKey);
                        if (loops)
                        {
                            state.FrameIndex = 0; // Wrap to beginning
                        }
                        else
                        {
                            state.FrameIndex = frameCount - 1; // Hold last frame
                            state.Playing = false;
                            break; // Stop processing frames
                        }
                    }

                    // Update duration for the newly selected frame
                    frameDuration = _assetManager.GetAnimationFrameDurationMs(state.SheetKey, state.ClipKey, state.FrameIndex);
                }

                _entityAnimations[entity] = state; // Save updated struct
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Animation Error] Entity {entity.Id}: {ex.Message}");
                state.Playing = false; // Failsafe
                _entityAnimations[entity] = state;
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var kvp in _entityAnimations)
        {
            var entity = kvp.Key;
            
            // Skip inactive entities
            if (entity is ClientEntity { Active: false } || entity is Player { CurrentHealth: <= 0 }) continue;

            var state = kvp.Value;

            try 
            {
                var texture = _assetManager.GetAnimationTexture(state.SheetKey);
                var sourceRect = _assetManager.GetAnimationFrameSourceRect(state.SheetKey, state.ClipKey, state.FrameIndex);

                // Center the draw origin based on the source rect size
                var destRect = new Rectangle(
                    (int)entity.Position.X - (sourceRect.Width / 2), 
                    (int)entity.Position.Y - (sourceRect.Height / 2), 
                    sourceRect.Width, 
                    sourceRect.Height
                );

                spriteBatch.Draw(texture, destRect, sourceRect, Color.White);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Draw Error] Entity {entity.Id}: {ex.Message}");
            }
        }
    }
}
