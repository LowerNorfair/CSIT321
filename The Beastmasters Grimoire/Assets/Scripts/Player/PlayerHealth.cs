/*
    DESCRIPTION: Player health class    

    AUTHOR DD/MM/YY: Nick 22/09/22

	- EDITOR DD/MM/YY CHANGES:
    - Nick 22/09/22: Added TakeDamage and Death methods. Updated Regen methods.
    - Kaleb 28/09/22: Bug fixes
    - Andreas 29/09/22: Made TakeDamage public so that we can actually use it outside of the script
    - Kaleb 04/10/22: Added Invuln for dash
    - Kunal 03/12/22: Added Death Function
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float totalHealth; // Player's total health points
    public float currentHealth; // Player's current health points
    public float totalHealthMultiplier; // Player's health points if buffed

    [Header("Health Regen Settings")]
    public float healthRegenRate; // Rate at which player regains health
    public float healthRegenDelay; // Time before health regen kicks back in
    public float healthRegenDelayCurrent; // Time delay remaining
    public float healthRegenMultiplier; // Player's health regen if buffed

    [Header("Damage Resistance Settings")]
    public float damageMultiplier; // Damage resist (can be positive or negative)
    public bool isInvulnerable; //Wheter the player is or isn't immune to all damage;

    [Header("Health Booleans")]
    public bool healthRegening; // "Is player currently regenerating health?"

    public Vector3 checkpointLocation;
    private float modifiedByBoost = 0.0f;

    public SaveBeaconScriptableObject attunedBeacon;

    void Start()
    {
        // if player saved while boosting health
        if(modifiedByBoost > 0.0f)
        {
            totalHealth -= modifiedByBoost;
            modifiedByBoost = 0.0f;
        }

        currentHealth = totalHealth; // Initial Health
        healthRegenDelayCurrent = 0; // Initial Regen Delay
    }

    void Update()
    {

        if (healthRegenDelayCurrent > 0)
        {
            // Regen Delay Timer
            healthRegenDelayCurrent -= Time.deltaTime;
        }
        // Regen Trigger
        else if (healthRegening)
        {
            RegenHealth();
        }
    }

    // Called when the player takes damager
    public void TakeDamage(float damage)
    {
        if (!isInvulnerable)
        {
            currentHealth -= damage; // Take damage
            healthRegenDelayCurrent = healthRegenDelay; // Set regen delay to max
            healthRegening = true;

            // Death Trigger
            if (currentHealth <= 0)
            {
                Death();
            }
        }
    }

    // Regenerate player health
    public void RegenHealth()
    {
        // If current health less than max health
        if (currentHealth < totalHealth)
        {
            currentHealth += (healthRegenRate * Time.deltaTime);
        }

        // Prevent overhealing and set regen delay to max
        else
        {
            currentHealth = totalHealth;
            healthRegening = false;
        }
    }

    public void BoostHealth(float value)
    {
        totalHealth += value;
        currentHealth += value;

        modifiedByBoost += value;
    }

    void Death()
    {
        currentHealth =totalHealth;
        GameManager.instance.GetComponent<FastTravelScript>().FastTravel(attunedBeacon);
    }
}
