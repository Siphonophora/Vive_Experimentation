using UnityEngine;
using UnityEngine.AI;

public class RobotController : MonoBehaviour {

    public float speed;
    public float rotationSpeed = 10f;
    public float viewDistance;
    public float targetHeightAllowedOffset = 0.01f;
    public float gripDistance;
    private string itemToMoveTag = "Trash";
    private string itemToAvoidTag = "Robot";
    private float avoidDistance = 0.025f;


    public Vector3 seekDir;
    public Vector3 avoidDir;
    private float avoidWeight = 5f;

    [SerializeField]
    private string Intent;



    
    private Transform waypointTarget;
    public Transform tractorBeamStart;
    public Transform tractorBeamEnd;
    public LineRenderer tractorBeamLR;
    private float tractorBeamArrivalRadius = 0.01f;
    private float trashArrivalRadius = 0.04f;
    private float tractorBeamForce = 0.2f;
    private int waypointindex = 0;

    public GameObject trashPoint;
    public GameObject robot;
    public GameObject targetToMove;

    private NavMeshAgent agent;

 
    // Use this for initialization
    void Start () {
        waypointTarget = Waypoints.points[0];
        agent = GetComponent<NavMeshAgent>();
        InvokeRepeating("CheckTargetValidity", 0f, 5f);
    }
	
	// Update is called once per frame
	void Update () {

        UpdateTarget();

        Vector3 currentTarget = SeekTarget();

        agent.destination = currentTarget;

        if (tractorBeamEnd.childCount > 0)
        {
            ApplyTractorBeam();
            ShowTractorBeam();
        }




    }

    private void CheckTargetValidity()
    {
        if (targetToMove == null || tractorBeamEnd.childCount > 0)
            return;

        Vector3 vectorToTarget = targetToMove.transform.position - transform.position;
        Vector3 heightToTarget = Vector3.Scale(vectorToTarget, Vector3.up);

        if (vectorToTarget.magnitude > viewDistance || heightToTarget.magnitude > targetHeightAllowedOffset)
            targetToMove = null;

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

            Intent = "Following waypoints";
            return waypointTarget.transform.position;

        }

        //Go Pickup a target
        if (!holdingSomething && Vector3.Distance(targetToMove.transform.position, transform.position) > gripDistance)
        {
            Intent = "Moving towards a target";
            return targetToMove.transform.position;
        }

        //Try to grab a target
        if (!holdingSomething && Vector3.Distance(targetToMove.transform.position, transform.position) <= gripDistance)
        {
            Intent = "Trying to grab a target";
            GrabTarget();
            return transform.position;
        }

        if (holdingSomething)
        {

            Vector3 TrashLocation = GetTrashTarget();

            if (Vector3.Distance(transform.position, TrashLocation) < trashArrivalRadius)
            {
                for (int i = 0; i < tractorBeamEnd.childCount; i++)
                {
                    DropTarget(tractorBeamEnd.GetChild(i).gameObject);
                }
                Intent = "Dropping target";
                return transform.position;

            }
            Intent = "Heading to trash";
            return TrashLocation;
        }

        Intent = "No intent (shouldn't happen)";
        return transform.position;


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

    private Vector3 GetTrashTarget()
    {
        Vector3 trashVector = TrashArea.points[1].transform.position - TrashArea.points[0].transform.position;
        Vector3 robotVector = transform.position - TrashArea.points[0].transform.position;

        Vector3 trashTargetAdjustment = Vector3.Project(robotVector, trashVector);
        
        return TrashArea.points[0].transform.position + trashTargetAdjustment;
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
            Vector3 vectorToTarget = target.transform.position - transform.position;
            Vector3 heightToTarget = Vector3.Scale(vectorToTarget , Vector3.up);


            float distanceToTarget = vectorToTarget.magnitude;
            float heightDifference = heightToTarget.magnitude;
            
            if (distanceToTarget < shortestDistance && heightDifference < targetHeightAllowedOffset && target.GetComponent<JunkController>().NeedsPickUp())
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
