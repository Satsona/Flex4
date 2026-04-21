BULLET ROULETTE
1. Game Overview

Bullet Roulette is a fast-paced multiplayer arena game for 2–4 players.
Players fight each other using weapons that have only one bullet.
After shooting, they must quickly find a new weapon.
The player with the highest score at the end of the match wins.
2. Core Gameplay Loop

1. Player spawns in the arena
2. Player searches for a weapon
3. Player picks up a weapon
4. Player shoots (only one bullet)
5. Weapon is removed
6. Player searches for another weapon
7. Players respawn after death
8. Repeat until the timer ends

3. Weapon System

3.1 Weapon Rules

• Each weapon has 1 ammo only
• Players can carry only one weapon
• No reload system
• After shooting:
o Weapon is dropped to the ground on the gun holder position

3.2 Weapon Pickup System (Technical)

• Weapons spawn at fixed spawn points
• Each spawn point has a spawn timer (30 seconds)
• Weapons use a Trigger Collider
• When a player touches the trigger:
o Weapon disappears
o Weapon is given to the player

Example Logic:
• OnTriggerEnter(Player)
o If player has no weapon:
▪ Enable weapon
▪ Disable weapon object

4. Shooting Mechanic

4.1 Shooting Rules

• Player presses shoot button:
o Weapon fires once
o Ammo becomes 0
o Weapon is removed

4.2 Hit Detection
• Use Raycast system
• If ray hits another player:
o Target dies instantly

4.3 Technical Flow
• Detect input (mouse click / button)
• Check:
o Player has weapon
o Ammo > 0
• Fire raycast
• If hit player:
o Kill player
• Decrease ammo
• Remove weapon

5. Player System

5.1 Movement

• Basic movement:
o WASD / controller input
• Optional jump
• Movement must feel smooth and responsive

5.2 Health System

• Players have 1 HP
• Any hit = instant death

5.3 Death System

• When player is hit:
o Player dies immediately
o Trigger respawn system

6. Respawn System

6.1 Respawn Rules

• Players respawn after death
• Respawn delay: 2–3 seconds
• Players respawn at player spawn points

6.2 Spawn Points

• Multiple spawn locations in the map
• System selects a random spawn point

6.3 Technical Flow

• OnPlayerDeath(player):
o Disable player
o Start respawn timer
• After delay:
o Choose random spawn point
o Move player
o Reset player state (alive, no weapon)

7. Weapon Spawn System

7.1 Spawn Points

• Placed around the arena
• Limited number (example: 5–10 points)

7.2 Spawn Logic (Technical)

• Each spawn point:
o Has a timer
o Checks if a weapon exists

Example Logic:
• If no weapon:
o Start timer
• When timer ends:
o Spawn random weapon

8. Scoring System

8.1 Score Rules

• Each kill gives 1000 points
• Only the killer gets the score
• Players do not lose points when they die

8.2 Score Tracking (Technical)

• Each player has a Score variable (integer)
Example Logic:
• OnPlayerKilled(killer, victim):
o killer.Score += 1000

9. Match Timer System

9.1 Timer Rules

• Each match has a fixed duration (2–5 minutes)
• Timer starts at the beginning
• When timer ends:
o Match ends immediately

9.2 Technical Flow

• Create a Match Timer
• Decrease over time

Example Logic:
• StartTimer()
• While timer > 0:
o timer -= deltaTime
• If timer <= 0:
o EndMatch()

10. Win Condition

10.1 Rules

• When timer ends:
o Player with highest score wins

10.2 Tie Case

• If scores are equal:
o Option 1: Draw

11. Multiplayer Logic

11.1 Player Count

• Minimum: 2 players
• Maximum: 4 players

11.2 Match Start

• Players spawn at different spawn points
• Countdown starts (3–2–1)

12. UI / UX Design

12.1 Main Menu

At the start of the game, players see the Main Menu.
Options:
• Host Game
• Join Game

12.2 Host Game Menu

When the player selects Host Game, a setup screen opens.
Settings:

• Player Limit
o 2 Players
o 3 Players
o 4 Players
• Map Selection
o Player chooses one of the available maps
o Maps can be shown as buttons or thumbnails
• Match Timer
o 5 Minutes
o 10 Minutes
o 15 Minutes
• Start Button
o Starts the match with selected settings

12.3 Join Game Menu

When the player selects Join Game:
• A list of available game sessions is shown
• Player selects a session and joins
(For prototype: this can be a simple lobby or direct connection system)

12.4 In-Game UI

During gameplay, the UI should be minimal and clear.
Core Elements:

• Weapon / Item Indicator
o Shows if player has a weapon or not
o Displays ammo count (1 or 0)
• Match Timer
o Displayed at the top of the screen
o Counts down during the match
• Score Display
o Shows player’s current score

12.5 Scoreboard System

• Players can open the scoreboard by pressing the TAB key
Scoreboard Content:
• Player names
• Player scores
• (Optional) Kill count
Behavior:
• Scoreboard is visible only while TAB key is pressed
• Updates in real-time

12.6 Kill Feedback

• When a player kills another player:
o Show “+1000” on the screen
o Optional sound or visual effect

12.7 End Game Screen

When the match ends:
Display:
• Winner player
• All player scores (ranked from highest to lowest)
Options:
• Rematch
• Return to Main Menu

12.8 Technical Notes (UI Implementation)

• UI should be built using modular widgets (UI panels)
• Timer and scores must update in real-time
Input Logic:
• OnKeyPressed (TAB):
o Show Scoreboard
• OnKeyReleased (TAB):
o Hide Scoreboard

13. Optional Features (Future Scope)

• Different weapon types (shotgun, sniper, etc.)
• Power-ups (speed boost, shield)
• Different arena maps
• Sound effects for tension
• Simple animations and effects

14. Multiplayer Game Market Research

Multiplayer games are very popular today because players enjoy playing with their
friends. Especially short and fun games are preferred by many players.
For example, games like Fall Guys and Stumble Guys offer simple mechanics and quick
matches. These games are easy to learn and provide fun experiences in a short amount
of time.

Bullet Roulette follows a similar design approach:
• Short match duration
• Simple but fun mechanics
• Fast-paced gameplay

The “one bullet” system adds tension to the game. Players must be careful and choose
the right moment to shoot.
Also, small-scale multiplayer games (2–4 players):
• Are easier to develop
• Have fewer technical issues
• Are suitable for prototypes
Target Audience:
• Casual players
• Players who enjoy playing with friends
• Players who prefer short and competitive matches
In conclusion, Bullet Roulette is designed as a simple and fun party-style multiplayer
game.

15. Technical Documentation

15.1 Project Overview

• Game Name: Bullet Roulette
• Game Engine: Unity
• Networking System: Netcode for GameObjects (NGO)
• Player Count: 2–4 players
The game uses a host-client system. One player acts as the host, and others join the
game.

15.2 Scene and Prefab Structure

Scenes:
• Main Menu
• Game Scene

Game Scene Includes:
• Player spawn points
• Weapon spawn points
• Arena environment
• UI (score, timer, etc.)
Prefabs:
• Player (with NetworkObject)
• Weapon (with NetworkObject)

15.3 NetworkObject Inventory

Player:
• NetworkObject
• Player script
• Ownership: Client-owned
Weapon:
• NetworkObject
• Weapon script
• Ownership: Server-owned

15.4 Data Synchronization

Some data must be the same for all players:
• Score
• Player alive status
• Match timer
These are controlled by the server.
RPC Logic:
• Player wants to shoot → sends request to server
• Server checks → if valid, action happens

15.5 Connection Flow

1. Player selects Host or Join
2. Host starts the game
3. Other players connect
4. Players spawn in the map
5. Game starts
On disconnect:
• Player leaves the game

15.6 Input and Authority Model

• Player input is handled on the client side
• Important actions are controlled by the server
Example:
• Shooting → checked by server
• Hit detection → calculated by server
This helps prevent cheating.

15.7 Known Limitations and Future Work

Current limitations:
• Basic lobby system
• Limited weapon variety
• Simple animations
Future improvements:
• More weapons
• Better visual effects
• More maps

16. Screenshot Section

<img width="3839" height="2073" alt="image" src="https://github.com/user-attachments/assets/5a97a8c4-7770-42d3-b042-6ab77e97be16" />
from Stick Fight: The Game

<img width="1024" height="576" alt="image" src="https://github.com/user-attachments/assets/c35ec0cd-201a-42cc-9ec8-cc0afae81dbb" />
from Pummel Party Barn Brawl minigame

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/8b5e1e61-d92d-4937-ab27-85c1afbcb259" />
from Counter Strike 2 Community Map

Our GitHub link: GitHub - Satsona/Flex4 · GitHub
