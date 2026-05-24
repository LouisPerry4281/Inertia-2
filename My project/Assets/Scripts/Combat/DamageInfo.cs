using UnityEngine;

public readonly struct DamageInfo
{
    public readonly GameObject source;
    public readonly float damage;
    public readonly float hitStop;
    public readonly float juiceGain;
    public readonly Vector3 hitPoint;
    public readonly Vector3 force;
    public readonly Vector3 launch;
    public readonly float armorDamage;
    public readonly bool requiresBrokenArmorForStagger;
    public readonly float stunDuration;
    public readonly bool finisher;

    public DamageInfo(
        GameObject source,
        float damage,
        float hitStop,
        float juiceGain,
        Vector3 hitPoint,
        Vector3 force,
        Vector3 launch = default,
        float armorDamage = 0f,
        bool requiresBrokenArmorForStagger = false,
        float stunDuration = 0f,
        bool finisher = false)
    {
        this.source = source;
        this.damage = damage;
        this.hitStop = hitStop;
        this.juiceGain = juiceGain;
        this.hitPoint = hitPoint;
        this.force = force;
        this.launch = launch;
        this.armorDamage = armorDamage;
        this.requiresBrokenArmorForStagger = requiresBrokenArmorForStagger;
        this.stunDuration = stunDuration;
        this.finisher = finisher;
    }
}
