# EFFECT_TEMPLATES_SPEC.md (v2.0)

This document defines the **Client-Side Visual & Audio Metadata** for LastLight. It acts as a lookup table that translates raw server data into a rich player experience.

## 1. The Role of Templates

While `abilities.json` defines the **Math** (Damage, Duration, Logic), the `effect_templates.json` defines the **Feel**.

*   **When is it used?** It is triggered every time the Client receives an `EffectEvent` packet from the Server.
*   **Network Requirement:** None. This file is local to the Client. The only network dependency is that the `EffectEvent.TemplateId` string must match an `id` in this file.
*   **Performance:** Templates are cached into a Dictionary at startup for O(1) lookup during high-intensity combat.

---

## 2. Template Object Specification

| Property | Type | Description |
| :--- | :--- | :--- |
| **`id`** | string | Unique identifier (e.g., `"standard_fire"`). |
| **`ui_icon`** | string | Sprite ID for the HUD status bar. |
| **`fct_color`** | string | "R,G,B" color for Floating Combat Text (e.g., `"255,0,0"` for red). |
| **`sfx_on_apply`** | string | **Optional.** Sound ID to play once when the effect starts. |
| **`sfx_loop`** | string | **Optional.** Sound ID to loop while the status is active. |
| **`particle_fx`** | string | **Optional.** Name of the particle system to play on the target. |
| **`vfx_anchor`** | enum | `feet`, `center`, `head`, `screen_center`. |
| **`screen_tint`**| string | **Optional.** "R,G,B,A" overlay (Applied only if the target is the Local Player). |

---

## 3. Example Execution Flow

1.  **Server:** Authoritative DOT tick occurs.
2.  **Network:** Server sends `EffectEvent { TemplateId: "poison_cloud", Value: 5, TargetId: 42 }`.
3.  **Client:** `ClientEffectHandler` receives the packet.
4.  **Lookup:** Client finds `"poison_cloud"` in `effect_templates.json`.
5.  **Action:** 
    *   Spawns **Green** (`fct_color`) "-5" text at the target's position.
    *   Plays the `"bubbles"` (`particle_fx`) anchored to the target's `center`.
    *   Plays the `"splat_quiet"` (`sfx_on_apply`) sound.

---

## 4. Sample JSON File Structure

```json
{
  "templates": [
    {
      "id": "poison_cloud",
      "ui_icon": "icon_debuff_poison",
      "fct_color": "50,200,50",
      "sfx_on_apply": "audio_poison_hit",
      "particle_fx": "particles_green_bubbles",
      "vfx_anchor": "center"
    },
    {
      "id": "standard_fire",
      "ui_icon": "icon_burn",
      "fct_color": "255,100,0",
      "sfx_loop": "audio_fire_burn_loop",
      "particle_fx": "particles_fire_flicker",
      "vfx_anchor": "feet",
      "screen_tint": "255,100,0,30"
    },
    {
      "id": "mana_surge",
      "ui_icon": "icon_mana_buff",
      "fct_color": "0,150,255",
      "sfx_on_apply": "audio_mana_sparkle",
      "particle_fx": "fx_blue_rings",
      "vfx_anchor": "center"
    }
  ]
}
```
