using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ShareYourPlaylist.Models
{
    public class CreatePlaylistViewModel
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; } // For image upload
        public string ImageUrl { get; set; } = string.Empty; // To hold uploaded image path for preview
        public List<string> SongUris { get; set; } = new List<string>(); // List of Spotify URIs to add songs
        public List<SongViewModel> Songs { get; set; } = new List<SongViewModel>(); // Songs to display
    }
}
