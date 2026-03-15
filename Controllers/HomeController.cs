using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using proyect.Models;

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

    public IActionResult GamePage(int gameId)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        Game g = BD.findGameById(gameId);
        ViewBag.gameInfo = g;
        ViewBag.images = BD.getGamePictures(gameId);
        if(usuario != null)
        ViewBag.isOwner = BD.IsOwner(gameId, usuario.Id);
        ViewBag.tags = BD.getGameTags(gameId);
        ViewBag.creator = BD.getNameById(g.IdPublisher);
        ViewBag.reviews = BD.getGameReviews(gameId);
        return View();
    }

    public IActionResult Privacy()
    {
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

    /*[HttpPost]
    public IActionResult Login(string usuario, string password, string password2)
    {
        string redirect="Login";
        if(password != password2)
        {
            ViewBag.Error = "Contraseñas no coinciden";
            return RedirectToAction("Login");
        }
        User user = BD.IniciarSesion(usuario, password);
        
        if(user != null)
        {
            HttpContext.Session.SetString("usuario", Objeto.ObjectToString(user));
            redirect = "Index";
        }
        else
        {
             ViewBag.Error = "Usuario o contraseña incorrectos";
        }
       
        return View(redirect);
    }*/
    

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
    public IActionResult Review(int gameId, string description, double rate)
    {
        User usuario = Objeto.StringToObject<User>(HttpContext.Session.GetString("usuario"));
        if(usuario != null && !BD.GetReviewed(usuario.Id,gameId))
        {
            BD.publishReview(gameId,usuario.Id,rate,BD.GetPlaytime(usuario.Id,gameId),description);
        }
        else
        {
            return RedirectToAction("Login");
        }
        return RedirectToAction("GamePage", new { gameId = gameId });
    }

}
