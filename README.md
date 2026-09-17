# Cities Skylines 1 — Automated Public Transport Planning

**Steam Workshop name:** Automated Public Transport Planning
**Game:** Cities: Skylines I

This branch holds the **source code of the mod itself**. Nothing unrelated to the application's own source — tooling, assets, documents, personal notes — belongs here.

Other languages: [繁體中文](README_TW.md)

## What the mod does

An in-game toolset that plans public transport for you:

- **Auto-plan** — reads the city's zoning and commuter demand, then generates a bus / metro / tram network
- **Refine existing lines** — keeps the lines you drew by hand and only corrects thin coverage or excessive overlap
- **Wipe and replan** — clears every line and recomputes a network from scratch
- **In-game UI** — all controls and a preview of the plan live in a panel inside the game

## Status

Early development. The mod loads, surveys a city read-only, and can place stops;
the planner itself is not written yet.

## Building

Requires the game installed and either MSBuild (Visual Studio 2022 Build Tools is
enough) or an equivalent. The .NET Framework 3.5 targeting pack is **not** needed:
the project compiles against the assemblies shipped with the game, under
`Cities_Data/Managed`.

Copy `Local.props.example` to `Local.props` and point `CitiesSkylinesDir` at your
install if the build cannot find it on its own. Set `ModsDir` as well to have a
successful build copy the assembly straight into the game's local mods folder.
`Local.props` is git-ignored, so nobody's paths end up in the repository.

```
MSBuild AutomatedPublicTransportPlanning/AutomatedPublicTransportPlanning.csproj /p:Configuration=Release
```

## Layout

```
AutomatedPublicTransportPlanning/
  Source/Mod.cs               IUserMod entry point
  Source/Loader.cs            read-only city survey on level load
  Source/Planning/            stop placement and prefab resolution
  Source/Spike/               throwaway diagnostics, removed once they have served
  Source/Util/Log.cs
```

## Branches

| Branch | Contents |
| --- | --- |
| `Source_Code` (default) | The mod's source code |
| `Cities_Skylines_1_Automated_Public_Transport_Planning_Preview` | Static preview site, served via GitHub Pages |

**Preview site:** https://spacesquare640.github.io/Cities_Skylines_1_Automated_Public_Transport_Planning/
