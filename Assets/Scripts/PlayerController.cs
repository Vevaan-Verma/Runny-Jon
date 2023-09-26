using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {

    [Header("References")]
    [SerializeField] private new Camera camera;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private Transform cameraPos;
    [SerializeField] private Transform muzzle;
    [SerializeField] private Image crosshair;
    private Animator animator;
    private Rigidbody rb;
    private LineRenderer lineRenderer;
    private Spring spring;

    [Header("Looking")]
    [SerializeField][Range(0f, 100f)] private float xSensitivity;
    [SerializeField][Range(0f, 100f)] private float ySensitivity;
    [SerializeField][Range(0f, 90f)] private float topCameraClamp;
    [SerializeField][Range(0f, 90f)] private float bottomCameraClamp;
    private float xRotation;
    private float yRotation;

    [Header("Movement")]
    [SerializeField] private float walkSpeed;
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float speedIncreaseMultiplier;
    [SerializeField] private float slopeIncreaseMultiplier;
    [SerializeField] private bool walkEnabled;
    [SerializeField] private bool sprintEnabled;
    [SerializeField] private bool jumpEnabled;
    [SerializeField] private bool slideEnabled;
    [SerializeField] private bool wallRunEnabled;
    [SerializeField] private bool swingEnabled;
    private float moveSpeed;
    private float desiredMoveSpeed;
    private float lastDesiredMoveSpeed;
    private float verticalInput;
    private float horizontalInput;
    private Vector3 movementDirection;
    private MovementState movementState;
    private Coroutine moveSpeedCoroutine;

    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    private bool jumpReady;

    [Header("Crouching")]
    [SerializeField] private float crouchHeight;
    private float startHeight;

    [Header("Sliding")]
    [SerializeField] private float maxSlideSpeed;
    [SerializeField] private float maxSlideTime;
    private float slideTimer;
    private bool isSliding;

    [Header("Wall Running")]
    [SerializeField] private float wallRunSpeed;
    [SerializeField] private float wallRunForce;
    [SerializeField] private float wallNormalForce;
    [SerializeField] private float exitWallTime;
    [SerializeField] private bool useWallRunTimer;
    [SerializeField] private float maxWallRunTime;
    [SerializeField] private LayerMask wallMask;
    private float wallRunTimer;
    private float exitWallTimer;
    private bool isWallRunning;
    private bool exitingWall;

    [Header("Wall Jumping")]
    [SerializeField] private float wallJumpUpForce;
    [SerializeField] private float wallJumpSideForce;

    [Header("Wall Climbing")]
    [SerializeField] private float wallClimbSpeed;
    private bool wallRunningUpwards;
    private bool wallRunningDownwards;

    [Header("Wall Run Gravity")]
    [SerializeField] private bool useWallRunGravity;
    [SerializeField] private float initialGravityCounterForce;
    [SerializeField] private float gravityDelay;
    [SerializeField] private float gravityDecrementRate;
    [SerializeField] private float gravityDecrementAmount;
    private float gravityCounterForce;
    private Coroutine gravityDelayCoroutine;

    [Header("Wall Run Animations")]
    [SerializeField] private float wallRunCameraFOV;
    [SerializeField] private float camFOVLerpDuration;
    [SerializeField] private float wallRunCamTilt;
    [SerializeField] private float camTiltLerpDuration;
    private float startCameraFOV;
    private float startCameraZTilt;
    private Coroutine lerpCamFOVCoroutine;
    private Coroutine lerpCamTiltCoroutine;

    [Header("Wall Detection")]
    [SerializeField] private float wallCheckDistance;
    [SerializeField] private float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Swinging")]
    [SerializeField] private float maxSwingSpeed;
    [SerializeField] private float maxSwingDistance;
    [SerializeField] private float horizontalThrustForce;
    [SerializeField] private float forwardThrustForce;
    [SerializeField] private float cableExtendSpeed;
    [SerializeField] private float jointSpring;
    [SerializeField] private float jointDamper;
    [SerializeField] private float jointMassScale;
    [SerializeField] private LayerMask swingableMask;
    private Vector3 swingPoint;
    private Vector3 currentSwingPosition;
    private SpringJoint joint;
    private bool isSwinging;

    [Header("Swing Rope Animation")]
    [SerializeField] private int quality;
    [SerializeField] private float damper;
    [SerializeField] private float strength;
    [SerializeField] private float velocity;
    [SerializeField] private float waveCount;
    [SerializeField] private float waveHeight;
    [SerializeField] private AnimationCurve effectCurve;

    [Header("Swing Prediction")]
    [SerializeField] private Transform predictionObj;
    [SerializeField] private float predictionRadius;
    private RaycastHit predictionHit;

    [Header("Headbob")]
    [SerializeField] private float walkBobSpeed;
    [SerializeField] private float walkBobAmount;
    [SerializeField] private float sprintBobSpeed;
    [SerializeField] private float sprintBobAmount;
    private Vector3 startCameraPos;
    private float timer;

    [Header("Ground Check")]
    [SerializeField] private Transform feet;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private LayerMask environmentMask;
    private bool isGrounded;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private float slopeCheckDistance;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Drag Control")]
    [SerializeField] private float groundDrag;

    [Header("Keybinds")]
    [SerializeField] private KeyCode sprintKey;
    [SerializeField] private KeyCode jumpKey;
    [SerializeField] private KeyCode slideKey;
    [SerializeField] private KeyCode upwardsWallRunKey;
    [SerializeField] private KeyCode downwardsWallRunKey;
    [SerializeField] private KeyCode cableExtendKey;

    public enum MovementState {

        None, Walking, Sprinting, Sliding, WallRunning, Swinging, Air

    }

    public enum SlopeType {

        None, Valid, Invalid

    }

    private void Start() {

        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        lineRenderer = GetComponent<LineRenderer>();
        spring = new Spring();

        rb.freezeRotation = true;
        spring.SetTarget(0);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        movementState = MovementState.None;

        jumpReady = true;

        startCameraFOV = camera.fieldOfView;
        startCameraZTilt = camera.transform.localRotation.z;

        startHeight = transform.localScale.y;
        startCameraPos = cameraPos.localPosition;

        predictionObj.gameObject.SetActive(false);

        gravityCounterForce = initialGravityCounterForce;

    }

    private void Update() {

        float mouseX = Input.GetAxisRaw("Mouse X") * xSensitivity * 10f * Time.fixedDeltaTime;
        float mouseY = Input.GetAxisRaw("Mouse Y") * ySensitivity * 10f * Time.fixedDeltaTime;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -topCameraClamp, bottomCameraClamp);

        cameraPos.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        transform.rotation = Quaternion.Euler(0, yRotation, 0);

        isGrounded = Physics.CheckSphere(feet.position, groundCheckRadius, environmentMask);
        animator.SetBool("isGrounded", isGrounded);

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        ControlSpeed();
        speedText.text = "Speed: " + (Mathf.Round(rb.velocity.magnitude * 100f) / 100f);
        HandleMovementState();

        if (Input.GetKeyDown(jumpKey) && jumpReady && jumpEnabled && isGrounded) {

            jumpReady = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);

        }

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && slideEnabled && !isSwinging && !isWallRunning)
            StartSlide();

        if ((Input.GetKeyUp(slideKey) || Input.GetKeyDown(jumpKey)) && isSliding)
            StopSlide();

        CheckWall();
        HandleWallRunState();

        if (Input.GetMouseButtonDown(1) && swingEnabled)
            StartSwing();

        if (Input.GetMouseButtonUp(1) && isSwinging)
            StopSwing();

        CheckSwingPoints();
        HandleHeadbob();

        if (isGrounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0f;

    }

    private void FixedUpdate() {

        movementDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        if (isSwinging && joint != null) {

            HandleSwingMovement();

        } else if (isWallRunning) {

            HandleWallRunMovement();

        } else if (CheckSlope() == SlopeType.Valid && !exitingSlope) {

            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 30f, ForceMode.Force);

            if (rb.velocity.y > 0f)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);

        } else if (CheckSlope() == SlopeType.Invalid) {

            rb.AddForce(Vector3.Cross(slopeHit.normal, Vector3.Cross(slopeHit.normal, Vector3.up)) * 500f, ForceMode.Acceleration);

        } else if (isGrounded) {

            rb.AddForce(movementDirection.normalized * moveSpeed * 10f, ForceMode.Force);

        } else {

            rb.AddForce(movementDirection.normalized * moveSpeed * airMultiplier * 10f, ForceMode.Force);

        }

        if (!isWallRunning)
            rb.useGravity = CheckSlope() == SlopeType.None || CheckSlope() == SlopeType.Invalid;

    }

    private void LateUpdate() {

        DrawRope();

    }

    public void HaltAllMovement() {

        walkEnabled = false;
        sprintEnabled = false;
        jumpEnabled = false;
        slideEnabled = false;
        wallRunEnabled = false;
        swingEnabled = false;
        movementState = MovementState.None;
        animator.SetBool("isWalking", false);
        animator.SetBool("isSprinting", false);
        animator.SetBool("isSliding", false);
        animator.SetBool("isWallRunningLeft", false);
        animator.SetBool("isWallRunningRight", false);
        animator.SetBool("isGrounded", true);

    }

    public void EnableWalk() {

        walkEnabled = true;

    }

    public void DisableWalk() {

        walkEnabled = false;

    }

    public void EnableSprint() {

        sprintEnabled = true;

    }

    public void DisableSprint() {

        sprintEnabled = false;

    }

    public void EnableJump() {

        jumpEnabled = true;

    }

    public void DisableJump() {

        jumpEnabled = false;

    }

    public void EnableSlide() {

        slideEnabled = true;

    }

    public void DisableSlide() {

        slideEnabled = false;

    }

    public void EnableWallRun() {

        wallRunEnabled = true;

    }

    public void DisableWallRun() {

        wallRunEnabled = false;

    }

    public void EnableSwing() {

        swingEnabled = true;

    }

    public void DisableSwing() {

        swingEnabled = false;

    }

    private void StartLerpCameraFOV(float targetFOV) {

        if (lerpCamFOVCoroutine != null)
            StopCoroutine(lerpCamFOVCoroutine);

        lerpCamFOVCoroutine = StartCoroutine(LerpCameraFOV(camera.fieldOfView, targetFOV));

    }

    private IEnumerator LerpCameraFOV(float startFOV, float targetFOV) {

        float currentTime = 0f;

        while (currentTime < camFOVLerpDuration) {

            currentTime += Time.deltaTime;
            camera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, currentTime / camFOVLerpDuration);
            yield return null;

        }

        camera.fieldOfView = targetFOV;
        lerpCamFOVCoroutine = null;

    }

    private void StartLerpCameraTilt(float targetZTilt) {

        if (lerpCamTiltCoroutine != null)
            StopCoroutine(lerpCamTiltCoroutine);

        lerpCamTiltCoroutine = StartCoroutine(LerpCameraTilt(camera.transform.localRotation.z, targetZTilt));

    }

    private IEnumerator LerpCameraTilt(float startZTilt, float targetZTilt) {

        float currentTime = 0f;

        while (currentTime < camTiltLerpDuration) {

            currentTime += Time.deltaTime;
            camera.transform.localRotation = Quaternion.Euler(camera.transform.localRotation.x, camera.transform.localRotation.y, Mathf.Lerp(startZTilt, targetZTilt, currentTime / camTiltLerpDuration));
            yield return null;

        }

        camera.transform.localRotation = Quaternion.Euler(camera.transform.localRotation.x, camera.transform.localRotation.y, targetZTilt);
        lerpCamTiltCoroutine = null;

    }

    private void HandleMovementState() {

        if (isSwinging && swingEnabled) {

            movementState = MovementState.Swinging;
            desiredMoveSpeed = maxSwingSpeed;

        } else if (isWallRunning && wallRunEnabled) {

            movementState = MovementState.WallRunning;
            desiredMoveSpeed = wallRunSpeed;

        } else if (isSliding && slideEnabled) {

            movementState = MovementState.Sliding;

            if (CheckSlope() == SlopeType.None && isGrounded)
                slideTimer -= Time.deltaTime;
            else if (CheckSlope() == SlopeType.Valid)
                desiredMoveSpeed = maxSlideSpeed;
            else
                desiredMoveSpeed = sprintSpeed;

            if (slideTimer <= 0f)
                StopSlide();

        } else if (isGrounded && movementDirection == Vector3.zero) {

            animator.SetBool("isWalking", false);
            animator.SetBool("isSprinting", false);
            movementState = MovementState.None;
            desiredMoveSpeed = walkSpeed;

        } else if (isGrounded && Input.GetKey(sprintKey) && sprintEnabled) {

            animator.SetBool("isWalking", false);
            animator.SetBool("isSprinting", true);
            movementState = MovementState.Sprinting;
            desiredMoveSpeed = sprintSpeed;

        } else if (isGrounded && walkEnabled) {

            animator.SetBool("isWalking", true);
            animator.SetBool("isSprinting", false);
            movementState = MovementState.Walking;
            desiredMoveSpeed = walkSpeed;

        } else if (!isGrounded && rb.velocity.magnitude > 0.01f) {

            animator.SetBool("isWalking", false);
            animator.SetBool("isSprinting", false);
            movementState = MovementState.Air;

        }

        if (Mathf.Abs(desiredMoveSpeed - lastDesiredMoveSpeed) > sprintSpeed - walkSpeed && moveSpeed != 0f) {

            if (moveSpeedCoroutine != null)
                StopCoroutine(moveSpeedCoroutine);

            moveSpeedCoroutine = StartCoroutine(LerpMoveSpeed());

        } else {

            moveSpeed = desiredMoveSpeed;

        }

        lastDesiredMoveSpeed = desiredMoveSpeed;

    }

    private IEnumerator LerpMoveSpeed() {

        float timer = 0f;
        float difference = Mathf.Abs(desiredMoveSpeed - moveSpeed);
        float startValue = moveSpeed;

        while (timer < difference) {

            moveSpeed = Mathf.Lerp(startValue, desiredMoveSpeed, timer / difference);

            if (CheckSlope() == SlopeType.Valid) {

                timer += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * (1f + (Vector3.Angle(Vector3.up, slopeHit.normal) / 90f));

            } else {

                timer += Time.deltaTime * speedIncreaseMultiplier;

            }

            yield return null;

        }

        moveSpeed = desiredMoveSpeed;

    }

    private void ControlSpeed() {

        if (CheckSlope() == SlopeType.Valid || CheckSlope() == SlopeType.Invalid && !exitingSlope) {

            if (rb.velocity.magnitude > moveSpeed)
                rb.velocity = rb.velocity.normalized * moveSpeed;

        } else {

            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            if (flatVel.magnitude > moveSpeed) {

                Vector3 controlledVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(controlledVel.x, rb.velocity.y, controlledVel.z);

            }
        }
    }

    private void Jump() {

        jumpReady = false;
        exitingSlope = true;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);

    }

    private void ResetJump() {

        jumpReady = true;
        exitingSlope = false;

    }

    private void StartSlide() {

        isSliding = true;
        crosshair.gameObject.SetActive(true);
        animator.SetBool("isSliding", true);
        transform.localScale = new Vector3(transform.localScale.x, crouchHeight, transform.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        slideTimer = maxSlideTime;

    }

    private void StopSlide() {

        isSliding = false;
        animator.SetBool("isSliding", false);
        transform.localScale = new Vector3(transform.localScale.x, startHeight, transform.localScale.z);

    }

    private void CheckWall() {

        wallRight = Physics.Raycast(transform.position, transform.right, out rightWallHit, wallCheckDistance, wallMask);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftWallHit, wallCheckDistance, wallMask);

    }

    private bool CanWallRun() {

        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, environmentMask);

    }

    private void HandleWallRunState() {

        wallRunningUpwards = Input.GetKey(upwardsWallRunKey);
        wallRunningDownwards = Input.GetKey(downwardsWallRunKey);

        if ((wallLeft || wallRight) && verticalInput > 0 && CanWallRun() && !exitingWall && !isGrounded) {

            if (!isWallRunning)
                StartWallRun();

            if (useWallRunTimer) {

                if (wallRunTimer > 0f)
                    wallRunTimer -= Time.deltaTime;

                if (wallRunTimer <= 0f && isWallRunning)
                    WallJump();

            }

            if (Input.GetKeyDown(jumpKey) && jumpEnabled)
                WallJump();

        } else if (exitingWall) {

            if (isWallRunning)
                StopWallRun();

            if (exitWallTimer > 0f)
                exitWallTimer -= Time.deltaTime;

            if (exitWallTimer <= 0f)
                exitingWall = false;

        } else {

            StopWallRun();

        }
    }

    private void StartWallRun() {

        if (isSliding)
            StopSlide();

        if (isSwinging)
            StopSwing();

        if (wallLeft)
            animator.SetBool("isWallRunningLeft", true);
        if (wallRight)
            animator.SetBool("isWallRunningRight", true);

        rb.useGravity = false;
        isWallRunning = true;
        crosshair.gameObject.SetActive(true);

        if (useWallRunTimer)
            wallRunTimer = maxWallRunTime;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        if (useWallRunGravity)
            gravityDelayCoroutine = StartCoroutine(HandleWallRunGravity());

        StartLerpCameraFOV(wallRunCameraFOV);

        if (wallLeft)
            StartLerpCameraTilt(-wallRunCamTilt);

        if (wallRight)
            StartLerpCameraTilt(wallRunCamTilt);

    }

    private IEnumerator HandleWallRunGravity() {

        yield return new WaitForSeconds(gravityDelay);

        while (isWallRunning) {

            rb.useGravity = true;
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
            yield return new WaitForSeconds(gravityDecrementRate);
            rb.useGravity = false;
            gravityCounterForce -= gravityDecrementAmount;

        }
    }

    private void HandleWallRunMovement() {

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((transform.forward - wallForward).magnitude > (transform.forward - -wallForward).magnitude)
            wallForward = -wallForward;

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (wallRunningUpwards)
            rb.velocity = new Vector3(rb.velocity.x, wallClimbSpeed, rb.velocity.z);

        if (wallRunningDownwards)
            rb.velocity = new Vector3(rb.velocity.x, -wallClimbSpeed, rb.velocity.z);

        if (!(wallLeft && horizontalInput > 0f) && !(wallRight && horizontalInput < 0f))
            rb.AddForce(-wallNormal * wallNormalForce, ForceMode.Force);

    }

    private void WallJump() {

        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 force = transform.up * wallJumpUpForce + wallNormal * wallJumpSideForce;

        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(force, ForceMode.Impulse);

    }

    private void StopWallRun() {

        animator.SetBool("isWallRunningLeft", false);
        animator.SetBool("isWallRunningRight", false);

        isWallRunning = false;
        StartLerpCameraFOV(startCameraFOV);
        StartLerpCameraTilt(startCameraZTilt);

        if (gravityDelayCoroutine != null)
            StopCoroutine(gravityDelayCoroutine);

    }

    public bool IsSwinging() {

        return isSwinging;

    }

    public Transform GetSwingMuzzle() {

        return muzzle;

    }

    public Vector3 GetSwingPoint() {

        return swingPoint;

    }

    private void CheckSwingPoints() {

        if (joint != null || isWallRunning || isSwinging)
            return;

        RaycastHit raycastHit;
        Physics.Raycast(cameraPos.position, cameraPos.forward, out raycastHit, maxSwingDistance, swingableMask);

        if (raycastHit.point != Vector3.zero) {

            // Direct Hit
            predictionHit = raycastHit;
            crosshair.gameObject.SetActive(false);

        } else {

            RaycastHit sphereCastHit;
            Physics.SphereCast(cameraPos.position, predictionRadius, cameraPos.forward, out sphereCastHit, maxSwingDistance, swingableMask);

            if (sphereCastHit.point != Vector3.zero) {

                // Indirect / Predicted Hit
                predictionHit = sphereCastHit;
                crosshair.gameObject.SetActive(true);

            } else {

                // Miss
                predictionHit.point = Vector3.zero;
                crosshair.gameObject.SetActive(true);

            }
        }

        Vector3 hitPoint = predictionHit.point;

        if (hitPoint != Vector3.zero && swingEnabled) {

            predictionObj.gameObject.SetActive(true);
            predictionObj.position = hitPoint + (predictionHit.normal * 0.002f);
            predictionObj.rotation = Quaternion.LookRotation(-predictionHit.normal, transform.up);

        } else {

            predictionObj.gameObject.SetActive(false);

        }
    }

    private void StartSwing() {

        if (predictionHit.point == Vector3.zero)
            return;

        predictionObj.gameObject.SetActive(false);
        isSwinging = true;
        crosshair.gameObject.SetActive(true);

        swingPoint = predictionHit.point;
        joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = swingPoint;

        float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

        joint.maxDistance = distanceFromPoint * 0.8f;
        joint.minDistance = distanceFromPoint * 0.25f;

        joint.spring = jointSpring;
        joint.damper = jointDamper;
        joint.massScale = jointMassScale;

    }

    private void DrawRope() {

        if (!isSwinging) {

            currentSwingPosition = muzzle.position;
            spring.Reset();

            if (lineRenderer.positionCount > 0)
                lineRenderer.positionCount = 0;

            return;

        }

        if (lineRenderer.positionCount == 0) {

            spring.SetVelocity(velocity);
            lineRenderer.positionCount = quality + 1;

        }

        spring.SetDamper(damper);
        spring.SetStrength(strength);
        spring.Update(Time.deltaTime);

        Vector3 up = Quaternion.LookRotation(swingPoint - muzzle.position).normalized * Vector3.up;

        // TODO: Make 12f a variable?
        currentSwingPosition = Vector3.Lerp(currentSwingPosition, swingPoint, Time.deltaTime * 12f);

        for (int i = 0; i < quality + 1; i++) {

            var delta = i / (float) quality;
            Vector3 offset = up * waveHeight * Mathf.Sin(delta * waveCount * Mathf.PI * spring.Value * effectCurve.Evaluate(delta));

            lineRenderer.SetPosition(i, Vector3.Lerp(muzzle.position, currentSwingPosition, delta) + offset);

        }
    }

    private void HandleSwingMovement() {

        if (horizontalInput != 0f)
            rb.AddForce((horizontalInput > 0f ? transform.right : -transform.right) * horizontalThrustForce * Time.deltaTime);

        if (verticalInput > 0f)
            rb.AddForce(transform.forward * forwardThrustForce * Time.deltaTime);

        if (Input.GetKey(jumpKey) && jumpEnabled) {

            Vector3 directionToPoint = swingPoint - transform.position;
            rb.AddForce(directionToPoint.normalized * forwardThrustForce * Time.deltaTime);

            float distanceFromPoint = Vector3.Distance(transform.position, swingPoint);

            joint.maxDistance = distanceFromPoint * 0.8f;
            joint.minDistance = distanceFromPoint * 0.25f;

        }

        if (Input.GetKey(cableExtendKey)) {

            float extendedDistanceFromPoint = Vector3.Distance(transform.position, swingPoint) + cableExtendSpeed;

            joint.maxDistance = extendedDistanceFromPoint * 0.8f;
            joint.minDistance = extendedDistanceFromPoint * 0.25f;

        }
    }

    private void StopSwing() {

        isSwinging = false;
        crosshair.gameObject.SetActive(true);
        Destroy(joint);

    }

    private void HandleHeadbob() {

        if (isGrounded) {

            if (Mathf.Abs(rb.velocity.x) > 0.1f || Mathf.Abs(rb.velocity.z) > 0.1f) {

                switch (movementState) {

                    case MovementState.Sprinting:

                    timer += sprintBobSpeed * Time.deltaTime;
                    cameraPos.localPosition = new Vector3(cameraPos.localPosition.x, startCameraPos.y + Mathf.Sin(timer) * sprintBobAmount, cameraPos.localPosition.z);
                    break;

                    case MovementState.Walking:

                    timer += walkBobSpeed * Time.deltaTime;
                    cameraPos.localPosition = new Vector3(cameraPos.localPosition.x, startCameraPos.y + Mathf.Sin(timer) * walkBobAmount, cameraPos.localPosition.z);
                    break;

                }
            }
        }
    }

    private SlopeType CheckSlope() {

        if (Physics.Raycast(feet.position, Vector3.down, out slopeHit, slopeCheckDistance)) {

            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);

            if (angle != 0f) {

                if (angle <= maxSlopeAngle)
                    return SlopeType.Valid;
                else
                    return SlopeType.Invalid;

            }
        }

        return SlopeType.None;

    }

    private Vector3 GetSlopeMoveDirection() {

        return Vector3.ProjectOnPlane(movementDirection, slopeHit.normal).normalized;

    }
}
