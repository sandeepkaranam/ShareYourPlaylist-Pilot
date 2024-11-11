using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ShareYourPlaylist.Models;
using ShareYourPlaylist.Services;

namespace ShareYourPlaylist.Data
{
    public sealed class PlaylistDataStore
    {
        private static readonly Lazy<PlaylistDataStore> instance = new Lazy<PlaylistDataStore>(() => new PlaylistDataStore());
        private readonly APIController apiController = APIController.Instance;
        private List<PlaylistViewModel> playlists = new List<PlaylistViewModel>();
        private bool isInitialized = false;

        private PlaylistDataStore() { }

        public static PlaylistDataStore Instance => instance.Value;

        // Initialize the playlists data from Spotify (called only once at startup)
        public async Task InitializeAsync()
        {
            if (isInitialized) return;

            try
            {
                var token = await apiController.GetTokenAsync();
                if (token != null)
                {
                    var fetchedPlaylists = await apiController.GetRandomPlaylistsAsync(token, 10);
                    if (fetchedPlaylists != null && fetchedPlaylists.Any())
                    {
                        playlists = fetchedPlaylists; // Populate the playlists list with API data
                        isInitialized = true;
                    }
                    else
                    {
                        Console.WriteLine("Failed to fetch playlists from Spotify API.");
                    }
                }
                else
                {
                    Console.WriteLine("Failed to obtain Spotify API token.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing playlists: {ex.Message}");
            }
        }

        public List<PlaylistViewModel> GetPlaylists()
        {
            return playlists;
        }


        // Method to get all playlists
        public List<PlaylistViewModel> GetAllPlaylists()
        {
            return playlists;
        }

        // AddPlaylist method to add a new playlist to the data store
        public void AddPlaylist(PlaylistViewModel playlist)
        {
            if (playlist != null)
            {
                playlists.Add(playlist);
            }
        }


        public PlaylistViewModel? GetPlaylistById(string playlistId)
        {
            var playlist = playlists.FirstOrDefault(p => p.Id == playlistId);
            if (playlist != null && !playlist.Songs.Any())
            {
                var token = apiController.GetTokenAsync().Result;
                var detailedPlaylist = apiController.GetPlaylistDetailsAsync(token, playlistId).Result;
                playlist.Songs = detailedPlaylist.Songs;
            }
            return playlist;
        }

        public async Task AddSongToPlaylist(string playlistId, string songUri)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null)
            {
                var token = await APIController.Instance.GetTokenAsync();
                var songDetails = await GetSongDetailsFromSpotify(songUri, token);

                if (songDetails != null)
                {
                    var newSong = new SongViewModel
                    {
                        Id = songDetails.Id,
                        Name = songDetails.Name,
                        Artist = songDetails.Artist,
                        Album = songDetails.Album,
                        Duration = songDetails.Duration,
                        SpotifyUri = songUri,
                        ImageUrl = songDetails.ImageUrl
                    };

                    playlist.Songs.Add(newSong);
                }
            }
        }

        private async Task<SongViewModel?> GetSongDetailsFromSpotify(string songUri, string token)
        {
            try
            {
                var trackId = ExtractTrackId(songUri);

                if (string.IsNullOrEmpty(trackId))
                {
                    Console.WriteLine("Invalid track URI or URL.");
                    return null;
                }

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await httpClient.GetAsync($"https://api.spotify.com/v1/tracks/{trackId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var trackJson = JsonDocument.Parse(json).RootElement;

                    var song = new SongViewModel
                    {
                        Id = trackJson.GetProperty("id").GetString(),
                        Name = trackJson.GetProperty("name").GetString(),
                        Artist = trackJson.GetProperty("artists")[0].GetProperty("name").GetString(),
                        Album = trackJson.GetProperty("album").GetProperty("name").GetString(),
                        Duration = TimeSpan.FromMilliseconds(trackJson.GetProperty("duration_ms").GetInt32()).ToString(@"mm\:ss"),
                        ImageUrl = trackJson.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString()
                    };

                    return song;
                }
                else
                {
                    Console.WriteLine($"Failed to fetch song details: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        private string ExtractTrackId(string songUri)
        {
            if (songUri.Contains("spotify:track:"))
            {
                return songUri.Split(':').Last();
            }
            else if (songUri.Contains("open.spotify.com/track/"))
            {
                var parts = songUri.Split(new[] { "track/", "?" }, StringSplitOptions.None);
                return parts.Length > 1 ? parts[1] : string.Empty;
            }
            else
            {
                return songUri;
            }
        }

        public void UpdateSongInPlaylist(string playlistId, string songId, string newArtist, string newAlbum)
        {
            var playlist = GetPlaylistById(playlistId);
            var song = playlist?.Songs.FirstOrDefault(s => s.Id == songId);
            if (song != null)
            {
                song.Artist = newArtist;
                song.Album = newAlbum;
            }
        }

        public void RemoveSongFromPlaylist(string playlistId, string songUri)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null)
            {
                var song = playlist.Songs.FirstOrDefault(s => s.SpotifyUri == songUri);
                if (song != null)
                {
                    playlist.Songs.Remove(song);
                }
            }
        }
    }
}
