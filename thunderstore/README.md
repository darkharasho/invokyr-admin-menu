# Invokyr Admin Menu

A host admin menu for Invokyr. Press **F1** while you host to choose which players may spend the team's money.

## Spending
- Every player in your game is listed with **Allow spending** / **Block spending**, plus **Allow everyone** and **Block everyone else**.
- A blocked player can't buy from the shop (upgrades, gadgets, furniture, consumables) or pay for room skins. The item stays on the shelf, the team's money stays where it is, and they see "The host has turned off spending for you".
- **New players**: turn on to have anyone joining your games for the first time start blocked until you allow them.
- Choices are saved by Steam id, so they carry over to your next games.

## Notes
- Only the host needs the mod; purchases are decided by the host's game. It does nothing for players who aren't hosting.
- Esc or F1 closes the menu. The key (text such as `F1` or `F1 + LeftControl`), the message blocked players see, and the saved list are in `BepInEx/config/com.darkharasho.invokyr.adminmenu.cfg`.
- Needs BepInEx 6 for IL2CPP (be.755 or newer).
