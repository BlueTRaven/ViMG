using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViMG.Cubes;
using ViMG.Entities;
using BrUtility;

namespace ViMG.Items
{
    public static class ItemHelper
    {
        public static void DropCoins(EntityManager entityManager, Vector3 position, int value)
        {
            GetCoins(value, out var coinsCopper, out var coinsBronze, out var coinsSilver, out var coinsGold, out _);

            for (int i = 0; i < coinsCopper.num; i++)
                entityManager.Add(new EntityItem(position, new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f), 
                    Cube.CUBE_SCALE * 1f, Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f)), new ItemInstance(coinsCopper, 1)));

            for (int i = 0; i < coinsBronze.num; i++)
                entityManager.Add(new EntityItem(position, new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f), 
                    Cube.CUBE_SCALE * 1f, Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f)), new ItemInstance(coinsBronze, 1)));

            for (int i = 0; i < coinsSilver.num; i++)
                entityManager.Add(new EntityItem(position, new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f), 
                    Cube.CUBE_SCALE * 1f, Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f)), new ItemInstance(coinsSilver, 1)));

            for (int i = 0; i < coinsGold.num; i++)
                entityManager.Add(new EntityItem(position, new Vector3(Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f), 
                    Cube.CUBE_SCALE * 1f, Main.random.NextFloat(-Cube.CUBE_SCALE * 3.5f, Cube.CUBE_SCALE * 3.5f)), new ItemInstance(coinsGold, 1)));
        }
        //coinsInDenomination: a 4-length span containing the different coins.
        //0: copper coins
        //1: bronze coins
        //2: silver coins
        //3: gold coins
        public static void GetCoins(int value, out ItemInstance coinsCopper, out ItemInstance coinsBronze, out ItemInstance coinsSilver, out ItemInstance coinsGold, out int maxDenom)
        {
            int valueGold = value / 1000;
            value -= 1000 * valueGold;
            int valueSilver = value / 100;
            value -= 100 * valueSilver;
            int valueBronze = value / 10;
            value -= 10 * valueBronze;

            maxDenom = 0;
            if (valueBronze > 0)
                maxDenom = 1;
            if (valueSilver > 0)
                maxDenom = 2;
            if (valueGold > 0)
                maxDenom = 3;

            coinsCopper = new ItemInstance(Main.Registry.ItemRegistry.Get("coin_copper"), value, 0);
            coinsBronze = new ItemInstance(Main.Registry.ItemRegistry.Get("coin_bronze"), valueBronze, 0);
            coinsSilver = new ItemInstance(Main.Registry.ItemRegistry.Get("coin_silver"), valueSilver, 0);
            coinsGold = new ItemInstance(Main.Registry.ItemRegistry.Get("coin_gold"), valueGold, 0);
        }
    }
}
