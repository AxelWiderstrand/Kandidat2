using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionEnemy : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 10f;
    //public PlayerHp PlayerHp;
    // Gets called at the start of the collision 
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player"){
            //Debug.Log("Entered collision with " + collision.gameObject.name);
            //collision.gameObject.PlayerHp.TakeDamage(damage);
            PlayerHp playerHp = collision.gameObject.GetComponent<PlayerHp>();
            playerHp.SetKnockBack(knockbackForce);
            playerHp.TakeDamage(damage, transform.position);
        }
        
    }

    // Gets called during the collision
    void OnCollisionStay(Collision collision)
    {
        //Debug.Log("Colliding with " + collision.gameObject.name);
    }

    // Gets called when the object exits the collision
    void OnCollisionExit(Collision collision)
    {
        //Debug.Log("Exited collision with " + collision.gameObject.name);
    }
}