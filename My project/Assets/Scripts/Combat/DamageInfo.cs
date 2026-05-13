using UnityEngine;

public readonly struct DamageInfo
{
    public readonly GameObject source;
    public readonly float damage;
    public readonly float hitStop;
    public readonly float juiceGain;
    public readonly Vector3 hitPoint;
    public readonly Vector3 force;

    public DamageInfo(GameObject source, float damage, float hitStop, float juiceGain, Vector3 hitPoint, Vector3 force)
    {
        this.source = source;
        this.damage = damage;
        this.hitStop = hitStop;
        this.juiceGain = juiceGain;
        this.hitPoint = hitPoint;
        this.force = force;
    }
}
