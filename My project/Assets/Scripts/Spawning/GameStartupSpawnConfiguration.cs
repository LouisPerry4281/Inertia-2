using UnityEngine;

[CreateAssetMenu(fileName = "GameStartupSpawnConfiguration", menuName = "Inertia/Game Startup Spawn Configuration")]
public class GameStartupSpawnConfiguration : ScriptableObject
{
    [Header("Prefabs")]
    public PlayerManager playerPrefab;
    public EnemyCombatAI enemyPrefab;
    public PlayerCamera playerCameraPrefab;
    public PlayerInputManager playerInputManagerPrefab;

    [Header("Spawn Points")]
    public Vector3 playerSpawnPosition = Vector3.zero;
    public Vector3 playerSpawnEulerAngles = Vector3.zero;
    public Vector3 enemySpawnPosition = new Vector3(0f, 0f, 5f);
    public Vector3 enemySpawnEulerAngles = new Vector3(0f, 180f, 0f);
}
