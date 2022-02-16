using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : Weapon
{
    public Shotgun() {
        WeaponName = "Shotgun";
        Flair = "Double-Barrel";
        BulletHoleSize = "Very Small";
        ReserveAmmo = 20;
        MaxReserveAmmo = 20;
        CurrentMag = 2;
        MaxMag = 2;
        DrawTime = 1f;
        BulletDamage = 15;
        DamageFalloff = 20;
        ReloadSpeed = 0.8f;
        FiringRate = 0.5f;
        PointValue = 2;
        PointMultiplier = 1.3f;
        HeadshotMultiplier = 1.2f;
        Pierce = false;
        ClipFed = false;
        MaxRange = 10;
        Automatic = false;
        Projectiles = 20;
        BulletSpreadRadius = 0.04f;
        ExplosionRadius = 0;
    }
}
