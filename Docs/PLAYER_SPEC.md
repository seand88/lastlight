# PLAYER SPECIFICATION

## 1. Overview
The Player system manages the character's state, resources, and distinct inventory collections. It orchestrates the base statistics that keep the player alive and the item collections that define their active abilities and progression.

## 2. Design Goals & Functional Requirements

### 2.1 Inventory Collections & Validation
The game categorizes player possessions into four distinct collections. Item movement between collections is strictly validated by the server using an item's base `Category` and `EquipSlot`.

1. **Equipment:** 5 slots for active items defining abilities (referencing `ITEM_SPEC.md`). These slots provide the player's core actions ("verbs"). Items must have `Category: Equipment` and their `EquipSlot` must match the target index exactly:
   - **Slot 0:** `Weapon`
   - **Slot 1:** `Helmet`
   - **Slot 2:** `BodyArmor`
   - **Slot 3:** `Gloves`
   - **Slot 4:** `Boots`
2. **Toolbelt:** Starts with 3 slots, upgradeable to 5-8 slots. Reserved for active combat usage. Items placed here **must** have `Category: Consumable`.
3. **Stash:** The player's bank, serving as large permanent storage. Accessible *only* in the lobby and strictly disabled during a dungeon run. Fixed size of **50 slots**. Accepts any item category.
4. **Dungeon Loot:** Transient storage used exclusively during a dungeon run. Players collect items (such as Loot Chests and Resource Crates) but cannot open or use them during the run. These items are lost on death but persist in-memory if the player disconnects and reconnects while the dungeon remains open. Survived runs allow players to unlock these at the end with leftover gold. Accepts any item category. Fixed size of **50 slots**.

### 2.2 Player Resources
The character’s moment-to-moment gameplay is governed by four primary resources:

- **Hit Points (HP):** Measures the player's remaining life. Base Max HP is 100. It is possible to reach up to 200 Max HP through temporary buffs such as food, scrolls, or equipment perks. HP does not regenerate naturally unless supported by specific Skills or Items.
- **Mana:** The energy used to fuel abilities. While the Weapon Generator (Slot 0) builds Mana on hit, active abilities (Special, Utility, Mobility) consume it. This creates a "Builder/Spender" combat loop.
- **Gold (Run-Only):** An ephemeral currency used exclusively within a dungeon instance. Gold is auto-collected from the ground and is used to purchase weapon tier upgrades and masteries at vendors. Gold is lost upon death or room exit.
- **Experience (XP):** A permanent progression resource earned by defeating enemies. XP accumulates to grant Levels, which in turn provide Skill Points used to purchase permanent character mutations in the Skill Tree.

### 2.3 Core Stats
In addition to resources, the player possesses six core statistics that scale their performance:

> NOTE: This system has data definitions but has no meaningful implementation.

- **Attack:** Increases raw damage output.
- **Defense:** Reduces incoming damage.
- **Speed:** Increases movement velocity.
- **Dexterity:** Increases projectile firing rate and reduces ability wind-up.
- **Vitality:** Enhances HP recovery effectiveness from all sources.
- **Wisdom:** Increases Mana generation rates and reduces ability cooldowns.

### 2.4 User Interface & Input
The character and inventory systems are accessed through specific keybinds and HUD elements:

| Input | Interface | Description |
| :--- | :--- | :--- |
| **`C` Key** | **Character Sheet** | Displays 5 named Equipment slots, current Toolbelt items, and numerical Resource values (HP, Mana, Gold, XP). |
| **`I` Key** | **Dungeon Loot** | Displays a 5x10 grid of items collected during the current run. If pressed in the Lobby, displays "Not in Dungeon". |
| **`B` Key** | **Bank/Stash** | Accessible only in the Lobby. Displays a 5x10 grid of permanent storage. |
| **HUD** | **Quick Toolbelt** | Located at the bottom of the screen. Shows Toolbelt icons; slots are clickable to activate/consume items. |

## 3. Data Specification

### 3.1 Persistence & JSON Representation
Character data is persisted to a SQLite database (`lastlight.db`) in the `Players` table within a JSON `Data` column.

**Example Player JSON:**
```json
{
  "Level": 12,
  "Experience": 4500,
  "MaxHealth": 100,
  "Attack": 15,
  "Defense": 5,
  "Speed": 12,
  "Dexterity": 10,
  "Vitality": 10,
  "Wisdom": 10,
  "ToolbeltSize": 4,
  "Equipment": [
    { "ItemId": 101, "DataId": "weapon_iron_bow", "CurrentTier": 2 },
    null,
    null,
    null,
    null
  ],
  "Toolbelt": [
    { "ItemId": 501, "DataId": "consumable_health_potion", "CurrentTier": 1 },
    null,
    null,
    null
  ],
  "Stash": []
}
```

#### Persistence Schema Table
| Property | Type | Description |
| :--- | :--- | :--- |
| `Level` | int | Current character level. |
| `Experience` | int | Total accumulated XP. |
| `MaxHealth` | int | Persistent base Max HP (progression-based). |
| `Attack` | int | Character scaling factor for damage. |
| `Defense` | int | Character scaling factor for DR. |
| `Speed` | int | Character scaling factor for movement. |
| `Dexterity` | int | Character scaling factor for fire rate. |
| `Vitality` | int | Character scaling factor for healing. |
| `Wisdom` | int | Character scaling factor for mana/CDR. |
| `ToolbeltSize` | int | Number of unlocked toolbelt slots (3 to 8). |
| `Equipment` | array | Array of 5 `ItemInfo` objects. |
| `Toolbelt` | array | Array of `ItemInfo` objects matching `ToolbeltSize`. |
| `Stash` | array | Array of 50 `ItemInfo` objects. |

*Note:* `DungeonLoot` and `RunGold` are explicitly **not** persisted to the database. They live purely in-memory, tied to the active instance.

### 3.2 Persistence DTO (Data Transfer Object)
The following class represents the exact JSON structure saved to the database:

```csharp
public class PlayerSaveData
{
    public int Level { get; set; } = 1;
    public int Experience { get; set; } = 0;
    public int MaxHealth { get; set; } = 100;

    // Stats
    public int Attack { get; set; } = 10;
    public int Defense { get; set; } = 0;
    public int Speed { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Vitality { get; set; } = 10;
    public int Wisdom { get; set; } = 10;

    // Progression
    public int ToolbeltSize { get; set; } = 3;

    // Collections
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[5];
    public ItemInfo[] Toolbelt { get; set; } = new ItemInfo[8]; // Max size, serialized based on ToolbeltSize
    public ItemInfo[] Stash { get; set; } = new ItemInfo[50];
}
```

## 4. Technical Implementation

### 4.1 C# Player Class Outline
The server's `ServerPlayer` class models these collections directly alongside the stat system and ephemeral resources:

```csharp
public class ServerPlayer : IEntity
{
    // Identity & Position
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }

    // Player Resources
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentMana { get; set; }
    public int MaxMana { get; set; }
    public int Experience { get; set; }
    public int Level { get; set; }
    public int RunGold { get; set; } // Ephemeral; used for upgrades

    // Core Stats (Scaling)
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Dexterity { get; set; }
    public int Vitality { get; set; }
    public int Wisdom { get; set; }

    // Progression
    public int ToolbeltSize { get; set; } = 3;

    // Inventory Collections
    public ItemInfo[] Equipment { get; set; } = new ItemInfo[5];
    public ItemInfo[] Toolbelt { get; set; } = new ItemInfo[8];
    public ItemInfo[] Stash { get; set; } = new ItemInfo[50];
    public ItemInfo[] DungeonLoot { get; set; } = new ItemInfo[50];
}
```
