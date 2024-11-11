using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ShareYourPlaylist.Models;

namespace ShareYourPlaylist.Services
{
    public sealed class APIController
    {
        private const string clientId = "437d3afd88974be588e195241a571aae";
        private const string clientSecret = "97a822b39ef14a3fba1bca2004f68805";
        private static readonly Lazy<APIController> instance = new Lazy<APIController>(() => new APIController());
        private static readonly HttpClient client = new HttpClient();
        private string? token;
        private DateTime tokenExpiry;

        private APIController() { }

        public static APIController Instance => instance.Value;

        public async Task<string> GetTokenAsync()
        {
            if (token != null && DateTime.UtcNow < tokenExpiry)
                return token;

            var authValue = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")
            {
                Content = new StringContent("grant_type=client_credentials", System.Text.Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Failed to obtain token.");
                return string.Empty;
            }

            var json = await response.Content.ReadAsStringAsync();
            using (var document = JsonDocument.Parse(json))
            {
                token = document.RootElement.GetProperty("access_token").GetString();
                var expiresIn = document.RootElement.GetProperty("expires_in").GetInt32();
                tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60); // Set expiry 1 minute earlier to be safe
            }
            return token ?? string.Empty;
        }

        // Fetch a list of featured/random playlists from Spotify
        public async Task<List<PlaylistViewModel>> GetRandomPlaylistsAsync(string token, int count)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"https://api.spotify.com/v1/browse/featured-playlists?limit={count}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch playlists: {response.StatusCode}");
                return new List<PlaylistViewModel>();
            }

            var json = await response.Content.ReadAsStringAsync();
            var playlists = new List<PlaylistViewModel>();

            var jsonDoc = JsonDocument.Parse(json);
            var items = jsonDoc.RootElement.GetProperty("playlists").GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                playlists.Add(new PlaylistViewModel
                {
                    Id = item.GetProperty("id").GetString(),
                    Name = item.GetProperty("name").GetString(),
                    ImageUrl = item.GetProperty("images")[0].GetProperty("url").GetString()
                });
            }

            return playlists;
        }

        // Fetch playlist details including tracks
        public async Task<PlaylistViewModel?> GetPlaylistDetailsAsync(string token, string playlistId)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"https://api.spotify.com/v1/playlists/{playlistId}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch playlist details for ID: {playlistId}, Status: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);

            var playlist = new PlaylistViewModel
            {
                Id = jsonDoc.RootElement.GetProperty("id").GetString(),
                Name = jsonDoc.RootElement.GetProperty("name").GetString(),
                Songs = new List<SongViewModel>()
            };

            foreach (var item in jsonDoc.RootElement.GetProperty("tracks").GetProperty("items").EnumerateArray())
            {
                var track = item.GetProperty("track");
                playlist.Songs.Add(new SongViewModel
                {
                    Id = track.GetProperty("id").GetString(),
                    Name = track.GetProperty("name").GetString(),
                    Artist = track.GetProperty("artists")[0].GetProperty("name").GetString(),
                    Album = track.GetProperty("album").GetProperty("name").GetString(),
                    Duration = TimeSpan.FromMilliseconds(track.GetProperty("duration_ms").GetInt32()).ToString(@"mm\:ss"),
                    SpotifyUri = track.GetProperty("uri").GetString(),
                    ImageUrl = track.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString()
                });
            }

            return playlist;
        }

        // Fetch song details by track ID
        public async Task<SongViewModel?> GetSongDetailsAsync(string token, string songUri)
        {
            var trackId = ExtractTrackId(songUri);
            if (string.IsNullOrEmpty(trackId)) return null;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.GetAsync($"https://api.spotify.com/v1/tracks/{trackId}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to fetch song details for ID: {trackId}, Status: {response.StatusCode}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json).RootElement;

            return new SongViewModel
            {
                Id = jsonDoc.GetProperty("id").GetString(),
                Name = jsonDoc.GetProperty("name").GetString(),
                Artist = jsonDoc.GetProperty("artists")[0].GetProperty("name").GetString(),
                Album = jsonDoc.GetProperty("album").GetProperty("name").GetString(),
                Duration = TimeSpan.FromMilliseconds(jsonDoc.GetProperty("duration_ms").GetInt32()).ToString(@"mm\:ss"),
                ImageUrl = jsonDoc.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString()
            };
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
            return songUri;
        }
    }
}
