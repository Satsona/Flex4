BULLET ROULETTE 

1. Game Overview 

Bullet Roulette is a fast-paced multiplayer arena game for 2–4 players. 
Players fight each other using weapons that have only one bullet. 
After shooting, they must quickly find a new weapon. 
The player with the highest score at the end of the match wins. 

 

2. Core Gameplay Loop 

Player spawns in the arena  
Player searches for a weapon  
Player picks up a weapon  
Player shoots (only one bullet)  
Weapon is removed  
Player searches for another weapon  
Players respawn after death  
Repeat until the timer ends  

 

3. Weapon System 

3.1 Weapon Rules 

Each weapon has 1 ammo only  
Players can carry only one weapon  
No reload system  
After shooting:  
Weapon is dropped to the ground on the gun holder position 

 

3.2 Weapon Pickup System (Technical) 

Weapons spawn at fixed spawn points  
Each spawn point has a spawn timer (30 seconds)  
Weapons use a Trigger Collider  

When a player touches the trigger:  
Weapon disappears  
Weapon is given to the player  
Example Logic: 
OnTriggerEnter(Player)  

If player has no weapon:  
Enable weapon  
Disable weapon object  

 

4. Shooting Mechanic 

4.1 Shooting Rules 

Player presses shoot button:  
Weapon fires once  
Ammo becomes 0  
Weapon is removed  

 

4.2 Hit Detection 

Use Raycast system  
If ray hits another player:  
Target dies instantly  

 

4.3 Technical Flow 

Detect input (mouse click / button)  

Check:  
Player has weapon  
Ammo > 0  
Fire raycast  
If hit player:  
Kill player  
Decrease ammo 
Remove weapon  

 

5. Player System 

5.1 Movement 

Basic movement:  
WASD / controller input  
Optional jump  
Movement must feel smooth and responsive  

 

5.2 Health System 

Players have 1 HP  
Any hit = instant death  

 

5.3 Death System 

When player is hit:  
Player dies immediately  
Trigger respawn system  

 

6. Respawn System 

6.1 Respawn Rules 

Players respawn after death  
Respawn delay: 2–3 seconds  
Players respawn at player spawn points  

 

6.2 Spawn Points 

Multiple spawn locations in the map  
System selects a random spawn point  

 

6.3 Technical Flow 

OnPlayerDeath(player):  
Disable player  
Start respawn timer  

After delay:  
Choose random spawn point  
Move player  
Reset player state (alive, no weapon)  

 

7. Weapon Spawn System 

7.1 Spawn Points 

Placed around the arena  
Limited number (example: 5–10 points)  

 

7.2 Spawn Logic (Technical) 

Each spawn point:  
Has a timer  
Checks if a weapon exists  
Example Logic: 

If no weapon:  
Start timer  
When timer ends:  
Spawn random weapon  

 

8. Scoring System 

8.1 Score Rules 

Each kill gives 1000 points  
Only the killer gets the score  
Players do not lose points when they die  

 

8.2 Score Tracking (Technical) 

Each player has a Score variable (integer)  
Example Logic: 
OnPlayerKilled(killer, victim):  
killer.Score += 1000  

 

9. Match Timer System 

9.1 Timer Rules 

Each match has a fixed duration (2–5 minutes)  
Timer starts at the beginning  
When timer ends:  
Match ends immediately  

 

9.2 Technical Flow 

Create a Match Timer  
Decrease over time  

Example Logic: 
StartTimer()  
While timer > 0:  
timer -= deltaTime  
If timer <= 0:  
EndMatch()  

 

10. Win Condition 

10.1 Rules 

When timer ends:  
Player with highest score wins  

 

10.2 Tie Case 

If scores are equal:  

Option 1: Draw  

 

11. Multiplayer Logic 

11.1 Player Count 

Minimum: 2 players  
Maximum: 4 players  

 

11.2 Match Start 

Players spawn at different spawn points  
Countdown starts (3–2–1)  

 

12. UI / UX Design 

12.1 Main Menu 

At the start of the game, players see the Main Menu. 
Options: 
Host Game  
Join Game  

 

12.2 Host Game Menu 

When the player selects Host Game, a setup screen opens. 
Settings: 

Player Limit  
2 Players  
3 Players  
4 Players  

Map Selection  

Player chooses one of the available maps  
Maps can be shown as buttons or thumbnails  

Match Timer  

5 Minutes  
10 Minutes  
15 Minutes  

Start Button  

Starts the match with selected settings  

 

12.3 Join Game Menu 

When the player selects Join Game: 

A list of available game sessions is shown  
Player selects a session and joins  
(For prototype: this can be a simple lobby or direct connection system) 

 

12.4 In-Game UI 

During gameplay, the UI should be minimal and clear. 

Core Elements: 

Weapon / Item Indicator  
Shows if player has a weapon or not  
Displays ammo count (1 or 0)  

Match Timer  

Displayed at the top of the screen  
Counts down during the match  

Score Display  

Shows player’s current score  

 

12.5 Scoreboard System 

Players can open the scoreboard by pressing the TAB key  

Scoreboard Content: 

Player names  
Player scores  
(Optional) Kill count  

Behavior: 
Scoreboard is visible only while TAB key is pressed  
Updates in real-time  

 

12.6 Kill Feedback 

When a player kills another player:  
Show “+1000” on the screen  
Optional sound or visual effect  

 

12.7 End Game Screen 

When the match ends: 

Display: 
Winner player  
All player scores (ranked from highest to lowest)  

Options: 
Rematch  
Return to Main Menu  

 

12.8 Technical Notes (UI Implementation) 

UI should be built using modular widgets (UI panels)  
Timer and scores must update in real-time  

Input Logic: 

OnKeyPressed (TAB):  
Show Scoreboard  

OnKeyReleased (TAB):  
Hide Scoreboard 

 

 

13. Optional Features (Future Scope) 

Different weapon types (shotgun, sniper, etc.)  
Power-ups (speed boost, shield)  
Different arena maps  
Sound effects for tension  
Simple animations and effects 

 
