﻿using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameInstance : MonoBehaviour
{
    public static GameInstance Singleton { get; private set; }
    public CharacterEntity characterPrefab;
    public BotEntity botPrefab;
    public CharacterData[] characters;
    public HeadData[] heads;
    public WeaponData[] weapons;
    public BotData[] bots;
    [Tooltip("Physic layer for characters to avoid it collision")]
    public int characterLayer = 8;
    public string watchAdsRespawnPlacement = "respawnPlacement";
    // An available list, list of item that already unlocked
    public static readonly List<HeadData> AvailableHeads = new List<HeadData>();
    public static readonly List<CharacterData> AvailableCharacters = new List<CharacterData>();
    public static readonly List<WeaponData> AvailableWeapons = new List<WeaponData>();
    // All item list
    public static readonly Dictionary<string, HeadData> Heads = new Dictionary<string, HeadData>();
    public static readonly Dictionary<string, CharacterData> Characters = new Dictionary<string, CharacterData>();
    public static readonly Dictionary<string, WeaponData> Weapons = new Dictionary<string, WeaponData>();
    private void Awake()
    {
        if (Singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        Singleton = this;
        DontDestroyOnLoad(gameObject);
        Physics.IgnoreLayerCollision(characterLayer, characterLayer, true);

        if (!ClientScene.prefabs.ContainsValue(characterPrefab.gameObject))
            ClientScene.RegisterPrefab(characterPrefab.gameObject);

        if (!ClientScene.prefabs.ContainsValue(botPrefab.gameObject))
            ClientScene.RegisterPrefab(botPrefab.gameObject);

        Heads.Clear();
        foreach (var head in heads)
        {
            Heads.Add(head.GetId(), head);
        }

        Characters.Clear();
        foreach (var characterModel in characters)
        {
            Characters.Add(characterModel.GetId(), characterModel);
        }

        Weapons.Clear();
        foreach (var weapon in weapons)
        {
            weapon.SetupAnimations();
            Weapons.Add(weapon.GetId(), weapon);
            var damagePrefab = weapon.damagePrefab;
            if (damagePrefab != null && !ClientScene.prefabs.ContainsValue(damagePrefab.gameObject))
                ClientScene.RegisterPrefab(damagePrefab.gameObject);
        }

        UpdateAvailableItems();
    }

    private void Start()
    {
        // If game running in batch mode, run as server
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Application.targetFrameRate = 30;
            Debug.Log("Running as server in batch mode");
            GameNetworkManager.Singleton.StartDedicateServer();
        }
    }

    public void UpdateAvailableItems()
    {
        AvailableHeads.Clear();
        foreach (var helmet in heads)
        {
            if (helmet != null && helmet.IsUnlock())
                AvailableHeads.Add(helmet);
        }

        AvailableCharacters.Clear();
        foreach (var character in characters)
        {
            if (character != null && character.IsUnlock())
                AvailableCharacters.Add(character);
        }

        AvailableWeapons.Clear();
        foreach (var weapon in weapons)
        {
            if (weapon != null && weapon.IsUnlock())
                AvailableWeapons.Add(weapon);
        }
    }

    public static HeadData GetHead(string key)
    {
        if (Heads.Count == 0)
            return null;
        HeadData result;
        Heads.TryGetValue(key, out result);
        return result;
    }

    public static CharacterData GetCharacter(string key)
    {
        if (Characters.Count == 0)
            return null;
        CharacterData result;
        Characters.TryGetValue(key, out result);
        return result;
    }

    public static WeaponData GetWeapon(string key)
    {
        if (Weapons.Count == 0)
            return null;
        WeaponData result;
        Weapons.TryGetValue(key, out result);
        return result;
    }

    public static HeadData GetAvailableHead(int index)
    {
        if (AvailableHeads.Count == 0)
            return null;
        if (index <= 0 || index >= AvailableHeads.Count)
            index = 0;
        return AvailableHeads[index];
    }

    public static CharacterData GetAvailableCharacter(int index)
    {
        if (AvailableCharacters.Count == 0)
            return null;
        if (index <= 0 || index >= AvailableCharacters.Count)
            index = 0;
        return AvailableCharacters[index];
    }

    public static WeaponData GetAvailableWeapon(int index)
    {
        if (AvailableWeapons.Count == 0)
            return null;
        if (index <= 0 || index >= AvailableWeapons.Count)
            index = 0;
        return AvailableWeapons[index];
    }
}
