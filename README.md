# SCP-035

[![Version](https://img.shields.io/github/v/release/MedveMarci/Scp035?label=Version&color=d500ff)](https://github.com/MedveMarci/Scp035/releases/latest) [![LabAPI Version](https://img.shields.io/badge/LabAPI_Version-1.1.7-51f4ff)](https://github.com/northwood-studios/LabAPI/releases/tag/1.1.7) [![SCP:SL Version](https://img.shields.io/badge/SCP:SL_Version-14.2.8-e5b200)](https://store.steampowered.com/app/700330/SCP_Secret_Laboratory/) [![Downloads](https://img.shields.io/github/downloads/MedveMarci/Scp035/total?label=Downloads&color=ffbf00)](https://github.com/MedveMarci/Scp035/releases) [![License](https://img.shields.io/github/license/MedveMarci/Scp035?label=License&color=2ea44f)](LICENSE)

An SCP: Secret Laboratory [LabAPI](https://github.com/northwood-studios/LabAPI) plugin which adds SCP-035 into the
game.

---

## Features

- Fully functional SCP-035 role and mask item.
- A little scene if the player spawns as SCP-035 at the beginning of the round.
- CASSIE announcement when someone picks up the mask.
- Works with CedMod: kills involving SCP-035 do not count as teamkills, so nobody gets auto-banned for them.
- Very customizable via the config file.

---

## Installation

1. Install [UncomplicatedCustomRoles](https://github.com/UncomplicatedCustomServer/UncomplicatedCustomRoles/releases/latest)
   (`UncomplicatedCustomRoles.dll`).
2. Download `Scp035.dll` from the [latest release](https://github.com/MedveMarci/Scp035/releases/latest).
3. Move `Scp035.dll` and `UncomplicatedCustomRoles.dll` to:
    - Windows: `%AppData%\SCP Secret Laboratory\LabAPI\plugins\global\`
    - Linux: `~/.config/SCP Secret Laboratory/LabAPI/plugins/global/`
4. Start the server once to generate the config, then edit it and restart.

---

## Commands

| Command                                       | Permission     | Description                                                                         |
|-----------------------------------------------|----------------|-------------------------------------------------------------------------------------|
| `spawnscp035 <player> [teleport: true/false]` | `scp035.spawn` | Turns the player into SCP-035, optionally teleporting them to SCP-035's spawn point |

To remove SCP-035 from a player, simply set them to a different role.

The permission is given in the LabAPI permissions file:

- Windows: `%AppData%\SCP Secret Laboratory\LabAPI\configs\permissions.yml`
- Linux: `~/.config/SCP Secret Laboratory/LabAPI/configs/permissions.yml`

---

## Support

<a href="https://discord.gg/KmpA8cfaSA"><img src="https://www.allkpop.com/upload/2021/01/content/262046/1611711962-discord-button.png" height="80"></a>

---

## Credits

- Made by [MedveMarci](https://github.com/MedveMarci)
