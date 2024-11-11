using System.Collections.Generic;

namespace ShareYourPlaylist.Models
{
    public class PlaylistViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // URL for playlist image
        public List<SongViewModel> Songs { get; set; } = new List<SongViewModel>();
    }
}
