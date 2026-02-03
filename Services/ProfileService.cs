using System;
using System.IO;
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
    private readonly JsonSerializerOptions _jsonOptions;

    public TacticalProfile? CurrentProfile => _currentProfile;

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
}
