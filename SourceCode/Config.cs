using BepInEx.Configuration;

namespace TelepathicObject
{
    public class Config
    {
        public static ConfigEntry<int> BurerSpawnWeight;
        public static ConfigEntry<float> BurerSpawnPower;
        public static ConfigEntry<int> BurerSpawnMax;
        public static ConfigEntry<string> BurerSpawnLevelsSet;
        public static ConfigEntry<string> BurerSpawnLevelsWithWeight;
        public static ConfigEntry<float> BurerDistanceToMelee;
        public static ConfigEntry<float> BurerTelepathicAttackTime;
        public static ConfigEntry<float> BurerSpeedInChase;
        public static ConfigEntry<int> BurerDamagePerHitOnPlayer;
        public static ConfigEntry<bool> DropItemsInSecondAttack;

        public static void Load()
        {
            BurerSpawnWeight = Plugin.config.Bind(
                "Spawn",
                "BurerSpawnWeight",
                50,
                new ConfigDescription(
                    "What is the chance of the Burer spawning - higher values make it more common (this is like adding tickets to a lottery - it doesn't guarantee getting picked but it vastly increases the chances)",
                    new AcceptableValueRange<int>(0, 99999)
                )
            );
            BurerSpawnPower = Plugin.config.Bind(
                "Spawn",
                "BurerSpawnPower",
                3f,
                new ConfigDescription(
                    "What's the spawn power of a Burer? How much does it subtract from the moon power pool on spawn?"
                )
            );
            BurerSpawnMax = Plugin.config.Bind(
                "Spawn",
                "BurerSpawnMax",
                3,
                new ConfigDescription(
                    "What's the maximum amount of Burers that can spawn on a given moon?"
                )
            );
            BurerSpawnLevelsSet = Plugin.config.Bind(
                "Spawn",
                "BurerSpawnLevelsSet",
                "all",
                new ConfigDescription(
                    "Which set of levels should by default let the Burer spawn on them? (Options are: all/none/modded/vanilla)"
                )
            );
            BurerSpawnLevelsWithWeight = Plugin.config.Bind(
                "Spawn",
                "BurerSpawnLevels",
                "experimentation:25, assurance:75, vow:50, march:100, offense:150, titan:300",
                new ConfigDescription(
                    "Which specific moons/levels can the Burer spawn on and with what weight? (This takes priority over the level set config option - names are matched leniently and case insensitive)"
                )
            );
            BurerDistanceToMelee = Plugin.config.Bind(
                "Behavior",
                "BurerDistanceToMelee",
                7.5f,
                new ConfigDescription(
                    "The distance at which the Burer stops fighting with telepathic attacks."
                )
            );
            BurerTelepathicAttackTime = Plugin.config.Bind(
                "Behavior",
                "BurerTelepathicAttackTime",
                6f,
                new ConfigDescription(
                    "The time it takes the Burer to complete both telepathic attacks (they are distributed evenly)."
                )
            );
            BurerSpeedInChase = Plugin.config.Bind(
                "Behavior",
                "BurerSpeedInChase",
                6f,
                new ConfigDescription(
                    "Burer's movement speed in chase."
                )
            );
            BurerDamagePerHitOnPlayer = Plugin.config.Bind(
               "Behavior",
               "BurerDamagePerHitOnPlayer",
               10,
               new ConfigDescription(
                   "Damage dealt by the Burer per hit on the player."
               )
            );
            DropItemsInSecondAttack = Plugin.config.Bind(
               "Behavior",
               "DropItemsInSecondAttack",
               true,
               new ConfigDescription(
                   "An option that configures whether Burer can knock out all items from the player's inventory on the second attack. If you find this ability too unfair, or are tired of a rather rare bug where the player will lose the ability to interact with anything if they pick up an item at the exact moment when the Burer tries to throw items out of the player's inventory, you can disable this option."
               )
           );
        }
    }
}
