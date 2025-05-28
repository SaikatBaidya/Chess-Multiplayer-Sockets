# Chess-Multiplayer

A simple two-player chess game system with a C# socket-based server and a modern web client.

## Overview

This project implements a synchronous socket-based chess server in C# and a web-based client using HTML, CSS, and vanilla JavaScript. The server manages player registration, pairing, move relaying, and quitting, while the client provides an interactive chessboard UI with drag-and-drop, capture bin, and real-time status updates.

## Features

- **RESTful HTTP/1.1 API** over raw sockets (no higher-level HTTP libraries)
- Player registration and pairing
- Real-time move relaying between two players
- Quit functionality with opponent notification
- Modern, responsive web client with styled UI
- Capture bin for taken pieces
- Clear status and error messages

## Folder Structure

```
chess-server-711/
├── GameClient/               # Web client files
│   ├── Styles.css            # Stylesheets
│   ├── Script.js             # JavaScript files
│   └── ChessClient.html      # Main HTML file
├── GameServer/               # C# server files
│   ├── ChessServer.cs        # Main server class
│   └── GameServer.csproj     # C# project file
└── README.md                 # This file
```

## Getting Started

To run the server and client locally, follow these steps:

1. Clone the repository: `[git clone https://github.com/SaikatBaidya/Chess-Server-711.git](https://github.com/SaikatBaidya/Chess-Multiplayer-Sockets.git)`
2. Open a terminal in the `GameServer` directory.
3. Run:
```
dotnet run
```
4. The server listens on `127.0.0.1:11000`.
5. Open `GameClient/ChessClient.html` in your web browser.
6. Enter your name and click "Pair Me" to join a game.

![Client Side UI](https://github.com/SaikatBaidya/Chess-Server-711/blob/8a7792021027d02e0590f948f005d52c5e46b7fe/client%20ss.png)

## API Endpoints

- `GET /register` — Register a new user (demo, returns random username)
- `GET /pairme?player=NAME` — Pair player for a game
- `GET /mymove?player=NAME&id=GAMEID&move=MOVE` — Submit a move
- `GET /theirmove?player=NAME&id=GAMEID` — Poll for opponent's move
- `GET /quit?player=NAME&id=GAMEID` — Quit the game

All responses are JSON except for errors.

### Client

The web client provides a chessboard UI with the following features:

- Drag-and-drop piece movement
- Capture bin for taken pieces
- Status messages for game events
- Responsive design for various screen sizes
