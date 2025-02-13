# Sacred Gold Lobby Server

This project is a reimplementation of the lobby servers once used in the game "Sacred" developed by Ascaron Entertainment.

## Overview

The lobby servers were responsible for maintaining lists of both official and community-run game servers, allowing clients to discover and connect to available game rooms.

This reimplementation aims to recreate the functionality of the original lobby servers, providing a platform for players to discover and connect to active game servers for multiplayer gameplay.

> [!CAUTION]
> Write slow in the lobby's chat! The game has an anti-flood system that prevents messages from being sent to the lobby
> If you write more than a message every two seconds, every subsequent message gets cached until the flood timer resets
> Then when writing a new message, all cached messages get sent

## Features

### Implemented
- **Login**: The servers once required registration on the ascaron website, here any username and password combination will do.
- **Open Net**: This gamemode allows you to use your local characters to play online!
- **Closed Net**: In this competitive gamemode the lobbyserver stores your characters so you can't load cheated ones.
- **Server List**: GameServers can connect to the lobbyserver and be added to the list of available servers
- **Lobby Chat**: You can now chat with other players and see other characters in the lobby

## Getting Started

### Download the LobbyServer
1. Download the latest release from the box on the right
2. Extract the archive
3. (Optionally) modify 'config.toml' to suit your needs

### Port forwarding
The port 7066/tcp (unless modified in the config) needs to be port forwarded if hosting a lobbyserver over the internet

Now when you run the executable GameServers and Clients will be able to connect to the lobbyserver!

### GameServer configuration
This needs to be done for every person that wants to host a game using GameServer.exe!

Open the file GameServer.cfg in Sacred's game folder with a text editor (such as notepad) and modify the following lines:  

`NETWORK_LOBBY : <your public ip or 127.0.0.1 if on the same pc as the lobbyserver>`<br>
`NETWORK_LOBBYPORT : 7066` (or your custom port you set in config.toml)

Ports 2005/udp and 2006/tcp need to be port forwarded if hosting a gameserver over the internet

Now open GameServer.exe, set a game name, click on the radio button labeled "Internet" and then click "Ok"  
The gameserver should now connect to the lobbyserver!

### Client configuration

Open the file Settings.cfg in Sacred's game folder with a text editor (such as notepad) and modify the following lines:

`NETWORK_LOBBY : <your public ip or 127.0.0.1 if on the same pc as the lobbyserver>`<br>
`NETWORK_LOBBYPORT : 7066` (or your custom port you set in config.toml)  
`NETWORK_CDKEY : AMVW2Y3MF2OTBTSS9TLC`<br>
`NETWORK_CDKEY2 : 3L4FLSIRGBQS8BTCMGE9`

You can also write the CD Keys from the in-game UI

Now open Sacred, click on "Multiplayer" then "Open Internet", choose a random username and password, then click "Log On".
After selecting a character you should automatically connect to a room and see the server list!

## Building

To get started with this reimplementation, follow these steps:

1. **Clone the Repository**: Clone this repository to your local machine using Git:

    ```bash
    git clone https://github.com/yourusername/sacred-lobby-server.git
    ```

2. **Build the Project**: Using Visual Studio or the .NET CLI, build the lobby server project:

    ```bash
    dotnet build
    ```

3. **Run the Lobby Server**: Using the dotnet CLI run:

    ```bash
    dotnet run
    ```

## Contributing

Contributions to this project are welcome! If you'd like to contribute, please fork the repository, make your changes, and submit a pull request.

## License

This project is licensed under the AGPLv3 License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

Special thanks to Ascaron Entertainment for creating Sacred and inspiring this reimplementation!

A really heartfelt thank you to the members of Sacred Tribute for their invaluable help and to all the members of the discord communities that helped testing, debugging and experimenting <3

