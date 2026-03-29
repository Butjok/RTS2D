using System.Collections;
using UnityEngine;

public class PlayerCameraManager : WorldBehaviour {

    [SerializeField] private PlayerController playerController;
    [SerializeField] private Vector3 position;
    [SerializeField] private Vector3 startPositionOffset = new(25, 25, 25);
    [SerializeField] private float yaw = 45;
    [SerializeField] private float pitch = 45;
    [SerializeField] private float rotationYawStepDegrees = 45;
    [SerializeField] private float rotationDuration = .25f;
    [SerializeField] private Easing.Type rotationEasing = Easing.Type.InOutQuadratic;
    public bool enableRotation = true;

    private IEnumerator rotationCoroutine;
    
    public void Initialize(PlayerController playerController) {
        this.playerController = playerController;
        position = this.playerController.transform.position + startPositionOffset;
    }

    public void AddMovementVector(Vector2 movementInput) {
        var playerCamera = playerController.PlayerCamera;
        var cameraForward = Vector3.Scale(playerCamera.transform.forward, new Vector3(1, 0, 1)).normalized;
        var cameraRight = new Vector3(cameraForward.z, 0, -cameraForward.x);
        var pitch = playerCamera.transform.rotation.eulerAngles.x;
        cameraForward /= Mathf.Cos(pitch * Mathf.Deg2Rad);
        var movementDirection = cameraForward * movementInput.y + cameraRight * movementInput.x;
        var speedUp = Input.GetKey(KeyCode.LeftShift) ? 2 : 1;
        position += movementDirection * (Time.deltaTime * 10 * speedUp);
    }

    public void GetView(out Vector3 outPosition, out Quaternion outRotation) {
        outPosition = position;
        outRotation = Quaternion.Euler(pitch, yaw, 0);
    }

    public bool TryRotateCamera(int direction) {
        if (!enableRotation)
            return true;
        if (rotationCoroutine != null)
            return false;
        rotationCoroutine = RotateCamera(direction);
        StartCoroutine(rotationCoroutine);
        return true;
    }

    private IEnumerator RotateCamera(int direction) {
        var centerRay = new Ray(position, Quaternion.Euler(pitch, yaw, 0) * Vector3.forward);
        if (Physics.Raycast(centerRay, out var hitInfo, 100)) {
            var pivot = hitInfo.point;
            var startOffsetWS = position - pivot;
            var startTime = Time.time;
            var lastOffsetWS = startOffsetWS;
            var lastYaw = 0f;
            while (Time.time < startTime + rotationDuration) {
                var t = (Time.time - startTime) / rotationDuration;
                t = Easing.Evaluate(rotationEasing, t);
                var yaw = direction * rotationYawStepDegrees * t;
                var offsetWS = Quaternion.Euler(0, yaw, 0) * startOffsetWS;
                position += offsetWS - lastOffsetWS;
                this.yaw += yaw - lastYaw;
                lastYaw = yaw;
                lastOffsetWS = offsetWS;
                yield return null;
            }

            // snap yaw to step degrees
            this.yaw = Mathf.Round(this.yaw / rotationYawStepDegrees) * rotationYawStepDegrees;
        }
        rotationCoroutine = null;
    }
}