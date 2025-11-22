using BepInEx.Configuration;
using System;

namespace EliteReworks2.Elites
{
    //Based off of TILER code
    public abstract class TweakBase<T> : TweakBase where T : TweakBase<T>
    {
        //This, which you will see on all the -base classes, will allow both you and other modders to enter through any class with this to access internal fields/properties/etc as if they were a member inheriting this -Base too from this class.
        public static T Instance { get; private set; }

        public TweakBase()
        {
            if (Instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting TweakBase was instantiated twice");
            Instance = this as T;
        }
    }

    public abstract class TweakBase
    {
        public abstract string ConfigCategoryString { get; }
        public abstract string ConfigOptionName { get; }

        public abstract string ConfigDescriptionString { get; }

        public ConfigEntry<bool> Enabled { get; private set; }


        protected virtual void ReadConfig(ConfigFile config)
        {
            Enabled = config.Bind<bool>(ConfigCategoryString, ConfigOptionName, true, ConfigDescriptionString);
        }

        internal void Init(ConfigFile config)
        {
            ReadConfig(config);
            if (!Enabled.Value) return;
            ApplyChanges();
        }

        protected virtual void ApplyChanges()
        {

        }
    }
}

