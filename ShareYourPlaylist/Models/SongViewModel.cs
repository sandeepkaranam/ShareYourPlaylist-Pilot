namespace ShareYourPlaylist.Models
{
    public class SongViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string SpotifyUri { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // URL for song image
    }
}
