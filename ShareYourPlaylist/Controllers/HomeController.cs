using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;
using ShareYourPlaylist.Models;
using ShareYourPlaylist.Services;
using ShareYourPlaylist.Data;

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
                Playlists = _dataStore.GetAllPlaylists()
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

        // Handle create playlist form submission
        [HttpPost]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult CreatePlaylist(CreatePlaylistViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Initialize a new playlist
                var playlist = new PlaylistViewModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = model.Name,
                    ImageUrl = model.ImageFile != null ? UploadImage(model.ImageFile) : GenerateDefaultImage(),
                    Songs = new List<SongViewModel>() // Initialize with an empty song list
                };

                // Add the new playlist to the data store
                _dataStore.AddPlaylist(playlist);

                TempData["SuccessMessage"] = "Playlist created successfully!";
                //return RedirectToAction("AddSongsToPlaylist", new { playlistId = playlist.Id });
                return RedirectToAction("Playlists");
            }

            return View(model);
        }

        // Display the details of a specific playlist with song addition option
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

        [HttpGet]
        public IActionResult AddSongsToPlaylist(string playlistId)
        {
            var playlist = _dataStore.GetPlaylistById(playlistId);
            if (playlist == null)
            {
                ViewData["Error"] = "Playlist not found.";
                return RedirectToAction("Playlists");
            }

            var model = new AddSongsViewModel
            {
                PlaylistId = playlist.Id,
                PlaylistName = playlist.Name,
                ImageUrl = playlist.ImageUrl,
                Songs = playlist.Songs
            };

            return View(model);
        }


        // Add a song to an existing playlist
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> AddSongToPlaylist(string playlistId, string songUri)
        {
            if (!string.IsNullOrEmpty(songUri))
            {
                await _dataStore.AddSongToPlaylist(playlistId, songUri);
                return RedirectToAction("AddSongsToPlaylist", new { playlistId });
            }

            ViewData["Error"] = "Invalid song URI.";
            return RedirectToAction("AddSongsToPlaylist", new { playlistId });
        }


        // Edit a song in the playlist
        [HttpPost]
        public IActionResult EditSong(string playlistId, string songId, string newArtist, string newAlbum)
        {
            _dataStore.UpdateSongInPlaylist(playlistId, songId, newArtist, newAlbum);
            TempData["SuccessMessage"] = "Song updated successfully!";
            return RedirectToAction("DisplayPlaylistSongs", new { playlistId });
        }

        // Remove a song from the playlist
        [HttpPost]
        public IActionResult RemoveSongFromPlaylist(string playlistId, string songUri)
        {
            _dataStore.RemoveSongFromPlaylist(playlistId, songUri);
            TempData["SuccessMessage"] = "Song removed successfully!";
            return RedirectToAction("DisplayPlaylistSongs", new { playlistId });
        }

        // Delete a playlist
        [HttpPost]
        public IActionResult DeletePlaylist(string playlistId)
        {
            _dataStore.RemovePlaylist(playlistId);
            TempData["SuccessMessage"] = "Playlist deleted successfully!";
            return RedirectToAction("Playlists");
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
