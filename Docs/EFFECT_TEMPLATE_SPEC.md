# EFFECT_TEMPLATES_SPEC.md (v1.0)

This file defines the visual and UI metadata for status effects, buffs, and debuffs. It allows `abilities.json` to remain logic-focused by offloading reusable visual data to these templates.

## 1. Template Object
Every effect template in `effect_templates.json` uses the following structure:

| Property | Type | Description |
| :--- | :--- | :--- |
| `id` | string | Unique identifier referenced by `abilities.json`. |
| `ui_icon` | string | Sprite ID for the target's status bar / buff bar. |
| `particle_fx` | string | **Optional.** Name of the particle system to play on the target. |
| `screen_tint`| string | **Optional.** "R,G,B,A" overlay if applied to the player. |

## 2. Sample Data Structure
This is how the `effect_templates.json` file should be organized in `LastLight.Common\Data`.

```json
{
  "templates": [
    {
      "id": "standard_fire",
      "ui_icon": "icon_burn",
      "particle_fx": "particles_fire_loop",
      "screen_tint": "255,100,0,50"
    },
    {
      "id": "standard_slow",
      "ui_icon": "icon_slow_boot",
      "particle_fx": "particles_frost_trail",
      "screen_tint": "0,150,255,50"
    },
    {
      "id": "mana_gain",
      "ui_icon": "icon_mana_spark",
      "particle_fx": "fx_blue_floaties"
    }
  ]
}