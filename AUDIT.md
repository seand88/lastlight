::: AUDIT TARGET: LastLight.Client.Core\WorldRenderer.cs :::


   - Allocation Alerts:
  Line 62: new List<IEntity>(_entityAnimations.Keys) creates a heap allocation every frame in Update.
  Line 111: foreach on Dictionary allocates an Enumerator if not optimized by the compiler version.


   - Iterator/LINQ Warnings:
  Line 64: foreach (var entity in entities) should be replaced with a for loop to avoid Enumerator allocation on the list.
  Line 111: foreach (var kvp in _entityAnimations) should be replaced with a custom struct-based enumerator or a pooled list of keys.


   - Lookup Optimizations:
  Line 73: _entityAnimations[entity] is a double lookup (TryGetValue then Indexer). Cache the value from TryGetValue.
  Line 79: _assetManager.GetAnimationFrameDurationMs involves dictionary lookups inside a while loop. Cache frame durations or use integer IDs.
  Line 84: _assetManager.GetAnimationFrameCount involves dictionary lookups inside a while loop. Cache count in AnimationState.
  Line 125: _assetManager.GetAnimationTexture and GetAnimationFrameSourceRect are called for every entity every frame. These lookups should be cached in the AnimationState or resolved via integer handles.

  ::: AUDIT TARGET: LastLight.Client.Core\BulletManager.cs :::


   - Allocation Alerts:
  Line 28: entities.GetAllEntities() returns IEnumerable, causing an allocation every frame for every bullet.
  Line 38: spawners.GetAllSpawners() returns IEnumerable, causing an allocation every frame for every bullet.
  Line 87: ParseColor uses string.Split and int.Parse which allocate strings and perform slow parsing.


   - Iterator/LINQ Warnings:
  Line 28: foreach(var e in entities.GetAllEntities()) inside a per-bullet loop creates O(N*M) complexity with heavy allocations.
  Line 38: foreach(var s in spawners.GetAllSpawners()) inside a per-bullet loop creates O(N*M) complexity with heavy allocations.
  Line 101: foreach (var bullet in _bullets) in Update should use a for loop with a count of active bullets.


   - Lookup Optimizations:
  Line 30: System.Math.Abs is called multiple times per bullet. Use squared distance for collision or cache Width/Height.
  Line 75: GameDataManager.Abilities.TryGetValue is called on every Spawn. Ability specs should be cached by integer ID. 

  ::: AUDIT TARGET: LastLight.Client.Core\Game1.cs :::


   - Allocation Alerts:
  Line 549: .Where(sp => sp.Active) in DrawHUD creates a LINQ allocation every frame.
  Line 550: .Where(en => en.Active) in DrawHUD creates a LINQ allocation every frame.
  Line 583: .Max(e => e.Score) in DrawHUD creates a LINQ allocation every frame.
  Line 596: .FirstOrDefault(...) in DrawHUD creates a LINQ allocation every frame.


   - Iterator/LINQ Warnings:
  Line 516: Nested for loops in DrawWorld iterate over the entire map (Width * Height) every frame without frustum culling.
  Line 542: Nested for loops in DrawHUD iterate over the entire map again for the minimap.
  Line 635: foreach(var p in _portals.Values) in Draw should be a for loop over a list or array.


   - Lookup Optimizations:
  Line 517: Switch statement with string keys ("grass", "water", etc.) inside nested loops is slow. Use integer tile types.
  Line 524: AssetManager.GetIconSourceRect is called for every tile every frame. Cache these rectangles in an array indexed by TileType.
  Line 577: GetIconRegion is called for every inventory/equipment slot. Cache regions at startup.

  ::: AUDIT TARGET: LastLight.Client.Core\ParticleManager.cs :::


   - Allocation Alerts:
  Line 33: _random.NextDouble() and Math.Cos/Sin are called in a loop during SpawnBurst.


   - Iterator/LINQ Warnings:
  Line 22: foreach (var p in _particles) in GetFreeParticle is a linear search. Use a Stack or Queue of free indices.
  Line 55: foreach (var p in _particles) in Update should use a for loop and only iterate up to an active count.

   - Lookup Optimizations:
  Line 57: p.Update(dt) is called for all 1000 particles even if inactive. Use an active particle count and swap-to-back on deactivation.


  === END OF AUDIT ===
