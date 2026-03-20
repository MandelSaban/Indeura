const { app, BrowserWindow, Menu, ipcMain, session, shell } = require("electron");
const https = require("https");
const path = require("path");
const fs = require("fs");
const extract = require("extract-zip");
const { exec } = require("child_process");

let mainWindow;

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true
    }
  });

  Menu.setApplicationMenu(null);

  mainWindow.loadURL("http://localhost:5042", {
    userAgent: "electron-launcher"
  });
}

app.whenReady().then(() => {
  const gamesFolder = "C:\\Indeura\\Games";

  if (!fs.existsSync(gamesFolder)) {
    fs.mkdirSync(gamesFolder, { recursive: true });
  }

  session.defaultSession.on("will-download", (event, item) => {
    const savePath = path.join(gamesFolder, item.getFilename());
    item.setSavePath(savePath);

    item.on("done", async (event, state) => {
      if (state === "completed") {
        console.log("Descarga terminada:", savePath);

        const extractFolder = path.join(
          gamesFolder,
          path.basename(savePath, ".zip")
        );

        try {
          if (!fs.existsSync(extractFolder)) {
            fs.mkdirSync(extractFolder, { recursive: true });
          }

          await extract(savePath, { dir: extractFolder });
          console.log("Juego extraido en:", extractFolder);

          // opcional: borrar zip
          fs.unlinkSync(savePath);
        } catch (err) {
          console.error("Error extrayendo zip:", err);
        }
      }
    });
  });

  createWindow();
});

// Descarga de última release
ipcMain.on("download-latest", (event, data) => {
  const apiUrl = `https://api.github.com/repos/${data.user}/${data.repo}/releases/latest`;
  const options = { headers: { "User-Agent": "electron-launcher" } };

  https.get(apiUrl, options, (res) => {
    let body = "";
    res.on("data", chunk => body += chunk);
    res.on("end", () => {
      const json = JSON.parse(body);
      const asset = json.assets[0];
      const downloadUrl = asset.browser_download_url;
      console.log("Descargando:", downloadUrl);
      mainWindow.webContents.downloadURL(downloadUrl);
    });
  });
});

// NUEVO: abrir un archivo en un path específico
ipcMain.on("open-game-folder", (event, folderPath) => {
  fs.readdir(folderPath, (err, files) => {
    if (err) {
      console.error("Error leyendo carpeta:", err);
      return;
    }

    // Filtrar solo .exe
    const exeFiles = files.filter(f => f.toLowerCase().endsWith(".exe"));

    if (exeFiles.length === 0) {
      console.error("No se encontró ningún .exe en:", folderPath);
      return;
    }

    // Prioridad: game.exe → sino el primero
    let selectedExe =
      exeFiles.find(f => f.toLowerCase() === "game.exe") ||
      exeFiles[0];

    const fullPath = path.join(folderPath, selectedExe);

    console.log("Ejecutando:", fullPath);

    exec(`"${fullPath}"`, (error) => {
      if (error) {
        console.error("Error al ejecutar el .exe:", error);
      }
    });
  });
});