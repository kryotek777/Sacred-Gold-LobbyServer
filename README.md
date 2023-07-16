# Sacred-Gold-LobbyServer

A reverse engineered lobbyserver for Sacred Gold

# Host the lobbyserver

Port forward 7066/tcp on your router
Run the executable

# Host a game

**DO NOT HOST CREATE A GAMESERVER FROM THE IN-GAME's UI** it doesn't join right away and you cannot go back, you'll be stuck and need to restart the game.

Open Gameserver.cfg and change these lines

`NETWORK_LOBBY : <your public ip or 127.0.0.1 if on the same pc as the lobbyserver>`<br>
`NETWORK_LOBBYPORT : 7066`

Open GameServer.exe and select Internet
Port forward 2005/udp and 2006/tcp on your router (or the custom ports you configured) 

# Connect

Open Settings.cfg and change these lines

`NETWORK_LOBBY : <your public ip or 127.0.0.1 if on the same pc as the lobbyserver>`
`NETWORK_LOBBYPORT : 7066`
`NETWORK_CDKEY : AMVW2Y3MF2OTBTSS9TLC`
`NETWORK_CDKEY2 : 3L4FLSIRGBQS8BTCMGE9`
