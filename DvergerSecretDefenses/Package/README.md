# DvergerSecretDefenses

Dverger-built defenses for your base. Currently adds the Dverger Thundercage, a deathsquito
lure and lightning turret that pulls deathsquitos out of the sky and fries them before they
ever reach you. Tired of those nasty little bugs killing you while you are just planting your barley peacefully?
Forget about benches and torch to prevent them to spawn, let them come, and enjoy watching them die as they deserve.

## Features

### Dverger Thundercage

![img]()

A buildable trap that acts as both bait and turret for deathsquitos.

- **Lures** — every 10 seconds it scans for deathsquitos within 200m and pulls their attention
  onto the cage instead of you. Lured squitos keep pathing towards it until they die or leave
  the lure range, at which point their normal AI is restored.
- **Zaps** — any lured squito that closes to within 20m takes heavy lightning damage.

| Material | Amount |
| --- | --- |
| Black Marble | 25 |
| Copper | 12 |
| Black Metal | 12 |
| Thunderstone | 8 |


## Configuration

Thundercage settings are server authoritative and can be changed by admins in-game. Defaults:

| Setting | Default | Description |
| --- | --- | --- |
| Lure Range | 200 | Range at which the thundercage will lure deathsquitos (0-300). |
| Scan Interval | 10 | Seconds between scans for new deathsquitos to lure. |
| Shot Interval | 1 | Seconds between shots. |
| Shot Range | 20 | Range at which the thundercage can shoot. |
| Shot Damage | 150 | Lightning damage per shot (0-500). |

`EnableDebugMode` is a client-side setting that turns on debug logging.

## Installation (manual)

1. Install [BepInEx](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) and
   [Jötunn](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/).
2. Extract `DvergerSecretDefenses.dll` into `<ValheimDir>\BepInEx\plugins`.

This mod must be installed by every player on a server as well as the server itself.

