using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DamageCaster : MonoBehaviour
{
    private Collider damageCasterCollider;
    public int damage = 30;
    public string targetTag;
    private List<Collider> damageTargetList;

    private void Awake() {
        damageCasterCollider = GetComponent<Collider>();
        damageCasterCollider.enabled = false;
        damageTargetList = new List<Collider>();
    }
    private void OnTriggerEnter(Collider other) {
        if(other.tag == targetTag && !damageTargetList.Contains(other))
        {
            Character targetCC = other.GetComponent<Character>();

            if(targetCC != null)
            {
                targetCC.ApplyDamage(damage, transform.parent.position);
                PlayerVFXManager playerVFXManager = transform.parent.GetComponent<PlayerVFXManager>();
                if(playerVFXManager != null)
                {
                    RaycastHit hit;
                    Vector3 orignalPos = transform.position + (-damageCasterCollider.bounds.extents.z) * transform.forward;
                    bool isHit = Physics.BoxCast(orignalPos, damageCasterCollider.bounds.extents / 2, transform.forward, out hit, transform.rotation, damageCasterCollider.bounds.extents.z, 1<<6);
                    if(isHit)
                    {
                        playerVFXManager.PlaySlash(hit.point + new Vector3(0, 0.5f, 0));
                    }
                }
            }
            damageTargetList.Add(other);
        }
    }

    public void EnableDamageCaster()
    {
        damageTargetList.Clear();
        damageCasterCollider.enabled = true;
    }
    public void DisableDamageCaster()
    {
        damageTargetList.Clear();
        damageCasterCollider.enabled = false;
    }
    // private void OnDrawGizmos() 
    // {
    //     if(damageCasterCollider == null)
    //     {
    //         damageCasterCollider = GetComponent<Collider>();
    //     }
    //     RaycastHit hit;
    //     Vector3 orignalPos = transform.position + (-damageCasterCollider.bounds.extents.z) * transform.forward;
    //     bool isHit = Physics.BoxCast(orignalPos, damageCasterCollider.bounds.extents / 2, transform.forward, out hit, transform.rotation, damageCasterCollider.bounds.extents.z, 1<<6);

    //     if(isHit)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.DrawSphere(hit.point, 0.3f);
    //     }
    // }
}
