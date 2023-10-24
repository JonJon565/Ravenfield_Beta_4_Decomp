using System;
using System.Collections.Generic;
using UnityEngine;

public class CapturePoint : SpawnPoint
{
	private const float UPDATE_RATE = 1f;

	private const float CAPTURE_RATE_PER_PERSON = 0.05f;

	private const int HQ_QUALITY_LEVEL = 5;

	public float captureRange = 10f;

	public bool canBeCaptured = true;

	public Transform flagParent;

	public GameObject lqFlag;

	public GameObject hqFlag;

	private float control = 1f;

	private int pendingOwner;

	private Renderer flagRenderer;

	private Action unsafeAction = new Action(10f);

	private void Awake()
	{
		if (QualitySettings.GetQualityLevel() >= 5)
		{
			lqFlag.SetActive(false);
			hqFlag.SetActive(true);
			flagRenderer = hqFlag.GetComponent<Renderer>();
		}
		else
		{
			lqFlag.SetActive(true);
			hqFlag.SetActive(false);
			flagRenderer = lqFlag.GetComponent<Renderer>();
		}
	}

	private void Start()
	{
		if (GameManager.instance.reverseMode)
		{
			if (owner == 0)
			{
				owner = 1;
			}
			else if (owner == 1)
			{
				owner = 0;
			}
		}
		SetOwner(owner);
		if (owner == -1)
		{
			if (GameManager.instance.assaultMode)
			{
				SetOwner(1);
			}
			else
			{
				control = 0f;
			}
		}
		InvokeRepeating("UpdateOwner", 1f, 1f);
	}

	private void Update()
	{
		Vector3 localPosition = flagParent.localPosition;
		localPosition.y = 1.2f + 4.8f * control;
		flagParent.localPosition = Vector3.Lerp(flagParent.localPosition, localPosition, 3f * Time.deltaTime);
	}

	private void UpdateOwner()
	{
		if (!canBeCaptured)
		{
			return;
		}
		List<Actor> list = ActorManager.AliveActorsInRange(base.transform.position, captureRange);
		Dictionary<int, int> dictionary = new Dictionary<int, int>();
		bool flag = false;
		foreach (Actor item in list)
		{
			if (dictionary.ContainsKey(item.team))
			{
				Dictionary<int, int> dictionary2;
				Dictionary<int, int> dictionary3 = (dictionary2 = dictionary);
				int team;
				int key = (team = item.team);
				team = dictionary2[team];
				dictionary3[key] = team + 1;
			}
			else
			{
				dictionary.Add(item.team, 1);
			}
			if (item.team != owner)
			{
				flag = true;
			}
		}
		int num = -1;
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < 2; i++)
		{
			if (dictionary.ContainsKey(i) && dictionary[i] > num3)
			{
				num = i;
				num2 = num3;
				num3 = dictionary[i];
			}
		}
		int num4 = num3 - num2;
		if (num != -1)
		{
			if (num != pendingOwner)
			{
				control -= (float)num4 * 0.05f;
				if (control <= 0f)
				{
					SetOwner(num);
					control = 0.01f;
				}
			}
			else
			{
				control = Mathf.Clamp01(control + (float)num4 * 0.05f);
				if (control == 1f && owner != pendingOwner)
				{
					SetOwner(pendingOwner);
				}
			}
		}
		if (flag)
		{
			unsafeAction.Start();
		}
		flagRenderer.enabled = control > 0f;
	}

	public override bool IsSafe()
	{
		return unsafeAction.TrueDone();
	}

	private void SetOwner(int team)
	{
		int num = 0;
		int num2 = 0;
		switch (team)
		{
		case 0:
			num2++;
			break;
		case 1:
			num++;
			break;
		}
		if (team != owner)
		{
			if (owner == 0)
			{
				num2--;
			}
			else if (owner == 1)
			{
				num--;
			}
		}
		owner = team;
		pendingOwner = team;
		flagRenderer.material.color = Color.Lerp(ColorScheme.TeamColor(team), Color.black, 0.2f);
		ScoreUi.AddFlag(num2, num);
		try
		{
			MinimapUi.UpdateSpawnPointButtons();
		}
		catch (Exception)
		{
		}
	}

	public override float GotoRadius()
	{
		return captureRange * 0.9f;
	}
}
