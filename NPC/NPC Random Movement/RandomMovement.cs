using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class RandomMovement : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform centrePoint;

    [Header("Variables")]
    public float range;
    [SerializeField] private float waitTime = 2.0f;
    [SerializeField] private float rotationSpeed = 5.0f;
    public bool waiting;
    private Vector3 target;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!waiting && agent.remainingDistance <= agent.stoppingDistance)
        {
            StartCoroutine(WaitAndMove());
        }

    }

    bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {

        Vector3 randomPoint = center + Random.insideUnitSphere * range; 
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    public IEnumerator WaitAndMove()
    {
        waiting = true;
        RotateTowardsTarget();
        yield return new WaitForSeconds(waitTime);

        Vector3 point;
        if (RandomPoint(centrePoint.position, range, out point))
        {
            Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
            agent.SetDestination(point);
            target = point;
        }

        waiting = false;
    }
    private void RotateTowardsTarget()
    {
        if (agent.pathPending || agent.remainingDistance <= agent.stoppingDistance)
            return;

        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        }
    }

}