using System;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Pickups.Projectiles;
using Exiled.Events.EventArgs.Player;
using MEC;
using UnityEngine;
using Exiled.API.Features.Toys;
using Light = Exiled.API.Features.Toys.Light;

namespace BetterFlashbangs;

public class Plugin : Plugin<Config>
{
    const int FlashbangMask = 1208221697; // FlashbangGrenade.BlindingMask;

    public override string Name => "BetterFlashbangs";
    public override string Prefix => "BetterFlashbangs";
    public override string Author => "@grummhd";
    public override Version Version => new(1, 0, 0);
    public override Version RequiredExiledVersion => new(9, 13, 3);

    public override void OnEnabled()
    {
        try
        {
            AudioClipStorage.LoadClip(Config.SoundPath, "flashbang");
        }
        catch (Exception e)
        {
            Log.Error(e);
            throw;
        }

        Exiled.Events.Handlers.Player.ThrownProjectile += Thrown;

        base.OnEnabled();
    }

    public override void OnDisabled()
    {
        Exiled.Events.Handlers.Player.ThrownProjectile -= Thrown;

        base.OnDisabled();
    }

    public void Thrown(ThrownProjectileEventArgs ev)
    {
        if (ev.Throwable.Type == ItemType.GrenadeFlash)
        {
            Projectile? flashbang = ev.Throwable.Projectile;

            Timing.CallDelayed(Config.FuseTime, () =>
            {
                flashbang.Destroy();
                PlaySound(ev.Player, "flashbang");

                var light = Light.Create(flashbang.Position);
                light.Range = 100;
                light.Intensity = 700;
                light.Color = Color.white;

                var primitive = Primitive.Create(PrimitiveType.Cube, flashbang.Position, new Vector3(.5f, .5f, .5f));
                primitive.Collidable = false;
                primitive.Visible = true;
                primitive.Color = Color.white;

                Timing.CallDelayed(0.1f, () =>
                {
                    primitive.Destroy();
                    light.Destroy();
                });

                var maxDistance = Config.Distance * Config.Distance;

                foreach (Player player in Player.List)
                {
                    float distance = SqrDistance(player.Position, flashbang.Position);

                    if (distance > maxDistance)
                        continue;

                    if (Physics.Linecast(flashbang.Position, player.Position, FlashbangMask))
                        continue;

                    Vector3 directionToFlashbang = (flashbang.Position - player.CameraTransform.position).normalized;

                    if (Vector3.Dot(player.CameraTransform.forward, directionToFlashbang) > 0)
                    {
                        int duration = distance switch
                        {
                            >= 35 * 35 => 1,
                            >= 20 * 20 => 2,
                            >= 10 * 10 => 3,
                            _ => 4,
                        };

                        player.EnableEffect(EffectType.Flashed, duration);
                    }
                }
            });
        }
    }

    public static float SqrDistance(Vector3 a, Vector3 b) => (a - b).sqrMagnitude;

    public void PlaySound(Player player, string soundName)
    {
        AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"flashbang{player.Id}", onIntialCreation: (p) => { p.AddSpeaker("Main", isSpatial: true, minDistance: 5f, maxDistance: 20f); }, destroyWhenAllClipsPlayed: true);

        audioPlayer.TryGetSpeaker("Main", out var speaker);
        speaker.Position = player.Position;

        audioPlayer.AddClip(soundName);
    }
}
