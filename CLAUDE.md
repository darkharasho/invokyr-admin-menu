## Project Context

A BepInEx 6 (BepInEx.Unity.IL2CPP, be.755) mod for Invokyr (Unity 6000.3, IL2CPP, Mirror networking, Steam): a host admin menu on F1 that allows or blocks each player from spending the team's money. Game code is in `vhs.dll` (namespaces like `_200_Dev.*`, `Entities.Player`, `User`), not Assembly-CSharp. Packaged Thunderstore-style under `darkharasho` and released on GitHub; Thunderstore has no Invokyr community yet.

## How the lock works (`SpendingLock.cs`)
- The team's money is `Bank` (`NetworkSingleton<Bank>.instance`, `GetCurrentTotalScore`, `SrvAddToTotalScore`); purchases are decided on the server.
- Shop: prefix on each concrete slot's `SrvOnBought(PlayerEntity)` (`ShopUpgradeItemSlot`, `ShopGadgetItemSlot`, `ShopFurnitureItemSlot`, `ShopConsumableItemSlot`) returns false without running it. The generic `ShopItemSlot<T>` methods aren't patched (shared generic code in IL2CPP).
- Room skins: prefix on `SkinSelector.UserCode_CmdTryPayForSkin__PlayerEntity__Int32` skips it.
- The interop decompile has no method bodies, so whether the price is taken before `SrvOnBought` is unknown: `LateTick` refunds any drop in the bank since the last frame when a purchase was blocked this frame. Check the BepInEx log ("Refunding ...") when testing.
- The blocked player gets `UIManager.Instance.TargetShowMessage(conn, text, 3)`, which unmodded clients show.
- Player identity: `UserInstance.SteamUserId` / `playerEntity` / `UserInstance.Me` (`Players.cs`). Rules are in `SpendingRules.cs` (pure, tested in `tests/`).
- Not covered: `Save.BuyLastChanceClock(int)` (static, no buyer).

## UI
IMGUI drawn with `GUI.Label`/`GUI.DrawTexture` only and our own input (`Ui.cs`, `Theme.cs`, shared with moonsweepers-admin-menu, where controls needing a control id failed to unstrip). `new GUIStyle(GUIStyle)` doesn't exist in this interop; use `new GUIStyle()`. While open: `InputsManager.Instance.Disable()`, free cursor, `EventSystem.current` disabled.

## Building and testing
- `scripts/update-refs.sh` copies `<game>/BepInEx/interop` and `core` into `~/.cache/invokyr` (the csproj's default paths) and regenerates `lib/refs` for CI. Rerun after a game update. refasmer fails on UnityEngine.CoreModule with `--omit-non-api-members=true`; the script falls back to `false`.
- Reading game code: `DOTNET_ROOT=/home/linuxbrew/.linuxbrew/opt/dotnet/libexec DOTNET_ROLL_FORWARD=Major nice -n 19 taskset -c 0-3 ~/.dotnet/tools/ilspycmd -p -o /tmp/invk-decomp ~/.cache/invokyr/interop/vhs.dll` (signatures only).
- `scripts/package.sh` builds `dist/darkharasho-InvokyrAdminMenu-<version>.zip`, flat (DLL at the root, not under `plugins/`): HML treats Invokyr as a game-folder game and only moves a flat plugin DLL into `BepInEx/plugins/<mod>/`. Install with `hml install nexus-invokyr <zip> --profile Dev`. Run builds with `nice -n 19 taskset -c 0-3`.
- Tests: `dotnet test tests/InvokyrAdminMenu.Tests`.
- Release: bump `Plugin.cs`, the csproj, `thunderstore/manifest.json`, `CHANGELOG.md`; push tag `v<version>`.
