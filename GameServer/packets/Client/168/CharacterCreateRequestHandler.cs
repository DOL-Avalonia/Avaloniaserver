/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using DOL.Database;
using DOL.Events;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Character Create and Customization handler.  Please maintain all commented debug statements
    /// in order to support future debugging. - Tolakram
    /// </summary>
    [PacketHandler(PacketHandlerType.TCP, eClientPackets.CharacterCreateRequest, "Handles character creation requests", eClientStatus.LoggedIn)]
    public class CharacterCreateRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Max Points to allow on player creation
        /// </summary>
        private const int MaxStartingBonusPoints = 30;

        /// <summary>
        /// Client Operation Value.
        /// </summary>
        public enum eOperation: uint
        {
            Delete = 0x12345678,
            Create = 0x23456789,
            Customize = 0x3456789A,
            Unknown = 0x456789AB,
        }

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            bool needRefresh = false;

            if (client.Version > GameClient.eClientVersion.Version1124) // 1125 support
            {
                var pakdata = new CreationCharacterData(packet, client);

                // Graveen: changed the following to allow GMs to have special chars in their names (_,-, etc..)
                var nameCheck = new Regex("^[A-Z][a-zA-Z]");
                if (!string.IsNullOrEmpty(pakdata.CharName) && (pakdata.CharName.Length < 3 || !nameCheck.IsMatch(pakdata.CharName)))
                {
                    if ((ePrivLevel)client.Account.PrivLevel == ePrivLevel.Player)
                    {
                        if (Properties.BAN_HACKERS)
                        {
                            client.BanAccount(string.Format("Autoban bad CharName '{0}'", pakdata.CharName));
                        }

                        client.Disconnect();
                        return;
                    }
                }

                switch (pakdata.Operation)
                {
                    case 3: // delete request
                        if (string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Deletion in 1.104+ check for removed character.
                            needRefresh |= CheckForDeletedCharacter(client.Account.Name, client, pakdata.CharacterSlot);
                        }
                        break;
                    case 2: // Customize face or stats
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Customizing ?
                            var character = client.Account.Characters != null ? client.Account.Characters.FirstOrDefault(ch => ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase)) : null;
                            if (character != null)
                            {
                                needRefresh |= CheckCharacterForUpdates1125(pakdata, client, character, pakdata.CustomizeType);
                            }
                        }
                        break;
                    case 1: // create request
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Creation ?
                            var character = client.Account.Characters != null ? client.Account.Characters.FirstOrDefault(ch => ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase)) : null;
                            if (character == null)
                            {
                                needRefresh |= CreateCharacter(pakdata, client, pakdata.CharacterSlot);
                            }
                        }
                        break;
                    default:
                        break;
                }

                if (needRefresh)
                {
                    client.Out.SendLoginGranted();
                    // live actually just sends server login request response... i think this sets the realm select button again too
                }
                return;
            }

            string accountName = packet.ReadString(24);

            if (log.IsDebugEnabled)
            {
                log.Debug($"CharacterCreateRequestHandler for account {accountName} using version {client.Version}");
            }

            if (!accountName.StartsWith(client.Account.Name)) // TODO more correctly check, client send accountName as account-S, -N, -H (if it not fit in 20, then only account)
            {
                if (Properties.BAN_HACKERS)
                {
                    client.BanAccount($"Autoban wrong Account '{accountName}'");
                }

                client.Disconnect();
                return;
            }

            // Realm
            eRealm currentRealm = eRealm.None;
            if (accountName.EndsWith("-S"))
            {
                currentRealm = eRealm.Albion;
            }
            else if (accountName.EndsWith("-N"))
            {
                currentRealm = eRealm.Midgard;
            }
            else if (accountName.EndsWith("-H"))
            {
                currentRealm = eRealm.Hibernia;
            }

            // Client character count support
            int charsCount = 10;

            for (int i = 0; i < charsCount; i++)
            {
                var pakdata = new CreationCharacterData(packet, client);

                // Graveen: changed the following to allow GMs to have special chars in their names (_,-, etc..)
                var nameCheck = new Regex("^[A-Z][a-zA-Z]");
                if (!string.IsNullOrEmpty(pakdata.CharName) && (pakdata.CharName.Length < 3 || !nameCheck.IsMatch(pakdata.CharName)))
                {
                    if ((ePrivLevel)client.Account.PrivLevel == ePrivLevel.Player)
                    {
                        if (Properties.BAN_HACKERS)
                        {
                            client.BanAccount($"Autoban bad CharName '{pakdata.CharName}'");
                        }

                        client.Disconnect();
                        return;
                    }
                }

                switch ((eOperation) pakdata.Operation)
                {
                    case eOperation.Delete:
                        if (string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Deletion in 1.104+ check for removed character.
                            needRefresh |= CheckForDeletedCharacter(accountName, client, i);
                        }

                        break;
                    case eOperation.Customize:
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Customizing ?
                            var character = client.Account.Characters?.FirstOrDefault(ch =>
                                ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase));
                            if (character != null)
                            {
                                needRefresh |= CheckCharacterForUpdates(pakdata, client, character);
                            }
                        }

                        break;
                    case eOperation.Create:
                        if (!string.IsNullOrEmpty(pakdata.CharName))
                        {
                            // Candidate for Creation ?
                            var character = client.Account.Characters?.FirstOrDefault(ch =>
                                ch.Name.Equals(pakdata.CharName, StringComparison.OrdinalIgnoreCase));
                            if (character == null)
                            {
                                needRefresh |= CreateCharacter(pakdata, client, i);
                            }
                        }

                        break;
                }
            }

            if (needRefresh)
            {
                client.Out.SendCharacterOverview(currentRealm);
            }
        }

        class CreationCharacterData
        {
            public string CharName { get; }

            public int CustomMode { get; }

            public int EyeSize { get; }

            public int LipSize { get; }

            public int EyeColor { get; }

            public int HairColor { get; }

            public int FaceType { get; }

            public int HairStyle { get; }

            public int MoodType { get; }

            public uint Operation { get; }

            public int Class { get; }

            public int Realm { get; }

            public int Race { get; }

            public int Gender { get; }

            public ushort CreationModel { get; }

            public int Region { get; }

            public int Strength { get; }

            public int Dexterity { get; }

            public int Constitution { get; }

            public int Quickness { get; }

            public int Intelligence { get; }

            public int Piety { get; }

            public int Empathy { get; }

            public int Charisma { get; }

            public int NewConstitution { get; }

            public int CharacterSlot { get; set; } // 1125 support

            public int CustomizeType { get; set; } // 1125 support

            /// <summary>
            /// Reads up ONE character iteration on the packet stream
            /// </summary>
            /// <param name="packet"></param>
            /// <param name="client"></param>
            public CreationCharacterData(GSPacketIn packet, GameClient client)
            {
                if (client.Version > GameClient.eClientVersion.Version1124) // 1125 support
                {
                    CharacterSlot = packet.ReadByte();
                    CharName = packet.ReadIntPascalStringLowEndian();
                    packet.Skip(4); // 0x18 0x00 0x00 0x00                   
                    CustomMode = packet.ReadByte();
                    EyeSize = packet.ReadByte();
                    LipSize = packet.ReadByte();
                    EyeColor = packet.ReadByte();
                    HairColor = packet.ReadByte();
                    FaceType = packet.ReadByte();
                    HairStyle = packet.ReadByte();
                    packet.Skip(3);
                    MoodType = packet.ReadByte();
                    packet.Skip(9); // one extra byte skipped?
                    Operation = (uint)packet.ReadByte(); // probably low end int, but im just gonna read the first byte
                    CustomizeType = packet.ReadByte(); // 1 = face 2 = attributes 3 = both
                    packet.Skip(2); // last two bytes in the supposed int
                    // the following are now low endian int pascal strings
                    packet.Skip(5); //Location string
                    packet.Skip(5); //Skip class name
                    packet.Skip(5); //Skip race name
                    packet.Skip(1); // not sure what this is? previous pak says level, but its unused anyway
                    Class = packet.ReadByte();
                    Realm = packet.ReadByte();
                    if (Realm > 0) // put inside this, as when a char is deleted, there is no realm sent TODO redo how account slot is stored in DB perhaps
                    {
                        CharacterSlot -= (Realm - 1) * 10; // calc to get character slot into same format used in database.
                    }
                    //The following byte contains
                    //1bit=start location ... in ShroudedIsles you can choose ...
                    //1bit=first race bit
                    //1bit=unknown
                    //1bit=gender (0=male, 1=female)
                    //4bit=race
                    byte startRaceGender1 = (byte)packet.ReadByte();                    
                    Race = startRaceGender1 & 0x1F;
                    Gender = ((startRaceGender1 >> 7) & 0x01);
                    //SIStartLocation = ((startRaceGender1 >> 7) != 0);
                    CreationModel = packet.ReadShortLowEndian();
                    Region = packet.ReadByte();
                    packet.Skip(1); //TODO second byte of region unused currently
                    packet.Skip(4); //TODO Unknown Int / last used?
                    Strength = packet.ReadByte();
                    Dexterity = packet.ReadByte();
                    Constitution = packet.ReadByte();
                    Quickness = packet.ReadByte();
                    Intelligence = packet.ReadByte();
                    Piety = packet.ReadByte();
                    Empathy = packet.ReadByte();
                    Charisma = packet.ReadByte();
                    packet.Skip(40); //TODO equipment                    
                    packet.Skip(3); // explained above
                    // New constitution must be read before skipping 4 bytes
                    NewConstitution = packet.ReadByte(); // 0x9F
                    // trailing 0x00
                    return;
                }
                // 1124
                // unk - probably indicates customize or create (these are moved from 1.99 4 added bytes)
                packet.ReadIntLowEndian();

                CharName = packet.ReadString(24);
                CustomMode = packet.ReadByte();
                EyeSize = packet.ReadByte();
                LipSize = packet.ReadByte();
                EyeColor = packet.ReadByte();
                HairColor = packet.ReadByte();
                FaceType = packet.ReadByte();
                HairStyle = packet.ReadByte();
                packet.Skip(3);
                MoodType = packet.ReadByte();
                packet.Skip(8);

                Operation = packet.ReadInt();
                packet.ReadByte();

                packet.Skip(24); // Location String
                packet.Skip(24); // Skip class name
                packet.Skip(24); // Skip race name

                packet.ReadByte(); // not safe! level
                Class = packet.ReadByte();
                Realm = packet.ReadByte();

                // The following byte contains
                // 1bit=start location ... in ShroudedIsles you can choose ...
                // 1bit=first race bit
                // 1bit=unknown
                // 1bit=gender (0=male, 1=female)
                // 4bit=race
                byte startRaceGender = (byte)packet.ReadByte();
                Race = (startRaceGender & 0x0F) + ((startRaceGender & 0x40) >> 2);
                Gender = (startRaceGender >> 4) & 0x01;

                CreationModel = packet.ReadShortLowEndian();
                Region = packet.ReadByte();
                packet.Skip(1); // TODO second byte of region unused currently
                packet.Skip(4); // TODO Unknown Int / last used?

                Strength = packet.ReadByte();
                Dexterity = packet.ReadByte();
                Constitution = packet.ReadByte();
                Quickness = packet.ReadByte();
                Intelligence = packet.ReadByte();
                Piety = packet.ReadByte();
                Empathy = packet.ReadByte();
                Charisma = packet.ReadByte();

                packet.Skip(40); // TODO equipment

                packet.ReadByte(); // 0x9C activeRightSlot
                packet.ReadByte(); // 0x9D activeLeftSlot
                packet.ReadByte(); // 0x9E siZone

                // New constitution must be read before skipping 4 bytes
                NewConstitution = packet.ReadByte(); // 0x9F
            }
        }

        private bool CreateCharacter(CreationCharacterData pdata, GameClient client, int accountSlot)
        {
            Account account = client.Account;
            var ch = new DOLCharacters
            {
                AccountName = account.Name,
                Name = pdata.CharName
            };

            if (pdata.CustomMode == 0x01)
            {
                ch.EyeSize = (byte)pdata.EyeSize;
                ch.LipSize = (byte)pdata.LipSize;
                ch.EyeColor = (byte)pdata.EyeColor;
                ch.HairColor = (byte)pdata.HairColor;
                ch.FaceType = (byte)pdata.FaceType;
                ch.HairStyle = (byte)pdata.HairStyle;
                ch.MoodType = (byte)pdata.MoodType;
                ch.CustomisationStep = 2; // disable config button

                if (log.IsDebugEnabled)
                {
                    log.Debug("Disable Config Button");
                }
            }

            ch.Level = 1;

            // Set Realm and Class
            ch.Realm = pdata.Realm;
            ch.Class = pdata.Class;

            // Set Account Slot, Gender
            ch.AccountSlot = accountSlot + ch.Realm * 100;
            ch.Gender = pdata.Gender;

            // Set Race
            ch.Race = pdata.Race;

            ch.CreationModel = pdata.CreationModel;
            ch.CurrentModel = ch.CreationModel;
            ch.Region = pdata.Region;

            ch.Strength = pdata.Strength;
            ch.Dexterity = pdata.Dexterity;
            ch.Constitution = pdata.Constitution;
            ch.Quickness = pdata.Quickness;
            ch.Intelligence = pdata.Intelligence;
            ch.Piety = pdata.Piety;
            ch.Empathy = pdata.Empathy;
            ch.Charisma = pdata.Charisma;
            ch.IsRenaissance = false;

            // defaults
            ch.CreationDate = DateTime.Now;

            ch.Endurance = 100;
            ch.MaxEndurance = 100;
            ch.Concentration = 100;
            switch (ch.Class)
            {
                case 6:
                case 26:
                case 47:
                    ch.Concentration = (int)(ch.Concentration * 1.25);
                    break;
                case 4:
                case 28:
                case 48:
                    ch.Concentration = (int)(ch.Concentration * 1.1);
                    break;
                default:
                    break;
            }
            ch.MaxSpeed = GamePlayer.PLAYER_BASE_SPEED;

            if (log.IsDebugEnabled)
            {
                log.Debug($"Creation {client.Version} character, class:{ch.Class}, realm:{ch.Realm}");
            }

            // Is class disabled ?
            List<string> disabledClasses = Properties.DISABLED_CLASSES.SplitCSV(true);
            var occurences =
                (from j in disabledClasses
                    where j == ch.Class.ToString()
                    select j)
                .Count();

            if (occurences > 0 && (ePrivLevel)client.Account.PrivLevel == ePrivLevel.Player)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Client {client.Account.Name} tried to create a disabled classe: {(eCharacterClass)ch.Class}");
                }

                return true;
            }

            // check if race disabled
            List<string> disabledRaces = Properties.DISABLED_RACES.SplitCSV(true);
            occurences =
                (from j in disabledRaces
                    where j == ch.Race.ToString()
                    select j)
                .Count();

            if (occurences > 0 && (ePrivLevel)client.Account.PrivLevel == ePrivLevel.Player)
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug($"Client {client.Account.Name} tried to create a disabled race: {(eRace)ch.Race}");
                }

                return true;
            }

            // If sending invalid Class ID
            if (!Enum.IsDefined(typeof(eCharacterClass), (eCharacterClass)ch.Class))
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"{client.Account.Name} tried to create a character with wrong class ID: {ch.Class}, realm:{ch.Realm}");
                }

                if (Properties.BAN_HACKERS)
                {
                    client.BanAccount($"Autoban character create class: id:{ch.Class} realm:{ch.Realm} name:{ch.Name} account:{account.Name}");
                    client.Disconnect();
                    return false;
                }

                return true;
            }

            //Remove Invalid Char Check 
            //Issue Client 1125 allows to create invalid class
            // check if client tried to create invalid char
            //if (!IsCharacterValid(ch))
            //{
            //    if (log.IsWarnEnabled)
            //    {
            //        log.Warn($"{ch.AccountName} tried to create invalid character:\nchar name={ch.Name}, gender={ch.Gender}, race={ch.Race}, realm={ch.Realm}, class={ch.Class}, region={ch.Region}\nstr={ch.Strength}, con={ch.Constitution}, dex={ch.Dexterity}, qui={ch.Quickness}, int={ch.Intelligence}, pie={ch.Piety}, emp={ch.Empathy}, chr={ch.Charisma}");
            //    }

            //    return true;
            //}

            // Save the character in the database
            GameServer.Database.AddObject(ch);

            // Fire the character creation event
            // This is Where Most Creation Script should take over to update any data they would like !
            GameEventMgr.Notify(DatabaseEvent.CharacterCreated, null, new CharacterEventArgs(ch, client));

            // write changes
            GameServer.Database.SaveObject(ch);

            // Log creation
            AuditMgr.AddAuditEntry(client, AuditType.Account, AuditSubtype.CharacterCreate, string.Empty, pdata.CharName);

            client.Account.Characters = null;

            if (log.IsInfoEnabled)
            {
                log.Info($"Character {pdata.CharName} created on Account {account}!");
            }

            // Reload Account Relations
            GameServer.Database.FillObjectRelations(client.Account);

            return true;
        }

        /// <summary>
        /// Check if a Character Needs update based to packet data
        /// </summary>
        /// <param name="pdata">packet data</param>
        /// <param name="client">client</param>
        /// <param name="character">db character</param>
        /// <returns>True if character need refreshment false if no refresh needed.</returns>
        private bool CheckCharacterForUpdates(CreationCharacterData pdata, GameClient client, DOLCharacters character)
        {
            int newModel = character.CurrentModel;

            if (pdata.CustomMode == 1 || pdata.CustomMode == 2 || pdata.CustomMode == 3)
            {
                if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                {
                    character.EyeSize = (byte)pdata.EyeSize;
                    character.LipSize = (byte)pdata.LipSize;
                    character.EyeColor = (byte)pdata.EyeColor;
                    character.HairColor = (byte)pdata.HairColor;
                    character.FaceType = (byte)pdata.FaceType;
                    character.HairStyle = (byte)pdata.HairStyle;
                    character.MoodType = (byte)pdata.MoodType;
                }

                if (pdata.CustomMode != 3)
                {
                    var stats = new Dictionary<eStat, int>
                    {
                        [eStat.STR] = pdata.Strength,
                        [eStat.DEX] = pdata.Dexterity,
                        [eStat.CON] = pdata.NewConstitution,
                        [eStat.QUI] = pdata.Quickness,
                        [eStat.INT] = pdata.Intelligence,
                        [eStat.PIE] = pdata.Piety,
                        [eStat.EMP] = pdata.Empathy,
                        [eStat.CHR] = pdata.Charisma
                    };

                    // check for changed stats.
                    bool flagChangedStats = false;
                    flagChangedStats |= stats[eStat.STR] != character.Strength;
                    flagChangedStats |= stats[eStat.CON] != character.Constitution;
                    flagChangedStats |= stats[eStat.DEX] != character.Dexterity;
                    flagChangedStats |= stats[eStat.QUI] != character.Quickness;
                    flagChangedStats |= stats[eStat.INT] != character.Intelligence;
                    flagChangedStats |= stats[eStat.PIE] != character.Piety;
                    flagChangedStats |= stats[eStat.EMP] != character.Empathy;
                    flagChangedStats |= stats[eStat.CHR] != character.Charisma;

                    if (flagChangedStats)
                    {
                        ICharacterClass charClass = ScriptMgr.FindCharacterClass(character.Class);

                        if (charClass != null)
                        {
                            bool valid = IsCustomPointsDistributionValid(character, stats, out var points);

                            // Hacking attemp ?
                            if (points > MaxStartingBonusPoints)
                            {
                                if ((ePrivLevel)client.Account.PrivLevel == ePrivLevel.Player)
                                {
                                    if (Properties.BAN_HACKERS)
                                    {
                                        client.BanAccount($"Autoban Hack char update : Wrong allowed points:{points}");
                                    }

                                    client.Disconnect();
                                    return false;
                                }
                            }

                            // Error in setting points
                            if (!valid)
                            {
                                return true;
                            }

                            if (Properties.ALLOW_CUSTOMIZE_STATS_AFTER_CREATION)
                            {
                                // Set Stats, valid is ok.
                                character.Strength = stats[eStat.STR];
                                character.Constitution = stats[eStat.CON];
                                character.Dexterity = stats[eStat.DEX];
                                character.Quickness = stats[eStat.QUI];
                                character.Intelligence = stats[eStat.INT];
                                character.Piety = stats[eStat.PIE];
                                character.Empathy = stats[eStat.EMP];
                                character.Charisma = stats[eStat.CHR];

                                if (log.IsInfoEnabled)
                                {
                                    log.Info($"Character {character.Name} Stats updated in cache!");
                                }

                                if (client.Player != null)
                                {
                                    foreach (var stat in stats.Keys)
                                    {
                                        client.Player.ChangeBaseStat(stat, (short)(stats[stat] - client.Player.GetBaseStat(stat)));
                                    }

                                    if (log.IsInfoEnabled)
                                    {
                                        log.Info($"Character {character.Name} Player Stats updated in cache!");
                                    }
                                }
                            }
                        }
                        else if (log.IsErrorEnabled)
                        {
                            log.Error($"No CharacterClass with ID {character.Class} found");
                        }
                    }
                }

                if (pdata.CustomMode == 2) // change player customization
                {
                    if (client.Account.PrivLevel == 1 && ((pdata.CreationModel >> 11) & 3) == 0)
                    {
                        if (Properties.BAN_HACKERS) // Player size must be > 0 (from 1 to 3)
                        {
                            client.BanAccount($"Autoban Hack char update : zero character size in model:{newModel}");
                            client.Disconnect();
                            return false;
                        }

                        return true;
                    }

                    character.CustomisationStep = 2; // disable config button

                    if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                    {
                        if (pdata.CreationModel != character.CreationModel)
                        {
                            character.CurrentModel = newModel;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info($"Character {character.Name} face properties configured by account {client.Account.Name}!");
                        }
                    }
                }
                else if (pdata.CustomMode == 3) // auto config -- seems someone thinks this is not possible?
                {
                    character.CustomisationStep = 3; // enable config button to player
                }

                // Save the character in the database
                GameServer.Database.SaveObject(character);
            }

            return false;
        }

        // 1125 support
        private bool CheckCharacterForUpdates1125(CreationCharacterData pdata, GameClient client, DOLCharacters character, int type)
        {
            int newModel = character.CurrentModel;

            if (pdata.CustomMode == 1 || pdata.CustomMode == 2 || pdata.CustomMode == 3)
            {
                bool flagChangedStats = false;

                if (type == 1 || type == 3) // face changes
                {
                    if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                    {
                        character.EyeSize = (byte)pdata.EyeSize;
                        character.LipSize = (byte)pdata.LipSize;
                        character.EyeColor = (byte)pdata.EyeColor;
                        character.HairColor = (byte)pdata.HairColor;
                        character.FaceType = (byte)pdata.FaceType;
                        character.HairStyle = (byte)pdata.HairStyle;
                        character.MoodType = (byte)pdata.MoodType;
                    }
                }
                if (type == 2 || type == 3) // attributes changes
                {
                    if (pdata.CustomMode != 3)//patch 0042 // TODO check out these different custommodes
                    {
                        var stats = new Dictionary<eStat, int>
                        {
                            [eStat.STR] = pdata.Strength, // Strength
                            [eStat.DEX] = pdata.Dexterity, // Dexterity
                            [eStat.CON] = pdata.NewConstitution, // New Constitution
                            [eStat.QUI] = pdata.Quickness, // Quickness
                            [eStat.INT] = pdata.Intelligence, // Intelligence
                            [eStat.PIE] = pdata.Piety, // Piety
                            [eStat.EMP] = pdata.Empathy, // Empathy
                            [eStat.CHR] = pdata.Charisma // Charisma
                        };

                        // check for changed stats.
                        flagChangedStats |= stats[eStat.STR] != character.Strength;
                        flagChangedStats |= stats[eStat.CON] != character.Constitution;
                        flagChangedStats |= stats[eStat.DEX] != character.Dexterity;
                        flagChangedStats |= stats[eStat.QUI] != character.Quickness;
                        flagChangedStats |= stats[eStat.INT] != character.Intelligence;
                        flagChangedStats |= stats[eStat.PIE] != character.Piety;
                        flagChangedStats |= stats[eStat.EMP] != character.Empathy;
                        flagChangedStats |= stats[eStat.CHR] != character.Charisma;

                        if (flagChangedStats)
                        {
                            ICharacterClass charClass = ScriptMgr.FindCharacterClass(character.Class);

                            if (charClass != null)
                            {
                                bool valid = IsCustomPointsDistributionValid(character, stats, out int points);

                                // Hacking attemp ?
                                if (points > MaxStartingBonusPoints)
                                {
                                    if ((ePrivLevel)client.Account.PrivLevel == ePrivLevel.Player)
                                    {
                                        if (Properties.BAN_HACKERS)
                                        {
                                            client.BanAccount(string.Format("Autoban Hack char update : Wrong allowed points:{0}", points));
                                        }

                                        client.Disconnect();
                                        return false;
                                    }
                                }

                                // Error in setting points
                                if (!valid)
                                {
                                    return true;
                                }

                                if (Properties.ALLOW_CUSTOMIZE_STATS_AFTER_CREATION)
                                {
                                    // Set Stats, valid is ok.
                                    character.Strength = stats[eStat.STR];
                                    character.Constitution = stats[eStat.CON];
                                    character.Dexterity = stats[eStat.DEX];
                                    character.Quickness = stats[eStat.QUI];
                                    character.Intelligence = stats[eStat.INT];
                                    character.Piety = stats[eStat.PIE];
                                    character.Empathy = stats[eStat.EMP];
                                    character.Charisma = stats[eStat.CHR];

                                    if (log.IsInfoEnabled)
                                    {
                                        log.InfoFormat("Character {0} Stats updated in cache!", character.Name);
                                    }

                                    if (client.Player != null)
                                    {
                                        foreach (var stat in stats.Keys)
                                        {
                                            client.Player.ChangeBaseStat(stat, (short)(stats[stat] - client.Player.GetBaseStat(stat)));
                                        }

                                        if (log.IsInfoEnabled)
                                        {
                                            log.InfoFormat("Character {0} Player Stats updated in cache!", character.Name);
                                        }
                                    }
                                }
                            }
                            else if (log.IsErrorEnabled)
                            {
                                log.ErrorFormat("No CharacterClass with ID {0} found", character.Class);
                            }
                        }
                    }
                }

                if (pdata.CustomMode == 2) // change player customization // is this changing starting race? im not sure TODO check
                {
                    if (client.Account.PrivLevel == 1 && ((pdata.CreationModel >> 11) & 3) == 0)
                    {
                        if (Properties.BAN_HACKERS) // Player size must be > 0 (from 1 to 3)
                        {
                            client.BanAccount(string.Format("Autoban Hack char update : zero character size in model:{0}", newModel));
                            client.Disconnect();
                            return false;
                        }
                        return true;
                    }

                    character.CustomisationStep = 2; // disable config button

                    if (Properties.ALLOW_CUSTOMIZE_FACE_AFTER_CREATION)
                    {
                        if (pdata.CreationModel != character.CreationModel)
                        {
                            character.CurrentModel = newModel;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.InfoFormat("Character {0} face properties configured by account {1}!", character.Name, client.Account.Name);
                        }
                    }
                }
                else if (pdata.CustomMode == 3) //auto config -- seems someone thinks this is not possible?
                {
                    character.CustomisationStep = 3; // enable config button to player
                }

                //Save the character in the database
                GameServer.Database.SaveObject(character);
            }

            return false;
        }

        public static bool CheckForDeletedCharacter(string accountName, GameClient client, int slot)
        {
            int charSlot = slot;
            if (client.Version > GameClient.eClientVersion.Version1124) // 1125 support
            {
                charSlot = client.Account.Realm * 100 + (slot - (client.Account.Realm - 1) * 10);
            }
            else // 1124
            {
                if (accountName.EndsWith("-S"))
                {
                    charSlot = 100 + slot;
                }
                else if (accountName.EndsWith("-N"))
                {
                    charSlot = 200 + slot;
                }
                else if (accountName.EndsWith("-H"))
                {
                    charSlot = 300 + slot;
                }
            }            

            DOLCharacters[] allChars = client.Account.Characters;

            if (allChars != null)
            {
                foreach (DOLCharacters character in allChars.ToArray())
                {
                    if (character.AccountSlot == charSlot && client.ClientState == GameClient.eClientState.CharScreen)
                    {
                        if (log.IsWarnEnabled)
                        {
                            log.Warn($"DB Character Delete:  Account {accountName}, Character: {character.Name}, slot position: {character.AccountSlot}, client slot {slot}");
                        }

                        if (allChars.Length < client.ActiveCharIndex && client.ActiveCharIndex > -1 && allChars[client.ActiveCharIndex] == character)
                        {
                            client.ActiveCharIndex = -1;
                        }

                        GameEventMgr.Notify(DatabaseEvent.CharacterDeleted, null, new CharacterEventArgs(character, client));

                        if (Properties.BACKUP_DELETED_CHARACTERS)
                        {
                            var backupCharacter = new DOLCharactersBackup(character);
                            backupCharacter.CustomParams.ForEach(param => GameServer.Database.AddObject(param));
                            GameServer.Database.AddObject(backupCharacter);

                            if (log.IsWarnEnabled)
                            {
                                log.Warn($"DB Character {character.ObjectId} backed up to DOLCharactersBackup and no associated content deleted.");
                            }
                        }
                        else
                        {
                            // delete associated data
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<InventoryItem>("`OwnerID` = @OwnerID", new QueryParameter("@OwnerID", character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.Error($"Error deleting char items, char OID={character.ObjectId}, Exception:{e}");
                                }
                            }

                            // delete quests
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<DBQuest>("`Character_ID` = @Character_ID", new QueryParameter("@Character_ID", character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.Error($"Error deleting char quests, char OID={character.ObjectId}, Exception:{e}");
                                }
                            }

                            // delete ML steps
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<DBCharacterXMasterLevel>("`Character_ID` = @Character_ID", new QueryParameter("@Character_ID", character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.Error($"Error deleting char ml steps, char OID={character.ObjectId}, Exception:{e}");
                                }
                            }

                            // delete gravestone if character has one
                            try
                            {
                                var objs = GameServer.Database.SelectObjects<DBGravestones>("`Gravestones_ID` = @Gravestones_ID", new QueryParameter("@Gravestones_ID", character.ObjectId));
                                GameServer.Database.DeleteObject(objs);
                            }
                            catch (Exception e)
                            {
                                if (log.IsErrorEnabled)
                                {
                                    log.ErrorFormat("No gravestone found for deleted character, char OID={0}, Exception:{1}", character.ObjectId, e);
                                }
                            }
                        }

                        string deletedChar = character.Name;

                        GameServer.Database.DeleteObject(character);
                        client.Account.Characters = null;
                        client.Player = null;
                        GameServer.Database.FillObjectRelations(client.Account);

                        if (client.Account.Characters == null || client.Account.Characters.Length == 0)
                        {
                            if (log.IsInfoEnabled)
                            {
                                log.Info($"Account {client.Account.Name} has no more chars. Realm reset!");
                            }

                            // Client has no more characters, so the client can choose the realm again!
                            client.Account.Realm = 0;
                        }

                        GameServer.Database.SaveObject(client.Account);

                        // Log deletion
                        AuditMgr.AddAuditEntry(client, AuditType.Character, AuditSubtype.CharacterDelete, string.Empty, deletedChar);

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Check if Custom Creation Points Distribution is Valid.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="stats"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static bool IsCustomPointsDistributionValid(DOLCharacters character, IDictionary<eStat, int> stats, out int points)
        {
            ICharacterClass charClass = ScriptMgr.FindCharacterClass(character.Class);

            if (charClass != null)
            {
                points = 0;

                // check if each stat is valid.
                foreach (var stat in stats.Keys)
                {
                    int raceAmount = GlobalConstants.STARTING_STATS_DICT[(eRace)character.Race][stat];

                    int classAmount = 0;

                    for (int level = character.Level; level > 5; level--)
                    {
                        if (charClass.PrimaryStat != eStat.UNDEFINED && charClass.PrimaryStat == stat)
                        {
                            classAmount++;
                        }

                        if (charClass.SecondaryStat != eStat.UNDEFINED && charClass.SecondaryStat == stat && (level - 6) % 2 == 0)
                        {
                            classAmount++;
                        }

                        if (charClass.TertiaryStat != eStat.UNDEFINED && charClass.TertiaryStat == stat && (level - 6) % 3 == 0)
                        {
                            classAmount++;
                        }
                    }

                    int above = stats[stat] - raceAmount - classAmount;

                    // Miss Some points...
                    if (above < 0)
                    {
                        return false;
                    }

                    points += above;
                    points += Math.Max(0, above - 10); // two points used
                    points += Math.Max(0, above - 15); // three points used
                }

                return points == MaxStartingBonusPoints;
            }

            points = -1;
            return false;
        }

        /// <summary>
        /// Verify whether created character is valid
        /// </summary>
        /// <param name="ch">The character to check</param>
        /// <returns>True if valid</returns>
        public static bool IsCharacterValid(DOLCharacters ch)
        {
            ICharacterClass charClass = ScriptMgr.FindCharacterClass(ch.Class);

            bool valid = true;
            try
            {
                if ((eRealm)ch.Realm < eRealm._FirstPlayerRealm || (eRealm)ch.Realm > eRealm._LastPlayerRealm)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong realm: {ch.Realm} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if (ch.Level != 1)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong level: {ch.Level} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if (!GlobalConstants.STARTING_CLASSES_DICT.ContainsKey((eRealm)ch.Realm) || !GlobalConstants.STARTING_CLASSES_DICT[(eRealm)ch.Realm].Contains((eCharacterClass)ch.Class))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong class: {ch.Class}, realm:{ch.Realm} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if(!charClass.EligibleRaces.Exists(s => (int)s.ID == ch.Race))
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong race: {ch.Race}, class:{ch.Class} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                var stats = new Dictionary<eStat, int> { { eStat.STR, ch.Strength }, { eStat.CON, ch.Constitution }, { eStat.DEX, ch.Dexterity }, { eStat.QUI, ch.Quickness },
                    { eStat.INT, ch.Intelligence }, { eStat.PIE, ch.Piety }, { eStat.EMP, ch.Empathy }, { eStat.CHR, ch.Charisma }, };

                valid &= IsCustomPointsDistributionValid(ch, stats, out var pointsUsed);

                if (pointsUsed != MaxStartingBonusPoints)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Points used: {pointsUsed} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                eGender gender = ch.Gender == 0 ? eGender.Male : eGender.Female;

                if (GlobalConstants.RACE_GENDER_CONSTRAINTS_DICT.ContainsKey((eRace)ch.Race) && GlobalConstants.RACE_GENDER_CONSTRAINTS_DICT[(eRace)ch.Race] != gender)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong Race gender: {ch.Gender}, race: {ch.Race} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }

                if (GlobalConstants.CLASS_GENDER_CONSTRAINTS_DICT.ContainsKey((eCharacterClass)ch.Class) && GlobalConstants.CLASS_GENDER_CONSTRAINTS_DICT[(eCharacterClass)ch.Class] != gender)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"Wrong class gender: {ch.Gender}, class:{ch.Class} on character creation from Account: {ch.AccountName}");
                    }

                    valid = false;
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"CharacterCreation error on account {ch.AccountName}, slot {ch.AccountSlot}. Exception:{e}");
                }

                valid = false;
            }

            return valid;
        }
    }
}