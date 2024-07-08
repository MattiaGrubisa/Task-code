using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCWaypoint : MonoBehaviour
{
    [Header("References")]
    private NavMeshAgent agent;
    public Transform[] waypoints;

    [Header("Variables")]
    [SerializeField] private float waitTime = 2.0f;
    [SerializeField] private float rotationSpeed = 5.0f;
    private int waypointIndex;
    private Vector3 target;
    private bool waiting;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        waiting = false;
        nextWaypoint();
    }

    void Update()
    {
        if (!waiting && Vector3.Distance(transform.position, target) < 1)
        {
            StartCoroutine(WaitAndMove());
        }
    }

    private void nextWaypoint()
    {
        target = waypoints[waypointIndex].position;
        RotateTowardsTarget();
        agent.SetDestination(target);
    }

    private void indexHandle()
    {
        waypointIndex++;
        if (waypointIndex == waypoints.Length)
        {
            waypointIndex = 0;
        }
    }

    public IEnumerator WaitAndMove()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);

        indexHandle();
        nextWaypoint();

        waiting = false;
    }

    private void RotateTowardsTarget()
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

}
