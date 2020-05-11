using DOL.Database;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
{
    [SpellHandler("CombineItem")]
    public class CombineItemSpellHandler
        : SpellHandler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CombineItemSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine)
            : base(caster, spell, spellLine)
        {
        }

        /// <summary>
        /// Check whether it's actually possible to do the combine.
        /// </summary>
        /// <param name="selectedTarget"></param>
        /// <returns></returns>
        public override bool CheckBeginCast(GameLiving selectedTarget)
        {
            if (!base.CheckBeginCast(selectedTarget))
            {
                return false;
            }

            if (!(Caster is GamePlayer player))
            {
                return false;
            }

            InventoryItem usedItem = player.UseItem;
            if (usedItem == null)
            {
                return false;
            }

            var neededItems = this.GetCombinableItems(usedItem.Id_nb);     
            
            if (neededItems == null || !neededItems.Any())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Do the combine.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="effectiveness"></param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (!(Caster is GamePlayer player))
            {
                return;
            }

            InventoryItem useItem = player.UseItem;
            if (useItem == null)
            {
                return;
            }

            var neededItems = this.GetCombinableItems(useItem.Id_nb);

            if (neededItems == null || !neededItems.Any())
            {
                return;
            }

            List<InventoryItem> removeItems = new List<InventoryItem>();         

            var backpack = player.Inventory.GetItemRange(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
            Combinable match = null;

            foreach (var combinable in neededItems)
            {
                List<string> ids = new List<string>();

                foreach (InventoryItem item in backpack)
                {
                    if (item != null && combinable.Items.Contains(item.Id_nb))
                    {
                        if (!ids.Contains(item.Id_nb))
                        {
                            ids.Add(item.Id_nb);
                            removeItems.Add(item);
                        }    
                    }
                }

                if (ids.Count == combinable.Items.Count())
                {
                    match = combinable;
                    break;
                }

                removeItems.Clear();
            }

            if (match == null)
            {
                return;
            }

            removeItems.Add(useItem);


            player.Out.SendSpellEffectAnimation(player, player, (ushort)match.SpellEfect , 0, false, 1);

            var combined =  WorldInventoryItem.CreateFromTemplate(match.TemplateId);

            if (combined == null)
            {
                log.Warn($"Missing item in ItemTemplate table '{match.TemplateId}' for CombineItem spell");
                return;
            }     

            if (player.ReceiveItem(player, combined))
            {
                foreach (InventoryItem item in removeItems)
                {
                    if (item.OwnerID == null)
                        item.OwnerID = player.InternalID;
                
                    player.Inventory.RemoveItem(item);
                }

                player.Out.SendMessage($"Vous avez créé {combined.Name } en combinant {useItem.Name} ainsi que { match.Items.Count() -1 } autres objects.", eChatType.CT_Skill, eChatLoc.CL_SystemWindow);
            }
        }

        private IEnumerable<Combinable> GetCombinableItems(string usedItemId)
        {
            var cbitems = GameServer.Database.SelectAllObjects<CombineItemDb>();

            if (cbitems == null || !cbitems.Any())
            {
                return null;
            }

            var neededItems = cbitems.Select(c => new Combinable() { Items = c.ItemsIds.Split(new char[] { '|' }), TemplateId = c.ItemTemplateId, SpellEfect = c.SpellEffect });

            return neededItems.Where(i => i.Items.Contains(usedItemId));
        }
    }


    public class Combinable
    {
        public IEnumerable<string> Items { get; set; }

        public string TemplateId { get; set; }

        public int SpellEfect { get; set; }
    }

}


    

