using UnityEngine;

public class Rocket : ExplodingProjectile
{
	protected Light light;

	protected override void Awake()
	{
		base.Awake();
		light = GetComponent<Light>();
	}

	protected override void Start()
	{
		base.Start();
		ParticleSystem[] array = trailParticles;
		foreach (ParticleSystem particleSystem in array)
		{
			particleSystem.Play(false);
		}
	}

	protected override bool Hit(Ray ray, RaycastHit hitInfo)
	{
		if (base.Hit(ray, hitInfo))
		{
			light.enabled = false;
			return true;
		}
		return false;
	}
}
