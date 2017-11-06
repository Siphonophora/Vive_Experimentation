using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JunkController : MonoBehaviour {

    public GameObject junk;
    public GameObject explosionEffect;

    public bool beingHeld = false;
    public bool dropped = false;
    private bool hasExploded = false;
    public float explosionTimer = 5f;
    public float explosionRadius = 0.1f;
    public float explosionForce = 1f;

	
	// Update is called once per frame
	void Update () {
        if (dropped && !hasExploded)
        {
            explosionTimer -= Time.deltaTime;
            if (explosionTimer < 0)
                Explode();
        }
	}

    public bool NeedsPickUp()
    {
        if(beingHeld || dropped){
            return false;
        }

        return true;
    }

    public bool PickUp(Transform robotTractor)
    {
        if (beingHeld || dropped)
        {
            return false;
        }
        else
        {
            junk.GetComponent<Rigidbody>().useGravity = false;
            transform.parent = robotTractor;
            beingHeld = true;
            return true;
        }


    }

    public void Drop()
    {
        dropped = true;
        transform.parent = null;
        junk.GetComponent<Rigidbody>().useGravity = true;

        return;

    }

    public void Explode()
    {
        // Show Effect
        GameObject effect = (GameObject)Instantiate(explosionEffect, transform.position, transform.rotation);
        Destroy(effect, 5f);

        //Get Nearby objects
        //Add forces
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach(Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if(rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }

        }

        // Remove Junk
        Destroy(gameObject);
    }
}
