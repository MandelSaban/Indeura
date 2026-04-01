using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using proyect.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Diagnostics.CodeAnalysis;
using RestSharp;

namespace proyect.Controllers;

public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Chat(int toUserId)
    {
        ViewBag.ToUser = toUserId;
        return View();
    }

    public IActionResult UpdateGameDescription(string description, string returnUrl, int gameId)
    {
        BD.UpdateGameDescription(description, gameId);
        return Redirect(returnUrl);
    }

    public IActionResult GamePage(int gameId)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        Game g = BD.findGameById(gameId);

        ViewBag.gameInfo = g;
        ViewBag.images = BD.getGamePictures(gameId);
        if (usuario != null)
        {
            ViewBag.isOwner = BD.IsOwner(gameId, usuario.Id);
            ViewBag.ImPublisher = (usuario.Id == g.IdPublisher);
        }

        ViewBag.tags = BD.getGameTags(gameId);
        ViewBag.creator = BD.getNameById(g.IdPublisher);
        ViewBag.reviews = BD.getGameReviews(gameId);
        return View();
    }


    [HttpPost]
    public async Task<IActionResult> UploadGameImage(IFormFile image, string returnUrl, int idGame)
    {
        Game game = BD.findGameById(idGame);

        if (game == null)
        {
            return Content("Game no encontrado");
        }

        if (image != null && image.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/GamePictures/" + idGame);

            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var extension = Path.GetExtension(image.FileName);
            var nombreArchivo = $"{game.Id}_{Guid.NewGuid():N}{extension}";
            var path = Path.Combine(uploads, nombreArchivo);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            BD.InsertImagesGame(idGame, nombreArchivo);
        }

        return Redirect(returnUrl);
    }


    [HttpPost]
    public async Task<IActionResult> SubirImagen(IFormFile avatar, string returnUrl)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));

        if (avatar != null && avatar.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/profilePictures");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            var extension = Path.GetExtension(avatar.FileName);
            var nombreArchivo = $"{usuario.Id}_{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploads, nombreArchivo);

            // 🔹 Guardar temporalmente el archivo subido
            var tempPath = Path.Combine(uploads, "temp_" + Guid.NewGuid() + extension);
            using (var tempStream = new FileStream(tempPath, FileMode.Create))
            {
                await avatar.CopyToAsync(tempStream);
            }

            // 🔹 Borrar la imagen anterior si existe
            if (!string.IsNullOrEmpty(usuario.ProfilePicture))
            {
                var oldPath = Path.Combine(uploads, usuario.ProfilePicture);
                if (System.IO.File.Exists(oldPath))
                {
                    try { System.IO.File.Delete(oldPath); } catch { }
                }
            }

            // 🔹 Procesar la imagen cuadrada desde el archivo temporal
            using (var image = SixLabors.ImageSharp.Image.Load(tempPath))
            {
                int lado = Math.Min(image.Width, image.Height);
                int x = (image.Width - lado) / 2;
                int y = (image.Height - lado) / 2;

                image.Mutate(xform => xform.Crop(new SixLabors.ImageSharp.Rectangle(x, y, lado, lado)));

                // opcional: redimensionar
                image.Mutate(xform => xform.Resize(400, 400));

                await image.SaveAsync(filePath);
            }

            // 🔹 Borrar el temporal
            System.IO.File.Delete(tempPath);

            // 🔹 Actualizar en BD y sesión
            BD.updateProfilePicture(nombreArchivo, usuario.Id);
            usuario.ProfilePicture = nombreArchivo;
            HttpContext.Session.SetString("usuario", Objeto.ObjectToString(usuario));
        }

        return Redirect(returnUrl);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Profile(int userId)
    {
        bool isMe = false;
        User user = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        ViewBag.usuario = BD.getUserData(userId);
        if (user != null)
        {
            isMe = (user.Id == userId);
        }

        ViewBag.me = isMe;

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [HttpGet("download/{gameId}")]
    public IActionResult DownloadGame(int gameId)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if (usuario == null)
        {
            return RedirectToAction("Login");
        }
        if (!BD.IsOwner(gameId, usuario.Id))
        {
            return RedirectToAction("GamePage", new { gameId = gameId });
        }
        var path = Path.Combine("Games", gameId + ".zip");


        if (!System.IO.File.Exists(path))
            return NotFound();


        var bytes = System.IO.File.ReadAllBytes(path);


        return File(bytes, "application/zip", gameId + ".zip");
    }

    [HttpGet("buy/{gameId}")]
    public IActionResult buy(int gameId)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if (usuario == null)
        {
            return RedirectToAction("Login");
        }
        if(!usuario.Verified)
        {
            return View("Verify");
        }
        BD.AddOwnership(gameId, usuario.Id);
        return RedirectToAction("GamePage", new { gameId = gameId });
    }

    [HttpGet]
    public IActionResult GamePublicationEnter()
    {
        return View("GamePublication");
    }

    
    public async Task<IActionResult> mail()
    {
        /*User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuarioToVerify"));
        var consumerKey = ApiKeyManager.MailConsumerKey;
        var consumerSecret = ApiKeyManager.MailSecretKey;

        string url = "https://api.turbo-smtp.com/api/v2/mail/send";

        var mailData = new
        {
            authuser = consumerKey,
            authpass = consumerSecret,

            from = "noreply@indeura.sytes.net",
            to = usuario.Email,
            subject = "Indeura verify email",
            content = "Please verify your account: " + usuario.verifyHash
        };

        using (HttpClient httpClient = new HttpClient())
        {
            // JSON data seriaization
            var json = JsonSerializer.Serialize(mailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Trigger POST request
            using (var response = await httpClient.PostAsync(url, content))
            {
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response: " + result);
                }
                else
                {
                    Console.WriteLine("Request error: " + response.StatusCode);
                }
            }
        }*/
        return View("Verify");
    }

    [HttpPost]
    public IActionResult GamePublication(string name, string description)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));

        if (BD.thisGameExists(name))
        {
            ViewBag.Error = "Ese nombre ya esta en uso";
            ViewBag.Desc = description;
            return View(); 
        }

        BD.CreateNewGamePage(name, description, usuario.Id, DateTime.Today);

        return RedirectToAction("Index");
    }

    public IActionResult Verify(string code)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuarioToVerify"));
        if(/*code == usuario.verifyHash*/ true)
        {
            BD.getVerified(usuario.Id);
            usuario.Verified = true;
            HttpContext.Session.SetString("usuario", Objeto.ObjectToString(usuario));
        }
        

        return View("Index");
    }


    [HttpPost]
    public IActionResult Login(string usuario, string password, string returnUrl)
    {

        User user = BD.IniciarSesion(usuario, password);

        if (user != null)
        {
            HttpContext.Session.SetString("usuario", Objeto.ObjectToString(user));

            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index");
        }

        ViewBag.Error = "Usuario o contraseña incorrectos";
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    public IActionResult Login()
    {
        return View();
    }

    public IActionResult LoginRedirect(string returnUrl)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View("Login");
    }

    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Register(string Nombre, string Contraseña, string Email)
    {
        string redirect = "Register";
        bool registrado = BD.Registrarte(Nombre, Contraseña, Email);

        if (registrado)
        {
            redirect = "mail";
            User user = BD.IniciarSesion(Nombre, Contraseña);
            HttpContext.Session.SetString("usuarioToVerify", Objeto.ObjectToString(user));
        }
        else
        {
            ViewBag.Error = "El usuario o email ya existe";
        }

        return RedirectToAction(redirect);
    }

    [HttpPost]
    public IActionResult Review(int gameId, string description, int rate)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if (usuario != null && !BD.GetReviewed(usuario.Id, gameId))
        {
            BD.publishReview(gameId, usuario.Id, (double)rate, BD.GetPlaytime(usuario.Id, gameId), description);
        }
        else
        {
            return RedirectToAction("Login");
        }
        return RedirectToAction("GamePage", new { gameId = gameId });
    }

    public IActionResult Store()
    {
        ViewBag.listGamesId = BD.getAllGamesId();
        return View();
    }

    public IActionResult UploadGame()
    {
        return View();
    }

    [HttpPost]
public async Task<IActionResult> UploadGame(IFormFile game, int gameId)
{
    if (game == null || game.Length == 0)
        return BadRequest("Archivo inválido");

    var extension = Path.GetExtension(game.FileName).ToLower();

    if (extension != ".zip")
        return BadRequest("Solo .zip");

    if (game.Length > 650 * 1024 * 1024)
        return BadRequest("El archivo supera los 650MB (límite VirusTotal)");


    var tempFolder = Path.Combine(Directory.GetCurrentDirectory(), "TempUploads");
    Directory.CreateDirectory(tempFolder);

    var tempPath = Path.Combine(tempFolder, gameId + extension);

    if (System.IO.File.Exists(tempPath))
        System.IO.File.Delete(tempPath);

    using (var stream = new FileStream(tempPath, FileMode.Create))
    {
        await game.CopyToAsync(stream);
    }

    string uploadUrl = "https://www.virustotal.com/api/v3/files";

    var client = new RestClient();

    // 🔴 Si pesa más de 32MB → pedir upload_url
    if (game.Length > 32 * 1024 * 1024)
    {
        var urlRequest = new RestRequest("https://www.virustotal.com/api/v3/files/upload_url");
        urlRequest.AddHeader("x-apikey", ApiKeyManager.VirusTotalKey);

        var urlResponse = await client.GetAsync(urlRequest);

        dynamic urlJson = Newtonsoft.Json.JsonConvert.DeserializeObject(urlResponse.Content);
        uploadUrl = urlJson.data;
    }

    // Subida del archivo
    var uploadRequest = new RestRequest(uploadUrl);
    uploadRequest.AddHeader("x-apikey", ApiKeyManager.VirusTotalKey);
    uploadRequest.AddFile("file", tempPath);

    var response = await client.PostAsync(uploadRequest);

    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);
    string analysisId = json.data.id;

    BD.SaveScan(gameId, tempPath, analysisId);

    return RedirectToAction("GamePage", new { gameId = gameId });
}

[HttpGet]
public async Task<IActionResult> CheckStatus(int gameId)
{
    var scan = BD.GetScan(gameId);

    if (scan == null)
        return NotFound("No existe");

    if (scan.Status != "Scanning")
        return Ok(scan.Status);

    var client = new RestClient($"https://www.virustotal.com/api/v3/analyses/{scan.AnalysisId}");
    var request = new RestRequest();
    request.AddHeader("x-apikey", ApiKeyManager.VirusTotalKey);

    var response = await client.GetAsync(request);

    dynamic json = Newtonsoft.Json.JsonConvert.DeserializeObject(response.Content);

    string status = json.data.attributes.status;

    if (status != "completed")
        return Ok("Scanning");

    int malicious = json.data.attributes.stats.malicious;

    if (malicious == 0)
    {
        var finalFolder = Path.Combine(Directory.GetCurrentDirectory(), "Games");
        Directory.CreateDirectory(finalFolder);

        var finalPath = Path.Combine(finalFolder, Path.GetFileName(scan.TempPath));

        System.IO.File.Move(scan.TempPath, finalPath);

        BD.UpdateStatus(gameId, "Safe");

        return Ok("Safe");
    }
    else
    {
        System.IO.File.Delete(scan.TempPath);

        BD.UpdateStatus(gameId, "Malicious");

        return Ok("Malicious");
    }
}

}
