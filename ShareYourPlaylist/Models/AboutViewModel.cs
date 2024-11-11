namespace ShareYourPlaylist.Models
{
    public class AboutViewModel
    {
        public string Title { get; set; } = "About Us";
        public string WelcomeMessage { get; set; } = "About ShareYourPlaylist";
        public string Description { get; set; } = "Learn more about our mission to connect people through music.";
        public string ImageUrl { get; set; } = "/images/about-us.jpg"; // Path to an about us image
    }
}
