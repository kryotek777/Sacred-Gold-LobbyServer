# Sacred Gold Lobby Server

Play online with your friends, make new allies and slain your foes in the world of Ancaria!  
This project brings back the online functionality of Sacred like it's 2007 again, with some modern tweaks.  
It allows the clients and servers to find each other, saves your characters, allows players to chat with each other, all while being 100% compatible with vanilla clients.

## Quick Start: "I just want to play!"

> [!NOTE]
> While this project is 100% vanilla compatible, the mods SacredNL and PureHD are heavily advised because they have lots fixes for multiplayer and allow you to play at resolutions different from 1024x768.  
> With the vanilla game you need to set your speed to MODEM/ISDN instead of LAN, which will hide most of the ambient NPCs!  
> This is done below while setting "NETWORK_SPEEDSETTINGS" of in the in-game UI  

### Setup your game

1. **Close the game** if it's already open.  
2. **Open your Sacred installation folder** and locate the file `Settings.cfg`.  
3. **Modify** the following lines (using a text editor like Notepad):
    ```ini
    NETWORK_CDKEY : AMVW2Y3MF2OTBTSS9TLC
    NETWORK_CDKEY2 : 3L4FLSIRGBQS8BTCMGE9
    NETWORK_LOBBY : 94.16.105.70
    NETWORK_LOBBYPORT : 7066
    ```
    **If playing with SacredNL and PureHD add the following line**:
    ```ini
    NETWORK_SPEEDSETTINGS : 2
    ```
    **If you're playing without any mods, write this instead**
    ```ini
    NETWORK_SPEEDSETTINGS : 1
    ```
    
4. **Save** the file.

### Choose a Game Mode

**OpenNet**: Play online with your own local characters, so you can bring your offline progress to multiplayer.
   - Open Sacred
   - Go to **Multiplayer** → **Open Internet**,
   - (Optionally) **Register** or else **Choose any username, any password and any email** (the password and email aren't needed and will be ignored)
     
**ClosedNet**: Characters are stored on the lobby for portability and to prevent cheating. Also has hardcore mode!  
   - Download the [ClosedNet client](https://kryotek.net/sacred/assets/bin/SacredClosedNet.exe)
   - Place it in your Sacred installation folder and launch it
   - Go **Multiplayer** → **Closed Internet**
   - **Register** (the email field is ignored)
   - **Login** with your new credentials

Now just select your character, and you’ll see the **server list**. Jump in and play!

> [!NOTE]
> Sacred has an **anti-flood system** that delays messages if you send more than one every two seconds. After you exceed this rate, new messages get cached until the timer resets, and then they all get sent at once. So, **type slowly** in the lobby chat to avoid getting flood-blocked!

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

Clients are set up in the same way as with the public instance (explained above), with the exception that the variable NETWORK_LOBBY needs to point to your server's IP address.

Then they can connect via **Multiplayer** → **Open Internet**, picking any username/password.

---

### Is There a Discord for Support?

Yes! Join us for help, to meet other players, and to share tips and experiences.  
[English/International](https://discord.gg/Duu4B8tgjv)  
[Italian](https://discord.gg/tnT4eYVf)  

---

## Acknowledgements
Heartfelt thanks to everyone who made this project possible:

To Zerisius, DarkHack, William Tokarev, DavideEDN, and many other friends who made all of this possible and supported me along the way.

To the amazing Discord communities and all the testers - your help with debugging, experimentation, ideas and encouragement has been truly invaluable.

To Ascaron Entertainment, for creating Sacred and sparking the inspiration for this reimplementation.

And finally to you, thanks for playing with us! <3

## Contributing

Contributions are welcome! If you’d like to help out, please fork the repository, make your changes, and submit a pull request.

You can directly support the project using [GitHub sponsors](https://github.com/sponsors/kryotek777) or [PayPal](https://paypal.me/kryotek777)

---

## License

This project is licensed under the AGPLv3 License - see the [LICENSE](LICENSE) file for details.
