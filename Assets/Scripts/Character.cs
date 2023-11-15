using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
public class Character : MonoBehaviour
{
    private CharacterController cC;
    public float moveSpeed = 5f;
    private Vector3 movementVelocity;
    private PlayerInput playerInput;
    private float verticalVelocity;
    public float gravity = -9.8f;
    private Animator animator;

    public int coins;

    // Enemy
    public bool isPlayer = true;
    private NavMeshAgent navMeshAgent;
    private Transform targetPlayer;

    // Player slides
    public float attackStartTime;
    public float attackSlideDuration = 0.4f;
    public float attackSlideSpeed = 0.06f;

    private Vector3 impactOnCharacter;

    private bool isInvincible;
    private float invincibleDuration = 2f;
    private float attackAnimationDuration;

    // Health
    private Health health;

    // Damage Caster
    private DamageCaster damageCaster;

    // State Machine
    public enum CharacterState
    {
        Normal, Attaking, Dead, BeingHit
    }
    public CharacterState currentState;

    // Material animation
    private MaterialPropertyBlock materialPropertyBlock;
    private SkinnedMeshRenderer skinnedMeshRenderer;

    public GameObject itemToDrop;
    private void Awake() {
        cC = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
        damageCaster = GetComponentInChildren<DamageCaster>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        materialPropertyBlock = new MaterialPropertyBlock();
        skinnedMeshRenderer.GetPropertyBlock(materialPropertyBlock);

        if(!isPlayer)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            targetPlayer = GameObject.FindWithTag("Player").transform;
            navMeshAgent.speed = moveSpeed;
        }
        else{playerInput = GetComponent<PlayerInput>();}
    }
    private void CalculatePlayerMovement()
    {
        if(playerInput.mouseButtonDown && cC.isGrounded)
        {
            SwitchToState(CharacterState.Attaking);
            return;
        }
        movementVelocity.Set(playerInput.horizontalInput, 0f, playerInput.verticalInput);
        movementVelocity.Normalize();
        movementVelocity = Quaternion.Euler(0, -45f, 0) * movementVelocity;
        animator.SetFloat("Speed", movementVelocity.magnitude);
        movementVelocity *= moveSpeed * Time.deltaTime;
        if(movementVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(movementVelocity);
        }
        animator.SetBool("AirBorne", !cC.isGrounded);
        
    }

    private void CalculateEnemyMovement()
    {
        if(Vector3.Distance(targetPlayer.position, transform.position) >= navMeshAgent.stoppingDistance)
        {
            navMeshAgent.SetDestination(targetPlayer.position);
            animator.SetFloat("Speed", 0.2f);
        }
        else
        {
            navMeshAgent.SetDestination(transform.position);
            animator.SetFloat("Speed", 0f);

            SwitchToState(CharacterState.Attaking);
        }
    }
    private void FixedUpdate() {
        switch(currentState)
        {
            case CharacterState.Normal:
                if(isPlayer){CalculatePlayerMovement();}
                else{CalculateEnemyMovement();}
            break;
            case CharacterState.Attaking:

            if(isPlayer)
            {
                movementVelocity = Vector3.zero;

                if(Time.time < attackStartTime + attackSlideDuration)
                {
                    float timePassed = Time.time - attackStartTime;
                    float lerpTime = timePassed / attackSlideDuration;
                    movementVelocity = Vector3.Lerp(transform.forward * attackSlideSpeed, Vector3.zero, lerpTime);
                }
                if(playerInput.mouseButtonDown && cC.isGrounded)
                {
                    string currentClipName = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name;
                    attackAnimationDuration = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                    if(currentClipName != "LittleAdventurerAndie_ATTACK_03" && attackAnimationDuration > 0.5f && attackAnimationDuration < 0.7f)
                    {
                        SwitchToState(CharacterState.Attaking);
                        CalculatePlayerMovement();
                    }
                }
            }
            break;
            case CharacterState.Dead:
            return;
            case CharacterState.BeingHit:
            if(impactOnCharacter.magnitude > 0.2f)
            {
                movementVelocity = impactOnCharacter * Time.deltaTime;
            }
            impactOnCharacter = Vector3.Lerp(impactOnCharacter, Vector3.zero, Time.deltaTime * 5);
            break;
        }
        

        if(isPlayer)
        {
            if(!cC.isGrounded)
            {
                verticalVelocity = gravity;
            }
            else{
                verticalVelocity = gravity * 0.3f;
            }
            movementVelocity += verticalVelocity * Vector3.up * Time.deltaTime;

            cC.Move(movementVelocity);
            movementVelocity = Vector3.zero;
        }
    }

    public void SwitchToState(CharacterState newState)
    {
        // Clear Cache
        if(isPlayer)
        {
            playerInput.mouseButtonDown = false;
        }
        
        // Exiting State
        switch(currentState)
        {
            case CharacterState.Normal:
            break;
            case CharacterState.Attaking:
            if(damageCaster!=null)
            {
                DisableDamageCaster();
            }
            if(isPlayer){GetComponent<PlayerVFXManager>().StopBlade();}
            break;
            case CharacterState.Dead:
            return;
            case CharacterState.BeingHit:
            break;
        }

        // Entering State
        switch(newState)
        {
            case CharacterState.Normal:
            break;
            case CharacterState.Attaking:
            if(!isPlayer)
            {
                Quaternion newRotation = Quaternion.LookRotation(targetPlayer.position - transform.position);
                transform.rotation = newRotation;
            }
            
            animator.SetTrigger("Attack");
            
            if(isPlayer)
            {
                attackStartTime = Time.time;
            }
            break;
            case CharacterState.Dead:
            cC.enabled = false;
            animator.SetTrigger("Dead");
            StartCoroutine(MaterialDissolve());
            break;
            case CharacterState.BeingHit:
            animator.SetTrigger("BeingHit");
            if(isPlayer)
            {
                isInvincible = true;
                StartCoroutine(DelayCancelInvincible());
            }
            break;
        }
        currentState =  newState;

        Debug.Log("Switched to" + currentState);
    }
    public void AttackAnimationEnds()
    {
        SwitchToState(CharacterState.Normal);
    }
    public void BeingHitAnimationEnds()
    {
        SwitchToState(CharacterState.Normal);
    }
    public void ApplyDamage(int damage, Vector3 attackerPos = new Vector3())
    {
        if(isInvincible){return;}

        if(health != null)
        {
            health.ApplyDamage(damage);
        }
        if(!isPlayer)
        {
            GetComponent<EnemyVFXManager>().PlayBeingHitVFX(attackerPos);
        }
        StartCoroutine(MaterialBlink());

        if(isPlayer)
        {
            SwitchToState(CharacterState.BeingHit);
            AddImpact(attackerPos, 10f);
        }
    }
    IEnumerator DelayCancelInvincible()
    {
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
    }
    public void AddImpact(Vector3 attackerPos, float force)
    {
        Vector3 impactDir = transform.position - attackerPos;
        impactDir.Normalize();
        impactDir.y = 0;
        impactOnCharacter = impactDir * force;

    }
    public void EnableDamageCaster()
    {
        damageCaster.EnableDamageCaster();
    }
    public void DisableDamageCaster()
    {
        damageCaster.DisableDamageCaster();
    }

    IEnumerator MaterialBlink()
    {
        materialPropertyBlock.SetFloat("_blink", 0.4f);
        skinnedMeshRenderer.SetPropertyBlock(materialPropertyBlock);

        yield return new WaitForSeconds(0.2f);
        
        materialPropertyBlock.SetFloat("_blink", 0f);
        skinnedMeshRenderer.SetPropertyBlock(materialPropertyBlock);
    }
    IEnumerator MaterialDissolve()
    {
        yield return new WaitForSeconds(2f);
        float dissolveTimeDuration = 2f;
        float currentDissolveTime = 0;
        float dissolveHeight_start = 20f;
        float dissolveHeight_target = -10f;
        float dissolveHeight;

        materialPropertyBlock.SetFloat("_enableDissolve", 1f);
        skinnedMeshRenderer.SetPropertyBlock(materialPropertyBlock);
        while(currentDissolveTime < dissolveTimeDuration)
        {
            currentDissolveTime += Time.deltaTime;
            dissolveHeight = math.lerp(dissolveHeight_start, dissolveHeight_target, currentDissolveTime / dissolveTimeDuration);
            materialPropertyBlock.SetFloat("_dissolve_height", dissolveHeight);
            skinnedMeshRenderer.SetPropertyBlock(materialPropertyBlock);
            yield return null;
        }

        DropItem();
    }
    public void DropItem()
    {
        if(itemToDrop != null)
        {
            Instantiate(itemToDrop, transform.position, Quaternion.identity);
        }
    }
    public void PickUpItem(PickUp item)
    {
        switch(item.type)
        {
            case PickUp.PickUpIType.Heal:
            AddHealth(item.value);
            break;
            case PickUp.PickUpIType.Coin:
            AddCoins(item.value);
            break;
        }
    }
    private void AddHealth(int health)
    {
        this.health.AddHealth(health);
        GetComponent<PlayerVFXManager>().PlayHealVFX();
    }
    private void AddCoins(int coins)
    {
        this.coins = coins;
    }
}
