using UnityEngine;

// One per controllable character (office worker, candidate, future ones —
// same script, different Inspector values per body). Doesn't read input
// itself — PlayerDriver calls Tick() only while this body is the one
// currently possessed. Ground check, gravity, collider/camera smoothing
// keep running every frame regardless of possession, so nothing glitches
// or pops when you switch back to this body later. Only the camera object
// itself gets toggled on/off — the body's own GameObject (and its
// CharacterController) is never deactivated, which is what avoided all
// the "Move called on inactive controller" issues from before.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPossessable
{
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

    [Header("Camera")]
    [Tooltip("This body's own camera object — only enabled while this body is possessed (e.g. the candidate's camera with its VHS filter).")]
    public GameObject cameraObject;
    public Transform camTransform;
    public float camSpeed = 8f;
    public Vector3 crouchCameraOffset = new Vector3(0f, -0.5f, 0f);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioEvent jumpAudio;

    [Header("State (read-only)")]
    public bool isGrounded;
    public bool isCrouching = false;
    public bool isSliding = false;
    public bool isAiring = false;
    [HideInInspector] public float currentHorizontalSpeed;

    [Header("Candidate Stats")]
    [Tooltip("Weight (lbs) treated as gravity-neutral — heavier candidates fall/land harder, lighter ones less so. Tune this against whatever candidates you've authored.")]
    public float neutralWeight = 130f;

    // Single canonical "is this the body currently being controlled" check —
    // any per-body script (Inventory, arms animator, footsteps, future ones)
    // should read this instead of re-deriving the PlayerDriver comparison itself.
    public bool IsPossessed => PlayerDriver.Instance != null && (Object)PlayerDriver.Instance.CurrentBody == this;

    // IPossessable forwarding — the fields above stay the canonical source
    // (Inspector display, internal code like HandleAiringDetection), these
    // just expose them generically to anything holding an IPossessable.
    public GameObject GameObject => gameObject;
    public bool IsGrounded => isGrounded;
    public float CurrentHorizontalSpeed => currentHorizontalSpeed;

    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool wasGroundedWhenCrouchStarted = false;

    private float timeStartedFalling = 0f;
    private bool wasFalling = false;

    private Vector3 groundNormal = Vector3.up;
    private float groundAngle = 0f;
    private RaycastHit groundHit;

    private float lastSlopeJumpTime = 0f;
    private bool wasOnSteepSlope = false;
    private float consecutiveSlopeJumps = 0f;

    private float originalHeight;
    private Vector3 originalCenter;
    private float targetHeight;
    private Vector3 targetCenter;
    private float currentHeight;
    private Vector3 currentCenter;

    private Vector3 originalCameraLocalPos;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Applied BEFORE the crouch baseline below is captured, so a
        // height-scaled collider/camera becomes the new standing baseline
        // rather than fighting the un-scaled one every crouch transition.
        // Null if this scene is being tested directly (no Dispatch ran) —
        // Inspector-authored defaults are left alone in that case.
        if (DispatchData.DispatchedCandidate != null)
            ApplyCandidateStats(DispatchData.DispatchedCandidate);

        originalHeight = controller.height;
        originalCenter = controller.center;
        currentHeight = originalHeight;
        currentCenter = originalCenter;
        targetHeight = originalHeight;
        targetCenter = originalCenter;

        if (camTransform != null)
            originalCameraLocalPos = camTransform.localPosition;
    }

    // Maps a dispatched candidate's CV stats onto this body. speed and
    // walkSpeed are the same unit already (both authored in the 2.5-4
    // range), so it's a direct swap; run/crouch scale with it to keep their
    // relative feel. Height is likewise already in meters, same as the
    // collider, so it's a direct assignment too. Weight is the one that
    // needs relative scaling (against the tunable neutralWeight) instead of
    // being used directly, since candidate weight is authored in pounds,
    // not the small numbers gravity (-20) implies.
    public void ApplyCandidateStats(Candidate candidate)
    {
        if (candidate == null) return;

        if (candidate.speed > 0f && walkSpeed > 0f)
        {
            float ratio = candidate.speed / walkSpeed;
            walkSpeed = candidate.speed;
            runSpeed *= ratio;
            crouchSpeed *= ratio;
        }

        if (candidate.weight > 0f && neutralWeight > 0f)
            gravity *= candidate.weight / neutralWeight;

        if (candidate.height > 0f)
        {
            float oldHeight = controller.height;
            float newHeight = candidate.height;
            float heightDelta = newHeight - oldHeight;

            controller.height = newHeight;

            // Center rises by half the delta so the capsule's bottom (feet)
            // stays planted on the ground and only the top (head) moves.
            Vector3 center = controller.center;
            center.y += heightDelta / 2f;
            controller.center = center;

            // Eye level rises by the full delta to match the new head height.
            if (camTransform != null)
            {
                Vector3 camPos = camTransform.localPosition;
                camPos.y += heightDelta;
                camTransform.localPosition = camPos;
            }
        }
    }

    // Runs always, whether or not this body is currently possessed.
    void Update()
    {
        HandleGroundCheck();
        HandleAiringDetection();
        UpdateColliderDimensions();
        UpdateCameraPosition();
        ApplyGravity();
        UpdateSpeedTracking();

        // Not currently possessed: let horizontal velocity settle to zero
        // instead of endlessly sliding in whatever direction it was last
        // moving when control was handed off.
        if (!IsPossessed)
        {
            playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 6f);
            playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 6f);
        }

        if (controller.enabled)
            controller.Move(playerVelocity * Time.deltaTime);
    }

    // Called by PlayerDriver only while this body is possessed.
    public void Tick(Vector2 moveInput, bool jumpInput, bool runInput, bool crouchInput)
    {
        HandleCrouchInput(crouchInput);
        HandleMovement(moveInput, runInput);
        HandleJump(jumpInput);
    }

    public void SetCameraActive(bool active)
    {
        if (cameraObject == null) return;

        if (active && MainCameraController.Instance != null)
            MainCameraController.Instance.CutTo(cameraObject);
        else
            cameraObject.SetActive(active);
    }

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

    #region Airing Detection

    void HandleAiringDetection()
    {
        bool isFalling = playerVelocity.y <= airingThreshold && !isGrounded;

        if (isFalling && !wasFalling)
        {
            timeStartedFalling = Time.time;
        }

        bool shouldBeAiring = false;

        if (isFalling)
        {
            float timeFalling = Time.time - timeStartedFalling;
            shouldBeAiring = timeFalling >= airingDelay;
        }

        if (isAiring != shouldBeAiring)
        {
            isAiring = shouldBeAiring;
        }

        wasFalling = isFalling;
    }

    #endregion

    #region Crouching System

    void HandleCrouchInput(bool crouchInput)
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
            targetCenter = originalCenter - Vector3.up * (heightDiff * 0.5f);
        }
        else
        {
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

    #region Camera

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

    void HandleMovement(Vector2 moveInput, bool runInput)
    {
        Vector3 move = CalculateMovementDirection(moveInput);
        Vector3 inputDir = move.normalized;
        float speed = GetCurrentMovementSpeed(runInput);

        if (isGrounded)
        {
            HandleGroundMovement(inputDir, speed);
        }
        else
        {
            HandleAirMovement(inputDir, speed);
        }
    }

    Vector3 CalculateMovementDirection(Vector2 moveInput)
    {
        // Looked up fresh (not cached) so it always resolves to THIS
        // body's own active camera — whichever body is currently possessed
        // is also the one whose camera is currently enabled, so Camera.main
        // always means "the camera actually rotating right now" here.
        Camera mainCam = Camera.main;
        if (mainCam == null) return Vector3.zero;

        Transform lookTransform = mainCam.transform;
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = lookTransform.forward * move.z + lookTransform.right * move.x;
        move.y = 0f;
        return move;
    }

    float GetCurrentMovementSpeed(bool runInput)
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
                playerVelocity.x = Mathf.Lerp(playerVelocity.x, 0, Time.deltaTime * 10f);
                playerVelocity.z = Mathf.Lerp(playerVelocity.z, 0, Time.deltaTime * 10f);
            }
            else
            {
                Vector3 allowedDir = slopeDir.normalized;
                float speedMultiplier = Mathf.Clamp01(1f - uphillDot * 2f);
                playerVelocity.x = allowedDir.x * speed * speedMultiplier * 0.3f;
                playerVelocity.z = allowedDir.z * speed * speedMultiplier * 0.3f;
            }
        }
        else
        {
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
        Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, groundNormal).normalized;
        Vector3 slideVelocity = slideDirection * slideForce;
        Vector3 controlVelocity = inputDir * speed * slideControl;
        Vector3 combinedVelocity = slideVelocity + controlVelocity;

        float slideBlend = Mathf.Clamp01((groundAngle - minSlideAngle) / (90f - minSlideAngle));

        Vector3 currentHorizontal = new Vector3(playerVelocity.x, 0, playerVelocity.z);
        Vector3 targetHorizontal = Vector3.Lerp(currentHorizontal, combinedVelocity, slideBlend * Time.deltaTime * 3f);

        playerVelocity.x = targetHorizontal.x;
        playerVelocity.z = targetHorizontal.z;
    }

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

    void HandleJump(bool jumpInput)
    {
        if (isGrounded && jumpInput)
        {
            if (CanJump(out float jumpHeightMultiplier))
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * jumpHeightMultiplier * -2f * gravity);

                if (jumpAudio != null && audioSource != null)
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
            float timeSinceLastJump = Time.time - lastSlopeJumpTime;

            if (timeSinceLastJump < slopeJumpCooldown)
            {
                canJump = false;
            }
            else
            {
                consecutiveSlopeJumps++;
                jumpHeightMultiplier = Mathf.Clamp01(1f - (consecutiveSlopeJumps * 0.2f));
                jumpHeightMultiplier = Mathf.Max(jumpHeightMultiplier, slopeJumpPenalty);

                lastSlopeJumpTime = Time.time;
            }
        }
        else
        {
            consecutiveSlopeJumps = 0f;
        }

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

    void UpdateSpeedTracking()
    {
        currentHorizontalSpeed = new Vector3(playerVelocity.x, 0, playerVelocity.z).magnitude;
    }
}