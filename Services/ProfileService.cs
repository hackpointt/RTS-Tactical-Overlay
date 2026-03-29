using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service for loading and saving tactical profiles from/to JSON files
/// </summary>
public class ProfileService : IProfileService
{
    private TacticalProfile? _currentProfile;
    private ProfileIndex? _profileIndex;
    private readonly JsonSerializerOptions _jsonOptions;
    private string _currentProfilePath = "";

    public TacticalProfile? CurrentProfile => _currentProfile;
    public string ProfilesDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profiles");

    public ProfileService()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<TacticalProfile?> LoadProfileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var profile = JsonSerializer.Deserialize<TacticalProfile>(json, _jsonOptions);

            if (profile != null && profile.IsValid())
            {
                _currentProfile = profile;
                return profile;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading profile: {ex.Message}");
            return null;
        }
    }

    public async Task SaveProfileAsync(TacticalProfile profile, string filePath)
    {
        try
        {
            if (!profile.IsValid())
            {
                throw new InvalidOperationException("Cannot save invalid profile");
            }

            var json = JsonSerializer.Serialize(profile, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving profile: {ex.Message}");
            throw;
        }
    }

    public void SetCurrentProfile(TacticalProfile? profile)
    {
        _currentProfile = profile;
    }

    public async Task InitializeAsync()
    {
        // Create profiles directory if it doesn't exist
        if (!Directory.Exists(ProfilesDirectory))
        {
            Directory.CreateDirectory(ProfilesDirectory);
        }

        // Load or create index
        _profileIndex = await LoadIndexAsync();

        // Migrate old sample-profile.json if exists and no profiles yet
        if (_profileIndex.Profiles.Count == 0)
        {
            await MigrateOldProfileAsync();
        }

        // Load active profile if set
        if (!string.IsNullOrEmpty(_profileIndex.ActiveProfileId))
        {
            await SwitchProfileAsync(_profileIndex.ActiveProfileId);
        }
        else if (_profileIndex.Profiles.Count > 0)
        {
            await SwitchProfileAsync(_profileIndex.Profiles[0].Id);
        }
    }

    private async Task MigrateOldProfileAsync()
    {
        var oldProfilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample-profile.json");
        if (!File.Exists(oldProfilePath)) return;

        try
        {
            var json = await File.ReadAllTextAsync(oldProfilePath);
            var oldProfile = JsonSerializer.Deserialize<TacticalProfile>(json, _jsonOptions);
            if (oldProfile == null) return;

            // Ensure it has an ID and alias
            if (string.IsNullOrEmpty(oldProfile.Id))
                oldProfile.Id = Guid.NewGuid().ToString();
            if (string.IsNullOrEmpty(oldProfile.Alias))
                oldProfile.Alias = oldProfile.Name.Length > 5 ? oldProfile.Name.Substring(0, 5).ToUpper() : oldProfile.Name.ToUpper();

            // Save to new location
            var fileName = $"profile-{oldProfile.Id}.json";
            var newPath = Path.Combine(ProfilesDirectory, fileName);
            await SaveProfileAsync(oldProfile, newPath);

            // Add to index
            _profileIndex!.Profiles.Add(new ProfileIndexEntry
            {
                Id = oldProfile.Id,
                Name = oldProfile.Name,
                Alias = oldProfile.Alias,
                FileName = fileName
            });
            _profileIndex.ActiveProfileId = oldProfile.Id;
            await SaveIndexAsync(_profileIndex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error migrating old profile: {ex.Message}");
        }
    }

    public async Task<ProfileIndex> LoadIndexAsync()
    {
        var indexPath = Path.Combine(ProfilesDirectory, "index.json");
        try
        {
            if (File.Exists(indexPath))
            {
                var json = await File.ReadAllTextAsync(indexPath);
                var index = JsonSerializer.Deserialize<ProfileIndex>(json, _jsonOptions);
                if (index != null)
                {
                    _profileIndex = index;
                    return index;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading index: {ex.Message}");
        }

        // Return empty index if not found or error
        _profileIndex = new ProfileIndex();
        return _profileIndex;
    }

    public async Task SaveIndexAsync(ProfileIndex index)
    {
        var indexPath = Path.Combine(ProfilesDirectory, "index.json");
        try
        {
            if (!Directory.Exists(ProfilesDirectory))
            {
                Directory.CreateDirectory(ProfilesDirectory);
            }

            var json = JsonSerializer.Serialize(index, _jsonOptions);
            await File.WriteAllTextAsync(indexPath, json);
            _profileIndex = index;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving index: {ex.Message}");
            throw;
        }
    }

    public async Task<List<ProfileIndexEntry>> GetAllProfilesAsync()
    {
        if (_profileIndex == null)
        {
            await LoadIndexAsync();
        }
        return _profileIndex?.Profiles ?? new List<ProfileIndexEntry>();
    }

    public async Task<TacticalProfile> CreateProfileAsync(string name, string alias)
    {
        var profile = new TacticalProfile
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Alias = alias.Length > 5 ? alias.Substring(0, 5).ToUpper() : alias.ToUpper(),
            Stages = new List<Stage>
            {
                new Stage(3.0, '1', "Unit Group 1")
            }
        };

        var fileName = $"profile-{profile.Id}.json";
        var filePath = Path.Combine(ProfilesDirectory, fileName);
        await SaveProfileAsync(profile, filePath);

        if (_profileIndex == null)
        {
            await LoadIndexAsync();
        }

        _profileIndex!.Profiles.Add(new ProfileIndexEntry
        {
            Id = profile.Id,
            Name = profile.Name,
            Alias = profile.Alias,
            FileName = fileName
        });
        await SaveIndexAsync(_profileIndex);

        return profile;
    }

    public async Task SaveCurrentProfileAsync()
    {
        if (_currentProfile == null || string.IsNullOrEmpty(_currentProfilePath))
        {
            return;
        }

        await SaveProfileAsync(_currentProfile, _currentProfilePath);

        // Update index entry if name/alias changed
        if (_profileIndex != null)
        {
            var entry = _profileIndex.Profiles.FirstOrDefault(p => p.Id == _currentProfile.Id);
            if (entry != null)
            {
                entry.Name = _currentProfile.Name;
                entry.Alias = _currentProfile.Alias;
                await SaveIndexAsync(_profileIndex);
            }
        }
    }

    public async Task DeleteProfileAsync(string profileId)
    {
        if (_profileIndex == null)
        {
            await LoadIndexAsync();
        }

        var entry = _profileIndex!.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (entry == null) return;

        // Delete the file
        var filePath = Path.Combine(ProfilesDirectory, entry.FileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        // Remove from index
        _profileIndex.Profiles.Remove(entry);

        // If this was the active profile, switch to another
        if (_profileIndex.ActiveProfileId == profileId)
        {
            _profileIndex.ActiveProfileId = _profileIndex.Profiles.FirstOrDefault()?.Id ?? "";
            if (!string.IsNullOrEmpty(_profileIndex.ActiveProfileId))
            {
                await SwitchProfileAsync(_profileIndex.ActiveProfileId);
            }
            else
            {
                _currentProfile = null;
                _currentProfilePath = "";
            }
        }

        await SaveIndexAsync(_profileIndex);
    }

    public async Task<TacticalProfile?> SwitchProfileAsync(string profileId)
    {
        if (_profileIndex == null)
        {
            await LoadIndexAsync();
        }

        var entry = _profileIndex!.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (entry == null) return null;

        var filePath = Path.Combine(ProfilesDirectory, entry.FileName);
        var profile = await LoadProfileAsync(filePath);

        if (profile != null)
        {
            _currentProfilePath = filePath;
            _profileIndex.ActiveProfileId = profileId;
            await SaveIndexAsync(_profileIndex);
        }

        return profile;
    }
}