using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    public Animator animator;

    void Update()
    {
        if (DoorOpen.buttonPushed == true)
        {
            animator.SetBool("Activated", true);
        }
        else
        {
            animator.SetBool("Activated", false);
        }
    }


}
