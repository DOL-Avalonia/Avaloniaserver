using DOL.GS;
using DOL.GS.Commands;
using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DOL.Database;
using DOL.Network;
using DOL.Config;

namespace DOL.Vol
{
    [TestFixture]
    public class VolTests
    {
        GamePlayerMoq stealer;
        GamePlayerMoq target;

        [TestFixtureSetUp]
        public void Init()
        {
            stealer = new GamePlayerMoq();
            target = new GamePlayerMoq();
        }

        [Test]
        public void ShouldLowLevelPlayerCanNotVol()
        {
            stealer.Level = 15;
            target.Level = 15;

            Assert.AreEqual(false, VolCommandHandler.CanVol(stealer, target));
        }

        [Test]
        public void ShouldHigherPlayerCanNotVolLowLevels()
        {
            stealer.Level = 25;
            target.Level = 15;

            Assert.AreEqual(false, VolCommandHandler.CanVol(stealer, target));
        }

        [Test]
        public void ShouldHigherPlayerCanVol()
        {
            stealer.Level = 25;
            target.Level = 30;

            Assert.AreEqual(true, VolCommandHandler.CanVol(stealer, target));
        }

        /*
        Je pense qu'on pourrait laisser la possibilité de voler de l'argent OU un item,
        mais probablement mettre 80% de chance que ce soit de l'argent et 20% de chance que ce soit 1 objet.
        Je pense qu'en effet il faudrait faire en sorte que la somme volée 
        soit multipliée par un facteur correspondant au level du joueur.
        Aussi, fixer cette somme random entre 5% et 70% de l'argent en poche chez le joueur,
        pour ne pas non plus qu'il perde tout, ce serait trop sinon.
        Cela pourrait être en effet atténué selon le level du joueur.
        Aussi faire en sorte que les joueurs de level < lvl 20 ne puissent pas être volés si possible.
        Sinon fixer la limite à 10 level de difference pour les niveaux superieur 
        (par exemple un joueur lv 45 pourra voler un joueur lvl 35 mais pas un joueur lvl 34).
        Pour la perte du Stealth oui je pense que plus le level du joueur en face est élevé plus le risque d'echec
        / perte de stealth doit etre élevé.
         */

        [Test]
        public void VolAbility_ShouldSameLevelRangeShouldSuccess()
        {
            stealer.Level = 30;
            target.Level = 35;

            var result = VolCommandHandler.Vol(stealer, target);

            Assert.AreNotEqual(VolResultStatus.FAILED, result.Status);
        }

    }
}
