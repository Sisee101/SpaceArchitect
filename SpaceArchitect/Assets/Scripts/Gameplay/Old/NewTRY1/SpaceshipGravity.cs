using UnityEngine;

public class SpaceshipGravity : MonoBehaviour
{
    public float gravitationalConstant = 6.674f;
    private Rigidbody spaceshipRb;
    private GameObject[] planets;
    private bool isLaunched = false;

    void Start()
    {
        spaceshipRb = GetComponent<Rigidbody>();
        FindAllPlanets();
    }

    void FixedUpdate()
    {
        if (isLaunched)
        {
            ApplyGravitationalForces();
        }
    }

    void FindAllPlanets()
    {
        planets = GameObject.FindGameObjectsWithTag("planet");
    }

    void ApplyGravitationalForces()
    {
        foreach (GameObject planet in planets)
        {
            if (planet != null)
            {
                Rigidbody planetRb = planet.GetComponent<Rigidbody>();
                if (planetRb != null)
                {
                    Vector3 direction = planet.transform.position - transform.position;
                    float distance = direction.magnitude;

                    // 避免距离过小导致力过大
                    if (distance < 0.1f) distance = 0.1f;

                    float forceMagnitude = gravitationalConstant * (spaceshipRb.mass * planetRb.mass) / Mathf.Pow(distance, 2);
                    Vector3 forceVector = direction.normalized * forceMagnitude;

                    spaceshipRb.AddForce(forceVector);
                }
            }
        }
    }

    public void SetLaunched(bool launched)
    {
        isLaunched = launched;
    }

    public void ResetSpaceship(Vector3 startPosition)
    {
        transform.position = startPosition;
        spaceshipRb.velocity = Vector3.zero;
        spaceshipRb.angularVelocity = Vector3.zero;
        isLaunched = false;
    }
}