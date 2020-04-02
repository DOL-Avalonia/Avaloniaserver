using AmteScripts.Managers;
using AmteScripts.Utils;
using System.Linq;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&rvr",
		ePrivLevel.GM,
		"Gestion du rvr et du PvP",
        "'/rvr open' Force l'ouverture des rvr (ne se ferme jamais)",
        "'/rvr close' Force la fermeture des rvr",
        "'/rvr unforce' Permet après un '/rvr open' de fermer les rvr s'il ne sont pas dans les bonnes horaires",
		"'/rvr openpvp [region]' Force l'ouverture du pvp (ne se ferme jamais)",
		"'/rvr closepvp' Force la fermeture du pvp",
        "'/rvr status' Permet de vérifier le status des rvr (open/close)",
		"'/rvr unforcepvp' Permet après un '/rvr openpvp' de fermer le pvp s'il n'est pas dans les bonnes horaires",
		"'/rvr refresh' Permet de rafraichir les maps disponible au rvr et au pvp")]
	public class RvRCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length <= 1)
			{
				DisplaySyntax(client);
				return;
			}

			ushort region = 0;
			switch (args[1].ToLower())
			{
				case "open":
                    if (RvrManager.Instance.Open(true))
                        DisplayMessage(client, "Les rvr ont été ouverts avec les régions " + string.Join("-",RvrManager.Instance.Regions.OrderBy(r => r)) + ".");
					else
                        DisplayMessage(client, "Les rvr n'ont pas pu être ouverts.");
					break;
				case "openpvp":
					if (args.Length >= 3 && !ushort.TryParse(args[2], out region))
					{
						DisplaySyntax(client);
						break;
					}
					if (PvpManager.Instance.Open(region, true))
						DisplayMessage(client, "Le pvp a été ouvert avec la région " + PvpManager.Instance.Region + ".");
					else
						DisplayMessage(client, "Le pvp n'a pas pu être ouvert sur la région " + region + ".");
					break;

				case "close":
                    DisplayMessage(client, RvrManager.Instance.Close() ? "Les zones rvr ont été fermées." : "Les zones rvr sont fermées.");
					break;

				case "closepvp":
					DisplayMessage(client, PvpManager.Instance.Close() ? "Les zones pvp ont été fermées." : "Les zones pvp sont fermées.");
					break;

				case "unforce":
					if (!RvrManager.Instance.IsOpen)
					{
                        DisplayMessage(client, "Les rvr doivent être ouverts pour le unforce.");
						break;
					}
                    RvrManager.Instance.Open(false);
                    DisplayMessage(client, "Les rvr seront fermés automatiquement s'il ne sont plus dans les bonnes horaires.");
					break;

				case "unforcepvp":
					if (!PvpManager.Instance.IsOpen)
					{
						DisplayMessage(client, "Le pvp doit être ouvert pour le unforce.");
						break;
					}
					PvpManager.Instance.Open(0, false);
					DisplayMessage(client, "Le pvp sera fermé automatiquement s'il n'est plus dans les bonnes horaires.");
					break;

                case "status":
					DisplayMessage(client, "Les RvR sont actuellement: " + (RvrManager.Instance.IsOpen ? "open, les regions sont: " + string.Join("-", RvrManager.Instance.Regions) + "." : "close"));
					DisplayMessage(client, "Les regions PvP sont actuellement: " + (PvpManager.Instance.IsOpen ? "open, les regions sont: " + string.Join(",", PvpManager.Instance.Maps) + "." : "close"));
					break;	

				case "refresh":
                    if (RvrManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Les rvr doivent être fermés pour rafraichir la liste des maps disponibles.");
                        break;
                    }
                    string regions = "";
                    RvrManager.Instance.FindRvRMaps().GroupBy(id => id).Foreach(id => regions += " " + id.Key);
					var pvp = string.Join(", ", PvpManager.Instance.FindPvPMaps());
  				    DisplayMessage(client, $"Le rvr utilise les maps: {regions}, le pvp utilise les maps: {pvp}.");
					break;				   
			}
		}
	}
}
