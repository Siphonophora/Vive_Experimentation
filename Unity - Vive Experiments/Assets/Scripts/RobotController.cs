using UnityEngine;

public class RobotController : MonoBehaviour {

    public float speed;
    public float rotationSpeed = 10f;
    public float viewDistance;
    public float gripDistance;
    private string itemToMoveTag = "Trash";
    private string itemToAvoidTag = "Robot";
    private float avoidDistance = 0.025f;


    public Vector3 seekDir;
    public Vector3 avoidDir;
    private float avoidWeight = 5f;




    
    private Transform waypointTarget;
    public Transform tractorBeamStart;
    public Transform tractorBeamEnd;
    public LineRenderer tractorBeamLR;
    private float tractorBeamArrivalRadius = 0.01f;
    private float tractorBeamForce = 0.2f;
    private int waypointindex = 0;

    public GameObject trashPoint;
    public GameObject robot;
    public GameObject targetToMove;


 
    // Use this for initialization
    void Start () {
        waypointTarget = Waypoints.points[0];
    }
	
	// Update is called once per frame
	void Update () {

        UpdateTarget();

        seekDir = SeekTarget();
        avoidDir = AvoidRobots();

        MoveRobot();

        if (tractorBeamEnd.childCount > 0)
        {
            ApplyTractorBeam();
            ShowTractorBeam();
        }




    }


    private Vector3 SeekTarget()
    {
        bool holdingSomething = tractorBeamEnd.childCount > 0;

        if (targetToMove == null && !holdingSomething)
        {
            //Follow waypoints
            
            if (Vector3.Distance(transform.position, waypointTarget.position) < 0.04f)
            {
                GetNextWaypoint();
            }

            return waypointTarget.transform.position - transform.position;

        }

        //Go Pickup a target
        if (!holdingSomething && Vector3.Distance(targetToMove.transform.position, transform.position) > gripDistance)
        {
            return targetToMove.transform.position - transform.position;
        }

        //Try to grab a target
        if (!holdingSomething && Vector3.Distance(targetToMove.transform.position, transform.position) <= gripDistance)
        {
            GrabTarget();
            return Vector3.zero;
        }

        if (holdingSomething)
        {
            ApplyTractorBeam();
            ShowTractorBeam();

            Vector3 trashDir = GetTrashDir();

            if (Vector3.Magnitude(trashDir) < 0.01f)
            {
                for (int i = 0; i < tractorBeamEnd.childCount; i++)
                {
                    DropTarget(tractorBeamEnd.GetChild(i).gameObject);
                }
                return Vector3.zero;
            }

            return trashDir;
        }

        Debug.Log("Check for error. Shouldnt have hit this line");
        return Vector3.zero;


    }

    private Vector3 AvoidRobots()
    {
        Vector3 avoid = Vector3.zero;

        GameObject[] robots = GameObject.FindGameObjectsWithTag(itemToAvoidTag);

        foreach (GameObject robot in robots)
        {
            if (robot != gameObject) //Don't check this object.
            {
                float distanceToRobot = Vector3.Distance(transform.position, robot.transform.position);
                if (distanceToRobot < avoidDistance)
                {
                    avoid += transform.position - robot.transform.position;
                }
            }
        }

        return avoid;
    }


    private void ApplyTractorBeam()
    {
        for (int i = 0; i < tractorBeamEnd.childCount; i++)
        {
            GameObject go = tractorBeamEnd.GetChild(i).gameObject;
            Vector3 beamForceDir = tractorBeamEnd.transform.position - go.transform.position;

            Vector3 beamForce = beamForceDir.normalized * tractorBeamForce;

            if (beamForceDir.magnitude < tractorBeamArrivalRadius)
            {
                float forceScale = beamForceDir.magnitude / tractorBeamArrivalRadius;
                beamForce *= forceScale;
            }
            

            go.GetComponent<Rigidbody>().AddForce(beamForce);

        }
    }

    private void ShowTractorBeam()
    {
        tractorBeamLR.SetPosition(0, tractorBeamStart.position);
        GameObject go = tractorBeamEnd.GetChild(0).gameObject;
        tractorBeamLR.SetPosition(1, go.transform.position);
    }

    private void MoveRobot()
    {
        //Accumulate behaviors into a single direction
        Vector3 dir = seekDir.normalized 
                      + avoidDir.normalized * avoidWeight;


        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);
        Quaternion lookRotation = Quaternion.LookRotation(dir);
        Vector3 rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed).eulerAngles;
        transform.rotation = Quaternion.Euler (0f, rotation.y, 0f);
    }


    void GrabTarget()
    {
        if (targetToMove.GetComponent<JunkController>().PickUp(tractorBeamEnd))
        {
            targetToMove = null;
            tractorBeamLR.enabled = true;
        }
    }

    void DropTarget(GameObject itemToDrop)
    {
        itemToDrop.GetComponent<JunkController>().Drop();
        tractorBeamLR.enabled = false;
        return;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, viewDistance);
        Gizmos.DrawWireSphere(tractorBeamEnd.position, tractorBeamArrivalRadius);
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

    private Vector3 GetTrashDir()
    {
        Vector3 trashVector = TrashArea.points[1].transform.position - TrashArea.points[0].transform.position;
        Vector3 robotVector = transform.position - TrashArea.points[0].transform.position;

        Vector3 trashTargetAdjustment = Vector3.Project(robotVector, trashVector);
        
        Vector3 trashTargetLocation = TrashArea.points[0].transform.position + trashTargetAdjustment;

        Vector3 trashTargetDir = trashTargetLocation - transform.position;

        return trashTargetDir;
    }

    void UpdateTarget()
    {


        //We have a target, but haven't picked it up
        if (targetToMove != null)
        {
            //Someone else picked up our target. We need a new one
            if (!targetToMove.GetComponent<JunkController>().NeedsPickUp())
            {
                targetToMove = null;
            }
            //The current target is fine. 
            else
            {
                return;
            }
        }

        if (tractorBeamEnd.childCount > 0)
            return;
        
        GameObject[] targets = GameObject.FindGameObjectsWithTag(itemToMoveTag);
    
        float shortestDistance = Mathf.Infinity;
        GameObject nearestTarget = null;

        foreach (GameObject target in targets)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToTarget < shortestDistance && target.GetComponent<JunkController>().NeedsPickUp())
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
