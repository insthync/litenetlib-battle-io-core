﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LiteNetLibManager;

[RequireComponent(typeof(CharacterEntity))]
public class BRCharacterEntityExtra : LiteNetLibBehaviour
{
    public static float BotSpawnDuration = 0f;
    [SyncField]
    public bool isSpawned;
    public bool isGroundOnce { get; private set; }
    public Transform CacheTransform { get; private set; }
    public CharacterEntity CacheCharacterEntity { get; private set; }
    public CharacterMovement CacheCharacterMovement { get; private set; }
    private float botRandomSpawn;
    private bool botSpawnCalled;
    private bool botDeadRemoveCalled;
    private float lastCircleCheckTime;

    private void Awake()
    {
        CacheTransform = transform;
        CacheCharacterEntity = GetComponent<CharacterEntity>();
        CacheCharacterEntity.enabled = false;
        CacheCharacterEntity.IsHidding = true;
        CacheCharacterMovement = GetComponent<CharacterMovement>();
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager != null)
        {
            if (IsServer && brGameManager.currentState != BRState.WaitingForPlayers)
            {
                CacheCharacterEntity.NetworkDestroy();
                return;
            }
        }
        botRandomSpawn = BotSpawnDuration = BotSpawnDuration + Random.Range(0.1f, 1f);
    }

    public override void OnSetOwnerClient(bool isOwnerClient)
    {
        base.OnSetOwnerClient(isOwnerClient);
        if (IsOwnerClient)
        {
            var brGameManager = GameplayManager.Singleton as BRGameplayManager;
            if (brGameManager != null && brGameManager.currentState != BRState.WaitingForPlayers)
                GameNetworkManager.Singleton.StopHost();
        }
    }

    private void Start()
    {
        CacheCharacterEntity.onDead += OnDead;
    }

    private void OnDestroy()
    {
        CacheCharacterEntity.onDead -= OnDead;
    }

    private void Update()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = CacheCharacterEntity as BotEntity;
        // Monster entity does not have to move following the air plane
        if (IsServer && CacheCharacterEntity is MonsterEntity)
        {
            if (!CacheCharacterMovement.enabled)
                CacheCharacterMovement.enabled = true;
            if (!CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = true;
            CacheCharacterEntity.IsHidding = false;
            if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
                isSpawned = true;
            return;
        }

        if (IsServer)
        {
            if (brGameManager.currentState != BRState.WaitingForPlayers && Time.realtimeSinceStartup - lastCircleCheckTime >= 1f)
            {
                var currentPosition = CacheTransform.position;
                currentPosition.y = 0;

                var centerPosition = brGameManager.currentCenterPosition;
                centerPosition.y = 0;
                var distance = Vector3.Distance(currentPosition, centerPosition);
                var currentRadius = brGameManager.currentRadius;
                if (distance > currentRadius)
                    CacheCharacterEntity.Hp -= Mathf.CeilToInt(brGameManager.CurrentCircleHpRateDps * CacheCharacterEntity.TotalHp);
                lastCircleCheckTime = Time.realtimeSinceStartup;
                if (botEntity != null)
                {
                    botEntity.isFixRandomMoveAroundPoint = currentRadius > 0 && distance > currentRadius;
                    botEntity.fixRandomMoveAroundPoint = centerPosition;
                    botEntity.fixRandomMoveAroundDistance = currentRadius;
                }
            }
        }

        if (brGameManager.currentState == BRState.WaitingForPlayers || isSpawned)
        {
            if (IsServer && !botDeadRemoveCalled && CacheCharacterEntity is BotEntity && CacheCharacterEntity.IsDead)
            {
                botDeadRemoveCalled = true;
                StartCoroutine(BotDeadRemoveRoutine());
            }
            if (!CacheCharacterMovement.enabled)
                CacheCharacterMovement.enabled = true;
            if (!CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = true;
            CacheCharacterEntity.IsHidding = false;
        }

        switch (brGameManager.spawnType)
        {
            case BRSpawnType.BattleRoyale:
                UpdateSpawnBattleRoyale();
                break;
            case BRSpawnType.Random:
                UpdateSpawnRandom();
                break;
        }

        if (isSpawned && !isGroundOnce && CacheCharacterMovement.IsGrounded)
            isGroundOnce = true;
    }

    private void UpdateSpawnBattleRoyale()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;
        var botEntity = CacheCharacterEntity as BotEntity;
        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned)
        {
            if (IsServer && !botSpawnCalled && botEntity != null && brGameManager.CanSpawnCharacter(CacheCharacterEntity))
            {
                botSpawnCalled = true;
                StartCoroutine(BotSpawnRoutine());
            }
            // Hide character and disable physics while in airplane
            if (CacheCharacterMovement.enabled)
                CacheCharacterMovement.enabled = false;
            if (CacheCharacterEntity.enabled)
                CacheCharacterEntity.enabled = false;
            CacheCharacterEntity.IsHidding = true;
            // Move position / rotation follow the airplane
            if (IsServer || IsOwnerClient)
            {
                CacheTransform.position = brGameManager.GetSpawnerPosition();
                CacheTransform.rotation = brGameManager.GetSpawnerRotation();
            }
        }
    }

    private void UpdateSpawnRandom()
    {
        var brGameManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameManager == null)
            return;

        if (brGameManager.currentState != BRState.WaitingForPlayers && !isSpawned && IsServer)
        {
            var position = CacheCharacterEntity.GetSpawnPosition();
            CacheCharacterEntity.CacheTransform.position = position;
            CacheCharacterEntity.TargetSpawn(CacheCharacterEntity.ConnectionId, position);
            isSpawned = true;
        }
    }

    IEnumerator BotSpawnRoutine()
    {
        yield return new WaitForSeconds(botRandomSpawn);
        ServerCharacterSpawn();
    }

    IEnumerator BotDeadRemoveRoutine()
    {
        yield return new WaitForSeconds(5f);
        NetworkDestroy();
    }

    private void OnDead()
    {
        if (!IsServer)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (brGameplayManager != null)
            RpcRankResult(BaseNetworkGameManager.Singleton.CountAliveCharacters() + 1);
    }

    IEnumerator ShowRankResultRoutine(int rank)
    {
        yield return new WaitForSeconds(3f);
        var ui = UIBRGameplay.Singleton;
        if (ui != null)
            ui.ShowRankResult(rank);
    }

    public void ServerCharacterSpawn()
    {
        if (!IsServer)
            return;
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null)
        {
            isSpawned = true;
            RpcCharacterSpawned(brGameplayManager.SpawnCharacter(CacheCharacterEntity) + new Vector3(Random.Range(-2.5f, 2.5f), 0, Random.Range(-2.5f, 2.5f)));
        }
    }

    public void CmdCharacterSpawn()
    {
        CallNetFunction(_CmdCharacterSpawn, FunctionReceivers.Server);
    }

    [NetFunction]
    public void _CmdCharacterSpawn()
    {
        var brGameplayManager = GameplayManager.Singleton as BRGameplayManager;
        if (!isSpawned && brGameplayManager != null && brGameplayManager.CanSpawnCharacter(CacheCharacterEntity))
            ServerCharacterSpawn();
    }

    public void RpcCharacterSpawned(Vector3 spawnPosition)
    {
        CallNetFunction(_RpcCharacterSpawned, FunctionReceivers.All, spawnPosition);
    }

    [NetFunction]
    public void _RpcCharacterSpawned(Vector3 spawnPosition)
    {
        CacheCharacterEntity.CacheTransform.position = spawnPosition;
        CacheCharacterMovement.enabled = true;
    }

    public void RpcRankResult(int rank)
    {
        CallNetFunction(_RpcRankResult, FunctionReceivers.All, rank);
    }

    [NetFunction]
    public void _RpcRankResult(int rank)
    {
        if (IsOwnerClient)
        {
            if (GameNetworkManager.Singleton.gameRule != null &&
                GameNetworkManager.Singleton.gameRule is BattleRoyaleNetworkGameRule)
                (GameNetworkManager.Singleton.gameRule as BattleRoyaleNetworkGameRule).SetRewards(rank);
            StartCoroutine(ShowRankResultRoutine(rank));
        }
    }
}
