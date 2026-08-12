using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float crouchSpeed = 1.5f;
    public float gravity = -20f;
    public float jumpHeight = 1.2f;

    [Header("Air Control")]
    public float airControl = 0.3f;

    [Header("Airing Detection")]
    [Tooltip("Minimum falling velocity to trigger airing state")]
    public float airingThreshold = -2f;
    [Tooltip("Delay before considering the player is airing (prevents quick ground transitions)")]
    public float airingDelay = 0.1f;

    [Header("Crouch Settings")]
    public float crouchHeightMultiplier = 0.5f;
    public float crouchTransitionSpeed = 8f;

    [Header("Ground Check Settings")]
    public LayerMask groundLayers = 1;
    public float grndCheckDistance = 0.1f;
    public float grndCheckRadius = 0.28f;

    [Header("Slope Settings")]
    public float slideForce = 8f;
    public float slideControl = 0.2f;
    public float minSlideAngle = 30f;
    [Tooltip("Maximum angle player can walk up normally")]
    public float maxWalkableAngle = 45f;
    [Tooltip("Prevents jump/crouch spam on steep slopes")]
    public float slopeJumpPenalty = 0.5f;
    [Tooltip("Time before allowing another jump on steep slopes")]
    public float slopeJumpCooldown = 1f;

    [Header("Camera Settings")]
    public Transform camTransform;
    public float camSpeed = 8f;
    public Vector3 crouchCameraOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Input Actions")]
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference runAction;
    public InputActionReference crouchAction;
    [Tooltip("Action distincte pour sortir d'un focus actif (chaise/écran). Peut être bindée sur Left Shift, même touche que Run — ce sont deux actions séparées, aucun conflit.")]
    public InputActionReference backAction;

    [Header("Audio Sources")]
    public AudioSource audioSource;
    public AudioEvent jumpAudio;

    [Header("State Variables")]
    public bool isGrounded;
    public bool isCrouching = false;
    public bool isSliding = false;
    public bool isAiring = false; // New airing state

    // Input lock state — reusable anywhere a system needs to freeze player input
    // (screen focus, cutscenes, menus, other interactions, etc.). Compteur plutôt
    // qu'un simple bool : permet des gels imbriqués (ex: assis ET en focus écran
    // en même temps) sans qu'en sortir du niveau intérieur ne réactive tout.
    private int inputLockCount = 0;
    public bool InputEnabled => inputLockCount <= 0;

    // Private variables
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool wasGroundedWhenCrouchStarted = false;

    // Airing detection variables
    private float timeStartedFalling = 0f;
    private bool wasFalling = false;

    // Slope sliding variables
    private Vector3 groundNormal = Vector3.up;
    private float groundAngle = 0f;
    private RaycastHit groundHit;

    // Slope climbing restrictions
    private float lastSlopeJumpTime = 0f;
    private bool wasOnSteepSlope = false;
    private float consecutiveSlopeJumps = 0f;

    // Collider variables
    private float originalHeight;
    private Vector3 originalCenter;
    private float targetHeight;
    private Vector3 targetCenter;
    private float currentHeight;
    private Vector3 currentCenter;

    // Speed variables
    [HideInInspector]
    public float currentHorizontalSpeed;

    // Camera variables
    private Vector3 originalCameraLocalPos;
    private Transform cameraTransform;

    // Input variables
    private Vector2 moveInput;
    private bool jumpInput;
    private bool runInput;
    private bool crouchInput;

    void Start()
    {
        InitializeComponents();
        InitializeColliderDimensions();
        InitializeCameraPosition();
        EnableInputActions();
    }

    void OnDestroy()
    {
        DisableInputActions();
    }

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this); // Prevent duplicates
        } else {
            Instance = this;
        }
    }
    void Update()
    {
        if (MenuManager.IsPaused) return;

        HandleFocusBackInput();
        HandleInput();
        HandleGroundCheck();
        HandleAiringDetection(); // New airing detection
        HandleCrouchInput();
        HandleMovement();
        HandleJump();
        ApplyGravity();
        UpdateColliderDimensions();
        UpdateCameraPosition();
        UpdateSpeedTracking();
    }

    #region Initialization Methods

    void InitializeComponents()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        
        if (cameraTransform == null)
        {
            Debug.LogWarning("Main camera not found. Player controller may not work correctly.");
        }
    }

    void InitializeColliderDimensions()
    {
        originalHeight = controller.height;
        originalCenter = controller.center;
        currentHeight = originalHeight;
        currentCenter = originalCenter;
        targetHeight = originalHeight;
        targetCenter = originalCenter;
    }

    void InitializeCameraPosition()
    {
        if (camTransform != null)
        {
            originalCameraLocalPos = camTransform.localPosition;
        }
    }

    void EnableInputActions()
    {
        if (moveAction != null)
        {
            moveAction.action.Enable();
        }
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
        }
        if (runAction != null)
        {
            runAction.action.Enable();
        }
        if (crouchAction != null)
        {
            crouchAction.action.Enable();
        }
        if (backAction != null)
        {
            backAction.action.Enable();
        }
    }

    void DisableInputActions()
    {
        if (moveAction != null)
        {
            moveAction.action.Disable();
        }
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
        }
        if (runAction != null)
        {
            runAction.action.Disable();
        }
        if (crouchAction != null)
        {
            crouchAction.action.Disable();
        }
        if (backAction != null)
        {
            backAction.action.Disable();
        }
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Sort d'un focus actif (chaise/écran) sur l'action Back (Left Shift).
    /// Volontairement séparé de HandleInput() : contrairement au reste des inputs
    /// de mouvement, celui-ci doit continuer à fonctionner même quand InputEnabled
    /// est à false (c'est justement dans cet état qu'on en a besoin).
    /// </summary>
    void HandleFocusBackInput()
    {
        if (backAction == null) return;
        if (!backAction.action.WasPressedThisFrame()) return;

        if (FocusStack.IsActive)
            FocusStack.TryHandleCancel();
    }

    void HandleInput()
    {
        // Input frozen (e.g. during a screen focus, cutscene, menu, etc.)
        // We keep everything else in Update() running (gravity, ground check,
        // collider transitions, camera offset) so the player doesn't glitch,
        // we just stop feeding new movement input.
        if (!InputEnabled)
        {
            moveInput = Vector2.zero;
            jumpInput = false;
            runInput = false;
            crouchInput = false;
            return;
        }

        // Get movement input
        moveInput = moveAction?.action.ReadValue<Vector2>() ?? Vector2.zero;
        
        // Get jump input (triggered this frame)
        jumpInput = jumpAction?.action.WasPressedThisFrame() ?? false;
        
        // Get run input (held down)
        runInput = runAction?.action.IsPressed() ?? false;
        
        // Get crouch input (held down)
        crouchInput = crouchAction?.action.IsPressed() ?? false;
    }

    /// <summary>
    /// Gèle/dégèle le mouvement du joueur. Réutilisable par n'importe quel système
    /// (chaise, focus écran, cutscène, menu...). Compteur imbriqué : si deux
    /// systèmes gèlent en même temps (ex: assis ET en focus écran), il faut que
    /// LES DEUX appellent SetInputEnabled(true) avant que le joueur redevienne
    /// réellement mobile — sortir d'un seul niveau ne réactive pas tout.
    ///
    /// Désactive aussi directement move/jump/run/crouch Action (pas juste une
    /// copie locale) : n'importe quel AUTRE script qui lirait ces mêmes actions
    /// directement (ex: un système de bruits de pas basé sur l'input brut plutôt
    /// que sur le déplacement réel) verra donc lui aussi "aucun input".
    /// </summary>
    public void SetInputEnabled(bool value)
    {
        bool wasEnabled = InputEnabled;

        inputLockCount = value
            ? Mathf.Max(0, inputLockCount - 1)
            : inputLockCount + 1;

        bool isEnabledNow = InputEnabled;
        if (wasEnabled == isEnabledNow) return; // aucun changement d'état réel (encore un niveau de gel actif, etc.)

        if (!isEnabledNow)
        {
            moveInput = Vector2.zero;
            jumpInput = false;
            runInput = false;
            crouchInput = false;

            if (moveAction != null) moveAction.action.Disable();
            if (jumpAction != null) jumpAction.action.Disable();
            if (runAction != null) runAction.action.Disable();
            if (crouchAction != null) crouchAction.action.Disable();
        }
        else
        {
            if (moveAction != null) moveAction.action.Enable();
            if (jumpAction != null) jumpAction.action.Enable();
            if (runAction != null) runAction.action.Enable();
            if (crouchAction != null) crouchAction.action.Enable();
        }
    }

    #endregion

    #region Airing Detection

    void HandleAiringDetection()
    {
        bool isFalling = playerVelocity.y <= airingThreshold && !isGrounded;
        
        // Track when we started falling
        if (isFalling && !wasFalling)
        {
            timeStartedFalling = Time.time;
        }
        
        // Update airing state
        bool shouldBeAiring = false;
        
        if (isFalling)
        {
            // Only consider airing if we've been falling for the required delay
            float timeFalling = Time.time - timeStartedFalling;
            shouldBeAiring = timeFalling >= airingDelay;
        }
        
        // Update airing state and animator
        if (isAiring != shouldBeAiring)
        {
            isAiring = shouldBeAiring;
        }
        
        wasFalling = isFalling;
    }

    #endregion

    #region Ground Detection

    void HandleGroundCheck()
    {
        Vector3 spherePosition = transform.position + controller.center - Vector3.up * (controller.height * 0.5f - grndCheckRadius);
        isGrounded = Physics.CheckSphere(spherePosition, grndCheckRadius, groundLayers) ||
                    Physics.SphereCast(spherePosition, grndCheckRadius, Vector3.down,
                                     out groundHit, grndCheckDistance, groundLayers);

        if (isGrounded)
        {
            UpdateGroundProperties();
            UpdateSlidingState();
            UpdateSteepSlopeTracking();
        }
        else
        {
            ResetGroundProperties();
        }
    }

    void UpdateGroundProperties()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, controller.height * 0.5f + grndCheckDistance, groundLayers))
        {
            groundNormal = hit.normal;
            groundAngle = Vector3.Angle(Vector3.up, groundNormal);
        }
        else
        {
            groundNormal = Vector3.up;
            groundAngle = 0f;
        }
    }

    void UpdateSlidingState()
    {
        isSliding = groundAngle > minSlideAngle;
    }

    void UpdateSteepSlopeTracking()
    {
        bool isOnSteepSlope = groundAngle > maxWalkableAngle;
        if (!wasOnSteepSlope && isOnSteepSlope)
        {
            // Just entered steep slope - reset consecutive jumps
            consecutiveSlopeJumps = 0f;
        }
        wasOnSteepSlope = isOnSteepSlope;
    }

    void ResetGroundProperties()
    {
        groundNormal = Vector3.up;
        groundAngle = 0f;
        isSliding = false;
        wasOnSteepSlope = false;
    }

    #endregion

    #region Crouching System

    void HandleCrouchInput()
    {
        // Prevent crouch spam on steep slopes
        if (crouchInput && !isCrouching)
        {
            if (CanCrouchOnCurrentSlope())
            {
                StartCrouching();
            }
        }
        else if (!crouchInput && isCrouching)
        {
            StopCrouching();
        }
    }

    bool CanCrouchOnCurrentSlope()
    {
        if (groundAngle > maxWalkableAngle)
        {
            float crouchEffectiveness = Mathf.Clamp01(1f - (groundAngle - maxWalkableAngle) / 30f);
            return crouchEffectiveness >= 0.3f;
        }
        return true;
    }

    void StartCrouching()
    {
        isCrouching = true;
        wasGroundedWhenCrouchStarted = isGrounded;

        float crouchHeight = originalHeight * crouchHeightMultiplier;
        float heightDiff = originalHeight - crouchHeight;
        targetHeight = crouchHeight;

        if (wasGroundedWhenCrouchStarted)
        {
            // Shrink downward
            targetCenter = originalCenter - Vector3.up * (heightDiff * 0.5f);
        }
        else
        {
            // Shrink upward
            targetCenter = originalCenter + Vector3.up * (heightDiff * 0.25f);
        }
    }

    void StopCrouching()
    {
        isCrouching = false;
        targetHeight = originalHeight;
        targetCenter = originalCenter;
    }

    void UpdateColliderDimensions()
    {
        currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        currentCenter = Vector3.MoveTowards(currentCenter, targetCenter, crouchTransitionSpeed * Time.deltaTime);

        controller.height = currentHeight;
        controller.center = currentCenter;
    }

    #endregion

    #region Camera System

    void UpdateCameraPosition()
    {
        if (camTransform == null) return;

        Vector3 centerOffset = currentCenter - originalCenter;
        Vector3 crouchOffset = isCrouching ? crouchCameraOffset : Vector3.zero;
        Vector3 targetCameraPos = originalCameraLocalPos + centerOffset + crouchOffset;

        camTransform.localPosition = Vector3.MoveTowards(
            camTransform.localPosition,
            targetCameraPos,
            camSpeed * Time.deltaTime
        );
    }

    #endregion

    #region Movement System

    void HandleMovement()
    {
        Vector3 move = CalculateMovementDirection();
        Vector3 inputDir = move.normalized;
        float speed = GetCurrentMovementSpeed();

        if (isGrounded)
        {
            HandleGroundMovement(inputDir, speed);
        }
        else
        {
            HandleAirMovement(inputDir, speed);
        }

        // Ne rien appliquer si le controller est désactivé (ex: assis sur une
        // chaise) — sinon Unity lève "CharacterController.Move called on
        // inactive controller". playerVelocity continue d'être calculée plus
        // haut sans problème, elle est juste ignorée tant que le controller
        // ne peut pas bouger.
        if (controller.enabled)
            controller.Move(playerVelocity * Time.deltaTime);
    }

    Vector3 CalculateMovementDirection()
    {
        if (cameraTransform == null) return Vector3.zero;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = cameraTransform.forward * move.z + cameraTransform.right * move.x;
        move.y = 0f;
        return move;
    }

    float GetCurrentMovementSpeed()
    {
        // Crouch always overrides other speeds
        if (isCrouching)
        {
            return crouchSpeed;
        }
        // Only allow running when grounded and not crouching
        else if (isGrounded && runInput)
        {
            return runSpeed;
        }
        // Default to walk speed
        return walkSpeed;
    }

    void HandleGroundMovement(Vector3 inputDir, float speed)
    {
        if (isSliding)
        {
            HandleSlopeSliding(inputDir, speed);
        }
        else if (groundAngle <= maxWalkableAngle)
        {
            HandleNormalGroundMovement(inputDir, speed);
        }
        else
        {
            HandleSteepSlopeMovement(inputDir, speed);
        }
    }

    void HandleNormalGroundMovement(Vector3 inputDir, float speed)
    {
        if (inputDir.magnitude > 0.1f)
        {
            Vector3 slopeAlignedDir = GetMovementDirectionProjectedOnPlane(inputDir, groundNormal, Vector3.up);
            playerVelocity.x = slopeAlignedDir.x * speed;
            playerVelocity.z = slopeAlignedDir.z * speed;
        }
        else
        {
            // No input, gradually stop
            playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 6f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 6f);
        }
    }

    void HandleSteepSlopeMovement(Vector3 inputDir, float speed)
    {
        if (inputDir.magnitude > 0.1f)
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(inputDir, groundNormal);
            float uphillDot = Vector3.Dot(slopeDir.normalized, -Vector3.ProjectOnPlane(Vector3.up, groundNormal).normalized);
            
            if (uphillDot > 0.3f)
            {
                // Prevent uphill movement on steep slopes
                playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 10f);
                playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 10f);
            }
            else
            {
                // Allow limited movement parallel to or down the slope
                Vector3 allowedDir = slopeDir.normalized;
                float speedMultiplier = Mathf.Clamp01(1f - uphillDot * 2f);
                playerVelocity.x = allowedDir.x * speed * speedMultiplier * 0.3f;
                playerVelocity.z = allowedDir.z * speed * speedMultiplier * 0.3f;
            }
        }
        else
        {
            // No input, stop movement
            playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 5f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 5f);
        }
    }

    void HandleAirMovement(Vector3 inputDir, float speed)
    {
        Vector3 desiredVel = inputDir * speed;
        Vector3 curHoriz = new Vector3(playerVelocity.x, 0, playerVelocity.z);
        Vector3 newHoriz = Vector3.Lerp(curHoriz, desiredVel, airControl * Time.deltaTime * 10f);
        playerVelocity.x = newHoriz.x;
        playerVelocity.z = newHoriz.z;
    }

    void HandleSlopeSliding(Vector3 inputDir, float speed)
    {
        // Calculate the slide direction (down the slope)
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
        
        // Apply sliding force
        Vector3 slideVelocity = slideDirection * slideForce;
        
        // Apply limited player control while sliding
        Vector3 controlVelocity = inputDir * speed * slideControl;
        
        // Combine slide velocity with control input
        Vector3 combinedVelocity = slideVelocity + controlVelocity;
        
        // Smooth transition to sliding velocity
        float slideBlend = Mathf.Clamp01((groundAngle - minSlideAngle) / (90f - minSlideAngle));
        
        Vector3 currentHorizontal = new Vector3(playerVelocity.x, 0, playerVelocity.z);
        Vector3 targetHorizontal = Vector3.Lerp(currentHorizontal, combinedVelocity, slideBlend * Time.deltaTime * 3f);
        
        playerVelocity.x = targetHorizontal.x;
        playerVelocity.z = targetHorizontal.z;
    }

    // Calculates slope-aligned movement direction for better ground following
    Vector3 GetMovementDirectionProjectedOnPlane(Vector3 movement, Vector3 groundNormal, Vector3 upDirection)
    {
        if (movement.magnitude < 0.001f) return Vector3.zero;

        Vector3 movementProjectedOnPlane = Vector3.ProjectOnPlane(movement, groundNormal);
        Vector3 axisToRotateAround = Vector3.Cross(movement, upDirection);
        float angle = Vector3.SignedAngle(movement, movementProjectedOnPlane, axisToRotateAround);
        Quaternion rotation = Quaternion.AngleAxis(angle, axisToRotateAround);
        return (rotation * movement).normalized;
    }

    #endregion

    #region Jump System

    void HandleJump()
    {
        if (isGrounded && jumpInput)
        {
            if (CanJump(out float jumpHeightMultiplier))
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * jumpHeightMultiplier * -2f * gravity);
                jumpAudio.PlayRandom(audioSource);
            }
        }
    }

    bool CanJump(out float jumpHeightMultiplier)
    {
        jumpHeightMultiplier = 1f;
        bool canJump = true;

        if (groundAngle > maxWalkableAngle)
        {
            // On steep slope - apply restrictions
            float timeSinceLastJump = Time.time - lastSlopeJumpTime;
            
            if (timeSinceLastJump < slopeJumpCooldown)
            {
                // Too soon since last jump on steep slope
                canJump = false;
            }
            else
            {
                // Allow jump but with penalties
                consecutiveSlopeJumps++;
                jumpHeightMultiplier = Mathf.Clamp01(1f - (consecutiveSlopeJumps * 0.2f));
                jumpHeightMultiplier = Mathf.Max(jumpHeightMultiplier, slopeJumpPenalty);
                
                lastSlopeJumpTime = Time.time;
            }
        }
        else
        {
            // On normal ground - reset consecutive jumps
            consecutiveSlopeJumps = 0f;
        }

        // Additional sliding penalty
        if (isSliding)
        {
            jumpHeightMultiplier *= 0.6f;
        }

        return canJump;
    }

    #endregion

    #region Physics

    void ApplyGravity()
    {
        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }

        playerVelocity.y += gravity * Time.deltaTime;
    }

    #endregion

    #region Speed Tracking

    void UpdateSpeedTracking()
    {
        //Debug.Log(playerVelocity.magnitude);
        currentHorizontalSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;
    }

    #endregion
}