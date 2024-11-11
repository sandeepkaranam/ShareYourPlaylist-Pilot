using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShareYourPlaylist.Models;
using ShareYourPlaylist.Services;
using ShareYourPlaylist.Data;
using ShareYourPlaylist.Views.Home;

namespace ShareYourPlaylist.Controllers
{
    public class HomeController : Controller
    {
        private readonly PlaylistDataStore _dataStore = PlaylistDataStore.Instance;

        // Home page action
        public IActionResult Index()
        {
            var model = new HomeViewModel
            {
                Title = "Welcome to ShareYourPlaylist",
                WelcomeMessage = "Explore playlists, create your own, and share the vibe with friends.",
                Description = "Discover, share, and create playlists to suit every mood.",
                ImageUrl = "/images/welcome-image.jpg"
            };
            return View(model);
        }

        // Display all playlists
        public async Task<IActionResult> Playlists()
        {
            await _dataStore.InitializeAsync(); // Ensure playlists are loaded
            var model = new PlaylistsViewModel
            {
                Playlists = _dataStore.GetPlaylists()
            };
            return View("Playlists", model);
        }

        // Display create playlist form
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult CreatePlaylist()
        {
            return View(new CreatePlaylistViewModel());
        }


        // Handle adding song URIs before creating the playlist
        [HttpPost]
        public IActionResult AddSongUriToTemporaryList(CreatePlaylistViewModel model, string songUri)
        {
            if (!string.IsNullOrEmpty(songUri))
            {
                model.SongUris.Add(songUri);
                TempData["SuccessMessage"] = "Song URI added successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Invalid Spotify URI.";
            }
            return View("CreatePlaylist", model); // Renders the form with the updated list
        }

        //// Handle create playlist form submission
        //[HttpPost]
        //public IActionResult CreatePlaylist(CreatePlaylistViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Create a new playlist
        //        var playlist = new PlaylistViewModel
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            Name = model.Name,
        //            ImageUrl = model.ImageFile != null ? UploadImage(model.ImageFile) : GenerateDefaultImage(),
        //            Songs = model.SongUris.Select(uri => new SongViewModel { SpotifyUri = uri }).ToList() // Prepopulate with song URIs
        //        };

        //        // Add the playlist to the data store
        //        _dataStore.AddPlaylist(playlist);

        //        TempData["SuccessMessage"] = "Playlist created successfully!";
        //        return RedirectToAction("Playlists");
        //    }
        //    return View(model);
        //}

        [HttpPost]
        public async Task<IActionResult> CreatePlaylist(CreatePlaylistViewModel model)
        {
            if (ModelState.IsValid)
            {
                var playlist = new PlaylistViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    ImageUrl = model.ImageFile != null ? UploadImage(model.ImageFile) : GenerateDefaultImage(),
                    Songs = new List<SongViewModel>() // Initialize the Songs list
                };

                var token = await APIController.Instance.GetTokenAsync();

                // Add songs based on URIs provided
                foreach (var songUri in model.SongUris)
                {
                    var song = await APIController.Instance.GetSongDetailsAsync(token, songUri);
                    if (song != null)
                    {
                        playlist.Songs.Add(song); // Add the song details to the playlist
                    }
                }

                _dataStore.AddPlaylist(playlist); // Save the playlist with songs

                TempData["SuccessMessage"] = "Playlist created successfully!";
                return RedirectToAction("Playlists");
            }

            return View(model);
        }


        // Display "About Us" page
        public IActionResult About()
        {
            var model = new AboutViewModel
            {
                Title = "About Us",
                WelcomeMessage = "About ShareYourPlaylist",
                Description = "Learn more about our mission to connect people through music.",
                ImageUrl = "/images/about-us.jpg"
            };
            return View(model);
        }

        // Display songs in a specific playlist
        public IActionResult DisplayPlaylistSongs(string playlistId)
        {
            var playlist = _dataStore.GetPlaylistById(playlistId);
            if (playlist == null)
            {
                ViewData["Error"] = "Playlist not found.";
                return RedirectToAction("Playlists");
            }

            return View("DisplayPlaylistSongs", playlist);
        }

        // Add a song to the playlist
        [HttpPost]
        public IActionResult AddSongToPlaylist(string playlistId, string songUri)
        {
            if (!string.IsNullOrEmpty(songUri))
            {
                _dataStore.AddSongToPlaylist(playlistId, songUri);
                return RedirectToAction("DisplayPlaylistSongs", new { playlistId });
            }

            ViewData["Error"] = "Invalid song URI.";
            return RedirectToAction("DisplayPlaylistSongs", new { playlistId });
        }

        // Edit a song in the playlist
        [HttpPost]
        public IActionResult EditSong(string playlistId, string songId, string newArtist, string newAlbum)
        {
            _dataStore.UpdateSongInPlaylist(playlistId, songId, newArtist, newAlbum);
            return RedirectToAction("DisplayPlaylistSongs", new { playlistId });
        }

        // Remove a song from the playlist
        [HttpPost]
        public IActionResult RemoveSongFromPlaylist(string playlistId, string songUri)
        {
            _dataStore.RemoveSongFromPlaylist(playlistId, songUri);
            return RedirectToAction("DisplayPlaylistSongs", new { playlistId });
        }

        // Helper method to handle image upload and save it to the server
        private string UploadImage(IFormFile imageFile)
        {
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }

            return "/images/" + uniqueFileName;
        }

        // Helper method to generate a default image (for example, a collage or a placeholder image)
        private string GenerateDefaultImage()
        {
            return "/images/default_playlist.jpg";
        }
    }
}
