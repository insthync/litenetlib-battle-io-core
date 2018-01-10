﻿using UnityEngine;
using UnityEngine.Networking;

public class IONetworkGameRule : BaseNetworkGameRule
{
    protected override BaseNetworkGameCharacter NewBot()
    {
        var gameInstance = GameInstance.Singleton;
        var botList = gameInstance.bots;
        var bot = botList[Random.Range(0, botList.Length)];
        var botEntity = Instantiate(gameInstance.botPrefab);
        botEntity.playerName = bot.name;
        botEntity.selectHead = bot.GetSelectHead();
        botEntity.selectCharacter = bot.GetSelectCharacter();
        botEntity.selectWeapon = bot.GetSelectWeapon();
        return botEntity;
    }

    protected override void EndMatch()
    {
    }

    public override bool CanCharacterRespawn(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        var gameplayManager = GameplayManager.Singleton;
        var targetCharacter = character as CharacterEntity;
        return Time.unscaledTime - targetCharacter.deathTime >= gameplayManager.respawnDuration;
    }

    public override bool RespawnCharacter(BaseNetworkGameCharacter character, params object[] extraParams)
    {
        var isWatchedAds = false;
        if (extraParams.Length > 0 && extraParams[0] is bool)
            isWatchedAds = (bool)extraParams[0];

        var targetCharacter = character as CharacterEntity;
        var gameplayManager = GameplayManager.Singleton;
        // For IO Modes, character stats will be reset when dead
        if (!isWatchedAds || targetCharacter.watchAdsCount >= gameplayManager.watchAdsRespawnAvailable)
        {
            targetCharacter.score = 0;
            targetCharacter.Exp = 0;
            targetCharacter.level = 1;
            targetCharacter.statPoint = 0;
            targetCharacter.killCount = 0;
            targetCharacter.assistCount = 0;
            targetCharacter.watchAdsCount = 0;
            targetCharacter.addStats = new CharacterStats();
        }
        else
            ++targetCharacter.watchAdsCount;

        return true;
    }
}
