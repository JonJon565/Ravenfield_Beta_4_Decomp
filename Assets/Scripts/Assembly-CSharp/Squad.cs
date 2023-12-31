using System.Collections.Generic;
using UnityEngine;

public class Squad
{
	public enum State
	{
		Stationary = 0,
		Moving = 1,
		DigIn = 2,
		MovingThenDigIn = 3,
		EnterVehicle = 4
	}

	private const float GROUPED_UP_DISTANCE = 7f;

	private static int nextNumber = 1;

	public List<AiActorController> members;

	public State state;

	public Vehicle squadVehicle;

	public int number;

	public SpawnPoint targetSpawnPoint;

	private float readyTime;

	private bool groupedUp;

	public Squad(List<AiActorController> members, float timeUntilReady)
	{
		number = nextNumber++;
		state = State.Stationary;
		this.members = members;
		foreach (AiActorController member in this.members)
		{
			member.AssignedToSquad(this);
		}
		readyTime = Time.time + timeUntilReady;
	}

	public bool Ready()
	{
		return Time.time > readyTime;
	}

	public void DropMember(AiActorController a)
	{
		members.Remove(a);
		if (members.Count == 0)
		{
			Disband();
		}
	}

	public AiActorController Leader()
	{
		return members[0];
	}

	public Actor GetTarget()
	{
		foreach (AiActorController member in members)
		{
			if (member.HasTarget())
			{
				return member.target;
			}
		}
		return null;
	}

	public bool HasTargetSpawnPoint()
	{
		return targetSpawnPoint != null;
	}

	public bool ShouldGotoSpawnPoint(SpawnPoint spawnPoint)
	{
		return spawnPoint.owner != Leader().actor.team || !spawnPoint.IsSafe();
	}

	public SpawnPoint ClosestSpawnPoint()
	{
		return ActorManager.ClosestSpawnPoint(Leader().actor.Position());
	}

	public void NewAttackOrder()
	{
		Actor actor = Leader().actor;
		SpawnPoint spawnPoint = ClosestSpawnPoint();
		if (spawnPoint.owner != actor.team)
		{
			AttackSpawnPoint(spawnPoint);
			return;
		}
		List<SpawnPoint> list = new List<SpawnPoint>();
		foreach (SpawnPoint adjacentSpawnPoint in spawnPoint.adjacentSpawnPoints)
		{
			if (adjacentSpawnPoint.owner != actor.team)
			{
				list.Add(adjacentSpawnPoint);
				if (adjacentSpawnPoint.owner >= 0)
				{
					list.Add(adjacentSpawnPoint);
				}
			}
		}
		if (list.Count > 0)
		{
			AttackSpawnPoint(list[Random.Range(0, list.Count)]);
		}
		else
		{
			AttackSpawnPoint(ActorManager.RandomEnemySpawnPoint(actor.team));
		}
	}

	public void ReissueAttackOrder()
	{
		AttackSpawnPoint(targetSpawnPoint);
	}

	public void AttackSpawnPoint(SpawnPoint spawnPoint)
	{
		targetSpawnPoint = spawnPoint;
		if (spawnPoint != null)
		{
			Vector3 vector = Random.insideUnitSphere.ToGround() * spawnPoint.GotoRadius();
			MoveTo(spawnPoint.transform.position + vector);
		}
	}

	public void MoveTo(Vector3 point)
	{
		LeaveAnyCover();
		state = State.Moving;
		foreach (AiActorController member in members)
		{
			member.Goto(point + Vector3.Scale(Random.insideUnitSphere, new Vector3(3f, 0f, 3f)));
			if (member.squadLeader)
			{
				member.EmoteMoveOrder(point);
			}
		}
	}

	public void MoveToAndDigIn(Vector3 point)
	{
		if (HasVehicle())
		{
			Debug.LogWarning("Squad dig in while in vehicle, ignore.");
			return;
		}
		state = State.DigIn;
		foreach (AiActorController member in members)
		{
			member.FindCoverAtPoint(point);
			member.EmoteHailPlayer();
		}
	}

	public void DigIn()
	{
		if (HasVehicle())
		{
			Debug.LogWarning("Squad dig in while in vehicle, ignore.");
		}
		else
		{
			if (state == State.DigIn)
			{
				return;
			}
			state = State.DigIn;
			foreach (AiActorController member in members)
			{
				member.FindCover();
				if (member.squadLeader)
				{
					member.EmoteHalt();
				}
			}
		}
	}

	public void DigInTowards(Vector3 direction)
	{
		if (state == State.DigIn)
		{
			return;
		}
		state = State.DigIn;
		foreach (AiActorController member in members)
		{
			member.FindCoverTowards(direction);
			if (member.squadLeader)
			{
				member.EmoteHalt();
			}
		}
	}

	public void EnterVehicle(Vehicle vehicle)
	{
		if (state == State.EnterVehicle)
		{
			return;
		}
		state = State.EnterVehicle;
		squadVehicle = vehicle;
		squadVehicle.claimedBySquad = true;
		if (members.Count > vehicle.EmptySeats())
		{
			return;
		}
		int i = 0;
		for (int j = 0; j < members.Count; j++)
		{
			for (; vehicle.seats[i].IsOccupied(); i++)
			{
			}
			members[j].GotoAndEnterSeat(vehicle.seats[i]);
			i++;
			if (members[j].squadLeader)
			{
				members[j].EmoteMoveOrder(vehicle.transform.position);
			}
		}
	}

	public void ExitVehicle()
	{
		foreach (AiActorController member in members)
		{
			member.LeaveSeat();
		}
		state = State.Stationary;
	}

	public bool IsTakingFire()
	{
		foreach (AiActorController member in members)
		{
			if (member.IsTakingFire())
			{
				return true;
			}
		}
		return false;
	}

	public bool AllSeated()
	{
		foreach (AiActorController member in members)
		{
			if (!member.actor.IsSeated())
			{
				return false;
			}
		}
		return true;
	}

	public bool HasVehicle()
	{
		return squadVehicle != null;
	}

	private void LeaveAnyCover()
	{
		if (state != State.DigIn)
		{
			return;
		}
		foreach (AiActorController member in members)
		{
			member.LeaveCover();
		}
	}

	private void Disband()
	{
		if (squadVehicle != null)
		{
			squadVehicle.claimedBySquad = false;
		}
	}

	public bool IsGroupedUp()
	{
		return groupedUp;
	}

	public void UpdateGroupedUpFlag()
	{
		if (members.Count < 2)
		{
			groupedUp = false;
			return;
		}
		Vector3 zero = Vector3.zero;
		foreach (AiActorController member in members)
		{
			zero += member.transform.position;
		}
		zero /= (float)members.Count;
		int num = 0;
		foreach (AiActorController member2 in members)
		{
			if (Vector3.Distance(member2.transform.position, zero) < 7f)
			{
				num++;
			}
		}
		groupedUp = num >= 2;
	}

	public bool MemberNeedsResupply()
	{
		foreach (AiActorController member in members)
		{
			if (member.actor.needsResupply)
			{
				return true;
			}
		}
		return false;
	}

	public bool MemberNeedsHealth()
	{
		foreach (AiActorController member in members)
		{
			if (member.actor.health < 80f)
			{
				return true;
			}
		}
		return false;
	}
}
