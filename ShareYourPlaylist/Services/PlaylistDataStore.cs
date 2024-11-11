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
                if (!string.IsNullOrEmpty(token))
                {
                    var fetchedPlaylists = await apiController.GetRandomPlaylistsAsync(token, 10);
                    playlists = fetchedPlaylists ?? new List<PlaylistViewModel>();
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

        // Get all playlists
        public List<PlaylistViewModel> GetAllPlaylists() => playlists;

        // Add a new playlist to the data store
        public void AddPlaylist(PlaylistViewModel playlist)
        {
            if (playlist != null)
            {
                playlists.Add(playlist);
            }
        }

        // Get a specific playlist by ID
        public PlaylistViewModel? GetPlaylistById(string playlistId)
        {
            var playlist = playlists.FirstOrDefault(p => p.Id == playlistId);
            if (playlist != null && !playlist.Songs.Any())
            {
                var token = apiController.GetTokenAsync().Result;
                var detailedPlaylist = apiController.GetPlaylistDetailsAsync(token, playlistId).Result;
                playlist.Songs = detailedPlaylist?.Songs ?? new List<SongViewModel>();
            }
            return playlist;
        }

        // Remove a playlist by ID
        public void RemovePlaylist(string playlistId)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null)
            {
                playlists.Remove(playlist);
            }
        }

        // Add a song to an existing playlist
        public async Task AddSongToPlaylist(string playlistId, string songUri)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null)
            {
                var token = await apiController.GetTokenAsync();
                var songDetails = await GetSongDetailsFromSpotify(songUri, token);

                if (songDetails != null)
                {
                    playlist.Songs.Add(songDetails);
                }
                else
                {
                    Console.WriteLine("Failed to add song to playlist.");
                }
            }
        }

        // Update song details within a playlist
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

        // Remove a song from a playlist by song URI
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

        // Helper method to get song details from Spotify API
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

                    return new SongViewModel
                    {
                        Id = trackJson.GetProperty("id").GetString(),
                        Name = trackJson.GetProperty("name").GetString(),
                        Artist = trackJson.GetProperty("artists")[0].GetProperty("name").GetString(),
                        Album = trackJson.GetProperty("album").GetProperty("name").GetString(),
                        Duration = TimeSpan.FromMilliseconds(trackJson.GetProperty("duration_ms").GetInt32()).ToString(@"mm\:ss"),
                        ImageUrl = trackJson.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString(),
                        SpotifyUri = songUri
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
            }
                return null;
            }
        }

        // Helper method to extract track ID from song URI
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

        public void DeletePlaylist(string playlistId)
        {
            var playlist = GetPlaylistById(playlistId);
            if (playlist != null)
            {
                playlists.Remove(playlist);
            }
        }
    }
}
