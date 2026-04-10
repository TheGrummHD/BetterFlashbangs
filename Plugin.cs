using System;
using AdminToys;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Player;
using MEC;
using Mirror;
using ProjectMER.Features;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterFlashbangs;

public class Plugin : Plugin<Config>
{
    public override string Name => "BetterFlashbangs";
    public override string Prefix => "BetterFlashbangs";
    public override string Author => "@grummhd";
    public override Version Version => new(1, 0, 0);
    public override Version RequiredExiledVersion => new (9, 13, 3);

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
            var flashbang = ev.Throwable.Projectile;

            Timing.CallDelayed(Config.FuseTime, () =>
            {
                flashbang.Destroy();
                PlaySound(ev.Player, "flashbang");

                LightSourceToy lightSourceToy = Object.Instantiate(PrefabManager.LightSource);

                lightSourceToy.NetworkLightColor = new Color(67, 67, 67);

                lightSourceToy.NetworkLightRange = 100;
                lightSourceToy.NetworkLightIntensity = 700;
                lightSourceToy.transform.position = flashbang.Position;

                NetworkServer.Spawn(lightSourceToy.gameObject);

                PrimitiveObjectToy primitive = Object.Instantiate(PrefabManager.PrimitiveObject);
                primitive.transform.localScale = new Vector3(.5f, .5f, .5f);

                primitive.NetworkPrimitiveFlags = PrimitiveFlags.Visible;
                primitive.NetworkPrimitiveType = PrimitiveType.Cube;
                primitive.NetworkMaterialColor = new Color(
                    2550f,
                    2550f,
                    2550f,
                    2550f
                );

                primitive.transform.position = flashbang.Position;

                Timing.CallDelayed(0.1f, () =>
                {
                    NetworkServer.Destroy(primitive.gameObject);
                    NetworkServer.Destroy(lightSourceToy.gameObject);
                });

                foreach (Player player in Player.List)
                {
                    int maxDistance = Config.Distance;
                    var direction = player.CameraTransform.position - flashbang.Position;
                    bool flag = Physics.Raycast(flashbang.Position, direction, out var raycastHit, maxDistance);
                    if (flag)
                    {
                        var root = raycastHit.collider.transform.root;
                        if (!Player.TryGet(root.gameObject, out Player p)) continue;
                    }

                    float distance = Vector3.Distance(player.Position, flashbang.Position);

                    if (distance > maxDistance) continue;

                    Vector3 p1 = player.CameraTransform.position + player.CameraTransform.forward * 1;

                    float g = Vector3.Distance(flashbang.Position, p1);
                    float aboba = distance * distance + 1;
                    var gy = Math.Sqrt(aboba);

                    if (gy > g)
                    {
                        // flash
                        int duration;

                        if (distance >= 35)
                        {
                            duration = 1;
                        }
                        else if (distance >= 20)
                        {
                            duration = 2;
                        }
                        else if (distance >= 10)
                        {
                            duration = 3;
                        }
                        else
                        {
                            duration = 4;
                        }

                        player.EnableEffect(EffectType.Flashed, duration: duration);
                    }
                }
            });
        }
    }

    public void PlaySound(Player player, string soundName)
    {
        AudioPlayer audioPlayer = AudioPlayer.CreateOrGet($"flashbang{player.Id}", onIntialCreation: (p) =>
        {
            p.AddSpeaker("Main", isSpatial: true, minDistance: 5f, maxDistance: 20f); 
        }, destroyWhenAllClipsPlayed:true);
        
        audioPlayer.TryGetSpeaker("Main", out var speaker);
        speaker.Position = player.Position;
        
        audioPlayer.AddClip(soundName);
    }
}