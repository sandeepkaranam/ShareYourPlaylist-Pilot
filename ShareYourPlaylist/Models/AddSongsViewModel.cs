namespace ShareYourPlaylist.Models
{
    public class AddSongsViewModel
    {
        public string PlaylistId { get; set; } = string.Empty;
        public string PlaylistName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = "/images/default_playlist.jpg"; // Default image if no image provided
        public List<SongViewModel> Songs { get; set; } = new List<SongViewModel>();
    }
}
