using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour {

    public Transform target;
    public Transform source;
    public Transform visionFieldPrefab;
    public Transform visionField;
    public int visionOffset;
    public float visionZPosition;

    UnityEngine.AI.NavMeshAgent agent;

	// Use this for initialization
	void Start () {
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }
	
	// Update is called once per frame
	void Update () {
        agent.SetDestination(target.position);
        if (visionField != null)
        {
            visionField.position = new Vector3(agent.nextPosition.x + visionOffset, visionField.position.y, visionField.position.z);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("SmallerObstacle"))
        {
            // Randomly choose whether to respawn, turn around, or continue through obstacle
            int decision = Random.Range(0, 3); // choose a number 0, 1, or 2
            if (decision == 0)
            {
                // Respawn
                respawn();
            }
            else if (decision == 1)
            {
                // Turn around
                Transform temp = target;
                target = source;
                source = temp;

                agent.SetDestination(target.position);
                visionOffset = -1 * visionOffset;
            }
            else
            {
                // Continue through obstacle. Enemy has no vision when in the obstacle.
                Destroy(visionField.gameObject);
                visionField = null;
            }
        }
        else if (other.gameObject.CompareTag("Door"))
        {
            respawn();
        }
        else if (other.gameObject.CompareTag("ObstacleBound"))
        {
            if (visionField == null)
            {
                // If enemy is coming out of the obstacle, restore its vision.
                visionField = Instantiate(visionFieldPrefab, new Vector3(agent.nextPosition.x + visionOffset, 1, visionZPosition), Quaternion.identity);
            }
        }
    }

    public void respawn()
    {
        // Randomly pick a doorway to respawn at and set the new target
        int decision = Random.Range(0, 2); // Pick a number 0 or 1
        if (decision == 0)
        {
            // Reset position to source and continue to current target.
               agent.nextPosition = source.GetChild(0).position;
               agent.SetDestination(target.position);
        }
        else
        {
            // Switch source and target, then reset position and continute to current target.
            Transform temp = target;
            target = source;
            source = temp;

            agent.nextPosition = source.GetChild(0).position;
            agent.SetDestination(target.position);
            visionOffset = -1 * visionOffset;
            
            // If necessary, respawn the vision field as well.
            if (visionField == null)
            {
                visionField = Instantiate(visionFieldPrefab, new Vector3(agent.nextPosition.x + visionOffset, 1, visionZPosition), Quaternion.identity);
            }
        }
    }
}
