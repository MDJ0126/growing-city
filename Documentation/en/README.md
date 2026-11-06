# GrowingCity prototype

[English](../en/README.md) · [한국어](../ko/README.md) · [日本語](../ja/README.md) · [简体中文](../zh-Hans/README.md) · [繁體中文](../zh-Hant/README.md) · [Deutsch](../de/README.md) · [Français](../fr/README.md) · [Español](../es/README.md) · [Português](../pt/README.md)

An automatically growing city becomes your bombing playground. Adjacent buildings combine into larger buildings; destroyed areas rebuild.

Open Assets/Scenes/City Prototype.unity in Unity 6000.0.83f1 with URP 17.0.4. This repository currently contains a Unity prototype; browser gameplay has not been verified.

Drag across the city and release to launch a formation in that direction. Aircraft fly from outside the screen to outside the screen. Keys: 1 bomb, 2 nuclear, Space pause/resume, Esc cancel drag.

Each aircraft starts at one bomb per second. Upgrade drop rate, capacity, blast radius, bomber power, formation size, flight speed, armor and health. Aircraft upgrades apply from the next sortie. There is no manual city growth purchase.

City growth accelerates automatically with elapsed game time: 1x initially, 2x at 60 seconds, up to 8x at 420 seconds. The HUD shows the current rate. Missiles only launch from level 3 buildings or above. Every twelfth construction introduces one of three volatile types: yellow fuel depot, red ammunition depot or cyan power station. Destroying one triggers nearby damage and can detonate other special buildings. A whole chain pays gold once, includes development loss, and leaves rebuildable ruins.

Every aircraft that arrives adds a fixed 50% normal sortie charge. Two arrivals provide one use; extra uses are banked. Normal passive recharge takes 10 game seconds and fills only up to one use.

Nuclear bombing has an independent 100-second cooldown, starting when a valid command is accepted, including queued commands. It starts ready. It does not consume normal sortie credit. Arrival bonuses and 3x speed do not reduce it. Pause stops the timer; NEW CITY resets it.

Buildings fire level-scaled missiles with limited tracking. Aircraft can take damage and be shot down. Larger buildings have more health; damage can shrink them. Destruction grants gold and reduces city development.

Language defaults to the system on first launch. The language button cycles through System and nine languages; an explicit choice is saved on this device. Unsupported system languages fall back to English.

## Controls and settings

Original external assets are read-only: Assets/polyperfect/Low Poly Epic City/. Project changes use owned assets and adapters. Do not redistribute store assets independently.

WebGL: https://mdj0126.github.io/growing-city/ — Unity 6000.0.83f1. Public source requires a separately licensed Low Poly Epic City import; see the root README.

[HTML](index.html)

Volatile buildings now have a bright blue body material; yellow/red/cyan foundations identify their types. Destruction uses sideways toppling, twisting collapse and blast lift. Structural facade sections and smaller debris use physics; some pieces are aimed beyond the viewport and others arc over roofs. Up to 36 structural sections share the existing total debris budget. The WebGL build is verified separately; see the root README for the tested scope.

The city starts with 96 occupied cells, including existing combinations and volatile targets. The default simulation seed produces 82 buildings. Each growth tick adds up to 4 cells on randomly weighted, separated connected edges, rather than selecting the nearest distance to the center. Construction is limited to 24 × 24 cells (576 cells, about 384 × 384 Unity units at cell size16). Full maps stop expanding, but destroyed areas still rebuild. From 90 to 300 game seconds, direct advanced construction gradually increases; at 300 seconds ordinary new or rebuilt lots roll 75% level3, 20% level2 and 5% basic. Volatile buildings keep their own rules. New cities reset this progression.
