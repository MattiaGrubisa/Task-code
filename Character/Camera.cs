using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    void Start()
    {
        transform.position = target.position + offset;
        transform.rotation = Quaternion.Euler(30, 45, 0);
    }

    void LateUpdate()
    {
        transform.position = target.position + offset;
    }
}
