# Sacred Gold Lobby Server

This project is a reimplementation of the original lobby servers once used in the game "Sacred," developed by Ascaron Entertainment. The lobby servers maintained lists of official and community-run game servers, allowing clients to discover and connect to available game rooms. This reimplementation aims to bring back that functionality, letting players find and join active servers for multiplayer gameplay.

> [!NOTE]
> Sacred has an **anti-flood system** that delays messages if you send more than one every two seconds. After you exceed this rate, new messages get cached until the timer resets, and then they all get sent at once. So, **type slowly** in the lobby chat to avoid getting flood-blocked!

---

## Overview

- **OpenNet**: Use your local characters to play online.  
- **ClosedNet**: Have your characters stored on the lobby, preventing edited or cheated saves for fair play. *(Currently, only the public instance has ClosedNet servers; self-hosted ClosedNet is not yet available.)*  
- **Server List**: GameServers can register themselves with the lobby, making them visible to players.  
- **Lobby Chat**: Chat with other players, see characters in the lobby, and coordinate games.  
- **Accounts**: Originally, registration happened on Ascaron's website; now, the lobby handles it all.  
- **Hardcore Mode**: ClosedNet also supports hardcore characters—die once, lose your character forever!

---

## Quick Start: "I Just Want to Play!"

1. **Close the game** if it's already open.  
2. **Open your Sacred installation folder** and locate the file `Settings.cfg`.  
3. **Modify** the following lines (using a text editor like Notepad):

    ```ini
    NETWORK_CDKEY : AMVW2Y3MF2OTBTSS9TLC
    NETWORK_CDKEY2 : 3L4FLSIRGBQS8BTCMGE9
    NETWORK_LOBBY : 94.16.105.70
    NETWORK_LOBBYPORT : 7066
    NETWORK_SPEEDSETTINGS : 1
    ```

4. **Save** the file.
5. **To play OpenNet**:
   - Open Sacred
   - Go to **Multiplayer** → **Open Internet**,
   - (Optionally) **Register** or else **Choose any username, any password and any email** (the password and email aren't needed and will be ignored)
   - **Log On**
7. **To play ClosedNet**:
   - Download the [ClosedNet client](https://kryotek.net/sacred/assets/bin/SacredClosedNet.exe)
   - Place it in your Sacred installation folder and launch it
   - Go **Multiplayer** → **Closed Internet**
   - **Register** (email not needed)
   - **Log On**
8. Select your character, and you’ll see the **server list**. Jump in and play!

*(If you want to learn more about OpenNet vs. ClosedNet, see the [FAQ](#faq) below.)*

---

## Self-Hosting Your Own Lobby

If you want full control over the lobby and game servers:

1. **Download the LobbyServer** from the [GitHub Releases](https://github.com/kryotek777/Sacred-Gold-LobbyServer/releases).
2. Extract the archive and optionally edit `config.toml` to suit your needs (port, welcome messages, etc.).
3. **Port Forward** `7066/tcp` (or whatever port you set in `config.toml`) for your lobby to be accessible over the internet.
4. **Run** the lobby server executable.

### GameServer Configuration

1. Open `GameServer.cfg` in your Sacred folder and edit:

    ```ini
    NETWORK_LOBBY : <your public IP or 127.0.0.1 if on the same machine as the lobby>
    NETWORK_LOBBYPORT : 7066
    ```

2. **Port Forward**`2006/tcp` for the GameServer if you’re hosting over the internet.
3. Run `GameServer.exe`, set a **game name**, choose **Internet**, then **OK**.
4. The GameServer will now register itself with the lobby.

### Client Configuration

Clients need to modify their own `Settings.cfg`:

```ini
NETWORK_LOBBY : <your public IP or 127.0.0.1 if on the same PC>
NETWORK_LOBBYPORT : 7066
NETWORK_CDKEY : AMVW2Y3MF2OTBTSS9TLC
NETWORK_CDKEY2 : 3L4FLSIRGBQS8BTCMGE9
```

Then they can connect via **Multiplayer** → **Open Internet**, picking any username/password.  

---

## FAQ <a id="faq"></a>

### What Are the Different Game Modes?

- **OpenNet**: Play online with your own local characters. You can edit them on your PC, so it’s more casual.  
- **ClosedNet**: Characters are stored on the lobby. This prevents cheating and ensures a fair, competitive environment.  

### How Do I Play in ClosedNet?

Currently, **ClosedNet** requires registration (the email field is ignored) and is only available on the **public instance** of the lobby. You **cannot self-host ClosedNet** yet. To play ClosedNet:

1. Download the [ClosedNet client](https://kryotek.net/sacred/assets/bin/SacredClosedNet.exe).  
2. Place `SacredClosedNet.exe` in your Sacred folder.  
3. Modify your `Settings.cfg` like in the Quick Start section.  
4. Run the **ClosedNet client**, register or log in with your credentials.  
5. Enjoy fair play without the worry of cheated characters!

### Is There a Discord for Support?

Yes! Join us on the [Sacred International Discord](https://discord.gg/Duu4B8tgjv) for help, to meet other players, and to share tips and experiences.

---

## Building

To get started with this reimplementation:

1. **Clone the Repository**  
   ```bash
   git clone https://github.com/kryotek777/Sacred-Gold-LobbyServer.git
   ```
2. **Build the Project**  
   ```bash
   dotnet build
   ```
3. **Run the Lobby Server**  
   ```bash
   dotnet run
   ```

---

## Contributing

Contributions are welcome! If you’d like to help out, please fork the repository, make your changes, and submit a pull request.

You can directly support the project using [GitHub sponsors](https://github.com/sponsors/kryotek777) or [PayPal](https://paypal.me/kryotek777)

---

## License

This project is licensed under the AGPLv3 License - see the [LICENSE](LICENSE) file for details.

---

## Acknowledgements

Special thanks to Ascaron Entertainment for creating Sacred and inspiring this reimplementation.  
Huge thanks to **Sacred Tribute**, the **Discord communities**, and all testers who helped with debugging and experimenting. Your support is invaluable! 
