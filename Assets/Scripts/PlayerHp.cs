using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHp : MonoBehaviour
{
    private int health;
    public int maxHealth = 3;
    public float knockbackForce;
    public float gracePeriodDuration = 0.5f; //Grace period duration in seconds
    private float lastDamageTime; //Latest time player took damage
    private ClientPlayer playerMovAndMore;

     // for disabling controlls, done in playerhp because playermovement not compartmentlized.
    public bool isStone = false;

    private List<PlayerHp> allPlayersHp = new List<PlayerHp>();


    [SerializeField] FloatingHealthBar healthBar;

    void Awake() 
    {
        healthBar = GetComponentInChildren<FloatingHealthBar>();
    }
    //Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        // Update if restart game
        healthBar.UpdateHealthBar(health, maxHealth);
        playerMovAndMore = GetComponent<ClientPlayer>();
        lastDamageTime = -gracePeriodDuration; //Initialize lastDamageTime to a time before the grace period

        //Find and add all all players hp to the list so it can be observed
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObject in playerObjects)
        {
            PlayerHp playerHp = playerObject.GetComponent<PlayerHp>();
            if (playerHp != null)
            {
                allPlayersHp.Add(playerHp);
            }
        }
    }

    public int getHealth(){
        return health;
    }

    public void setHealth(int newHealth){
        health = newHealth;
    }

    public void SetKnockBack(float force){
        knockbackForce = force;
    }

    public void TakeDamage(int amount, Vector3 enemyPosition){
        if (Time.time - lastDamageTime >= gracePeriodDuration)
        {
            health -= amount;
            healthBar.UpdateHealthBar(health, maxHealth);
            if (health <= 0){
                FatalDamage();
            }
            else{
                TakeKnockback(enemyPosition);
                lastDamageTime = Time.time; // Update lastDamageTime to the current time
            }
            CheckStatus();
        }
        
    }  

    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Player"){
            Debug.Log("Entered collision with " + collision.gameObject.name);
            //collision.gameObject.PlayerHp.TakeDamage(damage);
            //PlayerHp playerHp = collision.gameObject.GetComponent<PlayerHp>();
            
            //int health = playerHp.getHealth();
            if (health <= 0){
                //playerHp.setHealth(1);
                health = 1;
                isStone = false;
                healthBar.UpdateHealthBar(health, maxHealth);
                //playerHp.UpdateHealthBar(health, maxHealth);
            }
        }
    }

    void CheckStatus()
    {
        bool allPlayersDead = true;
        //Check if any player is still alive
        foreach (PlayerHp playerHp in allPlayersHp)
        {
            if (playerHp.getHealth() > 0)
            {
                allPlayersDead = false;
                break;
            }
        }
        // If all players are dead, show the loss screen
        if (allPlayersDead)
        {
            // Activate loss screen
            
            Debug.Log("All players are dead! Show loss screen.");
            //playerMovAndMore.
            // Show your loss screen UI or perform any other necessary actions.
        }
    }

    public void TakeDamage(int amount){
        health -= amount;
        healthBar.UpdateHealthBar(health, maxHealth);

        if (health <= 0){
            FatalDamage();
        }
        CheckStatus();
    }  

    private void FatalDamage(){
        //Debug.Log("Dead");
        //Destroy(gameObject);
        //playerMovAndMore.isStone = true; 
        isStone = true;
    }

    private void TakeKnockback(Vector3 enemyPosition){
        //knockback
        Vector3 knockbackDirection = transform.position - enemyPosition;
            knockbackDirection.y = 0; //Disables knockback in y direction
            knockbackDirection.Normalize();
            GetComponent<Rigidbody>().AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
        
        //Mostly ignores playerinput that could interupt the knockback
        Vector3 knockbackVelocity = GetComponent<Rigidbody>().velocity;
            knockbackVelocity.y = 0; //no y velocity
            float knockbackSpeed = Vector3.Dot(knockbackVelocity, knockbackDirection);
            if (knockbackSpeed > 0)
            {
                GetComponent<Rigidbody>().velocity -= knockbackSpeed * knockbackDirection;
            }
        
    }
}
