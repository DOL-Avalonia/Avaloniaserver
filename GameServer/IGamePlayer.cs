﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL
{
    public interface IGamePlayer
    {
        byte Level
        {
            get;
            set;
        }

        long GetCurrentMoney();

        bool HasAbility(string keyName);

        int GetBaseSpecLevel(string keyname);
    }
}