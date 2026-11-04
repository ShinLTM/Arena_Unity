using Arena.Combat;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    private int speed;
    NavMeshAgent agent;

    CapsuleCollider AttackCapsuleCollider;

    public Transform Player;

    [SerializeField] private Health _health;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        agent.destination = Player.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        attack();
    }

    void attack()
    {
        Debug.Log("feur");
    }
}
