using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth;
    public int currentHealth;
    private Character cC;
    private void Awake() 
    {
        currentHealth = maxHealth;
        cC = GetComponent<Character>();
    }
    public void ApplyDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log(gameObject.name + "took damage: " + damage);
        Debug.Log(gameObject.name + "current health: " + currentHealth);
        CheckHealth();
    }
    private void CheckHealth()
    {
        if(currentHealth <= 0)
        {
            cC.SwitchToState(Character.CharacterState.Dead);
        }
    }
}
