using UnityEngine;

public class RobotController : MonoBehaviour {

    public float speed;
    public float rotationSpeed = 10f;
    public float viewDistance;
    public float gripDistance;
    public string itemToMoveTag = "Trash";




    private Transform waypointTarget;
    private GameObject trashTarget;
    public Transform tractorBeam;
    private int waypointindex = 0;

    public GameObject trashPoint;
    public GameObject robot;
    public GameObject targetToMove;


 
    // Use this for initialization
    void Start () {
        trashTarget = new GameObject();
        waypointTarget = Waypoints.points[0];
        
    }
	
	// Update is called once per frame
	void Update () {

        UpdateTarget();

        bool holdingSomething = tractorBeam.childCount > 0;

        if (targetToMove == null && !holdingSomething)
        {
            //Follow waypoints

        MoveRobotTo(waypointTarget);

            if (Vector3.Distance(transform.position, waypointTarget.position) < 0.01f)
            {
                GetNextWaypoint();
            }



        }
        else 
        {
            //Go Pickup a target
            if (!holdingSomething && Vector3.Distance(targetToMove.transform.position, transform.position) > gripDistance)
            {
                MoveRobotTo(targetToMove.transform);
                return;
            }

            //Try to grab a target
            if (!holdingSomething && Vector3.Distance(targetToMove.transform.position, transform.position) <= gripDistance)
            {
                GrabTarget();
                return;
            }

            if (holdingSomething)
            {
                if (Vector3.Distance(transform.position, trashTarget.transform.position) < 0.01f)
                {
                    for (int i = 0; i < tractorBeam.childCount; i++)
                    {
                        DropTarget(tractorBeam.GetChild(i).gameObject);
                    }
                    return;
                }

                MoveRobotTo(trashTarget.transform);
            }

        }
    }

    private void MoveRobotTo(Transform target)
    {
        Vector3 dir = target.transform.position - transform.position;
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Vector3 rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed).eulerAngles;
        transform.rotation = Quaternion.Euler (0f, rotation.y, 0f);
    }


    void GrabTarget()
    {
        if (targetToMove.GetComponent<JunkController>().PickUp(tractorBeam))
        {
            targetToMove = null;
            GetTrashTarget();
        }
    }

    void DropTarget(GameObject itemToDrop)
    {
        itemToDrop.GetComponent<JunkController>().Drop();
        return;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
    }


    void GetNextWaypoint()
    {
        if(waypointindex >= Waypoints.points.Length -1)
        {
            waypointindex = 0;
        } else
        {
            waypointindex++;
        }
        
        waypointTarget = Waypoints.points[waypointindex];
    }

    void GetTrashTarget()
    {
       Vector3 trashVector = TrashArea.points[1].transform.position - TrashArea.points[0].transform.position;
       Vector3 robotVector = transform.position - TrashArea.points[0].transform.position;

       Vector3 trashTargetAdjustment = Vector3.Project(robotVector, trashVector);

        trashTarget.transform.position = TrashArea.points[0].transform.position;

        trashTarget.transform.Translate(trashTargetAdjustment);
    }

    void UpdateTarget()
    {


        //We have a target, but haven't picked it up
        if (targetToMove != null)
        {
            //Someone else picked up our target. We need a new one
            if (!targetToMove.GetComponent<JunkController>().needsPickUp())
            {
                targetToMove = null;
            }
            //The current target is fine. 
            else
            {
                return;
            }
        }

        if (tractorBeam.childCount > 0)
            return;
        
        GameObject[] targets = GameObject.FindGameObjectsWithTag(itemToMoveTag);
    
        float shortestDistance = Mathf.Infinity;
        GameObject nearestTarget = null;

        foreach (GameObject target in targets)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToTarget < shortestDistance && target.GetComponent<JunkController>().needsPickUp())
            {
                shortestDistance = distanceToTarget;
                nearestTarget = target;
            }
        }

        if(nearestTarget != null && shortestDistance < viewDistance)
        {
            targetToMove = nearestTarget;
        }
    }






}
