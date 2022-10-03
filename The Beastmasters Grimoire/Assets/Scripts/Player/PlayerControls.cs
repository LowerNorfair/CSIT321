/*
AUTHOR DD/MM/YY: Andreas 18/09/22
z
	- EDITOR DD/MM/YY CHANGES:
    - Andreas 18/09/22: Ported over Quentin's PlayerControls script. Ported over Kaleb's input for sprinting.
    - Kaleb 19/09/22: Added monster swapping input and functionality. Modified variables and awake method also. Input recognition added for most controls.
    - Kaleb 20/09/22: Fixed sprint button up bug and added comments for clarity.
    - Nick 20/09/22: Added player movement. Under FixedUpdate.
    - Kaleb 20/09/22: Renamed back to PlayerControls and modifed player movement.
    - Kaleb 28/09/22: Added player modes and tidied some code.
    - Kaleb 03/10/22: Dash fixes
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerControls : MonoBehaviour
{
    //Private variables
    private Rigidbody2D playerBody;
    private PlayerInput playerInput;
    private PlayerStamina playerStamina;
    private PlayerDash playerDash;
    private Vector2 movementVector;

    [Header("Player Variables")]
    public float playerSpeed;
    public bool canMove; //Bool for whether the player can currently move
    public enum PlayerMode { Basic, Spellcast, Capture }
    public PlayerMode playerMode;



    [Header("Beast Management")]
    public GameObject currentBeast; //The beast the player currently has selected
    public List<GameObject> availableBeasts; //All the beasts the player currently has equipped
    public int totalBeasts; //The total number of beasts the player can store
    public int currentBeastIndex; //The index of the beast the player is currently using, starts at 0 for arrays



    private void Awake()
    {
        //Private variables initialization
        playerBody = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerStamina = GetComponent<PlayerStamina>();
        playerDash = GetComponent<PlayerDash>();

        while (availableBeasts.Count > totalBeasts) //Make sure the player does not have more available beasts then the limit
        {
            availableBeasts.RemoveAt(availableBeasts.Count - 1);
        }

        //Initialize player controls and input system
        PlayerInputActions playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
        playerInputActions.Player.PauseMenu.performed += PauseMenu;
        playerInputActions.Player.GameMenu.performed += GameMenu;
        playerInputActions.Player.Attack.performed += Attack;
        playerInputActions.Player.SpellcastMode.performed += SpellcastMode;
        playerInputActions.Player.CaptureMode.performed += CaptureMode;
        playerInputActions.Player.Interact.performed += Interact;
        playerInputActions.Player.Sprint.performed += Sprint;
        playerInputActions.Player.Sprint.canceled += Sprint;
        playerInputActions.Player.Movement.performed += Movement;
        playerInputActions.Player.Movement.canceled += Movement;
        playerInputActions.Player.MonsterSwitch.performed += MonsterSwitch;
        playerInputActions.Player.MonsterSelect.performed += MonsterSelect;
        playerInputActions.Player.Mobility.performed += Mobility;

    }

    //For Movement
    private void FixedUpdate()
    {
        if (canMove)
        {
            playerBody.velocity = movementVector * playerSpeed;
        }
    }

    public void PauseMenu(InputAction.CallbackContext context)
    {

    }

    public void GameMenu(InputAction.CallbackContext context)
    {

    }

    public void Attack(InputAction.CallbackContext context)
    {
        switch (playerMode) //Decide which attack is used based on player mode
        {
            case PlayerMode.Basic:
                //Basic attack code goes here
                break;
            case PlayerMode.Spellcast:
                //Spellcasting code goes here
                break;
            case PlayerMode.Capture:
                //Capture code goes here
                break;
        }
    }

    public void SpellcastMode(InputAction.CallbackContext context)
    {
        if (playerMode == PlayerMode.Spellcast) //If the player is spellcasting, return to basic attacks
        {
            playerMode = PlayerMode.Basic;
            canMove = true;
        }

        else //Otherwise go to spellcasting mode and stop the player from moving
        {
            playerMode = PlayerMode.Spellcast;
            canMove = false;
            playerBody.velocity = Vector2.zero;
        }
    }

    public void CaptureMode(InputAction.CallbackContext context)
    {
        if (playerMode == PlayerMode.Capture) //If the player is capturing, return to basic attacks
        {
            playerMode = PlayerMode.Basic;
            canMove = true;
        }

        else //Otherwise go to capture mode and stop the player from moving
        {
            playerMode = PlayerMode.Capture;
            canMove = false;
            playerBody.velocity = Vector2.zero;
        }
    }

    public void Interact(InputAction.CallbackContext context)
    {

    }

    public void Sprint(InputAction.CallbackContext context) //Button down and up sets sprinting to true and false respectively
    {
        if (context.performed)
            playerStamina.isSprinting = true;
        else
            playerStamina.isSprinting = false;
    }

    public void Movement(InputAction.CallbackContext context)
    {
        movementVector = context.ReadValue<Vector2>();
    }

    public void MonsterSwitch(InputAction.CallbackContext context)
    {
        currentBeastIndex += (int)context.ReadValue<float>(); //Change the current beast index by -1 or 1 for Q and E respectively

        if (currentBeastIndex < 0) //Lower bound, set selected beast index to last beast
        {
            currentBeastIndex = totalBeasts - 1;
        }

        if (currentBeastIndex > totalBeasts - 1) //Upper bound, set selected beast index to first beast
        {
            currentBeastIndex = 0;
        }

        currentBeast = availableBeasts[currentBeastIndex]; //Change the currently selected beast
    }

    public void MonsterSelect(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() < totalBeasts)
        { //If the selected beast is not out of bounds change the selected beast
            currentBeastIndex = (int)context.ReadValue<float>();
            currentBeast = availableBeasts[currentBeastIndex];
        }
    }

    public void Mobility(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerDash.Dash();
            canMove = false;
        }
    }
}
