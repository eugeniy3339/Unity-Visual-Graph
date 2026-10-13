001

Status: Approved

Context - Creation of new project

Descion - The game is divided to modules:
Gameplay,
Data,
Presentation,
Editor,
Tests

All the modules can only read, get values from others, not set/change them, neither calling other modules methods (most of the time).

Gameplay module contains all gameplay scripts (as player movement, abilities f.e.)
Gameplay module is allowed only to communicate with data module.

Data module contains game data in it (as SO f.e.)
Data module isnt allowed to comunicate

Editor module contains editor tools/editors in it (custom editors for scripts/so f.e.)
Editor module is allowed to communicate with Gameplay and Data modules (for making editor scripts/tools only)

Presentation module controlls all the game visuals (character visuals, ui managers f.e.)
Presentation is allowed to communicate with Gameplay and Data modules

Tests module (for testing the game)
Tests module is allowed to communicate with Data, Gameplay and Test Runner

Consequences:
Project will be divided to the modules, that are communicating only with things they need to communicate with. F.E: If we delete visual scripts, from the game, or they will somehow break - gameplay will be still functioning.
Also we can change game data as player's speed for example without opening/changing player scripts.
