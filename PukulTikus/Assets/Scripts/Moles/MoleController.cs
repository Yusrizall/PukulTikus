using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MoleController : MonoBehaviour
{
    public MoleType type;

    [Tooltip("Visual pelindung/armor (nyalakan hanya untuk prefab armored).")]
    public GameObject armorVisual;

    [Tooltip("Berapa lama mole menunggu di lubang (override dari PhaseConfig jika > 0)")]
    public float lifetimeOverride = 0f;

    private int armor = 0;        // 1 untuk armored, 0 untuk lainnya
    private float lifetime = 1f;  // di-set saat spawn
    private Action<MoleController, bool, bool> onDespawn;
    // (self, killed, punishmentClicked)

    public void Init(MoleType t, float lt, Action<MoleController, bool, bool> onDespawnCb)
    {
        type = t;
        lifetime = lifetimeOverride > 0f ? lifetimeOverride : lt;
        onDespawn = onDespawnCb;

        armor = (type == MoleType.Armored) ? 1 : 0;

        // Tag awal sesuai tipe
        switch (type)
        {
            case MoleType.Normal: gameObject.tag = "Mole"; break;
            case MoleType.Armored: gameObject.tag = "MoleArmored"; break;
            case MoleType.Punishment: gameObject.tag = "Punishment"; break;
            case MoleType.Heart: gameObject.tag = "Heart"; break;
        }

        // Sinkronkan visual armor
        if (armorVisual) armorVisual.SetActive(armor > 0);

        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        // expired → despawn tanpa kill & tanpa punishment click
        onDespawn?.Invoke(this, false, false);
        Destroy(gameObject);
    }

    // Dipanggil saat dipukul/klik
    public void Hit(Action onArmorBreakVfx = null, Action onKillVfx = null)
    {
        // Heart tidak diklik untuk collect (hold-logic di HeartPickupController)
        if (type == MoleType.Heart)
            return;

        if (type == MoleType.Punishment)
        {
            onDespawn?.Invoke(this, false, true);
            Destroy(gameObject);
            return;
        }

        // Jika masih ber-armor, pecahkan dulu
        if (armor > 0)
        {
            armor = 0;
            if (armorVisual) armorVisual.SetActive(false); // lepas armor
            gameObject.tag = "Mole";                       // ubah tag jadi normal
            onArmorBreakVfx?.Invoke();                     // stat: validHits++ (di GameManager)
            return;                                        // belum kill; tunggu hit berikutnya
        }

        // Sudah tidak ber-armor → kill
        onKillVfx?.Invoke();
        onDespawn?.Invoke(this, true, false);
        Destroy(gameObject);
    }

    // Dipanggil HeartPickupController ketika hold selesai
    public void ConsumeHeartAndDespawn()
    {
        onDespawn?.Invoke(this, false, false);
        Destroy(gameObject);
    }
}
