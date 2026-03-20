using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using proyect.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;


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

     public IActionResult UpdateGameDescription(string description, string returnUrl)
    {

        return RedirectToAction(returnUrl);
    }

    public IActionResult GamePage(int gameId)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        Game g = BD.findGameById(gameId);

        ViewBag.ImPublisher = (usuario.Id == g.IdPublisher);
        ViewBag.gameInfo = g;
        ViewBag.images = BD.getGamePictures(gameId);
        if(usuario != null)
        ViewBag.isOwner = BD.IsOwner(gameId, usuario.Id);
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
        var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/GamePictures/"+idGame);

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

    return RedirectToAction(returnUrl);
}

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Profile(int userId)
    {
        ViewBag.usuario = BD.getUserData(userId);
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
        if(usuario == null)
        {
            return RedirectToAction("Login");
        }
        if (!BD.IsOwner(gameId,usuario.Id))
        {
            return RedirectToAction("GamePage", new { gameId = gameId });
        }
        var path = Path.Combine("Games", gameId + ".zip");


        if(!System.IO.File.Exists(path))
            return NotFound();


        var bytes = System.IO.File.ReadAllBytes(path);


        return File(bytes, "application/zip", gameId + ".zip");
    }

    [HttpGet("buy/{gameId}")]
    public IActionResult buy(int gameId)    
    {     
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if(usuario == null)
        {
            return RedirectToAction("Login");
        }
        BD.AddOwnership(gameId,usuario.Id);
        return RedirectToAction("GamePage", new { gameId = gameId });
    }

    public IActionResult GamePublication()
    {      
        return View();
    }

    public IActionResult GamePublication(string name, string description)
    {        
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if(BD.thisGameExists(name))
        {
            ViewBag.Error = "Ese nombre ya esta en uso";
            ViewBag.Desc = description;
            return RedirectToAction("GamePublication");
        }        
        
        BD.CreateNewGamePage(name, description, usuario.Id, DateTime.Today.ToString("dd/MM/yyyy"));
        return View();
    }
    

    [HttpPost]
    public IActionResult Login(string usuario, string password, string password2, string returnUrl)
        {
            if(password != password2)
        {
            ViewBag.Error = "Contraseñas no coinciden";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        User user = BD.IniciarSesion(usuario, password);

        if(user != null)
        {
            HttpContext.Session.SetString("usuario", Objeto.ObjectToString(user));

            if(!string.IsNullOrEmpty(returnUrl))
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
    public IActionResult Register(string Nombre, string Contraseña, string Email, bool IsDeveloper)
    {
        string redirect="Register";
        bool registrado = BD.Registrarte(Nombre, Contraseña, Email, IsDeveloper);

        if(registrado)
        {
            redirect="Index";
            User user = BD.IniciarSesion(Nombre, Contraseña);
            HttpContext.Session.SetString("usuario", Objeto.ObjectToString(user));
        }
        else
        {
            ViewBag.Error = "El usuario o email ya existe";
        }
        
        return View(redirect);
    }

    [HttpPost]
    public IActionResult Review(int gameId, string description, int rate)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if(usuario != null && !BD.GetReviewed(usuario.Id,gameId))
        {
            BD.publishReview(gameId,usuario.Id,(double)rate,BD.GetPlaytime(usuario.Id,gameId),description);
        }
        else
        {
            return RedirectToAction("Login");
        }
        return RedirectToAction("GamePage", new { gameId = gameId });
    }

}
