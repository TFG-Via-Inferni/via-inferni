using UnityEngine;

[DisallowMultipleComponent]
public class DamageSourceContext : MonoBehaviour
{
    public Transform OwnerRoot { get; private set; }
    public string WeaponId { get; private set; }
    public PlayerFormType Form { get; private set; }

    public void Configure(Transform ownerRoot, string weaponId, PlayerFormType form)
    {
        OwnerRoot = ownerRoot;
        WeaponId = weaponId;
        Form = form;
    }
}