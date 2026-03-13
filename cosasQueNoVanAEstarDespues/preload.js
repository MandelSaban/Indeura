const { contextBridge, ipcRenderer } = require("electron");

contextBridge.exposeInMainWorld("electronAPI", {
  downloadLatest: (user, repo) => ipcRenderer.send("download-latest", {user, repo})
});

