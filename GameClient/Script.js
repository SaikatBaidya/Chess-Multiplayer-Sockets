"use strict";

let username = "";
let gameId = "";

async function testRegister() {
   const resp = await fetch("http://localhost:11000/register");
   const data = await resp.json();
   alert("Registered username: " + data.username);
}

function createSquare(file, rank, colorClass) {
   const square = document.createElement("div");
   square.className = `square ${colorClass}`;
   square.dataset.position = file + rank;
   square.addEventListener("dragover", e => e.preventDefault());
   square.addEventListener("drop", handleDrop);
   return square;
}

function createLabel(content) {
   const label = document.createElement("div");
   label.className = "label";
   label.textContent = content;
   return label;
}

function createPiece(char) {
   const piece = document.createElement("div");
   piece.textContent = char;
   piece.setAttribute("draggable", "true");
   piece.className = "piece";
   piece.addEventListener("dragstart", handleDragStart);
   piece.addEventListener("dragend", handleDragEnd);
   return piece;
}

function handleDragStart(e) {
   e.dataTransfer.setData("text/plain", e.target.textContent);
   e.dataTransfer.setData("source", e.target.parentElement.dataset.position || "bin");
   e.target.classList.add("dragging");
}

function handleDragEnd(e) {
   e.target.classList.remove("dragging");
}

function handleDrop(e) {
   const pieceChar = e.dataTransfer.getData("text/plain");
   const source = e.dataTransfer.getData("source");
   const target = e.currentTarget;

   if (source !== "bin") {
      const sourceSquare = document.querySelector(`[data-position="${source}"]`);
      sourceSquare.innerHTML = "";
   } else {
      const draggedEl = document.querySelector('.piece.dragging');
      if (draggedEl) draggedEl.remove();
   }

   const newPiece = createPiece(pieceChar);
   target.innerHTML = "";
   target.appendChild(newPiece);

   if (username && gameId && source !== "bin") {
      const move = source + target.dataset.position;
      sendMove(move);
   }
}

function handleBinDrop(e) {
   const bin = document.getElementById("capture-bin");
   const pieceChar = e.dataTransfer.getData("text/plain");
   const source = e.dataTransfer.getData("source");

   if (source !== "bin") {
      const sourceSquare = document.querySelector(`[data-position="${source}"]`);
      sourceSquare.innerHTML = "";
      if (username && gameId) {
         const move = source + "bin";
         sendMove(move);
      }
   } else {
      const draggedEl = document.querySelector('.piece.dragging');
      if (draggedEl) draggedEl.remove();
   }

   const capturedPiece = createPiece(pieceChar);
   bin.appendChild(capturedPiece);
}

function buildBoard() {
   const board = document.getElementById("chessboard");
   const files = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h'];
   const ranks = [8, 7, 6, 5, 4, 3, 2, 1];
   const initialPieces = {
      1: ['♖', '♘', '♗', '♕', '♔', '♗', '♘', '♖'],
      2: Array(8).fill('♙'),
      7: Array(8).fill('♟'),
      8: ['♜', '♞', '♝', '♛', '♚', '♝', '♞', '♜']
   };

   for (let row = 0; row < 10; row++) {
      for (let col = 0; col < 10; col++) {
         if ((row === 0 || row === 9) && (col === 0 || col === 9)) {
            board.appendChild(document.createElement("div"));
         } else if (row === 0 || row === 9) {
            board.appendChild(createLabel(files[col - 1]));
         } else if (col === 0 || col === 9) {
            board.appendChild(createLabel(ranks[row - 1]));
         } else {
            const file = files[col - 1];
            const rank = ranks[row - 1];
            const isLight = (row + col) % 2 === 0;
            const square = createSquare(file, rank, isLight ? "light" : "dark");

            if (initialPieces[rank] && initialPieces[rank][col - 1]) {
               const piece = createPiece(initialPieces[rank][col - 1]);
               square.appendChild(piece);
            }

            board.appendChild(square);
         }
      }
   }
}

async function pairMe() {
   username = document.getElementById("playerName").value.trim();
   if (!username) {
      alert("Please enter your name.");
      return;
   }
   const resp = await fetch(`http://localhost:11000/pairme?player=${encodeURIComponent(username)}`);
   const data = await resp.json();
   gameId = data.gameId;
   document.getElementById("pairStatus").innerText =
      `Paired! Game ID: ${gameId}, State: ${data.state}, Player1: ${data.player1}, Player2: ${data.player2}`;
   pollOpponentMove();
}

async function sendMove(move) {
   try {
      const resp = await fetch(
         `http://localhost:11000/mymove?player=${encodeURIComponent(username)}&id=${encodeURIComponent(gameId)}&move=${encodeURIComponent(move)}`
      );
      const data = await resp.json();
      if (data.status === "ok") {
         console.log("Move sent:", move);
      } else {
         alert("Error sending move: " + JSON.stringify(data));
      }
   } catch (err) {
      alert("Failed to send move: " + err);
   }
}

let lastOpponentMove = null;

async function pollOpponentMove() {
   if (!username || !gameId) return;
   try {
      const resp = await fetch(
         `http://localhost:11000/theirmove?player=${encodeURIComponent(username)}&id=${encodeURIComponent(gameId)}`
      );
      const data = await resp.json();
      if (data.move && data.move !== lastOpponentMove) {
         lastOpponentMove = data.move;
         applyOpponentMove(data.move);
      }
   } catch (err) {
      console.log("Polling error:", err);
   }
   setTimeout(pollOpponentMove, 2000);
}

function applyOpponentMove(move) {
   if (!move || move.length < 4) return;
   const from = move.substring(0, 2);
   const to = move.substring(2);

   const fromSquare = document.querySelector(`[data-position="${from}"]`);
   if (!fromSquare) return;

   const piece = fromSquare.querySelector('.piece');
   fromSquare.innerHTML = "";

   if (to === "bin") {
      const bin = document.getElementById("capture-bin");
      if (piece) bin.appendChild(createPiece(piece.textContent));
   } else {
      const toSquare = document.querySelector(`[data-position="${to}"]`);
      if (!toSquare) return;
      toSquare.innerHTML = "";
      if (piece) toSquare.appendChild(createPiece(piece.textContent));
   }
}

async function quitGame() {
   if (!username || !gameId) {
      alert("Not in a game.");
      return;
   }
   try {
      const resp = await fetch(
            `http://localhost:11000/quit?player=${encodeURIComponent(username)}&id=${encodeURIComponent(gameId)}`
      );
      const data = await resp.json();
      if (data.status === "quit") {
            alert("You have quit the game.");
            location.reload();
      } else {
            alert("Game not found or already quit.");
      }
   } catch (err) {
      alert("Failed to quit: " + err);
   }
}

buildBoard();
