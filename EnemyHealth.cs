using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float health = 100f; // Enemy's starting health

    // Method to handle damage
    public void TakeDamage(float damage)
    {
        health -= damage; // Reduce health by the damage amount

        if (health <= 0f)
        {
            Die();
        }
    }

    // Method to kill the enemy
    private void Die()
    {
        // You can add death animations or any other logic here.
        Debug.Log("Enemy died!");
        
    }

    // Collision detection
    private void OnCollisionEnter(Collision collision)
    {
        // Check if the collision is with an arrow
        if (collision.gameObject.CompareTag("Arrow"))
        {
            this.GetComponent<Animator>().SetBool("death", true);

        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // Check if the collision is with an arrow
        if (other.gameObject.CompareTag("Arrow"))
        {
            this.GetComponent<Animator>().SetBool("death", true);

        }
    }
}
