using System;
using Arcen.AIW2.Core;
using Arcen.Universal;

namespace Arcen.AIW2.External
{
    public class UniversalNemesisBaseInfo : ExternalFactionBaseInfoRoot
    {
        public static readonly bool debug = false;
        //Important Value
        public static UniversalNemesisBaseInfo Instance;
        public int Intensity { get; private set; }
        public bool humanAllied = false;
        public bool aiAllied = false;
        public bool DarkAlliance = false;
        public bool SeedNearPlayer = false;
        public bool nemesisRecuperating = false;
        public Planet fleeDestination = null;
        public DoubleBufferedValue<GameEntity_Squad> Nemesis = new DoubleBufferedValue<GameEntity_Squad>(null);
        public string cachedValue_NemesisTag = "UniversalNemesis";
        public string NemesisUnitFactionTag = "NemesisFaction";

        #region Serialized
        public int bonusHull;
        public int bonusShield;
        public int lastSecondSpawnedCataclysm;
        public int lastSecondSpawnedCatastrophe;
        public int lastSecondSpawnedDisaster;
        public int lastSecondSpawnedVillain;
        public int lastSecondSpawnedHenchman;
        #endregion

        #region From Xml
        //Here are some constants
        private bool hasInitializedConstants = false;
        public int weak_nemesis_hull_divider;
        public int weak_nemesis_shield_divider;
        public int nemesis_hull_divider;
        public int nemesis_shield_divider;
        public int cataclysm_hull_divider;
        public int cataclysm_shield_divider;
        public int catastrophe_hull_divider;
        public int catastrophe_shield_divider;
        public int disaster_hull_divider;
        public int disaster_shield_divider;
        public int villain_hull_divider;
        public int villain_shield_divider;
        public int henchman_hull_divider;
        public int henchman_shield_divider;
        public int beacon_hull_divider;
        public int beacon_shield_divider;
        public int threshold_villain_spawn;
        public int threshold_disaster_spawn;
        public int threshold_catastrophe_spawn;
        public int threshold_cataclysm_spawn;
        public int gain_multiplier_per_intensity;


        private void InitializeConstantsIfNecessary()
        {
            if (!hasInitializedConstants)
            {
                hasInitializedConstants = true;
                
                this.weak_nemesis_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_weak_nemesis_hull_divider");
                this.weak_nemesis_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_weak_nemesis_shield_divider");
                this.nemesis_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_nemesis_hull_divider");
                this.nemesis_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_nemesis_shield_divider");
                this.cataclysm_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_cataclysm_hull_divider");
                this.cataclysm_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_cataclysm_shield_divider");
                this.catastrophe_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_catastrophe_hull_divider");
                this.catastrophe_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_catastrophe_shield_divider");
                this.disaster_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_disaster_hull_divider");
                this.disaster_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_disaster_shield_divider");
                this.villain_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_villain_hull_divider");
                this.villain_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_villain_shield_divider");
                this.henchman_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_henchman_hull_divider");
                this.henchman_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_henchman_shield_divider");
                this.beacon_hull_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_beacon_hull_divider");
                this.beacon_shield_divider = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_beacon_shield_divider");
                this.threshold_villain_spawn = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_threshold_villain_spawn");
                this.threshold_disaster_spawn = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_threshold_disaster_spawn");
                this.threshold_catastrophe_spawn = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_threshold_catastrophe_spawn");
                this.threshold_cataclysm_spawn = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_threshold_cataclysm_spawn");
                this.gain_multiplier_per_intensity = ExternalConstants.Instance.GetCustomInt32_Slow("custom_int_UniversalNemesis_gain_multiplier_per_intensity");
            }
        }
        #endregion

        #region Cleanup
        public UniversalNemesisBaseInfo()
        {
            Instance = this;
            Cleanup();
        }

        protected override void Cleanup()
        {
            bonusHull = 0;
            bonusShield = 0;
            lastSecondSpawnedCataclysm = 0;
            lastSecondSpawnedCatastrophe = 0;
            lastSecondSpawnedDisaster = 0;
            lastSecondSpawnedVillain = 0;
            lastSecondSpawnedHenchman = 0;
            //threshold_villain_spawn = 0;
            //threshold_disaster_spawn = 0;
            //threshold_catastrophe_spawn = 0;
            //threshold_cataclysm_spawn = 0;
            Intensity = 0;
            humanAllied = false;
            aiAllied = false;
            DarkAlliance = false;
            SeedNearPlayer = false;
            nemesisRecuperating = false;
            fleeDestination = null;
            Nemesis.Clear();
        }
        #endregion

        #region SubDoGeneralAggregationsPausedOrUnpaused
        #endregion

        #region Ser / Deser
        public override void SerializeFactionTo(SerMetaData MetaData, ArcenSerializationBuffer Buffer, SerializationCommandType SerializationCmdType)
        {
            
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, lastSecondSpawnedCataclysm);
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, lastSecondSpawnedCatastrophe);
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, lastSecondSpawnedDisaster);
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, lastSecondSpawnedVillain);
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, lastSecondSpawnedHenchman);
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, bonusHull);
            Buffer.AddInt32(MetaData, ReadStyle.NonNeg, bonusShield);
        }

        public override void DeserializeFactionIntoSelf(SerMetaData MetaData, ArcenDeserializationBuffer Buffer, SerializationCommandType SerializationCmdType)
        {
            this.lastSecondSpawnedCataclysm = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
            this.lastSecondSpawnedCatastrophe = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
            this.lastSecondSpawnedDisaster = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
            this.lastSecondSpawnedVillain = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
            this.lastSecondSpawnedHenchman = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
            this.bonusHull = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
            this.bonusShield = Buffer.ReadInt32(MetaData, ReadStyle.NonNeg);
        }
        #endregion

        #region Faction Settings
        public override int GetDifficultyOrdinal_OrNegativeOneIfNotRelevant() => Intensity;

        public string GetNemesisTag() => cachedValue_NemesisTag;

        //TODO
        protected override void DoRefreshFromFactionSettings()
        {
            // If we're a subfaction added by Splintering Spire, inherit their Intensity.
            Intensity = AttachedFaction.Config.GetIntValueForCustomFieldOrDefaultValue("Intensity", true);
            this.SeedNearPlayer = AttachedFaction.GetBoolValueForCustomFieldOrDefaultValue("SpawnNearPlayer", true);
            string invasionTime = AttachedFaction.Config.GetStringValueForCustomFieldOrDefaultValue("InvasionTime", true);
            Faction faction = this.AttachedFaction;
            if (faction.InvasionTime == -1)
            {
                //initialize the invasion time
                if (invasionTime == "Immediate")
                    faction.InvasionTime = 1;
                else if (invasionTime == "Early Game")
                    faction.InvasionTime = (60 * 60); //1 hours in
                else if (invasionTime == "Mid Game")
                    faction.InvasionTime = (2 * (60 * 60)); //1.5 hours in
                else if (invasionTime == "Late Game")
                    faction.InvasionTime = 3 * (60 * 60); //3 hours in
                if (faction.InvasionTime > 1)
                {
                    //this will be a desync on the client and host, but the host will correct the client in under 5 seconds.
                    if (Engine_Universal.PermanentQualityRandom.Next(0, 100) < 50)
                        faction.InvasionTime += Engine_Universal.PermanentQualityRandom.Next(0, faction.InvasionTime / 10);
                    else
                        faction.InvasionTime -= Engine_Universal.PermanentQualityRandom.Next(0, faction.InvasionTime / 10);
                }
            }
        }
        #endregion

        protected sealed override void DoFactionGeneralAggregationsPausedOrUnpaused()
        {
            InitializeConstantsIfNecessary();
        }

        public override void SetStartingFactionRelationships() => AllegianceHelper.EnemyThisFactionToAll(AttachedFaction);

        public override void DoPerSecondLogic_Stage2Aggregating_OnMainThreadAndPartOfSim_ClientAndHost(ArcenClientOrHostSimContextCore Context)
        {
            Nemesis.ClearConstructionValueForStartingConstruction();
            Nemesis.Construction = AttachedFaction.GetFirstMatching(GetNemesisTag(), true, true);

            Nemesis.SwitchConstructionToDisplay();
        }

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_ClientAndHost(ArcenClientOrHostSimContextCore Context)
        {
            if (Nemesis.Display != null)
                UpdateAllegiance();
        }

        //TODO
        private void UpdateAllegiance()
        {
            {
                if (ArcenStrings.Equals(this.Allegiance, "Hostile To All") ||
                   ArcenStrings.Equals(this.Allegiance, "HostileToAll") ||
                   string.IsNullOrEmpty(this.Allegiance))
                {
                    this.humanAllied = false;
                    this.aiAllied = false;
                    if (string.IsNullOrEmpty(this.Allegiance))
                        throw new Exception("empty CityShips allegiance '" + this.Allegiance + "'");
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This CityShips faction should be hostile to all (default)", Verbosity.DoNotShow);
                    //make sure this isn't set wrong somehow
                    AllegianceHelper.EnemyThisFactionToAll(AttachedFaction);
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Hostile To Players Only") ||
                        ArcenStrings.Equals(this.Allegiance, "HostileToPlayers"))
                {
                    this.aiAllied = true;
                    AllegianceHelper.AllyThisFactionToAI(AttachedFaction);
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This CityShips faction should be friendly to the AI and hostile to players", Verbosity.DoNotShow);

                }
                else if (ArcenStrings.Equals(this.Allegiance, "Minor Faction Team Red"))
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This CityShips faction is on team red", Verbosity.DoNotShow);
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Minor Faction Team Red");
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Minor Faction Team Blue"))
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This CityShips faction is on team blue", Verbosity.DoNotShow);

                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Minor Faction Team Blue");
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Minor Faction Team Green"))
                {
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This CityShips faction is on team green", Verbosity.DoNotShow);

                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Minor Faction Team Green");
                }

                else if (ArcenStrings.Equals(this.Allegiance, "HostileToAI") ||
                        ArcenStrings.Equals(this.Allegiance, "Friendly To Players"))
                {
                    this.humanAllied = true;
                    AllegianceHelper.AllyThisFactionToHumans(AttachedFaction);
                    if (debug)
                        ArcenDebugging.ArcenDebugLogSingleLine("This CityShips faction should be hostile to the AI and friendly to players", Verbosity.DoNotShow);
                }
                else if (ArcenStrings.Equals(this.Allegiance, "Dark Alliance"))
                {
                    AllegianceHelper.AllyThisFactionToMinorFactionTeam(AttachedFaction, "Dark Alliance");
                    this.DarkAlliance = true;
                }
                else
                {
                    throw new Exception("unknown CityShips allegiance '" + this.Allegiance + "'");
                }
            }
        }

        public override float CalculateYourPortionOfPredictedGameLoad_Where100IsANormalAI(ArcenCharacterBufferBase OptionalExplainCalculation)
        {
            DoRefreshFromFactionSettings();

            int load = 20 + (Intensity * 3);

            if (OptionalExplainCalculation != null)
                OptionalExplainCalculation.Add(load).Add($" Load From UniversalNemesis");
            return load;
        }

        #region GetShouldAttackNormallyExcludedTarget
        public override bool GetShouldAttackNormallyExcludedTarget(GameEntity_Squad Target)
        {
            try
            {
                Faction targetControllingFaction = Target.GetFactionOrNull_Safe();
                //bool planetHasWarpGate = false;
                //targetControllingFaction.DoForEntities( EntityRollupType.WarpEntryPoints, delegate ( GameEntity_Squad entity )
                //{
                //    if ( entity.Planet == Target.Planet )
                //        planetHasWarpGate = true;
                //    return DelReturn.Continue;
                //} );

                if (!ArcenStrings.Equals(this.Allegiance, "Friendly To Players"))
                {
                    //Human Allied factions will optionally leave an AI command station intact, so as to not drive AIP up so high
                    if (Target.TypeData.IsCommandStation)
                    {
                        return true;
                    }
                }
                if (Target.TypeData.GetHasTag("NormalPlanetNastyPick") || Target.TypeData.GetHasTag("DSAA"))
                    return true;

                return false;
            }
            catch (ArcenPleaseStopThisThreadException)
            {
                //this one is ok -- just means the thread is ending for some reason.  I guess we'll skip trying the normal handling here
                return false;
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLog("UnversalNemesis GetShouldAttackNormallyExcludedTarget error:" + e, Verbosity.ShowAsError);
                return false;
            }
        }
        #endregion

        //TODO
        #region UpdatePowerLevel
        public override void UpdatePowerLevel()
        {
            FInt result = FInt.Zero;
            
            int totalGains = this.bonusHull + this.bonusShield;
            if(totalGains > this.threshold_disaster_spawn)
            {
                result = FInt.FromParts(1, 000);
            }
            if(totalGains > this.threshold_catastrophe_spawn)
            {
                result = FInt.FromParts(2, 000);
            }
            if (totalGains > this.threshold_cataclysm_spawn)
            {
                result = FInt.FromParts(3, 000);
            }
            if (totalGains > this.threshold_cataclysm_spawn * 5)
            {
                result = FInt.FromParts(4, 000);
            }
            if (totalGains > this.threshold_cataclysm_spawn * 20)
            {
                result = FInt.FromParts(4, 500);
            }
            if (!nemesisRecuperating)
            {
                result += FInt.FromParts(0, 500);
            }
            this.AttachedFaction.OverallPowerLevel = result;

            //if ( World_AIW2.Instance.GameSecond % 60 == 0 )
            //    ArcenDebugging.ArcenDebugLogSingleLine("resulting power level: " + faction.OverallPowerLevel, Verbosity.DoNotShow );
        }
        #endregion
        
    }
}
