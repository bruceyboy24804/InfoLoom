using System.Collections.Generic;
using Game.Prefabs;

namespace InfoLoomTwo.Systems.TradeCostData.TradeCostDomain
{
    public enum OutsideConnectionType
    {
        Road = 0,
        Train = 1,
        Air = 3,
        Ship = 4,
        All = 5,
    }
    public static class OutsideConnectionTypeUtils
    {
        private static readonly Dictionary<OutsideConnectionType, OutsideConnectionTransferType> _OutsideConnectionTypeNames = new Dictionary<OutsideConnectionType, OutsideConnectionTransferType>()
        {
            {OutsideConnectionType.Road, OutsideConnectionTransferType.Road},
            {OutsideConnectionType.Train, OutsideConnectionTransferType.Train},
            {OutsideConnectionType.Air, OutsideConnectionTransferType.Air},
            {OutsideConnectionType.Ship, OutsideConnectionTransferType.Ship},
            {OutsideConnectionType.All, OutsideConnectionTransferType.All},
        };
        public static string GetName(OutsideConnectionType tradeCostsUtils)
        {
            return InfoLoomMod.Instance.ModName + tradeCostsUtils;
        }
        public static OutsideConnectionTransferType GetTransferType(OutsideConnectionType tradeCostsUtils)
        {
            return _OutsideConnectionTypeNames[tradeCostsUtils];
        }
    }
}