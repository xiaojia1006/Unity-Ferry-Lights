# Gameplay logic regression checks

Run from the project root in PowerShell:

```powershell
./Tests/GameplayLogic/Run.ps1
```

If Unity is installed elsewhere, pass `-UnityEditorData` with its `Editor/Data` directory.

The checks compile the actual inventory, shop, quest, experience and pause code against
minimal engine substitutes. They cover reward capacity, duplicate claims, quest UI event
routing, failed sales, missing drop configuration, multiple pause owners and multiple
level gains. Test output is written under `Temp/GameplayLogicTests`.

These are isolated logic checks. They do not validate Unity scene references, physics,
animation events or object lifecycle ordering in Play Mode.

For a real scene check, save the current scene and use Unity's
`Tools > RPG > Check Inventory In Play Mode`. The check enters Play Mode, uses the actual
inventory UI raycast results to dispatch left and right clicks, verifies pumpkin effects,
physical drops, missing player references and material consumption, then loads RPG2 and
repeats the checks with the persistent inventory. It exits Play Mode automatically.
Results are saved in `Logs/InventoryPlayModeChecks.txt`. Scene assets are not saved by
the check. Run this from RPG1 to include the transition to RPG2.

In Unity, also verify:

- Accept a quest at the RPG2 board; collect its targets and claim rewards once.
- Fill the inventory, try claiming, free space and claim again.
- Sell an unsupported item and confirm it stays in the inventory.
- Switch between bow and sword during an attack; check movement and attack animations.
- Shoot an enemy and inspect knockback distance and stun timing.
- Open both stats and skill tree, then close them one at a time.
- Gain multiple levels and confirm each level adds a skill point.
