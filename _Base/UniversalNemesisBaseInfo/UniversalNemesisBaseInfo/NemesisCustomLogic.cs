﻿using Arcen.AIW2.Core;
using Arcen.Universal;
using System;
using UnityEngine;
using Arcen.AIW2.External;

namespace Arcen.AIW2.External
{

    public class UniversalNemesis_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic, IGameEntityDescriptionAppender
    {
        public void AddToDescriptionBuffer(GameEntity_Squad RelatedEntityOrNull, GameEntityTypeData RelatedEntityTypeData, ArcenCharacterBufferBase Buffer)
        {
            try
            {
                UniversalNemesisBaseInfo factionExternal = RelatedEntityOrNull.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();

                Buffer.Add("Flee Destination: " + factionExternal.fleeDestination.ToString());
            }
            catch
            {

            }
            
        }

        //Doesn't work atm
        /*IGameEntitySpeedAdjuster
        public int GetAdjustedBaseSpeed(GameEntity_Squad RelatedEntity, ref int BaseSpeed)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            if(factionExternal.nemesisRecuperating)
                            {
                                BaseSpeed = 2200;
                            }
                        }
                    }
                }
            }
            catch
            {

            }
            return BaseSpeed;
        }
        */
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            //Add the gains on top, but based on the difficulty adjusted hull, otherwise weird things would happen due to being based on the unadjusted hull
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.nemesis_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
        }

        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            //To deal with the harmless game start error cringe
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();

                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            //Add the gains on top
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.nemesis_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            //Add the gains on top
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.nemesis_shield_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //Avoids gamestart harmless errors
            try
            {
                UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                GameEntity_Squad Nemesis = factionExternal.Nemesis.Display;
                PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
                //the flee destination will prevent the nemesis from changing directions multiple times before settling on an allied planet
                if (Nemesis.Planet == factionExternal.fleeDestination)
                {
                    factionExternal.fleeDestination = null;
                }
                if (factionExternal != null)
                {
                    if (Nemesis.GetHullPercent() < 75)
                    {
                        factionExternal.nemesisRecuperating = true;
                        
                        var factionData = Nemesis.Planet.GetStanceDataForFaction(factionExternal.AttachedFaction);
                        if (factionData[FactionStance.Hostile].TotalStrength > 2000 && factionExternal.fleeDestination == null)//  && !BaseInfo.Nemesis.Display.HasExplicitOrders()  && 
                        {
                            List<Planet> planetsToFlee = Planet.GetTemporaryPlanetList("UniversalNemesis-planetsToFlee", 10f);
                            planetsToFlee.Clear();
                            //flee
                            Nemesis.Planet.DoForPlanetsWithinXHops(-1, delegate (Planet planet, Int16 Distance)
                            {
                                //debugCode = 1600;
                                
                                var planetToFleeFactionData = planet.GetStanceDataForFaction(factionExternal.AttachedFaction);
                                if (planetToFleeFactionData[FactionStance.Self].TotalStrength > 10000 && planetToFleeFactionData[FactionStance.Hostile].TotalStrength < 2000)
                                {
                                    planetsToFlee.Add(planet);
                                }
                                return DelReturn.Continue;
                            });
                            if (planetsToFlee.Count == 0)
                            {
                                //stops memory leaks
                                Planet.ReleaseTemporaryPlanetList(planetsToFlee);
                                return; //stay on planet since there's nowhere to flee to
                            }
                            Planet target = null;
                            //choose a planet among our choices of planets to flee to
                            for (int i = 0; i < planetsToFlee.Count; i++)
                            {
                                if (Context.RandomToUse.Next(0, 100) < 20)
                                {
                                    //weighted choice
                                    target = planetsToFlee[i];
                                }
                            }
                            if (target == null) //if we didn't pick one randomly
                                target = planetsToFlee[0];
                            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache(Nemesis.PlanetFaction.Faction, "CityShipsSendShipToPlanet", Nemesis.Planet, target, PathingMode.Safest, Context, pathingCacheData);

                            if (pathCache != null && pathCache.PathToReadOnly.Count > 0)
                            {
                                GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse);
                                command.RelatedString = "CityShips_Dest";
                                command.RelatedEntityIDs.Add(Nemesis.PrimaryKeyID);
                                for (int k = 0; k < pathCache.PathToReadOnly.Count; k++)
                                    command.RelatedIntegers.Add(pathCache.PathToReadOnly[k].Index);
                                World_AIW2.Instance.QueueGameCommand(factionExternal.AttachedFaction, command, false);
                            }

                            factionExternal.fleeDestination = target;
                            Planet.ReleaseTemporaryPlanetList(planetsToFlee);
                        }
                        else //planet with no hostiles, just recuperate
                        {
                            //return;
                        }
                       
                    }
                    //note that this code makes it so the nemesis does nothing proactively from 50% to 90% hull, this is intended
                    else if (Nemesis.GetHullPercent() > 90) //good hull, so lets do stuff
                    {
                        List<Planet> planetsToAttack = Planet.GetTemporaryPlanetList("UniversalNemesis-planetsToAttack", 10f);

                        factionExternal.nemesisRecuperating = false;
                        //change targets once ennemy strength below 2 (so we get some leeway)
                        //factionExternal.AttackPlanetDecisionMaking(Nemesis, Context, pathingCacheData);
                        var factionData = Nemesis.Planet.GetStanceDataForFaction(factionExternal.AttachedFaction);
                        planetsToAttack.Clear();
                        if (factionData[FactionStance.Hostile].TotalStrength < 2000 && !Nemesis.HasExplicitOrders())
                        {
                            //create a beacon
                            GameEntityTypeData beaconTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalBeacon");
                            if (Nemesis.Planet.GetFirstMatching(beaconTypeData, false, false) == null)
                            {
                                PlanetFaction pFaction = Nemesis.PlanetFaction;
                                //hopefully this is dead center
                                ArcenHostOnlySimContext hostctx = Context.GetHostOnlyContext();
                                ArcenPoint spawnLocation = Nemesis.Planet.GetSafePlacementPointAroundPlanetCenter(hostctx, beaconTypeData, FInt.FromParts(0, 000), FInt.FromParts(0, 000));
                                GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, beaconTypeData, Nemesis.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, spawnLocation, hostctx, "UniversalBeacon");
                            }
                            Nemesis.Planet.DoForPlanetsWithinXHops(-1, delegate (Planet planet, Int16 Distance)
                            {
                                //debugCode = 1600;
                                var otherPlanetFactionData = planet.GetStanceDataForFaction(factionExternal.AttachedFaction);
                                if (otherPlanetFactionData[FactionStance.Hostile].TotalStrength > 2000 && !planet.IsZenithArchitraveTerritory && !(planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithDysonSphere") && !(planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "SpireSphere_Gray") && !(planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "SpireSphere_Chromatic") && !(planet.GetFirstMatching(GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "NanobotHive"), false, false) != null))
                                {
                                    planetsToAttack.Add(planet);
                                }
                                return DelReturn.Continue;
                            });
                            if (planetsToAttack.Count == 0)
                            {
                                //no restrictions, as a fallback so we keep moving (or we made a sea of neutral planets)
                                Nemesis.Planet.DoForPlanetsWithinXHops(1, delegate (Planet planet, Int16 Distance)
                                {
                                    //debugCode = 1600;
                                    if (!planet.IsZenithArchitraveTerritory && !(planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithDysonSphere") && !(planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "SpireSphere_Gray") && !(planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "SpireSphere_Chromatic") && !(planet.GetFirstMatching(GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "NanobotHive"), false, false) != null))
                                        planetsToAttack.Add(planet);
                                    return DelReturn.Continue;
                                });
                            }
                            Planet target = null;
                            //choose a planet among our choices of planets to attack
                            for (int i = 0; i < planetsToAttack.Count; i++)
                            {
                                if (Context.RandomToUse.Next(0, 100) < 40)
                                {
                                    //weighted choice
                                    target = planetsToAttack[i];

                                    break;
                                }
                            }
                            if (target == null) //if we didn't pick one randomly
                                target = planetsToAttack[0];
                            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache(Nemesis.PlanetFaction.Faction, "CityShipsSendShipToPlanet", Nemesis.Planet, target, PathingMode.Safest, Context, pathingCacheData);

                            if (pathCache != null && pathCache.PathToReadOnly.Count > 0)
                            {
                                GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse);
                                command.RelatedString = "CityShips_Dest";
                                command.RelatedEntityIDs.Add(Nemesis.PrimaryKeyID);
                                for (int k = 0; k < pathCache.PathToReadOnly.Count; k++)
                                    command.RelatedIntegers.Add(pathCache.PathToReadOnly[k].Index);
                                World_AIW2.Instance.QueueGameCommand(factionExternal.AttachedFaction, command, false);
                            }

                        }
                        //we stumbled upon ennemies while moving
                        else if (factionData[FactionStance.Hostile].TotalStrength > 2000 && Nemesis.HasExplicitOrders() && !Nemesis.Planet.IsZenithArchitraveTerritory && !(Nemesis.Planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "ZenithDysonSphere") && !(Nemesis.Planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "SpireSphere_Gray") && !(Nemesis.Planet.GetControllingOrInfluencingFaction().SpecialFactionData.InternalName == "SpireSphere_Chromatic") && !(Nemesis.Planet.GetFirstMatching(GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "NanobotHive"), false, false) != null))
                        {
                            GameCommand moveCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse);
                            moveCommand.PlanetOrderWasIssuedFrom = Nemesis.Planet.Index;
                            moveCommand.RelatedPoints.Add(Nemesis.WorldLocation);
                            moveCommand.RelatedEntityIDs.Add(Nemesis.PrimaryKeyID);
                            World_AIW2.Instance.QueueGameCommand(factionExternal.AttachedFaction, moveCommand, false);
                        }
                        Planet.ReleaseTemporaryPlanetList(planetsToAttack);
                    }
                }
                pathingCacheData.ReturnToPool();
            }
            catch
            {

            }
            
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }

    public class UniversalCataclysm_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic
    {
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.cataclysm_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
            return BaseCorrosionDamage;
        }
        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.cataclysm_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.cataclysm_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //wow
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }

    public class UniversalCatastrophe_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic
    {
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.catastrophe_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseCorrosionDamage = BaseCorrosionDamage * factionExternal.Intensity / 10;
                            BaseCorrosionDamage = BaseCorrosionDamage + (int)(BaseCorrosionDamage * (((float)factionExternal.bonusHull / factionExternal.catastrophe_hull_divider) / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
            return BaseCorrosionDamage;
        }
        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.catastrophe_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
             
            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.catastrophe_shield_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //wow
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }

    public class UniversalDisaster_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic
    {
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.disaster_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
            return BaseCorrosionDamage;
        }
        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.disaster_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
             
            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.disaster_shield_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //wow
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }

    public class UniversalVillain_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic
    {
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.villain_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
            return BaseCorrosionDamage;
        }
        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.villain_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.villain_shield_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //wow
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }

    public class UniversalHenchman_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic
    {
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.henchman_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
            return BaseCorrosionDamage;
        }
        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.henchman_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
           
            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                //if(UniversalNemesisBaseInfo. != null){

                //}
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.henchman_shield_divider);
                        }
                    }
                }
            }
            catch
            {

            }
            
            
            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //wow
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }

    public class UniversalBeacon_CustomLogic : IGameEntityStatAdjuster, IGameEntityDamageAdjuster, IGameEntityPerSecondSpecialLogic
    {
        public int GetAdjustedBaseDamage(GameEntity_Squad RelatedEntity, EntitySystem RelatedSystem, ref int BaseDamage, ref int BaseCorrosionDamage)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        //I am using RelatedEntity.DataForMark.BaseHullPoints to know how much damage to give
                        //IE we get a ratio of the hull increase and increase the weapons by the same ratio
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseDamage = BaseDamage * factionExternal.Intensity / 10;
                            BaseDamage = BaseDamage + (int)(BaseDamage * ((float)factionExternal.bonusHull / factionExternal.beacon_hull_divider / (RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10)));
                        }
                    }
                }
            }
            catch
            {

            }

            return BaseDamage;
            return BaseCorrosionDamage;
        }
        public int GetAdjustedBaseHull(GameEntity_Squad RelatedEntity, ref int BaseHull)
        {
            try
            {
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseHull = RelatedEntity.DataForMark.BaseHullPoints * factionExternal.Intensity / 10;
                            BaseHull = BaseHull + (factionExternal.bonusHull / factionExternal.beacon_hull_divider);
                        }
                    }
                }
            }
            catch
            {

            }


            return BaseHull;
        }

        public int GetAdjustedBaseShields(GameEntity_Squad RelatedEntity, ref int BaseShields)
        {
            try
            {
                /*if (UniversalNemesisBaseInfo.Instance != null)
                {

                }*/
                if (RelatedEntity.GetFactionInternalNameSafe() == "UniversalNemesis")
                {
                    if (!RelatedEntity.IsFakeEntity)
                    {
                        UniversalNemesisBaseInfo factionExternal = RelatedEntity.GetFactionOrNull_Safe().BaseInfo.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
                        if (factionExternal != null)
                        {
                            //-10% for every intensity below 10, up to 10% at D1
                            BaseShields = RelatedEntity.DataForMark.BaseShieldPoints * factionExternal.Intensity / 10;
                            BaseShields = BaseShields + (factionExternal.bonusShield / factionExternal.beacon_shield_divider);
                        }
                    }
                }
            }
            catch
            {

            }


            return BaseShields;
        }

        public void RunEntitySpecialPerSecondLogic(GameEntity_Base RelatedEntity, ArcenClientOrHostSimContextCore Context)
        {
            //wow
        }

        void IArcenExternalClassThatClearsDataPriorToMainMenuOrNewMap.ClearAllMyDataForQuitToMainMenuOrBeforeNewMap()
        {
        }
    }
}
