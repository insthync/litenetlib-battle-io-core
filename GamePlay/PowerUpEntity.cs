﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PowerUpEntity : NetworkBehaviour
{
    // We're going to respawn this power up so I decide to keep its prefab name to spawning when character triggered
    [HideInInspector]
    public string prefabName;
    public int hp;
    public int exp;
    public WeaponData changingWeapon;
    public EffectEntity powerUpEffect;

    private bool isDead;

    private void Awake()
    {
        var collider = GetComponent<Collider>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead)
            return;
        var character = other.GetComponent<CharacterEntity>();
        if (character != null && character.Hp > 0)
        {
            isDead = true;
            EffectEntity.PlayEffect(powerUpEffect, character.effectTransform);
            if (isServer)
            {
                character.Hp += Mathf.CeilToInt(hp * character.TotalHpRecoveryRate);
                character.Exp += Mathf.CeilToInt(exp * character.TotalExpRate);
                if (changingWeapon != null)
                    character.ChangeWeapon(changingWeapon);
                // Destroy this on all clients
                NetworkServer.Destroy(gameObject);
                GameplayManager.Singleton.SpawnPowerUp(prefabName);
            }
        }
    }
}
