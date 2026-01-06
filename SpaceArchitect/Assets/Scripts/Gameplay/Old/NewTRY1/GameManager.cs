using UnityEngine;

/// <summary>
/// 旧版游戏管理器（已弃用，保留用于兼容）
/// 新的GameManager位于 Scripts/Core/GameManager.cs
/// </summary>
public class LegacyGameManager : MonoBehaviour
{
    public GameObject spaceship;
    public Vector3 spaceshipStartPosition = new Vector3(0, 1, 0);

    private SpaceshipLauncher spaceshipLauncher;
    private SpaceshipGravity spaceshipGravity;

    void Start()
    {
        spaceshipLauncher = spaceship.GetComponent<SpaceshipLauncher>();
        spaceshipGravity = spaceship.GetComponent<SpaceshipGravity>();
    }

    void Update()
    {
        // ����Q�����÷ɴ�
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ResetSpaceship();
        }
    }

    void ResetSpaceship()
    {
        spaceshipGravity.ResetSpaceship(spaceshipStartPosition);
        spaceshipLauncher.ResetLauncher();
    }
}