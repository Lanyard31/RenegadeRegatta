using UnityEngine;

public class SeagullFlyover : MonoBehaviour
{
    [Header("Bird Setup")]
    public SeagullUnit[] birds;   // Ordered: leader first, then L/R pairs
    public float formationSpacing = 2f;
    public float verticalOffsetPerRow = 0.2f;

    [Header("Motion")]
    public float speed = 12f;
    public float turnRate = 2f;

    [Header("Lifecycle")]
    public float despawnDistance = 150f;
    public Transform playerShip; // or camera rig anchor

    private bool scattered;

    void Start()
    {
        PositionFormation();
    }

    void Update()
    {
        if (scattered) return;

        // straightforward forward glide
        transform.position += transform.forward * speed * Time.deltaTime;

        // cleanup if it’s well past the player/camera
        if (playerShip)
        {
            float dist = Vector3.Distance(transform.position, playerShip.position);
            if (dist > despawnDistance)
                Destroy(gameObject);
        }
    }

    public void Scatter()
    {
        if (scattered) return;
        scattered = true;

        foreach (var b in birds)
            b.Scatter();
    }

    void PositionFormation()
    {
        if (birds == null || birds.Length == 0) return;

        birds[0].transform.localPosition = Vector3.zero;

        int index = 1;
        int row = 1;

        while (index < birds.Length)
        {
            float xOffset = row * formationSpacing;
            float zOffset = -row * formationSpacing;
            float yOffset = row * verticalOffsetPerRow;

            // left bird
            if (index < birds.Length)
            {
                birds[index].transform.localPosition =
                    new Vector3(-xOffset, yOffset, zOffset);
                index++;
            }

            // right bird
            if (index < birds.Length)
            {
                birds[index].transform.localPosition =
                    new Vector3(xOffset, yOffset, zOffset);
                index++;
            }

            row++;
        }
    }
}
