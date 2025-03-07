using p5rpc.misc.customizablemerciless.Configuration;
using p5rpc.misc.customizablemerciless.Template;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Enums;
using Reloaded.Memory;
using Reloaded.Memory.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Memory.Utilities;
using Reloaded.Mod.Interfaces;
using System;
using System.Collections;
using System.Diagnostics;
using BF.File.Emulator;
using BF.File.Emulator.Interfaces;
using CriFs.V2.Hook;
using CriFs.V2.Hook.Interfaces;
using System.Runtime.CompilerServices;
using Reloaded.Mod.Interfaces.Internal;
using Tomlyn;
using Tomlyn.Model;

namespace p5rpc.misc.customizablemerciless
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        internal static nint BaseAddress { get; private set; }

        private nuint Exp_Multiplier;
        private nuint Money_Multiplier;
        private nuint Merciless_Taken_CritTech_Multiplier;
        private nuint Merciless_Given_CritTech_Multiplier;
        private nuint Merciless_Taken_Weak_Multiplier;
        private nuint Merciless_Given_Weak_Multiplier;
        private nuint Merciless_Damage_Taken_Multiplier;
        private nuint Merciless_Damage_Given_Multiplier;

        private nuint Money_Unknown_Multiplier;
        private nuint Default_Multiplier;
        private nuint Safe_Exp_Money_Multiplier;
        private nuint Easy_Exp_Money_Multiplier;
        private nuint Hard_Damage_Taken_Multiplier;

        private bool applied = false;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            var modDir = _modLoader.GetDirectoryForModId(_modConfig.ModId);

            using var thisProcess = Process.GetCurrentProcess();
            BaseAddress = thisProcess.MainModule!.BaseAddress;

            var reloadedHooksController = _modLoader.GetController<IReloadedHooks>();
            if (reloadedHooksController == null || !reloadedHooksController.TryGetTarget(out var reloadedHooks))
            {
                throw new Exception("Failed to get IReloadedHooks Controller");
            }

            var memory = Memory.Instance;

            Exp_Multiplier = memory.Allocate(4).Address;
            Money_Multiplier = memory.Allocate(4).Address;
            Merciless_Damage_Taken_Multiplier = memory.Allocate(4).Address;
            Merciless_Damage_Given_Multiplier = memory.Allocate(4).Address;
            Merciless_Taken_CritTech_Multiplier = memory.Allocate(4).Address;
            Merciless_Given_CritTech_Multiplier = memory.Allocate(4).Address;
            Merciless_Taken_Weak_Multiplier = memory.Allocate(4).Address;
            Merciless_Given_Weak_Multiplier = memory.Allocate(4).Address;

            Money_Unknown_Multiplier = memory.Allocate(4).Address;
            Default_Multiplier = memory.Allocate(4).Address;
            Safe_Exp_Money_Multiplier = memory.Allocate(4).Address;
            Easy_Exp_Money_Multiplier = memory.Allocate(4).Address;
            Hard_Damage_Taken_Multiplier = memory.Allocate(4).Address; 

            memory.Write(Money_Unknown_Multiplier, 1.15F);
            memory.Write(Default_Multiplier, 1F);
            memory.Write(Safe_Exp_Money_Multiplier, 1.5F);
            memory.Write(Easy_Exp_Money_Multiplier, 1.2F);
            memory.Write(Hard_Damage_Taken_Multiplier, 1.6F);

            this._modLoader.ModLoading += this.OnModLoading;
            this._modLoader.OnModLoaderInitialized += this.OnModLoaderInitialized;

            // Exp & Money

            SigScan("44 0F 28 DF 44 8B 5C 24", "SEPARATE_MONEY_MULTIPLIER_1", address =>
            {
                memory.SafeWrite((nuint)address, Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 4))));
            });

            SigScan("F3 44 0F 59 1D ?? ?? ?? ?? E9 ?? ?? ?? ?? 85 C0", "SEPARATE_MONEY_MULTIPLIER_2", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"mulss xmm11, [dword {Money_Unknown_Multiplier}]"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            SigScan("84 C0 74 ?? 0F 28 FE EB ?? B9 61 00 00 40", "SET_SAFE_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm11, [dword {Safe_Exp_Money_Multiplier}]",
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("F3 0F 10 3D ?? ?? ?? ?? E8 ?? ?? ?? ?? 44 0F 28 E0", "SEPARATE_EASY_EXP_MULTIPLIER", address =>
            {
                memory.SafeWrite((nuint)address, Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 8))));
            });

            SigScan("E8 ?? ?? ?? ?? 84 C0 75 ?? B9 62 00 00 40 E8 ?? ?? ?? ?? 84 C0 74", "SET_EASY_EXP_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "jz end_of_hook",
                    $"movss xmm7, [dword {Easy_Exp_Money_Multiplier}]",
                    $"movss xmm11, [dword {Easy_Exp_Money_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("84 C0 74 ?? 41 0F 28 F9 EB ?? B9 63 00 00 40", "SET_NORMAL_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm11, [dword {Default_Multiplier}]",
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("84 C0 74 ?? 41 0F 28 F9 EB ?? B9 64 00 00 40", "SET_HARD_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm11, [dword {Default_Multiplier}]",
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("E8 ?? ?? ?? ?? 84 C0 74 ?? F3 0F 10 3D ?? ?? ?? ?? E8", "SET_MERCILESS_EXP_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "jz end_of_hook",
                    $"movss xmm7, [dword {Exp_Multiplier}]",
                    $"movss xmm11, [dword {Money_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            // Exp & Money for Ryuji's Instakill ability

            SigScan("45 0F 28 C3 48 8D 15", "SEPARATE_MONEY_INSTAKILL_MULTIPLIER_1", address =>
            {
                memory.SafeWrite((nuint)address, Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 4))));
            });

            SigScan("F3 44 0F 59 05 ?? ?? ?? ?? E9 ?? ?? ?? ?? 85 C0", "SEPARATE_MONEY_INSTAKILL_MULTIPLIER_2", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"mulss xmm8, [dword {Money_Unknown_Multiplier}]"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            SigScan("84 C0 74 ?? 44 0F 28 DE", "SET_SAFE_INSTAKILL_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm8, [dword {Safe_Exp_Money_Multiplier}]",
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("F3 44 0F 10 1D ?? ?? ?? ?? EB ?? 85 C0", "SET_EASY_INSTAKILL_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm8, [dword {Easy_Exp_Money_Multiplier}]"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("E8 ?? ?? ?? ?? 84 C0 75 ?? B9 63 00 00 40 E8 ?? ?? ?? ?? 84 C0 75 ?? B9 64 00 00 40 E8 ?? ?? ?? ?? 84 C0 74 ?? F3 44 0F 10 1D", "SET_NORMAL_INSTAKILL_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "jz end_of_hook",
                    $"movss xmm8, [dword {Default_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("E8 ?? ?? ?? ?? 84 C0 75 ?? B9 64 00 00 40 E8 ?? ?? ?? ?? 84 C0 74 ?? F3 44 0F 10 1D", "SET_HARD_INSTAKILL_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "jz end_of_hook",
                    $"movss xmm8, [dword {Default_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("F3 44 0F 10 1D ?? ?? ?? ?? EB ?? 45 0F 28 DC", "SET_MERCILESS_INSTAKILL_EXP_MONEY_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm8, [dword {Money_Multiplier}]",
                    $"movss xmm11, [dword {Exp_Multiplier}]"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            // Damage Taken & Given

            SigScan("41 0F 28 F6 EB ?? F3 0F 10 35 ?? ?? ?? ?? 41 8B 4C 24", "SEPARATE_HARD_TAKEN_1", address =>
            {
                memory.SafeWrite((nuint)address, Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 4))));
            });

            SigScan("F3 0F 10 35 ?? ?? ?? ?? 41 8B 4C 24", "SEPARATE_HARD_TAKEN_2", address =>
            {
                memory.SafeWrite((nuint)address, Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 8))));
            });

            SigScan("84 C0 74 ?? 66 83 F9 02", "SET_HARD_TAKEN_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "jnz end_of_hook",
                    $"movss xmm6, [dword {Hard_Damage_Taken_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("E8 ?? ?? ?? ?? 84 C0 74 ?? 66 83 7B ?? 02 74 ?? 41 0F 28 F6", "SET_MERCILESS_TAKEN_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "jz end_of_hook",
                    $"movss xmm6, [dword {Merciless_Damage_Taken_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            SigScan("66 83 7B ?? 02 74 ?? 41 0F 28 F6", "SET_MERCILESS_GIVEN_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    $"movss xmm6, [dword {Merciless_Damage_Given_Multiplier}]",
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.ExecuteAfter).Activate();
            });

            // Merciless critical, technical, & weakness multipliers

            SigScan("F3 0F 59 35 ?? ?? ?? ?? E9 ?? ?? ?? ?? 66 83 FE 04", "SET_MERCILESS_CRITICAL_TECHNICAL_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "cmp word [rbx + 0x4], 0x2",
                    "jz isTaken",
                    $"mulss xmm6, [dword {Merciless_Given_CritTech_Multiplier}]",
                    "jmp end_of_hook",
                    "label isTaken",
                    $"mulss xmm6, [dword {Merciless_Taken_CritTech_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            SigScan("F3 0F 59 35 ?? ?? ?? ?? B9 D8 01 00 30", "SET_MERCILESS_WEAKNESS_MULTIPLIER", address =>
            {
                string[] Function =
                {
                    "use64",
                    "cmp word [rbx + 0x4], 0x2",
                    "jz isTaken",
                    $"mulss xmm6, [dword {Merciless_Given_Weak_Multiplier}]",
                    "jmp end_of_hook",
                    "label isTaken",
                    $"mulss xmm6, [dword {Merciless_Taken_Weak_Multiplier}]",
                    "label end_of_hook"
                };
                reloadedHooks.CreateAsmHook(Function, address, AsmHookBehaviour.DoNotExecuteOriginal).Activate();
            });

            // For more information about this template, please see
            // https://reloaded-project.github.io/Reloaded-II/ModTemplate/

            // If you want to implement e.g. unload support in your mod,
            // and some other neat features, override the methods in ModBase.

            // TODO: Implement some mod logic
        }
        private void OnModLoading(IModV1 mod, IModConfigV1 config)
        {
            var modDir = this._modLoader.GetDirectoryForModId(config.ModId);
            var tomlDir = Path.Join(modDir, "CustomizableMerciless.toml");

            if (File.Exists(tomlDir) & !applied & config.ModDependencies.Contains(this._modConfig.ModId) & _configuration.AllowToml)
            {
                string tomlContent = System.IO.File.ReadAllText(tomlDir);
                var tomlDoc = Toml.Parse(tomlContent);
                var tomlDict = new Dictionary<string, Dictionary<string, object>>();
                foreach (var table in tomlDoc.ToModel())
                {
                    var sectionName = table.Key;
                    var sectionData = table.Value as TomlTable;

                    if (sectionData != null)
                    {
                        var sectionDict = new Dictionary<string, object>();
                        foreach (var kvp in sectionData)
                        {
                            sectionDict[kvp.Key] = kvp.Value;
                        }
                        tomlDict[sectionName] = sectionDict;
                    }
                }

                _configuration.DamageTaken = Convert.ToDouble(tomlDict["Multipliers"]["DamageTaken"]);
                _configuration.DamageGiven = Convert.ToDouble(tomlDict["Multipliers"]["DamageGiven"]);
                _configuration.ExpWon = Convert.ToDouble(tomlDict["Multipliers"]["ExpWon"]);
                _configuration.MoneyWon = Convert.ToDouble(tomlDict["Multipliers"]["MoneyWon"]);
                _configuration.WeaknessTaken = Convert.ToDouble(tomlDict["Multipliers"]["WeaknessTaken"]);
                _configuration.WeaknessGiven = Convert.ToDouble(tomlDict["Multipliers"]["WeaknessGiven"]);
                _configuration.CritTechTaken = Convert.ToDouble(tomlDict["Multipliers"]["CritTechTaken"]);
                _configuration.CritTechGiven = Convert.ToDouble(tomlDict["Multipliers"]["CritTechGiven"]);

                _configuration.GallowsExp = Convert.ToBoolean(tomlDict["Others"]["GallowsExp"]);
                _configuration.ReturnSafeRoom = Convert.ToBoolean(tomlDict["Others"]["ReturnSafeRoom"]);
                applied = true;
            }

            // Debug lines

            _logger.WriteLine("Customizable Merciless: Exp Won: " + _configuration.ExpWon.ToString());
            _logger.WriteLine("Customizable Merciless: Money Won: " + _configuration.MoneyWon.ToString());
            _logger.WriteLine("Customizable Merciless: Damage Taken: " + _configuration.DamageTaken.ToString());
            _logger.WriteLine("Customizable Merciless: Damage Dealt: " + _configuration.DamageGiven.ToString());
            _logger.WriteLine("Customizable Merciless: Critical & Technical Taken: " + _configuration.CritTechTaken.ToString());
            _logger.WriteLine("Customizable Merciless: Critical & Technical Given: : " + _configuration.CritTechGiven.ToString());
            _logger.WriteLine("Customizable Merciless: Weakness Taken: " + _configuration.WeaknessTaken.ToString());
            _logger.WriteLine("Customizable Merciless: Weakness Given: " + _configuration.WeaknessGiven.ToString());

            _logger.WriteLine("Customizable Merciless: 1/3 Gallows Exp Rate: " + _configuration.GallowsExp.ToString());
            _logger.WriteLine("Customizable Merciless: Return to Prior Safe/Waiting Room: " + _configuration.ReturnSafeRoom.ToString());
        }

        private void OnModLoaderInitialized()
        {
            var criFsController = _modLoader.GetController<ICriFsRedirectorApi>();
            if (criFsController == null || !criFsController.TryGetTarget(out var criFsApi))
            {
                throw new Exception("Failed to get ICriFsRedirectorApi Controller");
            }

            var BfEmulatorController = _modLoader.GetController<IBfEmulator>();
            if (BfEmulatorController == null || !BfEmulatorController.TryGetTarget(out var BfEmulator))
            {
                throw new Exception("Failed to get IBfEmulator Controller");
            }

            var modDir = _modLoader.GetDirectoryForModId(_modConfig.ModId);

            var memory = Memory.Instance;

            memory.Write(Exp_Multiplier, (float)_configuration.ExpWon);
            memory.Write(Money_Multiplier, (float)_configuration.MoneyWon);
            memory.Write(Merciless_Damage_Taken_Multiplier, (float)_configuration.DamageTaken);
            memory.Write(Merciless_Damage_Given_Multiplier, (float)_configuration.DamageGiven);
            memory.Write(Merciless_Taken_CritTech_Multiplier, (float)_configuration.CritTechTaken);
            memory.Write(Merciless_Given_CritTech_Multiplier, (float)_configuration.CritTechGiven);
            memory.Write(Merciless_Taken_Weak_Multiplier, (float)_configuration.WeaknessTaken);
            memory.Write(Merciless_Given_Weak_Multiplier, (float)_configuration.WeaknessGiven);

            // Merciless gallows exp rate toggle

            if (!_configuration.GallowsExp)
            {
                SigScan("74 ?? B8 56 55 55 55 41 F7 E8", "GALLOWS_EXP_MULTIPLIER", address =>
                {
                    memory.SafeWrite((nuint)address, Convert.FromHexString(string.Concat(Enumerable.Repeat("90", 27))));
                });
            }

            // Return to prior safe/waiting room toggle

            if (_configuration.ReturnSafeRoom)
            {
                criFsApi.AddProbingPath(Path.Combine(modDir, $"ReturnSafeRoom"));
                BfEmulator.AddDirectory(Path.Combine(modDir, $"ReturnSafeRoom"));
            }

            // Debug lines

            _logger.WriteLine("Customizable Merciless: Exp Won: " + _configuration.ExpWon.ToString());
            _logger.WriteLine("Customizable Merciless: Money Won: " + _configuration.MoneyWon.ToString());
            _logger.WriteLine("Customizable Merciless: Damage Taken: " + _configuration.DamageTaken.ToString());
            _logger.WriteLine("Customizable Merciless: Damage Dealt: " + _configuration.DamageGiven.ToString());
            _logger.WriteLine("Customizable Merciless: Critical & Technical Taken: " + _configuration.CritTechTaken.ToString());
            _logger.WriteLine("Customizable Merciless: Critical & Technical Given: : " + _configuration.CritTechGiven.ToString());
            _logger.WriteLine("Customizable Merciless: Weakness Taken: " + _configuration.WeaknessTaken.ToString());
            _logger.WriteLine("Customizable Merciless: Weakness Given: " + _configuration.WeaknessGiven.ToString());

            _logger.WriteLine("Customizable Merciless: 1/3 Gallows Exp Rate: " + _configuration.GallowsExp.ToString());
            _logger.WriteLine("Customizable Merciless: Return to Prior Safe/Waiting Room: " + _configuration.ReturnSafeRoom.ToString());
        }

        private void SigScan(string pattern, string name, Action<nint> action)
        {
            var startupScannerController = _modLoader.GetController<IStartupScanner>();
            if (startupScannerController == null || !startupScannerController.TryGetTarget(out var startupScanner))
            {
                throw new Exception("Failed to get IStartupScanner Controller");
            }

            startupScanner.AddMainModuleScan(pattern, result =>
            {
                if (result.Found)
                {
                    action(result.Offset + BaseAddress);
                    _logger.WriteLine($"Customizable Merciless: {name} Signature Found: {result.Offset + BaseAddress:X}");
                }
                else
                {
                    _logger.WriteLine($"Customizable Merciless: {name}: Signature Not Found ");
                }
            });
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}