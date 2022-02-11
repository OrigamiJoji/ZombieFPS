using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour {

    #region Sensitivity
    [Header("~ Sensitivity <3")]
    [Tooltip("Mouse X sensitivity")]
    public float sensX;
    [Tooltip("Mouse Y sensitivity")]
    public float sensY;

    private float mouseX;
    private float mouseY;
    #endregion
    #region References
    [Header("~ References <3")]
    [Tooltip("Player Camera (Component not GameObject")]
    public Camera playerCamera;
    [Tooltip("Player orientation object, child of player")]
    public Transform orientation;

    private float multiplier = 0.01f;
    private float xRotation;
    private float yRotation;
    #endregion


    private void Update() {
            mouseX = Input.GetAxisRaw("Mouse X");
            mouseY = Input.GetAxisRaw("Mouse Y");

            yRotation += mouseX * sensX * multiplier;
            xRotation -= mouseY * sensY * multiplier;

            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            playerCamera.transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            orientation.transform.rotation = Quaternion.Euler(0, yRotation, 0);
    }

}
