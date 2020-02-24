using AmteScripts.Managers;
using System.Linq;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&rvr",
        ePrivLevel.GM,
        "Gestion du rvr",
        "'/rvr open' Force l'ouverture des rvr (ne se ferme jamais)",
        "'/rvr close' Force la fermeture des rvr",
        "'/rvr unforce' Permet après un '/rvr open' de fermer les rvr s'il ne sont pas dans les bonnes horaires",
        "'/rvr refresh' Permet de rafraichir les maps disponibles aux rvr (voir le wiki)",
        "'/rvr status' Permet de vérifier le status des rvr (open/close)")]
    public class RvRCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length <= 1)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "open":   
                    if (RvrManager.Instance.Open(true))
                        DisplayMessage(client, "Les rvr ont été ouverts avec les régions " + string.Join("-",RvrManager.Instance.Regions.OrderBy(r => r)) + ".");
                    else
                        DisplayMessage(client, "Les rvr n'ont pas pu être ouverts.");
                    break;

                case "close":
                    DisplayMessage(client, RvrManager.Instance.Close() ? "Les rvr ont été fermés." : "Les rvr n'ont pas pu être fermés.");
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

                case "refresh":
                    if (RvrManager.Instance.IsOpen)
                    {
                        DisplayMessage(client, "Les rvr doivent être fermés pour rafraichir la liste des maps disponibles.");
                        break;
                    }
                    string regions = "";
                    RvrManager.Instance.FindRvRMaps().GroupBy(id => id).Foreach(id => regions += " " + id.Key);
                    DisplayMessage(client, "Les rvr utilisent les maps:" + regions + ".");
                    break;
                case "status":
                    DisplayMessage(client, "Les rvr sont actuellement: " + (RvrManager.Instance.IsOpen ? "open" : "close") + ", les regions open sont: " + string.Join("-", RvrManager.Instance.Regions) + ".");
                    break;
            }
        }
    }
}