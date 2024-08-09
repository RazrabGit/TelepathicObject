using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

namespace TelepathicObject
{

	public class BurerAI : EnemyAI
	{
		public AISearchRoutine searchForPlayers;

		private float checkLineOfSightInterval;

		public float maxSearchAndRoamRadius = 100f;

		[Space(5f)]
		public float noticePlayerTimer;

		private bool hasEnteredChaseMode;

		private bool lostPlayerInChase;

		private bool beginningChasingThisClient;

		private Collider[] nearPlayerColliders;

		public AudioClip shortRoar;

		public AudioClip[] hitWallSFX;

		public AudioClip bitePlayerSFX;

		private Vector3 previousPosition;

		private float previousVelocity;

		private float averageVelocity;

		private float velocityInterval;

		private float velocityAverageCount;

		private float wallCollisionSFXDebounce;

		private float timeSinceHittingPlayer;

		//private bool ateTargetPlayerBody;

		//private Coroutine eatPlayerBodyCoroutine;

		public Transform mouthTarget;

		public AudioClip eatPlayerSFX;

		public AudioClip[] hitCrawlerSFX;

		public AudioClip[] longRoarSFX;

		public DeadBodyInfo currentlyHeldBody;

		//private bool pullingSecondLimb;

		private float agentSpeedWithNegative;

		private Vector3 lastPositionOfSeenPlayer;

		[Space(5f)]
		public float BaseAcceleration = 55f;

		public float SpeedAccelerationEffect = 2f;

		public float SpeedIncreaseRate = 5f;

		private float lastTimeHit;

		private float distanceToMelee = TelepathicObject.Config.BurerDistanceToMelee.Value;

		private float timerToNextTelepathicAttack;

		private float telepathicAttackTime = TelepathicObject.Config.BurerTelepathicAttackTime.Value;

		private bool isTelepathing;

		private bool isChasingOnPlayer;

		private bool isAttackingNowAsTelepathic;

		private float timerBeforeTelephaticAttack = 0.9f;

		private float timerBeforeSecondAttack;

		private bool isSecondAttackCasted;

		public AudioClip SearchingStageSFX;

		private float timerToNextCrySound;

		public AudioSource creatureSearchingSFX;

		private float speedInChase = TelepathicObject.Config.BurerSpeedInChase.Value;

		private int damagePerHitOnPlayer = TelepathicObject.Config.BurerDamagePerHitOnPlayer.Value;

		private bool DropItemsOnSecondAttack = TelepathicObject.Config.DropItemsInSecondAttack.Value;


		public override void Start()
		{
			base.Start();
			nearPlayerColliders = new Collider[4];
		}

		public override void DoAIInterval()
		{
			base.DoAIInterval();
			if (StartOfRound.Instance.livingPlayers == 0 || isEnemyDead)
			{
				return;
			}
			switch (currentBehaviourStateIndex)
			{
				case 0:
					if (!searchForPlayers.inProgress)
					{
						StartSearch(base.transform.position, searchForPlayers);
					}
					break;
				case 1:
					CheckForVeryClosePlayer();
					if (lostPlayerInChase)
					{
						movingTowardsTargetPlayer = false;
						if (!searchForPlayers.inProgress)
						{
							searchForPlayers.searchWidth = 30f;
							StartSearch(lastPositionOfSeenPlayer, searchForPlayers);
						}
					}
					else if (searchForPlayers.inProgress)
					{
						StopSearch(searchForPlayers);
						isChasingOnPlayer = true;
						movingTowardsTargetPlayer = true;
					}
					break;
			}
		}

		public override void FinishedCurrentSearchRoutine()
		{
			base.FinishedCurrentSearchRoutine();
			searchForPlayers.searchWidth = Mathf.Clamp(searchForPlayers.searchWidth + 10f, 1f, maxSearchAndRoamRadius);
		}

		public override void Update()
		{
			base.Update();

			if (isEnemyDead)
			{
				return;
			}
			if (!base.IsOwner)
			{
				inSpecialAnimation = false;
			}
			CalculateAgentSpeed();
			timeSinceHittingPlayer += Time.deltaTime;
			timerToNextTelepathicAttack -= Time.deltaTime;
			timerToNextCrySound -= Time.deltaTime;
			if (isAttackingNowAsTelepathic)
			{
				timerBeforeTelephaticAttack -= Time.deltaTime;
			}

			if (isSecondAttackCasted)
			{
				timerBeforeSecondAttack -= Time.deltaTime;
			}

			if (currentBehaviourStateIndex == 0)
			{
				if (timerToNextCrySound <= 0f)
				{
					PlaySearchingSoundServerRpc();
					timerToNextCrySound = 33f;
				}
			}
			else
			{
				StopSearchingSoundServerRpc();
				timerToNextCrySound = 0f;
			}

			if (GameNetworkManager.Instance.localPlayerController.HasLineOfSightToPosition(base.transform.position + Vector3.up * 0.25f, 80f, 25, 5f))
			{
				if (currentBehaviourStateIndex == 1)
				{
					GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.8f);
				}
				else
				{
					GameNetworkManager.Instance.localPlayerController.IncreaseFearLevelOverTime(0.8f, 0.5f);
				}
			}
			switch (currentBehaviourStateIndex)
			{
				case 0:
					{
						if (hasEnteredChaseMode)
						{
							hasEnteredChaseMode = false;
							searchForPlayers.searchWidth = 25f;
							beginningChasingThisClient = false;
							noticePlayerTimer = 0f;
							useSecondaryAudiosOnAnimatedObjects = false;
							openDoorSpeedMultiplier = 0.6f;
							agent.stoppingDistance = 0f;
							agent.speed = 7f;
						}
						if (checkLineOfSightInterval <= 0.05f)
						{
							checkLineOfSightInterval += Time.deltaTime;
							break;
						}
						checkLineOfSightInterval = 0f;
						PlayerControllerB playerControllerB3;
						if (stunnedByPlayer != null)
						{
							playerControllerB3 = stunnedByPlayer;
							noticePlayerTimer = 1f;
						}
						else
						{
							playerControllerB3 = CheckLineOfSightForPlayer(45f, 30);
						}
						if (playerControllerB3 == GameNetworkManager.Instance.localPlayerController)
						{
							noticePlayerTimer = Mathf.Clamp(noticePlayerTimer + 0.05f, 0f, 10f);
							if (noticePlayerTimer > 0.2f && !beginningChasingThisClient)
							{
								beginningChasingThisClient = true;
								BeginChasingPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
								ChangeOwnershipOfEnemy(playerControllerB3.actualClientId);
							}
						}
						else
						{
							noticePlayerTimer -= Time.deltaTime;
						}
						break;
					}
				case 1:
					{
						if (!hasEnteredChaseMode)
						{
							hasEnteredChaseMode = true;
							lostPlayerInChase = false;
							checkLineOfSightInterval = 0f;
							noticePlayerTimer = 0f;
							beginningChasingThisClient = false;
							useSecondaryAudiosOnAnimatedObjects = true;
							openDoorSpeedMultiplier = 1.5f;
							agent.stoppingDistance = 0.5f;
							agent.speed = 0f;
						}
						if (!base.IsOwner || stunNormalizedTimer > 0f)
						{
							break;
						}
						if (checkLineOfSightInterval <= 0.075f)
						{
							checkLineOfSightInterval += Time.deltaTime;
							break;
						}
						checkLineOfSightInterval = 0f;
                        if (targetPlayer != null && targetPlayer.deadBody != null && targetPlayer.deadBody.grabBodyObject != null && targetPlayer.deadBody.grabBodyObject.grabbableToEnemies)
                        {
							lostPlayerInChase = true;
						}
                        if (inSpecialAnimation)
						{
							break;
						}
						if (lostPlayerInChase)
						{
							isTelepathing = false;
							PlayerControllerB playerControllerB = CheckLineOfSightForPlayer(45f, 30);
							if ((bool)playerControllerB)
							{
								noticePlayerTimer = 0f;
								lostPlayerInChase = false;
								MakeScreechNoiseServerRpc();
								if (playerControllerB != targetPlayer)
								{
									SetMovingTowardsTargetPlayer(playerControllerB);
									//ateTargetPlayerBody = false;
									ChangeOwnershipOfEnemy(playerControllerB.actualClientId);
								}
							}
							else
							{
								noticePlayerTimer -= 0.075f;
								if (noticePlayerTimer < -15f)
								{
									SwitchToBehaviourState(0);
								}
							}
							break;
						}
                        PlayerControllerB playerControllerB2 = CheckLineOfSightForPlayer(65f, 80);
                        if (playerControllerB2 != null)
                        {
							if (playerControllerB2 == targetPlayer)
                            {
								noticePlayerTimer = 0f;
                                lastPositionOfSeenPlayer = playerControllerB2.transform.position;
                            }
							//if (playerControllerB2 != targetPlayer)
							//{
							//    targetPlayer = playerControllerB2;
							//    ateTargetPlayerBody = false;
							//    ChangeOwnershipOfEnemy(targetPlayer.actualClientId);
							//}
						}
                        else
						{
							noticePlayerTimer += 0.075f;
							if (noticePlayerTimer > 1.8f)
							{
								lostPlayerInChase = true;
							}
						}
						if (!lostPlayerInChase)
						{

							if (Vector3.Distance(targetPlayer.transform.position, transform.position) > distanceToMelee)
							{
								isTelepathing = true;
								TelepathicAttack((int)targetPlayer.actualClientId);

							}
							else
							{
								isTelepathing = false;
								movingTowardsTargetPlayer = true;
							}

						}

						break;
					}
			}
		}

		private void CalculateAgentSpeed()
		{
			if (stunNormalizedTimer >= 0f)
			{
				agent.speed = 0.1f;
				agent.acceleration = 200f;
				creatureAnimator.SetBool("stunned", value: true);
				return;
			}
			creatureAnimator.SetBool("stunned", value: false);
			creatureAnimator.SetFloat("speedMultiplier", Mathf.Clamp(averageVelocity / 12f * 2.5f, 0.1f, 6f));
			float num = (base.transform.position - previousPosition).magnitude / (Time.deltaTime / 1.4f);
			if (velocityInterval <= 0f)
			{
				previousVelocity = averageVelocity;
				velocityInterval = 0.05f;
				velocityAverageCount += 1f;
				if (velocityAverageCount > 5f)
				{
					averageVelocity += (num - averageVelocity) / 3f;
				}
				else
				{
					averageVelocity += num;
					if (velocityAverageCount == 2f)
					{
						averageVelocity /= velocityAverageCount;
					}
				}
			}
			else
			{
				velocityInterval -= Time.deltaTime;
			}
			if (base.IsOwner && averageVelocity - num > Mathf.Clamp(num * 0.17f, 2f, 100f) && num > 3f && currentBehaviourStateIndex == 1)
			{
				if (wallCollisionSFXDebounce > 0.5f)
				{
					if (base.IsServer)
					{
						//CollideWithWallServerRpc();
					}
					else
					{
						//CollideWithWallClientRpc();
					}
				}
				agentSpeedWithNegative *= 0.2f;
				wallCollisionSFXDebounce = 0f;
			}
			wallCollisionSFXDebounce += Time.deltaTime;
			previousPosition = base.transform.position;
			if (currentBehaviourStateIndex == 0)
			{
				agent.speed = 6f;
				agent.acceleration = 26f;
			}
			else if (currentBehaviourStateIndex == 1)
			{
				//float num2 = SpeedIncreaseRate;
				//if (Time.realtimeSinceStartup - lastTimeHit < 1f)
				//{
				//	num2 += 4.25f;
				//}
				//agentSpeedWithNegative += Time.deltaTime * num2;
				//agent.speed = Mathf.Clamp(agentSpeedWithNegative, -3f, 16f);
				//agent.acceleration = Mathf.Clamp(BaseAcceleration - averageVelocity * SpeedAccelerationEffect, 4f, 40f);
				//if (agent.acceleration > 22f)
				//{
				//	agent.angularSpeed = 800f;
				//	agent.acceleration += 20f;
				//}
				//else
				//{
				//	agent.angularSpeed = 230f;
				//}
				//agent.speed = 5.5f;
				//agent.acceleration = 70f;
				if (!lostPlayerInChase)
                {
					if (isTelepathing)
					{
						if (timerToNextTelepathicAttack <= 0)
						{
							agent.speed = 0.1f;
							agent.acceleration = 200f;
						}
						else if (timerToNextTelepathicAttack <= telepathicAttackTime / 2 & timerToNextTelepathicAttack > 0 & !isSecondAttackCasted)
						{
							agent.speed = 0.1f;
							agent.acceleration = 200f;
						}
						else if (timerBeforeSecondAttack <= 0)
						{
							agent.speed = speedInChase;
							agent.acceleration = 70f;
						}


					}
					else
					{
						agent.speed = speedInChase;
						agent.acceleration = 70f;
					}
				}
				else
				{
					agent.speed = 6f;
					agent.acceleration = 26f;
				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void StopSearchingSoundServerRpc()
		{
			StopSearchingSoundClientRpc();
		}

		[ClientRpc]
		public void StopSearchingSoundClientRpc()
		{
			creatureSearchingSFX.Stop();
		}

		[ServerRpc(RequireOwnership = false)]
		public void PlaySearchingSoundServerRpc()
		{
			PlaySearchingSoundClientRpc();
		}

		[ClientRpc]
		public void PlaySearchingSoundClientRpc()
		{
			creatureSearchingSFX.Stop();
			creatureSearchingSFX.PlayOneShot(SearchingStageSFX);
			Debug.Log("Burer: Playing crying sound, because searching players.");
		}

		public void TelepathicAttack(int targetPlayerId)
		{
			PlayerControllerB currentTargetPlayer = StartOfRound.Instance.allPlayerScripts[targetPlayerId];
			if (isTelepathing)
			{
				if (timerToNextTelepathicAttack <= 0)
				{
					if (!isAttackingNowAsTelepathic)
					{
						if (!inSpecialAnimation)
						{
							TelepathicAttackAnimServerRpc((int)currentTargetPlayer.actualClientId);

						}
					}
					isAttackingNowAsTelepathic = true;
					base.transform.rotation = Quaternion.LookRotation(currentTargetPlayer.transform.position - base.transform.position);
					movingTowardsTargetPlayer = false;

					if (timerBeforeTelephaticAttack <= 0)
					{
						ExplosionAttackServerRpc((int)currentTargetPlayer.actualClientId);
						timerToNextTelepathicAttack = telepathicAttackTime;
						TelepathicAttackSoundServerRpc((int)currentTargetPlayer.actualClientId);
						isAttackingNowAsTelepathic = false;
						timerBeforeTelephaticAttack = 0.9f;
						isSecondAttackCasted = false;
					}
				}
				else if (timerToNextTelepathicAttack <= telepathicAttackTime / 2 & timerToNextTelepathicAttack > 0 & !isSecondAttackCasted)
				{
					isSecondAttackCasted = true;
					timerBeforeSecondAttack = 1f;
					movingTowardsTargetPlayer = false;
					base.transform.rotation = Quaternion.LookRotation(currentTargetPlayer.transform.position - base.transform.position);
					if (!inSpecialAnimation)
					{
						SecondAttackAnimServerRpc((int)currentTargetPlayer.actualClientId);
					}

					SecondAttackSoundServerRpc((int)currentTargetPlayer.actualClientId);
					if (DropItemsOnSecondAttack)
                    {
						currentTargetPlayer.DropAllHeldItemsAndSync();
					}
					currentTargetPlayer.sprintMeter = 0.2f;
				}
				else if (timerBeforeSecondAttack <= 0)
				{
					movingTowardsTargetPlayer = true;
				}

			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void ExplosionAttackServerRpc(int playerId)
        {
			ExplosionAttackClientRpc(playerId);
        }

		[ClientRpc]
		public void ExplosionAttackClientRpc(int playerId)
        {
			PlayerControllerB currentTargetPlayer = StartOfRound.Instance.allPlayerScripts[playerId];
			Landmine.SpawnExplosion(currentTargetPlayer.transform.position + currentTargetPlayer.transform.forward * 0.2f, spawnExplosionEffect: false, 0f, 3f, 0, 1f);
		}

		[ServerRpc(RequireOwnership = false)]
		public void CollideWithWallServerRpc()
		{
			CollideWithWallClientRpc();
		}

		[ClientRpc]
		public void CollideWithWallClientRpc()
		{
			{
				//RoundManager.PlayRandomClip(creatureSFX, hitWallSFX);
				float num = Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position);
				if (num < 15f)
				{
					HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
				}
				else if (num < 24f)
				{
					HUDManager.Instance.ShakeCamera(ScreenShakeType.Small);
				}
			}
		}
		private void CheckForVeryClosePlayer()
		{
			if (Physics.OverlapSphereNonAlloc(base.transform.position, 1.5f, nearPlayerColliders, 8, QueryTriggerInteraction.Ignore) > 0)
			{
				PlayerControllerB component = nearPlayerColliders[0].transform.GetComponent<PlayerControllerB>();
				if (component != null && component != targetPlayer && !Physics.Linecast(base.transform.position + Vector3.up * 0.3f, component.transform.position, StartOfRound.Instance.collidersAndRoomMask))
				{
					targetPlayer = component;
				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void BeginChasingPlayerServerRpc(int playerObjectId)
		{
			BeginChasingPlayerClientRpc(playerObjectId);
		}

		[ClientRpc]
		public void BeginChasingPlayerClientRpc(int playerObjectId)
		{
			MakeScreech();
			SwitchToBehaviourStateOnLocalClient(1);
			SetMovingTowardsTargetPlayer(StartOfRound.Instance.allPlayerScripts[playerObjectId]);
			isChasingOnPlayer = true;
		}

		[ServerRpc(RequireOwnership = false)]
		public void MakeScreechNoiseServerRpc()
		{
			MakeScreechNoiseClientRpc();
		}

		[ClientRpc]
		public void MakeScreechNoiseClientRpc()
		{
			MakeScreech();
		}

		private void MakeScreech()
		{
			int num = Random.Range(0, longRoarSFX.Length);
			creatureVoice.PlayOneShot(longRoarSFX[num]);
			WalkieTalkie.TransmitOneShotAudio(creatureVoice, longRoarSFX[num]);
			if (Vector3.Distance(GameNetworkManager.Instance.localPlayerController.transform.position, base.transform.position) < 15f)
			{
				GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(0.75f);
			}
		}

		public override void OnCollideWithPlayer(Collider other)
		{
			base.OnCollideWithPlayer(other);
			if (!(timeSinceHittingPlayer < 0.65f))
			{
				PlayerControllerB playerControllerB = MeetsStandardPlayerCollisionConditions(other);
				if (playerControllerB != null)
				{
					timeSinceHittingPlayer = 0f;
					base.transform.rotation = Quaternion.LookRotation(playerControllerB.transform.position - base.transform.position);

					agent.speed = 0f;
					playerControllerB.DamagePlayer(damagePerHitOnPlayer, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling);
					HitPlayerServerRpc((int)GameNetworkManager.Instance.localPlayerController.playerClientId);
					GameNetworkManager.Instance.localPlayerController.JumpToFearLevel(1f);

					if (currentBehaviourStateIndex == 1)
                    {
						if (!lostPlayerInChase && targetPlayer == playerControllerB)
                        {
							noticePlayerTimer = 0f;
						}
						else if (lostPlayerInChase)
                        {
							noticePlayerTimer = 0f;
							lostPlayerInChase = false;
							MakeScreechNoiseServerRpc();
							if (playerControllerB != targetPlayer)
							{
								SetMovingTowardsTargetPlayer(playerControllerB);
								ChangeOwnershipOfEnemy(playerControllerB.actualClientId);
							}
						}
                    }
					else
                    {
						beginningChasingThisClient = true;
						BeginChasingPlayerServerRpc((int)playerControllerB.actualClientId);
						ChangeOwnershipOfEnemy(playerControllerB.actualClientId);
					}

				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void SecondAttackSoundServerRpc(int playerId)
		{
			SecondAttackSoundClientRpc(playerId);
		}

		[ClientRpc]
		public void SecondAttackSoundClientRpc(int playerId)
		{
			{
				if (!inSpecialAnimation)
				{
					creatureVoice.PlayOneShot(hitCrawlerSFX[0]);
				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void SecondAttackAnimServerRpc(int playerId)
		{
			SecondAttackAnimClientRpc(playerId);
		}

		[ClientRpc]
		public void SecondAttackAnimClientRpc(int playerId)
		{
			{
				if (!inSpecialAnimation)
				{
					DoAnimationClientRpc("HitPlayer");
				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void TelepathicAttackSoundServerRpc(int playerId)
		{
			TelepathicAttackSoundClientRpc(playerId);
		}

		[ClientRpc]
		public void TelepathicAttackSoundClientRpc(int playerId)
		{
			{
				if (!inSpecialAnimation)
				{
					creatureVoice.PlayOneShot(bitePlayerSFX);
				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void TelepathicAttackAnimServerRpc(int playerId)
		{
			TelepathicAttackAnimClientRpc(playerId);
		}

		[ClientRpc]
		public void TelepathicAttackAnimClientRpc(int playerId)
		{
			{
				if (!inSpecialAnimation)
				{
					DoAnimationClientRpc("TelepathicAttack");
				}
			}
		}

		[ServerRpc(RequireOwnership = false)]
		public void HitPlayerServerRpc(int playerId)
		{
			HitPlayerClientRpc(playerId);
		}

		[ClientRpc]
		public void HitPlayerClientRpc(int playerId)
		{
			{
				if (!inSpecialAnimation)
				{
					creatureAnimator.SetTrigger("HitPlayer");
				}
				creatureVoice.PlayOneShot(bitePlayerSFX);
				agentSpeedWithNegative = Random.Range(-2f, 0.25f);
			}
		}
		//[ServerRpc(RequireOwnership = false)]
		//public void EatPlayerBodyServerRpc(int playerId)
		//{
		//	EatPlayerBodyClientRpc(playerId);
		//}

		//[ClientRpc]
		//public void EatPlayerBodyClientRpc(int playerId)
		//{
		//	if (!base.IsOwner && eatPlayerBodyCoroutine == null)
		//	{
		//		StartCoroutine(EatPlayerBodyAnimation(playerId));
		//	}
		//}
		//private IEnumerator EatPlayerBodyAnimation(int playerId)
		//{
		//	PlayerControllerB playerScript = StartOfRound.Instance.allPlayerScripts[playerId];
		//	float startTime = Time.realtimeSinceStartup;
		//	yield return new WaitUntil(() => (playerScript.deadBody != null && playerScript.deadBody.grabBodyObject != null) || Time.realtimeSinceStartup - startTime > 2f);
		//	DeadBodyInfo deadBody = null;
		//	if (StartOfRound.Instance.allPlayerScripts[playerId].deadBody != null)
		//	{
		//		if (debugEnemyAI)
		//		{
		//			Debug.Log("Burer: Body is not null!");
		//		}
		//		deadBody = StartOfRound.Instance.allPlayerScripts[playerId].deadBody;
		//	}
		//	yield return null;
		//	if (debugEnemyAI)
		//	{
		//		Debug.Log($"{deadBody != null}; {deadBody.grabBodyObject != null}; {!deadBody.isInShip}; {!deadBody.grabBodyObject.isHeld}; {Vector3.Distance(base.transform.position, deadBody.bodyParts[0].transform.position)}");
		//	}
		//	if (deadBody != null && deadBody.grabBodyObject != null && !deadBody.isInShip && !deadBody.grabBodyObject.isHeld && !isEnemyDead && Vector3.Distance(base.transform.position, deadBody.bodyParts[0].transform.position) < 6.7f)
		//	{
		//		creatureAnimator.SetTrigger("EatPlayer");
		//		creatureVoice.pitch = Random.Range(0.85f, 1.1f);
		//		creatureVoice.PlayOneShot(eatPlayerSFX);
		//		deadBody.canBeGrabbedBackByPlayers = false;
		//		currentlyHeldBody = deadBody;
		//		pullingSecondLimb = deadBody.attachedTo != null;
		//		if (pullingSecondLimb)
		//		{
		//			deadBody.secondaryAttachedLimb = deadBody.bodyParts[3];
		//			deadBody.secondaryAttachedTo = mouthTarget;
		//		}
		//		else
		//		{
		//			deadBody.attachedLimb = deadBody.bodyParts[0];
		//			deadBody.attachedTo = mouthTarget;
		//		}
		//		yield return new WaitForSeconds(2.75f);
		//	}
		//	Debug.Log("Burer: leaving special animation");
		//	inSpecialAnimation = false;
		//	DropPlayerBody();
		//	eatPlayerBodyCoroutine = null;
		//}

		//private void DropPlayerBody()
		//{
		//	if (currentlyHeldBody != null)
		//	{
		//		if (pullingSecondLimb)
		//		{
		//			currentlyHeldBody.secondaryAttachedLimb = null;
		//			currentlyHeldBody.secondaryAttachedTo = null;
		//		}
		//		else
		//		{
		//			currentlyHeldBody.attachedLimb = null;
		//			currentlyHeldBody.attachedTo = null;
		//		}
		//	}
		//}

		public override void KillEnemy(bool destroy = false)
		{
			base.KillEnemy();
			//if (eatPlayerBodyCoroutine != null)
			//{
			//	StopCoroutine(eatPlayerBodyCoroutine);
			//}
			//DropPlayerBody();
		}

		public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
		{
			base.HitEnemy(force, playerWhoHit, playHitSFX, hitID);
			if (!isEnemyDead)
			{
				agent.speed = 2f;
				if (!inSpecialAnimation)
				{
					creatureAnimator.SetTrigger("HurtEnemy");
				}
				enemyHP -= force;
				agentSpeedWithNegative = Random.Range(-2.8f, -2f);
				lastTimeHit = Time.realtimeSinceStartup;
				averageVelocity = 0f;
				RoundManager.PlayRandomClip(creatureVoice, hitCrawlerSFX);
				if (enemyHP <= 0 && base.IsOwner)
				{
					KillEnemyOnOwnerClient();
				}
				if (enemyHP > 0)
                {
					if (playerWhoHit != null)
                    {
						if (currentBehaviourStateIndex == 1)
						{
							if (!lostPlayerInChase && targetPlayer == playerWhoHit)
							{
								noticePlayerTimer = 0f;
							}
							else if (lostPlayerInChase)
							{
								noticePlayerTimer = 0f;
								lostPlayerInChase = false;
								MakeScreechNoiseServerRpc();
								if (playerWhoHit != targetPlayer)
								{
									SetMovingTowardsTargetPlayer(playerWhoHit);
									ChangeOwnershipOfEnemy(playerWhoHit.actualClientId);
								}
							}
						}
						else
						{
							beginningChasingThisClient = true;
							BeginChasingPlayerServerRpc((int)playerWhoHit.actualClientId);
							ChangeOwnershipOfEnemy(playerWhoHit.actualClientId);
						}
					}
                }
			}
		}

		[ClientRpc]
		public void DoAnimationClientRpc(string animationName)
		{
			creatureAnimator.SetTrigger(animationName);
		}
	}
}


