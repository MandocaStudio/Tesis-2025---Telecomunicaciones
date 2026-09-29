using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Recorrido en primera persona por la estación (Módulo 3D).
///
/// Caminar: W A S D, flechas o stick izquierdo · Mirar: ratón o stick derecho · Correr: Shift o
/// gatillo izquierdo · Soltar el cursor: Esc o Start · Volver a caminar: clic en la escena o A.
///
/// El cursor solo se bloquea mientras se camina, y se suelta siempre al salir de la escena o al
/// perder el foco: así el botón Atrás del HUD se puede pulsar y el menú nunca aparece sin cursor.
///
/// Sustituye en Modulo3D a Assets/script/movement.cs, del compañero, que sigue sin tocar en
/// Practica Test. Aquí hacía falta lo que aquel no tiene: mirar arriba (las antenas grandes miden
/// 30 m), soltar el cursor, subir los escalones de las losas (CharacterController) y el Input
/// System que usa el resto del proyecto.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonWalker : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("m/s. Algo más que un paso normal: la parcela mide 200 × 140 m.")]
    [SerializeField] float walkSpeed = 4.5f;
    [Tooltip("m/s con Shift o el gatillo izquierdo.")]
    [SerializeField] float sprintSpeed = 9f;
    [SerializeField] float gravity = 9.81f;

    [Header("Mirada")]
    [Tooltip("La cámara, hija de este objeto, a la altura de los ojos.")]
    [SerializeField] Transform head;
    [Tooltip("Grados por píxel de ratón.")]
    [SerializeField] float mouseSensitivity = 0.12f;
    [Tooltip("Grados por segundo con el stick a fondo.")]
    [SerializeField] float stickSensitivity = 140f;
    [Tooltip("Hasta dónde se puede mirar arriba y abajo, en grados.")]
    [SerializeField] float maxPitch = 85f;

    /// <summary>Velocidad hacia abajo mientras apoya, para que isGrounded no parpadee al bajar escalones.</summary>
    const float GroundStick = 2f;

    /// <summary>True mientras se camina con el cursor bloqueado.</summary>
    public bool Walking { get; private set; }
    public event Action<bool> WalkingChanged;

    /// <summary>
    /// Lo pone el HUD: dice si una posición de pantalla cae sobre un control de la UI. Así un clic
    /// en el botón Atrás no bloquea el cursor antes de que el botón reciba el clic.
    /// </summary>
    public Func<Vector2, bool> IsPointerOverUi { get; set; }

    CharacterController body;
    InputAction move, lookMouse, lookStick, sprint, release, resume;
    InputAction[] actions;
    float pitch, verticalSpeed;

    void Awake()
    {
        body = GetComponent<CharacterController>();

        move = new InputAction("Caminar", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick");

        // Ratón y stick van por separado: el ratón da píxeles por fotograma, el stick una
        // inclinación de -1 a 1 que hay que multiplicar por el tiempo.
        lookMouse = new InputAction("Mirar (ratón)", InputActionType.Value, "<Mouse>/delta");
        lookStick = new InputAction("Mirar (stick)", InputActionType.Value, "<Gamepad>/rightStick");

        sprint = new InputAction("Correr", InputActionType.Button, "<Keyboard>/leftShift");
        sprint.AddBinding("<Gamepad>/leftTrigger");
        release = new InputAction("Soltar cursor", InputActionType.Button, "<Keyboard>/escape");
        release.AddBinding("<Gamepad>/start");
        resume = new InputAction("Volver a caminar", InputActionType.Button, "<Mouse>/leftButton");
        resume.AddBinding("<Gamepad>/buttonSouth");

        actions = new[] { move, lookMouse, lookStick, sprint, release, resume };

        if (head != null)
            pitch = Mathf.DeltaAngle(0f, head.localEulerAngles.x);
    }

    void OnEnable()
    {
        foreach (var a in actions) a.Enable();
        SetWalking(false);
    }

    void OnDisable()
    {
        foreach (var a in actions) a.Disable();
        SetWalking(false);
    }

    void OnDestroy()
    {
        foreach (var a in actions) a.Dispose();
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) SetWalking(false);
    }

    void Update()
    {
        if (Walking && release.WasPressedThisFrame())
            SetWalking(false);
        else if (!Walking && resume.WasPressedThisFrame() && !ResumeClickedOnUi())
            SetWalking(true);

        if (Walking) Look();
        Move();
    }

    bool ResumeClickedOnUi()
    {
        if (IsPointerOverUi == null || !(resume.activeControl?.device is Mouse mouse)) return false;
        return IsPointerOverUi(mouse.position.ReadValue());
    }

    void Look()
    {
        Vector2 delta = lookMouse.ReadValue<Vector2>() * mouseSensitivity
                      + lookStick.ReadValue<Vector2>() * (stickSensitivity * Time.deltaTime);
        transform.Rotate(0f, delta.x, 0f);
        pitch = Mathf.Clamp(pitch - delta.y, -maxPitch, maxPitch);
        if (head != null) head.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void Move()
    {
        Vector2 input = Walking ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        float speed = sprint.IsPressed() ? sprintSpeed : walkSpeed;
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * speed;

        // La gravedad sigue actuando con el cursor suelto: nadie se queda flotando al pausar.
        verticalSpeed = body.isGrounded ? -GroundStick : verticalSpeed - gravity * Time.deltaTime;
        velocity.y = verticalSpeed;
        body.Move(velocity * Time.deltaTime);
    }

    void SetWalking(bool walking)
    {
        Walking = walking;
        Cursor.lockState = walking ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !walking;
        WalkingChanged?.Invoke(walking);
    }
}
