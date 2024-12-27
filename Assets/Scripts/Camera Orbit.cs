using UnityEngine;

public class CameraOrbit : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Vector3 target = Vector3.zero; // The point the camera will orbit around

    [Header("Orbit Settings")]
    [SerializeField] private float orbitSpeed = 20.0f; // Degrees per second
    [SerializeField] private float distance = 10.0f;    // Distance from the target

    [Header("Vertical Positioning")]
    [SerializeField] private float elevation = 0.0f;    // Elevation angle in degrees

    private float currentAngle = 0.0f; // Current horizontal angle

    private void Start()
    {
        // Initialize the camera's position based on the initial angle and elevation
        UpdateCameraPosition();
    }

    private void Update()
    {
        // Increment the angle based on orbit speed and time
        currentAngle += orbitSpeed * Time.deltaTime;
        if (currentAngle >= 360f)
            currentAngle -= 360f;

        // Update the camera's position each frame
        UpdateCameraPosition();
    }

    /// <summary>
    /// Updates the camera's position to orbit around the target at the current angle and elevation.
    /// </summary>
    private void UpdateCameraPosition()
    {
        // Convert elevation angle to radians
        float elevRad = Mathf.Deg2Rad * elevation;

        // Calculate the horizontal and vertical offsets
        float horizontalOffset = distance * Mathf.Cos(elevRad) * Mathf.Cos(Mathf.Deg2Rad * currentAngle);
        float verticalOffset = distance * Mathf.Sin(elevRad);
        float depthOffset = distance * Mathf.Cos(elevRad) * Mathf.Sin(Mathf.Deg2Rad * currentAngle);

        // Set the new position
        Vector3 newPosition = target + new Vector3(horizontalOffset, verticalOffset, depthOffset);
        transform.position = newPosition;

        // Make the camera look at the target
        transform.LookAt(target);
    }

    /// <summary>
    /// Optional: Draws a gizmo in the editor to visualize the target point.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Draw a red sphere at the target position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(target, 0.2f);

        // Draw a line from the camera to the target
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, target);
    }
}