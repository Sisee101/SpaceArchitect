using UnityEngine;

public class GameManager : MonoBehaviour
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