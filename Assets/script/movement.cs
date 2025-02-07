using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] float speed = 5f;
    [SerializeField] float rotationSpeed = 200f;
    [SerializeField] Rigidbody rbPlayer;

    void Start()
    {
        rbPlayer = GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked; // Bloquea el cursor al centro de la pantalla
    }

    void FixedUpdate()
    {
        // Movimiento con teclado o joystick izquierdo
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.forward * moveZ + transform.right * moveX;
        rbPlayer.linearVelocity = new Vector3(move.x * speed, rbPlayer.linearVelocity.y, move.z * speed);
    }

    void Update()
    {
        if (Input.GetAxis("Mouse X") >= 0.5 || Input.GetAxis("RightStickX") >= 0.5 || Input.GetAxis("Mouse X") <= -0.5 || Input.GetAxis("RightStickX") <= -0.5)
        {
            // Rotación con mouse y joystick derecho
            float mouseX = Input.GetAxis("Mouse X") + Input.GetAxis("RightStickX");
            float rotation = mouseX * rotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.up * rotation);
        }

    }
}
