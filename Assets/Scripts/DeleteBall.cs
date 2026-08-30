using UnityEngine;

public class DeleteBall : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Killzone"))
        {
            Destroy(gameObject);
        }
    }
}
