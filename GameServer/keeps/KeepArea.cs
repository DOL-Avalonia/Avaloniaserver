using DOL.GS.PacketHandler;
using DOL.Database;

namespace DOL.GS.Keeps
{
    public class KeepArea : Area.Circle
    {
        public AbstractGameKeep Keep = null;
        private const int PK_RADIUS = 4000;
        private const int KEEP_RADIUS = 3000;
        private const int TOWER_RADIUS = 1500;

        public KeepArea()
            : base()
        {
            DisplayMessage = ServerProperties.Properties.NOTIFY_KEEP_AREA_MESSAGE;
        }

        public KeepArea(AbstractGameKeep keep)
            : base(keep.Name, keep.X, keep.Y, 0, keep.IsPortalKeep ? PK_RADIUS : (keep is GameKeepTower ? TOWER_RADIUS : KEEP_RADIUS)) {
            Keep = keep;
            DisplayMessage = ServerProperties.Properties.NOTIFY_KEEP_AREA_MESSAGE;
        }

        public override void OnPlayerEnter(GamePlayer player)
        {
            // [Ganrod] Nidel: NPE
            if (player == null || Keep == null)
            {
                return;
            }

            base.OnPlayerEnter(player);
            if (Keep.Guild != null)
            {
                player.Out.SendMessage("Controlled by " + Keep.Guild.Name + ".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        public void ChangeRadius(int newRadius)
        {
            GameServer.KeepManager.Log.Debug("ChangeRadius called for " + Keep.Name + " currently is " + Radius + " changing to " + newRadius);

            // setting radius to default
            if (newRadius == 0 && Radius != 0)
            {
                if (DbArea != null)
                {
                    GameServer.Database.DeleteObject(DbArea);
                }

                Radius = Keep is GameKeep ? (Keep.IsPortalKeep ? PK_RADIUS : KEEP_RADIUS) : TOWER_RADIUS;
                return;
            }

            // setting different radius when radius was already something
            if (newRadius > 0 && Radius >= 0)
            {
                Radius = newRadius;
                if (DbArea != null)
                {
                    DbArea.Radius = Radius;
                    GameServer.Database.SaveObject(DbArea);
                }
                else
                {
                    DbArea = new DBArea();
                    DbArea.CanBroadcast = CanBroadcast;
                    DbArea.CheckLOS = CheckLOS;
                    DbArea.ClassType = GetType().ToString();
                    DbArea.Description = Description;
                    DbArea.Radius = Radius;
                    DbArea.Region = (ushort)Keep.Region;
                    DbArea.Sound = Sound;
                    DbArea.X = X;
                    DbArea.Y = Y;
                    DbArea.Z = Z;

                    GameServer.Database.AddObject(DbArea);
                }
            }
        }

        public override void LoadFromDatabase(DBArea area)
        {
            base.LoadFromDatabase(area);
            GameServer.KeepManager.Log.Debug("KeepArea " + area.Description + " LoadFromDatabase called");
            GameServer.KeepManager.Log.Debug("X: " + area.X + "(" + X + ") Y: " + area.Y + "(" + Y + ") Region:" + area.Region + " Radius: " + Radius);
        }
    }
}
