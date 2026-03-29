using System.Collections.Generic;
using System.Threading.Tasks;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service for loading and saving tactical profiles
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Loads a profile from a JSON file
    /// </summary>
    Task<TacticalProfile?> LoadProfileAsync(string filePath);

    /// <summary>
    /// Saves a profile to a JSON file
    /// </summary>
    Task SaveProfileAsync(TacticalProfile profile, string filePath);

    /// <summary>
    /// Gets the currently loaded profile
    /// </summary>
    TacticalProfile? CurrentProfile { get; }

    /// <summary>
    /// Sets the current profile
    /// </summary>
    void SetCurrentProfile(TacticalProfile? profile);

    /// <summary>
    /// Gets the profiles directory path
    /// </summary>
    string ProfilesDirectory { get; }

    /// <summary>
    /// Loads the profile index
    /// </summary>
    Task<ProfileIndex> LoadIndexAsync();

    /// <summary>
    /// Saves the profile index
    /// </summary>
    Task SaveIndexAsync(ProfileIndex index);

    /// <summary>
    /// Gets all available profiles from the index
    /// </summary>
    Task<List<ProfileIndexEntry>> GetAllProfilesAsync();

    /// <summary>
    /// Creates a new profile with the given name and alias
    /// </summary>
    Task<TacticalProfile> CreateProfileAsync(string name, string alias);

    /// <summary>
    /// Deletes a profile by its ID
    /// </summary>
    Task DeleteProfileAsync(string profileId);

    /// <summary>
    /// Switches to a different profile by ID
    /// </summary>
    Task<TacticalProfile?> SwitchProfileAsync(string profileId);

    /// <summary>
    /// Saves the current profile to its file
    /// </summary>
    Task SaveCurrentProfileAsync();

    /// <summary>
    /// Initializes the profile system (creates directory, migrates old profiles)
    /// </summary>
    Task InitializeAsync();
}
