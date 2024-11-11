using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace ShareYourPlaylist.Models
{
    public class CreatePlaylistViewModel
    {
        public string Name { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; } // For image upload
        public List<string> SongUris { get; set; } = new List<string>(); // List of Spotify URIs to add songs
    }
}
