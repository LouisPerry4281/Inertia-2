using System;
using System.Collections;
using UnityEngine;

public class PlayerJuiceManager : MonoBehaviour
{
    public static PlayerJuiceManager instance;

    [Header("Juice Amounts")]
    public float currentJuice = 50;
    [SerializeField] private float maxJuice = 100;
    
    [Header("Juice Decay")]
    [SerializeField] private float juiceDecayRate = 0.5f;
    [SerializeField] private float juiceDecayDelay = 3f;
    private float juiceDecayTimer = 0f;
    bool isDecaying = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        CheckJuiceDecay();
        JuiceDecay();
    }

    public void AddJuice(float juiceToAdd)
    {
        ResetJuiceTimer();
        
        currentJuice += juiceToAdd;

        if (currentJuice > maxJuice)
        {
            currentJuice = maxJuice;
        }
        
        //Update UI Stuff
    }

    public void RemoveJuice(float juiceToRemove)
    {
        currentJuice -= juiceToRemove;

        if (currentJuice < 0)
        {
            currentJuice = 0;
        }
        
        //Update UI Stuff
    }

    private void CheckJuiceDecay()
    {
        juiceDecayTimer += Time.deltaTime;

        if (juiceDecayTimer >= juiceDecayDelay)
        {
            isDecaying = true;
        }
        else
        {
            isDecaying = false;
        }
    }

    private void JuiceDecay()
    {
        if (!isDecaying)
            return;

        RemoveJuice(juiceDecayRate * Time.deltaTime);
    }

    public void ResetJuiceTimer()
    {
        juiceDecayTimer = 0f;
    }
}
