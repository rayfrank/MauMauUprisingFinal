using UnityEngine;

public class Arrow : MonoBehaviour
{
    [SerializeField] private float damage = 100f; // Set the damage value

    // Method to get the damage
    public float GetDamage()
    {
        return damage;
    }

    // You can add logic to destroy the arrow after impact if necessary
    private void OnCollisionEnter(Collision collision)
    {
        // You can destroy the arrow after it hits something
        Destroy(gameObject);
    }
}
