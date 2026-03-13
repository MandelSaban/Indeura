const { app, BrowserWindow, Menu, ipcMain, session } = require("electron");
const https = require("https");
const path = require("path");
const fs = require("fs");
const extract = require("extract-zip");


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

  // carpeta donde se guardan los juegos
  const gamesFolder = "C:\\Indeura\\Games";

  if (!fs.existsSync(gamesFolder)) {
    fs.mkdirSync(gamesFolder, { recursive: true });
  }

  // intercepta TODAS las descargas
  session.defaultSession.on("will-download", (event, item) => {

  const gamesFolder = "C:\\Indeura\\Games";
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

ipcMain.on("download-latest", (event, data) => {

  const apiUrl = `https://api.github.com/repos/${data.user}/${data.repo}/releases/latest`;

  const options = {
    headers: {
      "User-Agent": "electron-launcher"
    }
  };

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
