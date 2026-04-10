using System.ComponentModel;
using System.IO;
using Exiled.API.Features;
using Exiled.API.Interfaces;

namespace BetterFlashbangs;

public class Config : IConfig
{
    public bool IsEnabled { get; set; } = true;
    public bool Debug { get; set; } = false;

    [Description("Distance between player and flashbang.")]
    public int Distance { get; set; } = 10;

    [Description("Flashbangs explode timer (<2.7s)")]
    public float FuseTime { get; set; } = 1.5f;

    [Description("Flashbangs sound path")]
    public string SoundPath { get; set; } = Path.Combine(Paths.Configs, "Audio", "flashbang.ogg");
}