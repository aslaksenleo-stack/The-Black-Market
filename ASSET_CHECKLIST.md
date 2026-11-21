# Asset Checklist

**Art direction (v0.3.0):** stylised, slightly cartoonish low-poly, generated in code (see ARCHITECTURE "Art
pipeline"). 🟩 = stylised generated asset in the final art direction, which may later be replaced by hand-made models.
Status: ⬜ missing · 🟨 placeholder · 🟩 generated (final style) · ✅ final. All audio must be original or properly licensed.

| Licensed asset | Licence | Location |
|---|---|---|
| Alegreya Sans (Bold, Medium) | SIL Open Font License 1.1 | `Assets/Resources/Fonts/` (`OFL-AlegreyaSans.txt`) |
| Alegreya SC (Black) | SIL Open Font License 1.1 | `Assets/Resources/Fonts/` (`OFL-AlegreyaSC.txt`) |
| All sounds | Original, synthesised in code (`SoundKit`) | — |

Mobile budgets (targets, to be verified on device in Milestone 9):
items ≤ 3k triangles, 512–1024 px textures · characters ≤ 15k tris · shop scene ≤ 150k tris on screen, ≤ 150 draw calls.

## Shop
| Asset | Status | Notes |
|---|---|---|
| Counter | 🟩 | Wooden, worn, brass fittings |
| Display shelves (3 upgrade tiers) | 🟩 one tier | Tiers must look visibly better |
| Tables | 🟩 display case | Display table with glass case |
| Storage area / crates | 🟩 | Grows with Storage upgrade |
| Lights (lamps, lanterns) | 🟩 | Two warm ceiling lamps + portal glow |
| Entrance door | 🟩 | Swings open with the shop bell |
| Walls / floor / ceiling | 🟩 | Realistic materials |
| Portal device | 🟩 | Brass frame + glowing core; activation VFX missing |
| Appraisal equipment (basic, advanced, scanner) | ⬜ | |
| Decorations | 🟩 | Ledger, bell, scales, shelf items |

## Characters
| Asset | Status | Notes |
|---|---|---|
| Customer models ×6 types | 🟩 8 looks | Collector, Scientist, Adventurer, Wealthy Buyer, Engineer, Curious Visitor. Each must be recognisable by silhouette |
| Merchant models ×5 personalities | ⬜ | Collector, Desperate Trader, Professional, Mysterious Dealer, Opportunist |
| Idle / talking / walking animations | 🟩 | Procedural (CustomerAvatar), no rig needed |

## Marketplaces
| Asset | Status |
|---|---|
| Old World: medieval buildings, wooden stalls, stone paths, lanterns, signs, props | ⬜ |
| The Unknown: organic architecture, alien plants, unusual materials, strange lighting, props | ⬜ |

## UI
| Asset | Status |
|---|---|
| Font (licensed, readable at small sizes) | ✅ Alegreya Sans / SC (OFL) |
| Icon set: coins, rarity, categories, HUD buttons | 🟩 drawn in code (UISprites) |
| Panel / button sprites (one consistent style) | 🟩 rounded, drawn in code |
| 30 item inventory icons (rendered from the 3D models, not AI-generic) | 🟩 rendered at runtime (18 items) |

## Audio
| Sound | Status | Licence |
|---|---|---|
| Shop ambience | 🟩 | Original (synthesised) |
| Customer footsteps | 🟩 | Original (synthesised) |
| Door opening | 🟩 | Original (synthesised) |
| Transaction confirm / coins | 🟩 | Original (synthesised) |
| Item inspection | 🟩 | Original (synthesised) |
| Portal activation | ⬜ | |
| Old World ambience | ⬜ | |
| Unknown ambience | ⬜ | |
| Merchant interaction | ⬜ | |
| UI clicks / accept / reject | 🟩 | Original (synthesised) |

## Items: proposed first 30 (editable)
Every item needs a unique model with a distinct silhouette, not a recoloured one.

| # | Name | Dimension | Category | Rarity | Main buyers |
|---|---|---|---|---|---|
| 1 | Brass Astrolabe | Old World | Tools | Uncommon | Adventurer, Collector |
| 2 | Gilded Signet Ring | Old World | Jewelry | Uncommon | Wealthy Buyer, Collector |
| 3 | Cracked Scrying Orb | Old World | Artifacts | Rare | Collector, Scientist |
| 4 | Alchemist's Retort | Old World | Tools | Common | Scientist |
| 5 | Dragonbone Dagger | Old World | Artifacts | Rare | Adventurer |
| 6 | Illuminated Bestiary | Old World | Collectibles | Rare | Collector, Scientist |
| 7 | Pewter Tankard | Old World | Collectibles | Common | Curious Visitor |
| 8 | Silver Reliquary Locket | Old World | Jewelry | Rare | Wealthy Buyer |
| 9 | Clockwork Songbird | Old World | Technology | Epic | Engineer, Collector |
| 10 | Watchman's Iron Lantern | Old World | Tools | Common | Adventurer, Curious Visitor |
| 11 | Wax-Sealed Royal Charter | Old World | Collectibles | Uncommon | Collector |
| 12 | Ember Salt Crystals | Old World | Materials | Common | Scientist, Engineer |
| 13 | Knight's Visored Helm | Old World | Artifacts | Uncommon | Adventurer, Collector |
| 14 | Jarred Mandrake Root | Old World | Biological specimens | Uncommon | Scientist |
| 15 | Crown of the Drowned King | Old World | Jewelry | Legendary | Wealthy Buyer, Collector |
| 16 | Pulsing Seed Pod | The Unknown | Biological specimens | Uncommon | Scientist |
| 17 | Glass-Shell Beetle | The Unknown | Biological specimens | Rare | Scientist, Collector |
| 18 | Resonance Tuning Fork | The Unknown | Technology | Uncommon | Engineer |
| 19 | Liquid Mirror Shard | The Unknown | Materials | Rare | Scientist, Engineer |
| 20 | Spiral Data Coil | The Unknown | Technology | Rare | Engineer |
| 21 | Gravity-Null Lodestone (in cradle) | The Unknown | Materials | Epic | Engineer, Scientist |
| 22 | Bioluminescent Moss Jar | The Unknown | Biological specimens | Common | Curious Visitor, Scientist |
| 23 | Hexagonal Star Map | The Unknown | Artifacts | Epic | Adventurer, Collector |
| 24 | Chitin Gauntlet | The Unknown | Tools | Uncommon | Adventurer, Engineer |
| 25 | Whispering Shell | The Unknown | Collectibles | Uncommon | Curious Visitor, Collector |
| 26 | Prism Engine Core | The Unknown | Technology | Legendary | Engineer, Wealthy Buyer |
| 27 | Spore Lantern | The Unknown | Tools | Common | Adventurer, Curious Visitor |
| 28 | Orbiting Ring Band | The Unknown | Jewelry | Rare | Wealthy Buyer |
| 29 | Fossilised Thought Stone | The Unknown | Artifacts | Rare | Scientist, Collector |
| 30 | Sealed Void Canister | The Unknown | Artifacts | Unknown | Varies (appraisal) |

Plus unidentified presentation: cloth-wrapped bundle and sealed crate variants used before appraisal.
