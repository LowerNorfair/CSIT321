/*
    DESCRIPTION: Player manager class, handles keyboard input

    AUTHOR DD/MM/YY: Andreas 18/09/22

	- EDITOR DD/MM/YY CHANGES:
    - Andreas 18/09/22: Ported over Quentin's PlayerControls script. Ported over Kaleb's input for sprinting.
    - Kaleb 19/09/22: Added monster swapping input and functionality. Modified variables and awake method also. Input recognition added for most controls.
    - Kaleb 20/09/22: Fixed sprint button up bug and added comments for clarity.
    - Nick 20/09/22: Added player movement. Under FixedUpdate.
    - Kaleb 20/09/22: Renamed back to PlayerControls and modifed player movement.
    - Kaleb 28/09/22: Added player modes and tidied some code.
    - Kaleb 03/10/22: Dash fixes
    - Kaleb 04/10/22: GameManager setup
    - Quentin 07/10/22: Added animation
    - Kaleb 08/10/22: Anim Fixes
    - Kaleb 13/11/22: Spellcasting Implementation
    - Kaleb 15/11/22: Capture Mode Implementation
    - Kaleb 15/11/22: Capture Mode Fixes
    - Kaleb 02/12/22: Interaction system
    - Kaleb 19/12/22 Singleton setup
    - Kaleb 07/01/23 Capture Redesign
    - Quentin 9/2/23 'Data' struct for storing persistant data
    - Kaleb 09/03/23: Beast management improvements
    - Kunal 15/04/23: Spell background image cooldown
    - Quentin 27/4/23: Animator stuff for spell casting
    - Quentin 9/5/23: Bug fixes for knockback & attacking while moving
    - Andreas 5/21/23: AOE
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    public bool testingMode;
    public static PlayerManager instance = null;
    //Private variables
    private PauseMenuScript pauseFunction;
    private GameMenu gameMenuFunction;

    private Rigidbody2D playerBody;
    private PlayerInput playerInput;
    private PlayerDash playerDash;
    private PlayerBasicAttack playerBasicAttack;
    private InteractionObject interactionObject;
    [HideInInspector] public Vector2 movementVector;
    private Vector3 directionVector;
    private Vector3 mousePos;
    private Vector3 worldPosition;
    private IEnumerator capture;
    private IEnumerator spellCastPause;
    private PlayerInputActions playerInputActions;
    private bool isCapturing;
    [HideInInspector] public Animator animator;

    [HideInInspector] public bool usingBeamSpell = false;
    private GameObject beamRef;
    private bool beamFired = true;

    private bool stopMoving = false;
    [HideInInspector] public bool isMoving = false;

    [HideInInspector] public bool inDialogue = false;
    [HideInInspector] public bool inGameMenu = false;
    [HideInInspector] public bool inPauseMenu = false;

    [Header("Player Variables")]
    public float playerSpeed;
    public bool canMove; //Bool for whether the player can currently move
    public bool canAttack = true;
    public enum PlayerMode { Basic, Spellcast, Capture }
    public PlayerMode playerMode;
    public Vector3 levelSwapPosition; //The position the player will be when they swap levels.

    [Header("Capture Variables")]
    public GameObject captureProjectile;
    public float captureProjectileCooldown;
    public float capturePower;

    [Header("Other Data")]
    public GameObject fizzleEffect;
    public GameObject book;
    private Animator bookAnimator;

    //Tutorial Booleans
    public bool canCapture = false;
    public bool canBasic = false;
    public bool canSpellcast = false;

    // Audio
    [HideInInspector] public AudioSource[] audioSources;
    [HideInInspector] public enum audioName { WALK, SWORDSWING, SWORDHIT, CAPTUREPROJ, DASH };


    // Serializable struct for data that will be saved/loaded //
    [System.Serializable]
    public struct Data
    {
        [HideInInspector] public PlayerStamina playerStamina;
        [HideInInspector] public PlayerHealth playerHealth;

        [Header("Beast Management")]
        public GameObject currentBeast; //The beast the player currently has selected   
        public List<EnemyScriptableObject> availableBeasts; //All the beasts the player currently has equipped
        public List<float> availableBeastsCooldowns; //The corresponding cooldowns for each beast the player currently has equipped
        public int totalBeasts; //The total number of beasts the player can store
        public int currentBeastIndex; //The index of the beast the player is currently using, starts at 0 for arrays

        public List<string> bestiaryEntries;

        public List<Quest> playerQuests;
        public List<string> questNames;
        public List<int> questStage;

        public string currentBeastName;
        public List<string> availableBeastNames;
    }
    public Data data = new Data();


    private void Awake()
    {
        if (testingMode)
        {
            canCapture = canBasic = canSpellcast = true;
        }
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SetupOnce();
        }
        else
        {
            Destroy(gameObject);
        }
        //Private variables initialization
        playerBody = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        data.playerStamina = GetComponent<PlayerStamina>();
        data.playerHealth = GetComponent<PlayerHealth>();
        playerDash = GetComponent<PlayerDash>();
        playerBasicAttack = GetComponent<PlayerBasicAttack>();
        interactionObject = GetComponentInChildren<InteractionObject>();
        animator = GetComponent<Animator>();

        while (data.availableBeasts.Count > data.totalBeasts) //Make sure the player does not have more available beasts then the limit
        {
            data.availableBeasts.RemoveAt(data.availableBeasts.Count - 1);
            data.availableBeastsCooldowns.RemoveAt(data.availableBeasts.Count - 1);
        }
        //data.currentBeast = data.availableBeasts[0].SpellObject;

        audioSources = GetComponentsInChildren<AudioSource>();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (levelSwapPosition.magnitude != 0)
        {
            transform.position = levelSwapPosition;
        }
        GameObject.FindGameObjectWithTag("Cinemachine").GetComponent<Cinemachine.CinemachineVirtualCamera>().Follow = this.transform;
    }

    void SetupOnce()
    {
        //Initialize player controls and input system
        playerInputActions = new PlayerInputActions();
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }



    //Delay setting gameManager by 1 frame for gameManager setup.
    private void Start()
    {
        pauseFunction = GameObject.FindGameObjectWithTag("GameManager").GetComponent<PauseMenuScript>();
        gameMenuFunction = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameMenu>();
        animator.SetBool("isIdle", true);
        for(int i=0;i<data.availableBeasts.Count;i++){
            GameManager.instance.UpdateSpellImage(data.availableBeasts[i],i);
        }
    }

    void Update()
    {
        if (GameManager.instance.isPaused)
        {
            movementVector = Vector2.zero;
            data.playerStamina.isSprinting = false;
            animator.SetBool("isIdle", true);
            animator.SetBool("isWalking", false);
            animator.SetBool("isSprinting", false);
        }
    }

    //For Movement
    private void FixedUpdate()
    {
        if (canMove)
        {
            playerBody.velocity = movementVector * playerSpeed;
        }
        if (stopMoving && !(animator.GetBool("isWalking") || animator.GetBool("isSprinting"))) playerBody.velocity = Vector2.zero;


        // update player sprite directions
        if (animator.GetBool("isWalking") || animator.GetBool("isSprinting"))
        {
            animator.SetFloat("Move X", movementVector.x);
            animator.SetFloat("Move Y", movementVector.y);
        }
        else if (animator.GetBool("isCapturing"))
        {
            mousePos = (Vector3)Mouse.current.position.ReadValue() - Camera.main.WorldToScreenPoint(transform.position);

            // Set sprite direction
            animator.SetFloat("Move X", mousePos.x);
            animator.SetFloat("Move Y", mousePos.y);
        }
    }

    // context: esc
    public void PauseMenu(InputAction.CallbackContext context)
    {
        if (context.performed && !inDialogue && !inGameMenu)
        {
            pauseFunction.PauseGame();
        }
    }

    // context: tab
    public void GameMenu(InputAction.CallbackContext context)
    {
        if (context.performed && !inDialogue && !inPauseMenu)
        {
            gameMenuFunction.PauseGame();
        }
    }

    // Context: RMB
    public void Attack(InputAction.CallbackContext context)
    {
        if (GameManager.instance.isPaused)
        {
            if(inDialogue) StartCoroutine(interactionObject.Interact());
            return;
        }

        switch (playerMode) //Decide which attack is used based on player mode
        {
            case PlayerMode.Basic:
                if (context.performed && canAttack && canBasic)
                {
                    canAttack = false;
                    stopMoving = true;
                    book.SetActive(false);
                    animator.SetBool("isWalking", false);
                    animator.SetBool("isSprinting", false);
                    animator.SetTrigger("basicAttack");
                    playerBasicAttack.BasicAttack();
                }
                break;
            case PlayerMode.Spellcast:
                if (canSpellcast)
                {
                    if (!bookAnimator) bookAnimator = book.GetComponent<Animator>();

                    mousePos = (Vector3)Mouse.current.position.ReadValue() - Camera.main.WorldToScreenPoint(transform.position);
                    animator.SetFloat("Move X", mousePos.x);
                    animator.SetFloat("Move Y", mousePos.y);
                    animator.SetBool("castSpell", true);

                    book.SetActive(true);
                    bookAnimator.SetBool("isFiring", true);


                    if (data.currentBeast == null)
                    {
                        //Create Fizzle Effect and play warning sound
                        mousePos = (Vector3)Mouse.current.position.ReadValue() - Camera.main.WorldToScreenPoint(transform.position);
                        Instantiate(fizzleEffect,
                            transform.position + mousePos.normalized,
                            Quaternion.AngleAxis(Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg - 90f, Vector3.forward));
                        //Same as exiting spellcasting
                        animator.SetBool("isCasting", false);
                        animator.SetBool("castSpell", false);
                        playerMode = PlayerMode.Basic;
                        canMove = true;
                    }
                    else if (data.availableBeastsCooldowns[data.currentBeastIndex] <= 0)
                    {
                        // for beam spell
                        if (!usingBeamSpell && data.availableBeasts[data.currentBeastIndex].SpellScriptable.SpellType == SpellTypeEnum.Beam)
                        {
                            GameObject beamObject = Instantiate(data.currentBeast);
                            beamObject.GetComponent<Projectile>().playerS = data.availableBeasts[data.currentBeastIndex].SpellScriptable;

                            beamRef = beamObject;
                            usingBeamSpell = true;
                            beamFired = false;
                        }
                        // using beam 2nd click for firing
                        else if (usingBeamSpell)
                        {
                            beamRef.GetComponentInChildren<BeamForPlayer>().FireBeam();
                            beamFired = true;

                            //Same as exiting spellcasting
                            playerMode = PlayerMode.Basic;
                            canMove = true;
                            StartCoroutine(AbilityCooldown(data.currentBeastIndex));

                            spellCastPause = SpellCastPause(2.0f);
                            StartCoroutine(spellCastPause);
                        }
                        // for aoe spell
                        else if (data.availableBeasts[data.currentBeastIndex].SpellScriptable.SpellType == SpellTypeEnum.AOE)
                        {
                            mousePos = Input.mousePosition;
                            mousePos.z = Camera.main.nearClipPlane;
                            worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
                            GameObject tempSpell = Instantiate(data.currentBeast,
                                worldPosition,
                                Quaternion.AngleAxis(-90f, Vector3.forward));
                            tempSpell.GetComponent<Projectile>().playerS = data.availableBeasts[data.currentBeastIndex].SpellScriptable;
                            
                            //Same as exiting spellcasting
                            playerMode = PlayerMode.Basic;
                            canMove = true;
                            StartCoroutine(AbilityCooldown(data.currentBeastIndex));
                        }
                        // for passive spell
                        else if(data.availableBeasts[data.currentBeastIndex].SpellScriptable.SpellType == SpellTypeEnum.Passive)
                        {
                            GameObject tempSpell = Instantiate(data.currentBeast, transform);
                            tempSpell.GetComponent<PassiveSpell>().SetValues(data.availableBeasts[data.currentBeastIndex].SpellScriptable);

                            //Same as exiting spellcasting
                            playerMode = PlayerMode.Basic;
                            canMove = true;
                            StartCoroutine(AbilityCooldown(data.currentBeastIndex));
                        }
                        // other spells
                        else
                        {
                            mousePos = (Vector3)Mouse.current.position.ReadValue() - Camera.main.WorldToScreenPoint(transform.position);
                            mousePos.z=0;
                            GameObject tempSpell = Instantiate(data.currentBeast,
                                transform.position + mousePos.normalized,
                                Quaternion.AngleAxis(Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg - 90f, Vector3.forward));
                            tempSpell.GetComponent<Projectile>().playerS = data.availableBeasts[data.currentBeastIndex].SpellScriptable;


                            //Same as exiting spellcasting
                            playerMode = PlayerMode.Basic;
                            canMove = true;
                            StartCoroutine(AbilityCooldown(data.currentBeastIndex));
                        }
                    }
                    else
                    {
                        //Something will happen when spells on CD
                    }

                    if (!usingBeamSpell)
                    {
                        spellCastPause = SpellCastPause(2.0f);
                        StartCoroutine(spellCastPause);
                    }

                }
                break;
            case PlayerMode.Capture:
                if (canCapture)
                {
                    if (!bookAnimator) bookAnimator = book.GetComponent<Animator>();

                    if (!isCapturing)
                    {
                        isCapturing = true;
                        capture = Capture(context);
                        bookAnimator.SetBool("isFiring", true);
                        StartCoroutine(capture);
                    }
                }
                break;
        }
    }

    // Context: RMB
    public void SpellcastMode(InputAction.CallbackContext context)
    {
        if (GameManager.instance.isPaused) return;
        if (!canSpellcast) return;

        if (playerMode == PlayerMode.Spellcast) //If the player is spellcasting, return to basic attacks
        {
            animator.SetBool("isCasting", false);
            playerMode = PlayerMode.Basic;
            canMove = true;

            ExitSpellcast();
        }

        else //Otherwise go to spellcasting mode and stop the player from moving
        {
            // stop any lingering spell cast animation 
            if(spellCastPause != null) StopCoroutine(spellCastPause);
            ExitSpellcast();

            animator.SetBool("isCasting", true); animator.SetBool("isCapturing", false); animator.SetBool("isWalking", false);
            playerMode = PlayerMode.Spellcast;
            canMove = false;
            playerBody.velocity = Vector2.zero;
        }
    }

    // Context: C
    public void CaptureMode(InputAction.CallbackContext context)
    {
        if (GameManager.instance.isPaused) return;
        if (!canCapture) return;

        if (playerMode == PlayerMode.Capture) //If the player is capturing, return to basic attacks
        {
            animator.SetBool("isCapturing", false);
            playerMode = PlayerMode.Basic;
            canMove = true;
            book.SetActive(false);
        }

        else //Otherwise go to capture mode and stop the player from moving
        {
            animator.SetBool("isCapturing", true); animator.SetBool("isCasting", false); animator.SetBool("isWalking", false);
            playerMode = PlayerMode.Capture;
            canMove = false;
            playerBody.velocity = Vector2.zero;
            book.SetActive(true);
        }
    }

    // Context: F
    public void Interact(InputAction.CallbackContext context)
    {
        //if (GameManager.instance.isPaused) return;

        if (context.performed)
        {
            StartCoroutine(interactionObject.Interact());
        }
    }

    // Context: Shift
    public void Sprint(InputAction.CallbackContext context) //Button down and up sets sprinting to true and false respectively
    {
        if (GameManager.instance.isPaused) return;

        if (context.performed)
        {
            //animator.SetBool("isSprinting", true);
            data.playerStamina.isSprinting = true;

            animator.SetFloat("SprintMult", 2);
        }
        else
        {
            //animator.SetBool("isSprinting", false);
            data.playerStamina.isSprinting = false;

            animator.SetFloat("SprintMult", 1);
        }
    }

    // Context: WASD
    public void Movement(InputAction.CallbackContext context)
    {
        movementVector = context.ReadValue<Vector2>();

        if (movementVector != Vector2.zero && playerMode != PlayerMode.Basic)
        {
            // exiting capture/casting by moving
            animator.SetBool("isCapturing", false); animator.SetBool("isCasting", false);
            book.SetActive(false);
            playerMode = PlayerMode.Basic;
            canMove = true;
        }

        if (GameManager.instance.isPaused) return;

        if (context.performed && canMove)
        {
            animator.SetBool("isWalking", true);
            ExitSpellcast();
            isMoving = true;
        }
        else
        {
            animator.SetBool("isWalking", false);
            isMoving = false;
        }



        if (movementVector.sqrMagnitude == 1) //Reposition the interaction object
        {
            directionVector = movementVector;
            directionVector.x *= 0.7f;
            directionVector.y *= 0.7f;
            interactionObject.gameObject.transform.position = (transform.position + directionVector);
        }
    }

    // Context: Q, E
    public void MonsterSwitch(InputAction.CallbackContext context)
    {
        if (GameManager.instance.isPaused) return;

        data.currentBeastIndex += (int)context.ReadValue<float>(); //Change the current beast index by -1 or 1 for Q and E respectively

        if (data.currentBeastIndex < 0) //Lower bound, set selected beast index to last beast
        {
            data.currentBeastIndex = data.totalBeasts - 1;
        }

        if (data.currentBeastIndex > data.totalBeasts - 1) //Upper bound, set selected beast index to first beast
        {
            data.currentBeastIndex = 0;
        }
        if (data.availableBeasts[data.currentBeastIndex] != null)
        {
            data.currentBeast = data.availableBeasts[data.currentBeastIndex].SpellScriptable.SpellProjectile; ; //Change the currently selected beast
        }
        else
        {
            data.currentBeast = null;
        }
        GameManager.instance.UpdateDisplayedSpell(data.currentBeastIndex);
    }

    public void MonsterSelect(InputAction.CallbackContext context)
    {
        if (GameManager.instance.isPaused) return;

        if (context.ReadValue<float>() < data.totalBeasts)
        { //If the selected beast is not out of bounds change the selected beast
            data.currentBeastIndex = (int)context.ReadValue<float>();
            if (data.availableBeasts[data.currentBeastIndex] != null)
            {
                data.currentBeast = data.availableBeasts[data.currentBeastIndex].SpellScriptable.SpellProjectile; ; //Change the currently selected beast
            }
            else
            {
                data.currentBeast = null;
            }
            GameManager.instance.UpdateDisplayedSpell(data.currentBeastIndex);
        }
    }

    // context: space
    public void Mobility(InputAction.CallbackContext context)
    {
        if (GameManager.instance.isPaused) return;

        if (context.performed && playerDash.canDash && movementVector != Vector2.zero)
        {
            audioSources[(int)audioName.DASH].Play();
            playerDash.Dash(movementVector);
            canMove = false;
        }
    }

    IEnumerator Capture(InputAction.CallbackContext context)
    {
        while (context.performed && playerMode == PlayerMode.Capture)
        {
            mousePos = (Vector3)Mouse.current.position.ReadValue() - Camera.main.WorldToScreenPoint(transform.position);
            audioSources[(int)audioName.CAPTUREPROJ].Play();
            Instantiate(captureProjectile,
                        transform.position + mousePos.normalized,
                        Quaternion.AngleAxis(Mathf.Atan2(mousePos.y, mousePos.x) * Mathf.Rad2Deg - 90f, Vector3.forward));


            yield return new WaitForSeconds(captureProjectileCooldown);
        }

        isCapturing = false;
        bookAnimator.SetBool("isFiring", false);
        yield return null;
    }

    public IEnumerator Stun(Vector2 dir)
    {
        canMove = false;
        data.playerHealth.isInvulnerable = true;
        playerBody.AddForce(dir * 10f, ForceMode2D.Impulse);
        yield return new WaitForSeconds(0.1f);

        playerBody.velocity = Vector3.zero;
        canMove = true;
        animator.SetBool("isCasting", false); animator.SetBool("isCapturing", false);
        playerMode = PlayerMode.Basic;
        data.playerHealth.isInvulnerable = false;

    }

    public IEnumerator AbilityCooldown(int i)
    {
        data.availableBeastsCooldowns[i] = data.availableBeasts[i].SpellScriptable.SpellCooldown;
        while (data.availableBeastsCooldowns[i] >= 0)
        {
            data.availableBeastsCooldowns[i] -= Time.fixedDeltaTime;
            GameManager.instance.cooldownImage[i].fillAmount = data.availableBeastsCooldowns[i]/data.availableBeasts[i].SpellScriptable.SpellCooldown;
            yield return new WaitForFixedUpdate();
        }
        data.availableBeastsCooldowns[i] = 0;
    }

    public IEnumerator SpellCastPause(float i)
    {
        yield return new WaitForSeconds(i);
        ExitSpellcast();
    }

    public void ExitSpellcast()
    {
        if(bookAnimator) bookAnimator.SetBool("isFiring", false);
        animator.SetBool("isCasting", false);
        animator.SetBool("castSpell", false);
        book.SetActive(false);

        // if exiting spellcasting before firing beam
        if (usingBeamSpell && !beamFired)
        {
            usingBeamSpell = false;
            beamFired = true;
            Destroy(beamRef);
        }
    }

    public int UpdateAvailableBeast(EnemyScriptableObject beast, int number)
    {
        int changedNum = number;
        data.availableBeasts[number] = beast;
        if (data.currentBeastIndex == number)
        {
            data.currentBeast = data.availableBeasts[data.currentBeastIndex].SpellScriptable.SpellProjectile;
        }

        for (int i = 0; i < data.totalBeasts; i++)
        {
            if (data.availableBeasts[i] == beast && i != number)
            {
                data.availableBeasts[i] = null;
                changedNum = i;
            }
        }
        return changedNum;
    }


    public void ModifyPlayerAttack(float boostValue)
    {
        playerBasicAttack.BoostAttack(boostValue);
    }
}
