using DOL.GameEvents;
using DOL.GS.PacketHandler;
using DOLDatabase.Tables;
using System;
using System.Linq;

namespace DOL.GS
{
    public class MoneyEventNPC
        : GameNPC
    {
        private string Id;
        public readonly string InteractDefault = "MoneyEventNPC.InteractTextDefault";
        public readonly string ValidateTextDefault = "MoneyEventNPC.ValidateTextDefault";
        public readonly string NeedMoreMoneyTextDefault = "MoneyEventNPC.NeedMoreMoneyTextDefault";

        public MoneyEventNPC()
        : base()
        {
        }

        public long CurrentMoney => Money.GetMoney(this.CurrentMithril, CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper);

        public int CurrentMithril
        {
            get;
            set;
        }

        public int CurrentGold
        {
            get;
            set;
        }

        public int CurrentPlatinum
        {
            get;
            set;
        }

        public int CurrentSilver
        {
            get;
            set;
        }

        public int CurrentCopper
        {
            get;
            set;
        }   

        public string ServingEventID
        {
            get;
            set;
        }

        public long RequiredMoney
        {
            get;
            set;
        }

        public string NeedMoreMoneyText
        {
            get;
            set;
        }

        public string ValidateText
        {
            get;
            set;
        }

        public string InteractText
        {
            get;
            set;
        }

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            if (this.CheckEventValidity() == null)
                return false;


            TurnTo(player, 5000);
            string currentMoney = Money.GetString(Money.GetMoney(this.CurrentMithril, this.CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper));
            string text = InteractText ?? Language.LanguageMgr.GetTranslation(player.Client.Account.Language, InteractDefault, Money.GetString(this.RequiredMoney), currentMoney);
            player.Out.SendMessage(text, eChatType.CT_System, eChatLoc.CL_PopupWindow);

            return true;
        }

        public override bool ReceiveMoney(GameLiving source, long money)
        {
            var player = source as GamePlayer;

            if (player == null)
                return base.ReceiveMoney(source, money);

            var ev = this.CheckEventValidity();

            if (ev == null)
                return base.ReceiveMoney(source, money);


            this.CurrentGold += Money.GetGold(money);
            this.CurrentPlatinum += Money.GetPlatinum(money);
            this.CurrentMithril += Money.GetMithril(money);
            this.CurrentSilver += Money.GetSilver(money);
            this.CurrentCopper += Money.GetCopper(money);

            if (CurrentMoney >= RequiredMoney)
            {
                var text = ValidateText ?? Language.LanguageMgr.GetTranslation(player.Client.Account.Language, ValidateTextDefault);
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
                GameEventManager.Instance.StartEvent(ev);
            }
            else
            {
                var text = NeedMoreMoneyText ?? Language.LanguageMgr.GetTranslation(player.Client.Account.Language, NeedMoreMoneyTextDefault);
                player.Client.Out.SendMessage(text, eChatType.CT_Chat, eChatLoc.CL_PopupWindow);
            }

            player.RemoveMoney(money);

            return true;
        }

        private GameEvent CheckEventValidity()
        {
            if (ServingEventID == null)
                return null;

            var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(ServingEventID));

            if (ev == null || ev.StartConditionType != StartingConditionType.Money || ev.StartedTime.HasValue)
            {
                return null;
            }

            return ev;
        }

        public override void LoadFromDatabase(Database.DataObject obj)
        {
            base.LoadFromDatabase(obj);

            var eventNpc = GameServer.Database.SelectObjects<MoneyNpcDb>("`MobID` = @MobID", new Database.QueryParameter("MobID", obj.ObjectId))?.FirstOrDefault();
            
            if (eventNpc != null)
            {
                this.Id = eventNpc.ObjectId;
                this.CurrentGold = Money.GetGold(eventNpc.CurrentAmount);
                this.CurrentCopper = Money.GetCopper(eventNpc.CurrentAmount);
                this.CurrentMithril = Money.GetMithril(eventNpc.CurrentAmount);
                this.CurrentPlatinum = Money.GetPlatinum(eventNpc.CurrentAmount);
                this.CurrentSilver = Money.GetSilver(eventNpc.CurrentAmount);
                this.ServingEventID = eventNpc.EventID;
                this.RequiredMoney = eventNpc.RequiredMoney;
                this.NeedMoreMoneyText = eventNpc.NeedMoreMoneyText;
                this.ValidateText = eventNpc.ValidateText;
                this.InteractText = eventNpc.InteractText;
            }
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            return eQuestIndicator.Lesson;
        }


        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();

            MoneyNpcDb db = null;

            if (Id == null)
            {
                db = new MoneyNpcDb();  
            }
            else
            {
                db = GameServer.Database.FindObjectByKey<MoneyNpcDb>(Id);
            }
            
            if (db != null)
            {
                db.CurrentAmount = Money.GetMoney(CurrentMithril, CurrentPlatinum, CurrentGold, CurrentSilver, CurrentCopper);
                db.EventID = ServingEventID ?? string.Empty;
                db.RequiredMoney = RequiredMoney;
                db.MobID = this.InternalID;
                db.MobName = this.Name;

                if (InteractText != null)
                    db.InteractText = InteractText;

                if (NeedMoreMoneyText != null)
                    db.NeedMoreMoneyText = NeedMoreMoneyText;

                if (ValidateText != null)
                    db.ValidateText = ValidateText;
            }

            if (Id == null)
            {
                GameServer.Database.AddObject(db);
                Id = db.ObjectId;
            }
            else
            {
                GameServer.Database.SaveObject(db);
            }
        }

    }
}
