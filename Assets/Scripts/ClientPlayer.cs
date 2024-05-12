using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using FischlWorks_FogWar;
using UnityEngine.UI;
public class ClientPlayer : NetworkBehaviour
{
    [SyncVar(hook = "OnColourChanged")] public Color theColour; // all objects have a colour set by their owner
    private static Color ourColour;
    private static bool firstTime = true;
    private Vector3 moveTarget;
    private bool moveTargetChanged = false;
    private bool isFogInitiated = false;
    public GameObject joystick;
    private float speed = 2f;
    private float rotationSpeed = 720f;

    private float dirX;
    private float dirZ;

    private Animator animator;
    private PlayerHp playerHp;


    public override void OnStartClient()
    {
        // Tell our object to be our own colour when it spawns so we can recognize it
        if (isOwned)
        {
            // The very first time we create a random colour to keep using when spawning new objects
            if (firstTime)
            {
                animator = GetComponent<Animator>();
                // Color for player and joystick.
                ourColour = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
                firstTime = false;

                // Create the joystick and place it in the canvas
                joystick = Instantiate(joystick, new Vector3(256, 256, 0), Quaternion.identity,
                GameObject.FindGameObjectWithTag("joystickCanvas").transform);

                // change color of joystick
                joystick.GetComponent<Image>().color = ourColour;
                joystick.transform.Find("Handle").gameObject.GetComponent<Image>().color = ourColour;
            }

            // Tell everyone about it through the SyncVar that we have authority over
            // This triggers OnColourChanged for everyone
            CmdSetPlayerColor(ourColour);
            
        }
    }

    /**
     * Ask the server to change the colour of the object
     */
    [Command]
    public void CmdSetPlayerColor(Color newColour)
    {
        theColour = newColour;
    }

    /**
     * Called when someone changes colour on an object they own
     */
    public void OnColourChanged(Color oldColor, Color newColour)
    {
        transform.Find("RPGHero").gameObject.GetComponent<Renderer>().material.SetColor("_EmissionColor", newColour);
        Debug.Log("Find hero: " + transform.Find("RPGHero"));
    }


    // Update is called once per frame
    void Update()
    {
        // create healthlog
        playerHp = GetComponent<PlayerHp>();
        if (isOwned & playerHp.isStone == false)
        {   
            Debug.Log("isStone " + playerHp.isStone);

            joystickMovement();
            // Set up fogrevealers, bad fix. Each client has to do this
            initializeFogRevealers();
        }
        else if (isOwned) {
            Debug.Log("isStone; " + playerHp.isStone);
            animator.SetBool("IsMoving", false);
        }
    }

    private void joystickMovement()
    {
        dirX = joystick.GetComponent<FixedJoystick>().Horizontal;
        dirZ = joystick.GetComponent<FixedJoystick>().Vertical;
        Vector3 movementDirection = new Vector3(dirX, 0, dirZ);
        movementDirection.Normalize();
        transform.Translate(movementDirection * speed * Time.deltaTime, Space.World);

        if (movementDirection != Vector3.zero)
        {
            animator.SetBool("IsMoving", true);
            Quaternion toRotation = Quaternion.LookRotation(movementDirection, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, rotationSpeed * Time.deltaTime);
            CmdSetMoveTarget(this.GetComponent<Rigidbody>().position);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }

    private void initializeFogRevealers() 
    {   
        if (isFogInitiated) 
        {
            return;
        }
        
        int counter = 0;
        foreach (GameObject go  in GameObject.FindGameObjectsWithTag("Player"))
        {
            counter++;
        }

        // If statement to prevent adding multiple fogrevealers for each player
        if (counter == 2) {
            foreach (GameObject player  in GameObject.FindGameObjectsWithTag("Player"))
            {
                csFogWar script = GameObject.FindWithTag("FogOfWar").GetComponent<csFogWar>();
                script.AddFogRevealer(new csFogWar.FogRevealer(player.transform, 2, true));
            }
            isFogInitiated = true;
        }
    }

    private void UpdatePosition(Vector3 inputPosition)
    {
        Camera cam = Camera.main;
        inputPosition.z = cam.transform.position.y;
        Vector3 moveTarget = cam.ScreenToWorldPoint(inputPosition);
        CmdSetMoveTarget(moveTarget);

        this.GetComponent<Rigidbody>().MovePosition(moveTarget);
    }

    [Command]
    private void CmdSetMoveTarget(Vector3 target)
    {
        moveTarget = target;
        moveTargetChanged = true;
    }

    private void FixedUpdate()
    {
        if (isServer)
        {
            if (moveTargetChanged)
            {
                moveTargetChanged = false;
                this.GetComponent<Rigidbody>().MovePosition(moveTarget);
            }
        }
    }
}
