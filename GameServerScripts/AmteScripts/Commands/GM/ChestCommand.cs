﻿using System;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Commands;
using System.Linq;

namespace DOL.GS.Scripts
{
    [CmdAttribute(
         "&chest",
         ePrivLevel.GM,
         "Commands.GM.Chest.Description",
         "Commands.GM.Chest.Usage.Create",
         "Commands.GM.Chest.Usage.Model",
         "Commands.GM.Chest.Usage.Item",
         "Commands.GM.Chest.Usage.Add",
         "Commands.GM.Chest.Usage.Remove",
         "Commands.GM.Chest.Usage.Name",
         "Commands.GM.Chest.Usage.Movehere",
         "Commands.GM.Chest.Usage.Delete",
         "Commands.GM.Chest.Usage.Reset",
         "Commands.GM.Chest.Usage.Info",
         "Commands.GM.Chest.Usage.Copy",
         "Commands.GM.Chest.Usage.RandomCopy",
         "Commands.GM.Chest.Usage.Key",
         "Commands.GM.Chest.Usage.Difficult",
         "Commands.GM.Chest.Usage.traprate",
         "Commands.GM.Chest.Usage.NPCTemplate",
         "Commands.GM.Chest.Usage.Respawn",
         "Commands.GM.Chest.Usage.IsTeleport",
         "Commands.GM.Chest.Usage.Teleporter",
         "Commands.GM.Chest.Usage.TPrequirement",
         "Commands.GM.Chest.Usage.TPEffect",
         "Commands.GM.Chest.Usage.TPIsRenaissance",
         "Commands.GM.Chest.Usage.IsOpeningRenaissance",
         "Commands.GM.Chest.Usage.PunishSpellId",
         "Commands.GM.Chest.Usage.PickableAnim",
         "Commands.GM.Chest.Usage.InfoInterval",
         "Commands.GM.Chest.Usage.LongDistance")]
    public class ChestCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client.Player == null) return;
            GamePlayer player = client.Player;

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            GameCoffre coffre = player.TargetObject as GameCoffre;
            switch (args[1].ToLower())
            {
                #region create - model
                case "create":
                    coffre = new GameCoffre
                    {
                        Name = "Coffre",
                        Model = 1596,
                        X = player.X,
                        Y = player.Y,
                        Z = player.Z,
                        Heading = player.Heading,
                        CurrentRegionID = player.CurrentRegionID,
                        ItemInterval = 60,
                        ItemChance = 100
                    };
                    coffre.InitTimer();
                    coffre.LoadedFromScript = false;
                    coffre.AddToWorld();
                    coffre.SaveIntoDatabase();
                    ChatUtil.SendSystemMessage(client, "Vous avez créé un coffre (OID:" + coffre.ObjectID + ")");
                    break;

                case "model":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.Model = (ushort)int.Parse(args[2]);
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le model " + coffre.Model);
                    break;
                #endregion

                case "interval":
                    int min = 0;

                    if (coffre != null && args.Length == 3 && int.TryParse(args[2], out min) && min >= 0)
                    {
                        coffre.CoffreOpeningInterval = min;
                        coffre.SaveIntoDatabase();
                        ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant l'interval d'ouverture de " + min + " minutes");
                    }
                    else
                    {
                        DisplaySyntax(client);                        
                    }

                    break;

                case "longdistance":
                    bool isLongDitance = false;

                    if (coffre != null && args.Length == 3 && bool.TryParse(args[2], out isLongDitance))
                    {
                        coffre.IsLargeCoffre = isLongDitance;
                        coffre.SaveIntoDatabase();
                        ChatUtil.SendSystemMessage(client, "La valeur LongDistance du coffre \"" + coffre.Name + " est maintenant de: " + isLongDitance);
                    }
                    else
                    {
                        DisplaySyntax(client);
                    }

                    break;

                #region item - add - remove
                case "item":
                    if (coffre == null || args.Length < 4)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.ItemChance = int.Parse(args[2]);
                        coffre.ItemInterval = int.Parse(args[3]);
                        coffre.InitTimer();
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant " + coffre.ItemChance + " chances de faire apparaitre un item toutes les " + coffre.ItemInterval + " minutes.");
                    break;

                case "add":
                    if (coffre == null || args.Length < 4)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        if (!coffre.ModifyItemList(args[2], int.Parse(args[3])))
                        {
                            ChatUtil.SendSystemMessage(client, "L'item \"" + args[2] + "\" n'existe pas !");
                            break;
                        }
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" peut faire apparaitre un item \"" + args[2] + "\" avec un taux de chance de " + args[3]);
                    break;
                case "remove":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        if (!coffre.DeleteItemFromItemList(args[2]))
                        {
                            ChatUtil.SendSystemMessage(client, "L'item \"" + args[2] + "\" n'existe pas !");
                            break;
                        }
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" ne peut plus faire apparaitre l'item \"" + args[2] + "\"");
                    break;
                #endregion

                #region name - movehere - delete
                case "name":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    coffre.Name = args[2];
                    coffre.SaveIntoDatabase();
                    break;

                case "movehere":
                    if (coffre == null)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    coffre.X = player.X;
                    coffre.Y = player.Y;
                    coffre.Z = player.Z;
                    coffre.Heading = player.Heading;
                    coffre.SaveIntoDatabase();
                    ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été déplacé à votre position.");
                    break;

                case "delete":
                    if (coffre == null)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    coffre.Delete();
                    coffre.DeleteFromDatabase();
                    ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été supprimé.");
                    break;
                #endregion

                #region reset - info
                case "reset":
                    if (coffre == null)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    GameCoffre oldCoffre = coffre;
                    coffre = new GameCoffre();
                    coffre.Name = "Coffre";
                    coffre.Model = 1596;
                    coffre.X = oldCoffre.X;
                    coffre.Y = oldCoffre.Y;
                    coffre.Z = oldCoffre.Z;
                    coffre.Heading = oldCoffre.Heading;
                    coffre.CurrentRegionID = oldCoffre.CurrentRegionID;
                    coffre.ItemInterval = 60;
                    coffre.ItemChance = 100;
                    coffre.LastOpen = DateTime.MinValue;
                    oldCoffre.RemoveFromWorld();
                    oldCoffre.DeleteFromDatabase();
                    coffre.AddToWorld();

                    coffre.SaveIntoDatabase();
                    ChatUtil.SendSystemMessage(client, "Le coffre selectionné a été remit à zéro.");
                    break;

                case "info":
                    if (coffre == null)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    player.Out.SendCustomTextWindow(coffre.Name, coffre.DelveInfo());
                    break;
                #endregion

                #region copy - randomcopy
                case "randomcopy":
                case "copy":
                    if (coffre == null)
                    {
                        DisplaySyntax(client);
                        break;
                    }

                    GameCoffre coffre2;
                    if (args[1].ToLower() == "randomcopy")
                    {
                        List<GameCoffre.CoffreItem> items = new List<GameCoffre.CoffreItem>(coffre.Items);
                        foreach (GameCoffre.CoffreItem item in items)
                        {
                            if (Util.Chance(50))
                                item.Chance += (int)(item.Chance * Util.RandomDouble() / 10);
                            else if (item.Chance > 1)
                            {
                                item.Chance -= (int)(item.Chance * Util.RandomDouble() / 10);
                                if (item.Chance < 1)
                                    item.Chance = 1;
                            }
                        }
                        coffre2 = new GameCoffre(items);
                    }
                    else
                        coffre2 = new GameCoffre(coffre.Items);

                    coffre2.Name = coffre.Name + "_cpy";
                    coffre2.X = player.X;
                    coffre2.Y = player.Y;
                    coffre2.Z = player.Z;
                    coffre2.Heading = player.Heading;
                    coffre2.CurrentRegion = player.CurrentRegion;
                    coffre2.Model = coffre.Model;
                    coffre2.ItemInterval = coffre.ItemInterval;
                    coffre2.TpEffect = coffre.TpEffect;
                    coffre2.TpIsRenaissance = coffre.TpIsRenaissance;
                    coffre2.TpLevelRequirement = coffre.TpLevelRequirement;
                    coffre2.TpX = coffre.TpX;
                    coffre2.TpY = coffre.TpY;
                    coffre2.TpZ = coffre.TpZ;
                    coffre2.TpRegion = coffre.TpRegion;
                    coffre2.TrapRate = coffre.TrapRate;
                    coffre2.NpctemplateId = coffre.NpctemplateId;
                    coffre2.PunishSpellId = coffre.PunishSpellId;
                    coffre2.IsOpeningRenaissanceType = coffre.IsOpeningRenaissanceType;
                    coffre2.IsTeleporter = coffre.IsTeleporter;
                    coffre2.KeyItem = coffre.KeyItem;
                    coffre2.InitTimer();    

                    coffre2.ItemChance = coffre.ItemChance;
                    if (args[1].ToLower() == "randomcopy")
                    {
                        if (Util.Chance(50))
                            coffre2.ItemInterval += (int)(coffre2.ItemInterval * Util.RandomDouble() / 10);
                        else if (coffre2.ItemInterval > 1)
                        {
                            coffre2.ItemInterval -= (int)(coffre2.ItemInterval * Util.RandomDouble() / 10);
                            if (coffre2.ItemInterval < 1)
                                coffre2.ItemInterval = 1;
                        }
                        if (Util.Chance(50) && coffre2.ItemChance < 100)
                        {
                            coffre2.ItemChance += (int)(coffre2.ItemChance * Util.RandomDouble() / 10);
                            if (coffre2.ItemChance > 100)
                                coffre2.ItemChance = 100;
                        }
                        else if (coffre2.ItemChance > 0)
                        {
                            coffre2.ItemChance -= (int)(coffre2.ItemChance * Util.RandomDouble() / 10);
                            if (coffre2.ItemChance < 0)
                                coffre2.ItemChance = 0;
                        }
                    }
                    coffre2.AddToWorld();
                    coffre2.SaveIntoDatabase();
                    ChatUtil.SendSystemMessage(client, "Vous avez créé un coffre (OID:" + coffre2.ObjectID + ")");
                    break;
                #endregion

                #region key - difficult
                case "key":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    if (args[2].ToLower() == "nokey")
                        coffre.KeyItem = "";
                    else
                    {
                        if (GameServer.Database.SelectObject<ItemTemplate>("Id_nb = '" + GameServer.Database.Escape(args[2]) + "'") == null)
                        {
                            ChatUtil.SendSystemMessage(client, "La clef ayant l'id_nb \"" + args[2] + "\" n'existe pas.");
                            break;
                        }
                        coffre.KeyItem = args[2];
                    }
                    coffre.SaveIntoDatabase();

                    if (coffre.KeyItem == "")
                        ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" n'a plus besoin de clef pour être ouvert.");
                    else
                        ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a besoin de la clef ayant l'id_nb \"" + coffre.KeyItem + "\" pour être ouvert.");
                    break;

                case "difficult":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        int num = int.Parse(args[2]);
                        if (num >= 100)
                            num = 99;
                        if (num < 0)
                            num = 0;
                        coffre.LockDifficult = num;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    if (coffre.LockDifficult > 0)
                        ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant une difficulté pour être crocheter de " + coffre.LockDifficult + "%.");
                    else
                        ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" ne peut plus être crocheté.");
                    break;

                case "traprate":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.TrapRate = int.Parse(args[2]);
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le traprate de " + coffre.TrapRate);
                    break;

                case "npctemplate":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.NpctemplateId = args[2];
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + coffre.Name + "\" a maintenant le npctemplate de " + coffre.NpctemplateId);
                    break;

                case "isteleporter":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.IsTeleporter = !coffre.IsTeleporter;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut IsTeleporter du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.IsTeleporter);
                    break;


                case "pickableanim":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.HasPickableAnim = !coffre.HasPickableAnim;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut HasPickbleAnim du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.HasPickableAnim);
                    break;

                case "isopeningrenaissance":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.IsOpeningRenaissanceType = !coffre.IsOpeningRenaissanceType;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut IsOpeningRenaissanceType du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.IsOpeningRenaissanceType);
                    break;

                case "respawn":
                    if (args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    GameCoffre selectedCoffre = null;
                    try
                    {
                        string name = args[2];

                        if (name != null)
                        {
                            var coffres = GameCoffre.Coffres.Where(c => c.Name.Equals(name));

                            if (coffres != null && coffres.Count() == 1)
                            {
                                selectedCoffre = coffres.First();
                            }                           
                        }                   

                        if (selectedCoffre == null)
                        {
                            ChatUtil.SendSystemMessage(client, "Le coffre \"" + name + "\" n'a pas été trouvé ou plusieurs coffres avec le meme nom existent.");
                            break;
                        }
                        else
                        {
                            selectedCoffre.RespawnTimer.Stop();
                            selectedCoffre.RespawnCoffre();
                        }                     
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le coffre \"" + selectedCoffre.Name + "\" apparait devant vous.");
                    break;

                case "teleporter":
                    if (coffre == null || args.Length <= 6)
                    {
                        DisplaySyntax(client);
                        return;
                    }
                    int X;
                    int Y;
                    int Z;
                    ushort heading;
                    ushort RegionID;
                    try
                    {
                        X = int.Parse(args[2]);
                        Y = int.Parse(args[3]);
                        Z = int.Parse(args[4]);         
                        heading = (ushort)int.Parse(args[5]);
                        RegionID = (ushort)int.Parse(args[6]);
                    }
                    catch { DisplaySyntax(client); return; }

                    coffre.TpX = X;
                    coffre.TpY = Y;
                    coffre.TpZ = Z;
                    coffre.TpRegion = RegionID;
                    coffre.TPHeading = heading;
                    coffre.SaveIntoDatabase();
                    player.Out.SendMessage("Le Coffre \"" + coffre + "\" a recu une nouvelle destination de téléportation",  PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                    
                    if (!coffre.IsTeleporter)
                    {
                        player.Out.SendMessage("Le statut IsTeleporter du Coffre \"" + coffre + "\" n'est pas activé, changez le pour activer la téléportation.", PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                    }
                    break;

                case "tprequirement":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        int level = int.Parse(args[2]);
                        if (level < 1)
                        {
                            level = 1;
                        }

                        coffre.TpLevelRequirement = level;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le Niveau minimum pour utiliser le téléporteur du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.TpLevelRequirement);
                    break;

                case "punishspellid":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        int spellID = int.Parse(args[2]);   
                        coffre.PunishSpellId = spellID;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le PunishSpellID du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.PunishSpellId);
                    break;

                case "tpeffect":
                    if (coffre == null || args.Length < 3)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {            
                        coffre.TpEffect = int.Parse(args[2]);
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "L'effet utilisé pour le téléporteur du coffre \"" + coffre.Name + "\" est maitenant lié au SpellID: " + coffre.TpEffect);
                    break;

                case "tpisrenaissance":
                    if (coffre == null || args.Length != 2)
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    try
                    {
                        coffre.TpIsRenaissance = !coffre.TpIsRenaissance;
                        coffre.SaveIntoDatabase();
                    }
                    catch
                    {
                        DisplaySyntax(client);
                        break;
                    }
                    ChatUtil.SendSystemMessage(client, "Le statut IsRenaissance du coffre \"" + coffre.Name + "\" est maitenant: " + coffre.TpIsRenaissance);
                    break;
                    #endregion
            }
        }
    }
}
