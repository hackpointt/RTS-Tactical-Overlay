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
}
