using UnityEngine;

public class MoveObjectsInRectangle : MonoBehaviour
{
    [Header("Orbiting Objects")]
    [Tooltip("Assign exactly four distinct objects. They will be spaced evenly around the square orbit.")]
    [SerializeField] private Transform[] objectsToMove;

    [Header("Square Orbit")]
    [Tooltip("Side length of the square path in world units. Set to 12 for a 12-by-12 path.")]
    [SerializeField, Min(0.01f)] private float sideLength = 12f;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speed = 1f;
    [SerializeField] private bool clockwise = true;

    private float perimeter;
    private float distanceTravelled;

    private void Start()
    {
        if (objectsToMove == null || objectsToMove.Length != 4)
        {
            Debug.LogError("MoveObjectsInRectangle needs exactly four orbiting objects assigned.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] == null)
            {
                Debug.LogError($"MoveObjectsInRectangle has an unassigned object at index {i}.", this);
                enabled = false;
                return;
            }

            for (int j = 0; j < i; j++)
            {
                if (objectsToMove[i] == objectsToMove[j])
                {
                    Debug.LogError("MoveObjectsInRectangle has the same object assigned more than once.", this);
                    enabled = false;
                    return;
                }
            }
        }

        sideLength = Mathf.Max(0.01f, sideLength);
        perimeter = 4f * sideLength;

        UpdateObjectPositions(GetOrbitTransform());
    }

    private void Update()
    {
        distanceTravelled = Mathf.Repeat(distanceTravelled + speed * Time.deltaTime, perimeter);
        UpdateObjectPositions(GetOrbitTransform());
    }

    private void UpdateObjectPositions(Matrix4x4 orbitTransform)
    {
        float direction = clockwise ? 1f : -1f;
        float spacing = perimeter / objectsToMove.Length;

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            float distance = Mathf.Repeat(distanceTravelled * direction + spacing * i, perimeter);
            Vector3 localPosition = GetOrbitPosition(distance, sideLength);
            objectsToMove[i].position = orbitTransform.MultiplyPoint3x4(localPosition);
        }
    }

    private Matrix4x4 GetOrbitTransform()
    {
        return Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
    }

    private void OnDrawGizmosSelected()
    {
        float pathSideLength = Mathf.Max(0.01f, sideLength);
        float pathPerimeter = pathSideLength * 4f;
        float halfSide = pathSideLength * 0.5f;
        Matrix4x4 orbitTransform = GetOrbitTransform();
        Vector3 bottomLeft = orbitTransform.MultiplyPoint3x4(new Vector3(-halfSide, 0f, -halfSide));
        Vector3 bottomRight = orbitTransform.MultiplyPoint3x4(new Vector3(halfSide, 0f, -halfSide));
        Vector3 topRight = orbitTransform.MultiplyPoint3x4(new Vector3(halfSide, 0f, halfSide));
        Vector3 topLeft = orbitTransform.MultiplyPoint3x4(new Vector3(-halfSide, 0f, halfSide));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        if (objectsToMove == null || objectsToMove.Length != 4)
            return;

        float travelled = Application.isPlaying ? distanceTravelled : 0f;
        float direction = clockwise ? 1f : -1f;
        float spacing = pathPerimeter / objectsToMove.Length;
        Gizmos.color = Color.yellow;

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            float distance = Mathf.Repeat(travelled * direction + spacing * i, pathPerimeter);
            Vector3 localPosition = GetOrbitPosition(distance, pathSideLength);
            Vector3 expectedPosition = orbitTransform.MultiplyPoint3x4(localPosition);
            Gizmos.DrawSphere(expectedPosition, pathSideLength * 0.025f);

            if (Application.isPlaying && objectsToMove[i] != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(objectsToMove[i].position, pathSideLength * 0.04f);
                Gizmos.color = Color.yellow;
            }
        }
    }

    private static Vector3 GetOrbitPosition(float distance, float pathSideLength)
    {
        float halfPathSideLength = pathSideLength * 0.5f;
        float x;
        float z;

        if (distance < pathSideLength)
        {
            x = -halfPathSideLength + distance;
            z = -halfPathSideLength;
        }
        else if (distance < 2f * pathSideLength)
        {
            x = halfPathSideLength;
            z = -halfPathSideLength + distance - pathSideLength;
        }
        else if (distance < 3f * pathSideLength)
        {
            x = halfPathSideLength - distance + 2f * pathSideLength;
            z = halfPathSideLength;
        }
        else
        {
            x = -halfPathSideLength;
            z = halfPathSideLength - distance + 3f * pathSideLength;
        }

        return new Vector3(x, 0f, z);
    }
}
