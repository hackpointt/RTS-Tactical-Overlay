using System.Collections.Generic;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Represents an entry in the profile index
/// </summary>
public class ProfileIndexEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public string FileName { get; set; } = "";
}

/// <summary>
/// Index file that tracks all available profiles
/// </summary>
public class ProfileIndex
{
    public string ActiveProfileId { get; set; } = "";
    public List<ProfileIndexEntry> Profiles { get; set; } = new();
}
