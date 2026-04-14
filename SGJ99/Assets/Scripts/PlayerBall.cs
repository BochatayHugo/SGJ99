using UnityEngine;

public class PlayerBall : MonoBehaviour
{
    public float speed = 10f;
    [SerializeField] private Rigidbody rigidBody;

    void FixedUpdate()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Input.GetKey(KeyCode.S)) moveX = 1f;
        if (Input.GetKey(KeyCode.W)) moveX = -1f;
        if (Input.GetKey(KeyCode.D)) moveZ = 1f;
        if (Input.GetKey(KeyCode.A)) moveZ = -1f;

        Vector3 movement = new Vector3(moveX, 0f, moveZ).normalized;
        rigidBody.AddForce(movement * speed);
    }
}