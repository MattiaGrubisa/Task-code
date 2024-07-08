using System;
using System.Collections;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsPlayer;
    private NPCWaypoint waypoint;
    public Transform[] waypoints;

    [Header("Variables")]
    public float timeBetweenAttacks;
    private bool alreadyAttacked;
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;
    [SerializeField] private float waitTime = 2.0f;
    private int waypointIndex;
    private Vector3 target;
    [SerializeField] private bool waiting;
    private Vector3 lastSeenPosition;
    private bool wasChased;

    private void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        waiting = false;
        nextWaypoint();
    }

    private void Update()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

        if (!playerInSightRange && !playerInAttackRange) { 
            if (wasChased)
            {
                GoToLastSeenPosition();
            }
            else
            {
                Patrol();
            }
    };
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();

    }

    private void GoToLastSeenPosition()
    {
        agent.SetDestination(lastSeenPosition);
        if (Vector3.Distance(transform.position, lastSeenPosition) < 1f)
        {
            StartCoroutine(WaitAndResumePatrol());
        }
    }

    IEnumerator WaitAndResumePatrol()
    {
        waiting = true;
        yield return new WaitForSeconds(waitTime);
        wasChased = false;
        nextWaypoint();
        waiting = false;
    }

    private void Patrol()
    {
        if (!waiting)
        {
            agent.SetDestination(waypoints[waypointIndex].position);
            if (Vector3.Distance(transform.position, target) < 1f)
            {
                StartCoroutine(WaitAndMove());
            }
        }
    }
    private void ChasePlayer()
    {
        lastSeenPosition = player.position;
        agent.SetDestination(player.position);
        waiting = false;
        wasChased = true;
    }


    private void AttackPlayer()
    {
        waiting = false;
        agent.SetDestination(transform.position);
        transform.LookAt(player);
        Debug.Log("Attack");

        if (!alreadyAttacked)
        {
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    private void nextWaypoint()
    {
        target = waypoints[waypointIndex].position;
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
}
