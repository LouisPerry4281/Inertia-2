using UnityEngine;

public static class GameStartupSpawner
{
    private const string ConfigurationResourcePath = "GameStartupSpawnConfiguration";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SpawnStartupPrefabs()
    {
        GameStartupSpawnConfiguration configuration = Resources.Load<GameStartupSpawnConfiguration>(ConfigurationResourcePath);
        if (configuration == null)
        {
            Debug.LogWarning($"No {nameof(GameStartupSpawnConfiguration)} found at Resources/{ConfigurationResourcePath}.");
            return;
        }

        PlayerManager player = Object.FindAnyObjectByType<PlayerManager>();
        if (player == null && configuration.playerPrefab != null)
        {
            player = Object.Instantiate(
                configuration.playerPrefab,
                configuration.playerSpawnPosition,
                Quaternion.Euler(configuration.playerSpawnEulerAngles));
            player.name = configuration.playerPrefab.name;
        }

        if (Object.FindAnyObjectByType<PlayerCamera>() == null && configuration.playerCameraPrefab != null)
        {
            PlayerCamera playerCamera = Object.Instantiate(configuration.playerCameraPrefab);
            playerCamera.name = configuration.playerCameraPrefab.name;
            playerCamera.player = player;
        }

        if (Object.FindAnyObjectByType<PlayerInputManager>() == null && configuration.playerInputManagerPrefab != null)
        {
            PlayerInputManager inputManager = Object.Instantiate(configuration.playerInputManagerPrefab);
            inputManager.name = configuration.playerInputManagerPrefab.name;
            inputManager.player = player;
        }

        EnemyCombatAI enemy = Object.FindAnyObjectByType<EnemyCombatAI>();
        if (enemy == null && configuration.enemyPrefab != null)
        {
            enemy = Object.Instantiate(
                configuration.enemyPrefab,
                configuration.enemySpawnPosition,
                Quaternion.Euler(configuration.enemySpawnEulerAngles));
            enemy.name = configuration.enemyPrefab.name;
        }
    }
}
