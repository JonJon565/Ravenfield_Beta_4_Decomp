using UnityEngine;

public class Hitbox : MonoBehaviour
{
	public const int LAYER = 8;

	public const int RAGDOLL_LAYER = 10;

	private const float RIGIDBODY_HIT_FORCE = 0.01f;

	public Hurtable parent;

	public float multiplier = 1f;

	public bool ProjectileHit(Projectile p, Vector3 position)
	{
		return parent.Damage(p.Damage() * multiplier, p.BalanceDamage(), p.configuration.piercing, position, p.transform.forward, p.configuration.impactForce * p.transform.forward);
	}

	public bool RigidbodyHit(Rigidbody r, Vector3 position)
	{
		return parent.Damage(20f, 200f, false, position, r.velocity.normalized, r.velocity * r.mass * 0.01f);
	}
}
