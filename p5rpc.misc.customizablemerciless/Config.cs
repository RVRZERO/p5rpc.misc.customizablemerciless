using p5rpc.misc.customizablemerciless.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;
using System.ComponentModel;

namespace p5rpc.misc.customizablemerciless.Configuration
{
    public class Config : Configurable<Config>
    {
        /*
            User Properties:
                - Please put all of your configurable properties here.
    
            By default, configuration saves as "Config.json" in mod user config folder.    
            Need more config files/classes? See Configuration.cs
    
            Available Attributes:
            - Category
            - DisplayName
            - Description
            - DefaultValue

            // Technically Supported but not Useful
            - Browsable
            - Localizable

            The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
        */

        [Category("Multipliers")]
        [DisplayName("Damage Taken")]
        [Description("Write the multiplier for Damage Taken in Merciless difficulty.")]
        [DefaultValue(1.60D)]
        public double MercilessTaken { get; set; } = 1.60D;

        [Category("Multipliers")]
        [DisplayName("Damage Given")]
        [Description("Write the multiplier for Damage Given in Merciless difficulty.")]
        [DefaultValue(0.8D)]
        public double MercilessGiven { get; set; } = 0.8D;

        [Category("Multipliers")]
        [DisplayName("Exp Won")]
        [Description("Write the multiplier for Exp Won in Merciless difficulty.")]
        [DefaultValue(1.20D)]
        public double MercilessExp { get; set; } = 1.20D;

        [Category("Multipliers")]
        [DisplayName("Money Won")]
        [Description("Write the multiplier for Money Won in Merciless difficulty.")]
        [DefaultValue(1.20D)]
        public double MercilessMoney { get; set; } = 1.20D;

        [Category("Multipliers")]
        [DisplayName("Weakness Taken")]
        [Description("Write the multiplier for Weakness Taken attacks in Merciless difficulty.")]
        [DefaultValue(3.00D)]
        public double MercilessWeakTaken { get; set; } = 3.00D;

        [Category("Multipliers")]
        [DisplayName("Weakness Given")]
        [Description("Write the multiplier for Weakness Given attacks in Merciless difficulty.")]
        [DefaultValue(3.00D)]
        public double MercilessWeakGiven { get; set; } = 3.00D;

        [Category("Multipliers")]
        [DisplayName("Critical & Technical Taken")]
        [Description("Write the multiplier for Technical & Critical Taken attacks in Merciless difficulty.")]
        [DefaultValue(3.00D)]
        public double MercilessCritTechTaken { get; set; } = 3.00D;

        [Category("Multipliers")]
        [DisplayName("Critical & Technical Given")]
        [Description("Write the multiplier for Technical & Critical Given attacks in Merciless difficulty.")]
        [DefaultValue(3.00D)]
        public double MercilessCritTechGiven { get; set; } = 3.00D;

        [Category("Others")]
        [DisplayName("1/3 Gallows Exp Rate")]
        [Description("Enable this to have Merciless difficulty Gallows Exp Rate in Merciless difficulty.")]
        [DefaultValue(true)]
        public bool GallowsExp { get; set; } = true;

        [Category("Others")]
        [DisplayName("Return to Prior\nSafe/Waiting Room")]
        [Description("Enable this to be able to Return to Prior Safe/Waiting Room in Merciless difficulty.")]
        [DefaultValue(false)]
        public bool RetryButton { get; set; } = false;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
