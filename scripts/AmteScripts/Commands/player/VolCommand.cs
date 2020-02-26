using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.SkillHandler;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Commands
{
	[CmdAttribute(
	  "&vol",
	  ePrivLevel.Player,
	  "Permet de voler un joueur",
	  "/vol [joueur]")]
	public class VolCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public const int MIN_VOL_TIME = 5; // Secondes
		public const int MAX_VOL_TIME = 15;
		public const string PLAYER_STEALER = "vol_player_stealer";
		public const string TARGET_STOLE = "vol_target_stole";
		public const string PLAYER_VOL_TIMER = "player_vol_timer";


		public static void VolHandlerOnPlayerMove(GamePlayer player)
		{
			if (player != null)
			{
				GamePlayer Source = player.TempProperties.getProperty<object>(PLAYER_STEALER, null) as GamePlayer;
				if (Source != null)
				{
					if (!CanVol(Source, player))
						CancelVol(Source);
				}
				else
				{
					RegionTimer Timer = player.TempProperties.getProperty<object>(PLAYER_VOL_TIMER, null) as RegionTimer;
					if (Timer != null)
					{
						player.Out.SendMessage("Votre vol a échoué car vous avez bougé !",
							eChatType.CT_Important, eChatLoc.CL_SystemWindow);

						Timer.Stop();

						player.Out.SendCloseTimerWindow();
						player.TempProperties.removeProperty(PLAYER_VOL_TIMER);
					}
				}
			}
		}

		public static bool CanVol(IGamePlayer stealer, IGamePlayer target)
		{
			if (stealer.Level < 25 || target.Level < 20)
			{
				return false;
			}

			return true;
		}


		public static VolResult Vol(IGamePlayer stealer, IGamePlayer target)
		{
			var result = new VolResult();
			int deltaLevel = Math.Abs(stealer.Level - target.Level);
			if (deltaLevel > 20)
			{
				result.Status = VolResultStatus.STEALTHLOST;				
				
			}else if (deltaLevel > 10 && deltaLevel < 20)
			{
				result.Status = VolResultStatus.FAILED;
			}
			else
			{

			}

			return result;	
		}

		public static void CancelVol(GamePlayer Player)
		{
			CancelVol(Player,
				Player.TempProperties.getProperty<object>(PLAYER_VOL_TIMER, null) as RegionTimer);
		}

		public static void CancelVol(GamePlayer Player, RegionTimer Timer)
		{
			Player.TempProperties.setProperty(
				VolAbilityHandler.DISABLE_PROPERTY,
				Player.CurrentRegion.Time);
			Player.DisableSkill(SkillBase.GetAbility(Abilities.Vol),
				VolAbilityHandler.DISABLE_DURATION);

			Timer.Stop();

			Player.Out.SendCloseTimerWindow();

			(Timer.Properties.getProperty<object>(TARGET_STOLE, null) as GamePlayer).TempProperties.removeProperty(PLAYER_STEALER);

			Player.TempProperties.removeProperty(PLAYER_VOL_TIMER);
		}

		public void OnCommand(GameClient client, string[] args)
		{
			GamePlayer Player = client.Player;
			if (Player.IsMezzed)
			{
				Player.Out.SendMessage("Vous ne pouvez voler étant hypnotisé !",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			if (Player.IsStunned)
			{
				Player.Out.SendMessage("Vous ne pouvez voler étant assomé !",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (Player.PlayerAfkMessage != null)
			{
				Player.Out.SendMessage("Vous ne pouvez voler lorsque vous " +
					"êtes afk ! Tapez /afk pour le désactiver.",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			if (!Player.IsAlive)
			{
				Player.Out.SendMessage("Vous ne pouvez voler étant mort !",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			
			if (Player.TempProperties.getProperty<object>(PLAYER_VOL_TIMER, null) != null)
			{
				Player.Out.SendMessage("Vous êtes déjà en train de voler quelqu'un !",
					eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				return;
			}
			

			long VolChangeTick = Player.TempProperties.getProperty<long>(
				VolAbilityHandler.DISABLE_PROPERTY, 0L);
			long ChangeTime = Player.CurrentRegion.Time - VolChangeTick;
			if (ChangeTime < VolAbilityHandler.DISABLE_DURATION)
			{
				Player.Out.SendMessage("Vous devez attendre " +
					((VolAbilityHandler.DISABLE_DURATION - ChangeTime) / 1000).ToString() +
					" secondes avant de pouvoir voler à nouveau !",
					eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			GamePlayer Target = Player.TargetObject as GamePlayer;
			if (Target == null && args.Length >= 2)
			{
				Target = WorldMgr.GetClientByPlayerName(args[1],
					false, true).Player;
			}

			if (CanVol(Player, Target))
			{
				int VolTime = Util.Random(MIN_VOL_TIME, MAX_VOL_TIME);

				string RealName = Target.GetName(Player);
				Player.Out.SendMessage("Vous commencez à voler " + RealName,
					eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				Player.Out.SendTimerWindow("Vous êtes actuellement en train " +
						" de voler " + RealName, VolTime);

				RegionTimer Timer = new RegionTimer(Player);
				Timer.Callback = new RegionTimerCallback(VolTarget);
				Timer.Properties.setProperty(PLAYER_STEALER, Player);
				Timer.Properties.setProperty(TARGET_STOLE, Target);
				Timer.Start(VolTime * 1000);

				Target.TempProperties.setProperty(PLAYER_STEALER, Player);
				Player.TempProperties.setProperty(PLAYER_VOL_TIMER, Timer);

				Player.TempProperties.setProperty(
					VolAbilityHandler.DISABLE_PROPERTY,
					Player.CurrentRegion.Time);
			}
			else
			{
				Player.Out.SendMessage("Vous ne pouvez volez ce personnage !",
					eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
		}

		public int VolTarget(RegionTimer Timer)
		{
			GamePlayer stealer = (GamePlayer)Timer.Properties.getProperty<object>(PLAYER_STEALER, null);
			GamePlayer target = (GamePlayer)Timer.Properties.getProperty<object>(TARGET_STOLE, null);

			VolResult result = Vol(stealer, target);
			if (result.Status == VolResultStatus.STEALTHLOST && stealer.IsStealthed)
			{
				stealer.Stealth(false);
				CancelVol(stealer, Timer);
			}		
			else if (result.Status == VolResultStatus.FAILED)
			{
				stealer.Out.SendMessage("Vous n'avez pas réussi à voler ce personnage !",
					eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				CancelVol(stealer, Timer);
			}
			else
			{
				PerformVolAction(stealer, target, result);
			}
			
			return 0;
		}

		private void PerformVolAction(GamePlayer stealer, GamePlayer target, VolResult vol)
		{
			if (vol.Status == VolResultStatus.SUCCESS_MONEY)
			{
				target.RemoveMoney(vol.Money);
				target.Out.SendMessage("Vous venez d'etre dérobé de la somme de " + Money.GetString(vol.Money), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

			}else if (vol.Status == VolResultStatus.SUSSCES_ITEM)
			{		

				if (!stealer.Inventory.IsSlotsFree(1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					stealer.Out.SendMessage("Votre inventaire est plein, il va etre difficle de voler quelque chose", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				}
				else
				{
					int index = new Random(2).Next(target.Inventory.VisibleItems.Count);
					var item = target.Inventory.VisibleItems.ElementAt(index);
					target.Inventory.RemoveItem(item);

					if (!stealer.Inventory.AddItem(eInventorySlot.FirstBackpack, item))
					{
						stealer.Inventory.AddItem(eInventorySlot.LastBackpack, item);
					}
				}
			}
		}
	}


	public enum VolResultStatus
	{
		SUCCESS_MONEY,
		SUSSCES_ITEM,
		FAILED,
		STEALTHLOST
	}

	public class VolResult
	{
		public VolResultStatus Status { get; set; }

		public long Money { get; set; }

	}
}
