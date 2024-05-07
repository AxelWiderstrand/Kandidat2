using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class FloatingHealthBar : MonoBehaviour
{

    [SerializeField] private Camera cam;
    [SerializeField] Transform target;
    [SerializeField] private Slider slider;
    [SerializeField] private Vector3 offSet;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        cam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        // Buggy code.
        // Stop the health bar from rotating (rotates with the attached object otherwise)
       // transform.rotation = cam.transform.rotation;
     //   transform.position = target.position + offSet;
    }

    public void UpdateHealthBar(float currentValue, float maxValue) 
    {

        slider.value = currentValue / maxValue;
        Debug.Log("New slider value is: " + slider.value);
    }
}
