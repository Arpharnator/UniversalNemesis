using Arcen.AIW2.Core;
using Arcen.AIW2.External.BulkPathfinding;
using Arcen.Universal;
using System;

namespace Arcen.AIW2.External
{
    public class UniversalNemesisDeepInfo : ExternalFactionDeepInfoRoot, IBulkPathfinding
    {
        public UniversalNemesisBaseInfo BaseInfo;
        public static UniversalNemesisDeepInfo Instance = null;
        protected override int MinimumSecondsBetweenLongRangePlannings => 8;
        public override void DoAnyInitializationImmediatelyAfterFactionAssigned()
        {
            this.BaseInfo = this.AttachedFaction.GetExternalBaseInfoAs<UniversalNemesisBaseInfo>();
            Instance = this;
        }

        protected override void Cleanup()
        {
            Instance = null;
            BaseInfo = null;
        }
        private readonly List<Planet> workingAllowedSpawnPlanets = List<Planet>.Create_WillNeverBeGCed(30, "UniversalNemesisFactionDeepInfo-workingAllowedSpawnPlanets");

        public override void DoPerSecondLogic_Stage3Main_OnMainThreadAndPartOfSim_HostOnly(ArcenHostOnlySimContext Context)
        {
            if (!AttachedFaction.HasDoneInvasionStyleAction &&
                     (AttachedFaction.InvasionTime > 0 && AttachedFaction.InvasionTime <= World_AIW2.Instance.GameSecond))
            {
                //debugCode = 500;
                //Lets default to just putting the nanocaust hive on a completely random non-player non-ai-king planet
                //TODO: improve this
                GameEntityTypeData entityData ;
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalNemesisKing");
                /*
                if (BaseInfo.Intensity > 5)
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalNemesisKing");
                }*/
                /*
                else
                {
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "WeakUniversalNemesisKing");
                }*/

                Planet spawnPlanet = null;
                {
                    workingAllowedSpawnPlanets.Clear();
                    int preferredHomeworldDistance = 16;
                    if (this.BaseInfo.SeedNearPlayer && World_AIW2.Instance.PlayerOwnedPlanets != 0)
                    {
                        World_AIW2.Instance.DoForPlanetsSingleThread(false, delegate (Planet planet)
                        {
                            if (planet.GetControllingFactionType() == FactionType.Player)
                            {
                                workingAllowedSpawnPlanets.Add(planet);
                                return DelReturn.Continue;
                            }

                            return DelReturn.Continue;
                        });
                    }
                    else if (!this.BaseInfo.SeedNearPlayer)
                    {
                        do
                        {
                            //debugCode = 600;
                            World_AIW2.Instance.DoForPlanetsSingleThread(false, delegate (Planet planet)
                            {
                                if (planet.GetControllingFactionType() == FactionType.Player)
                                    return DelReturn.Continue;
                                if (planet.GetFactionWithSpecialInfluenceHere().Type != FactionType.NaturalObject && preferredHomeworldDistance >= 6) //don't seed over a minor faction if we are finding good spots
                                {
                                    return DelReturn.Continue;
                                }
                                if (planet.IsPlanetToBeDestroyed || planet.HasPlanetBeenDestroyed)
                                    return DelReturn.Continue;
                                if (planet.PopulationType == PlanetPopulationType.AIBastionWorld ||
                                        planet.IsZenithArchitraveTerritory)
                                {
                                    return DelReturn.Continue;
                                }
                                //debugCode = 800;
                                if (planet.OriginalHopsToAIHomeworld >= preferredHomeworldDistance &&
                                        (planet.OriginalHopsToHumanHomeworld == -1 ||
                                        planet.OriginalHopsToHumanHomeworld >= preferredHomeworldDistance))
                                    workingAllowedSpawnPlanets.Add(planet);
                                return DelReturn.Continue;
                            });

                            preferredHomeworldDistance--;
                            //No need to go past the first loop if we are to seed near the player
                            if (preferredHomeworldDistance == 0)
                                break;
                        } while (workingAllowedSpawnPlanets.Count < 24);
                    }

                    //debugCode = 900;
                    //if (workingAllowedSpawnPlanets.Count == 0)
                    //    throw new Exception("Unable to find a place to spawn the Azaran Empire");

                    if (workingAllowedSpawnPlanets.Count != 0)
                    {
                        // This is not actually random unless we set the seed ourselves.
                        // Since other processing happening before us tends to set the seed to the same value repeatedly.
                        Context.RandomToUse.ReinitializeWithSeed(Engine_Universal.PermanentQualityRandom.Next() + AttachedFaction.FactionIndex);
                        spawnPlanet = workingAllowedSpawnPlanets[Context.RandomToUse.Next(0, workingAllowedSpawnPlanets.Count)];

                        // always instead of spawning on this planet, create a new planet linked to it
                        //spawnPlanet = CreateSpawnPlanet(Context, spawnPlanet);
                        PlanetFaction pFaction = spawnPlanet.GetPlanetFactionForFaction(AttachedFaction);
                        ArcenPoint spawnLocation = spawnPlanet.GetSafePlacementPointAroundPlanetCenter(Context, entityData, FInt.FromParts(0, 200), FInt.FromParts(0, 600));

                        var azarKing = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, entityData.MarkFor(pFaction),
                                                    pFaction.FleetUsedAtPlanet, 0, spawnLocation, Context, "UniversalNemesisKing");
                        AttachedFaction.HasDoneInvasionStyleAction = true;
                        azarKing.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                        SquadViewChatHandlerBase chatHandlerOrNull = ChatClickHandler.CreateNewAs<SquadViewChatHandlerBase>("ShipGeneralFocus");
                        if (chatHandlerOrNull != null)
                            chatHandlerOrNull.SquadToView = LazyLoadSquadWrapper.Create(azarKing);

                        string planetStr = "";
                        if (spawnPlanet.GetDoHumansHaveVision())
                        {
                            planetStr = " from " + spawnPlanet.Name;
                        }

                        var str = string.Format("<color=#{0}>{1}</color> are invading{2}!", AttachedFaction.FactionCenterColor.ColorHexBrighter, AttachedFaction.GetDisplayName(), planetStr);
                        World_AIW2.Instance.QueueChatMessageOrCommand(str, ChatType.LogToCentralChat, chatHandlerOrNull);
                    }


                }


            }
            //No nemesis no spawning
            if (this.BaseInfo.Nemesis.Display == null)
            {
                return;
            }
            int totalGains = BaseInfo.bonusHull + BaseInfo.bonusShield;
            if (World_AIW2.Instance.GameSecond - BaseInfo.lastSecondSpawnedCataclysm > 3600)
            {
                if (totalGains > BaseInfo.threshold_cataclysm_spawn)
                {
                    BaseInfo.lastSecondSpawnedCataclysm = World_AIW2.Instance.GameSecond;
                    GameEntityTypeData entityData;
                    PlanetFaction pFaction = BaseInfo.Nemesis.Display.PlanetFaction;
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalCataclysm");
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, BaseInfo.Nemesis.Display.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, BaseInfo.Nemesis.Display.WorldLocation, Context, "Macrophage-Harvester");
                    newEntity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                }
            }
            if (World_AIW2.Instance.GameSecond - BaseInfo.lastSecondSpawnedCatastrophe > 1800)
            {
                if (totalGains > BaseInfo.threshold_catastrophe_spawn)
                {
                    BaseInfo.lastSecondSpawnedCatastrophe = World_AIW2.Instance.GameSecond;
                    GameEntityTypeData entityData;
                    PlanetFaction pFaction = BaseInfo.Nemesis.Display.PlanetFaction;
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalCatastrophe");
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, BaseInfo.Nemesis.Display.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, BaseInfo.Nemesis.Display.WorldLocation, Context, "Macrophage-Harvester");
                    newEntity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                    /*
                    AttachedFaction.DoForEntities("UniversalCataclysm", delegate (GameEntity_Squad entity)
                    {
                        PlanetFaction plFaction = entity.PlanetFaction;
                        GameEntity_Squad newEntitySecond = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(plFaction, entityData, entity.CurrentMarkLevel,
                        pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Macrophage-Harvester");
                        newEntitySecond.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                        return DelReturn.Continue;
                    });*/
                }
            }
            if (World_AIW2.Instance.GameSecond - BaseInfo.lastSecondSpawnedDisaster > 900)
            {
                if (totalGains > BaseInfo.threshold_disaster_spawn)
                {
                    BaseInfo.lastSecondSpawnedDisaster = World_AIW2.Instance.GameSecond;
                    GameEntityTypeData entityData;
                    PlanetFaction pFaction = BaseInfo.Nemesis.Display.PlanetFaction;
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalDisaster");
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, BaseInfo.Nemesis.Display.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, BaseInfo.Nemesis.Display.WorldLocation, Context, "Macrophage-Harvester");
                    newEntity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                    /*
                    AttachedFaction.DoForEntities("UniversalCatastrophe", delegate (GameEntity_Squad entity)
                    {
                        PlanetFaction plFaction = entity.PlanetFaction;
                        GameEntity_Squad newEntitySecond = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(plFaction, entityData, entity.CurrentMarkLevel,
                        pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Macrophage-Harvester");
                        newEntitySecond.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                        return DelReturn.Continue;
                    });*/
                }
            }
            if (World_AIW2.Instance.GameSecond - BaseInfo.lastSecondSpawnedVillain > 180)
            {
                if (totalGains > BaseInfo.threshold_villain_spawn)
                {
                    BaseInfo.lastSecondSpawnedVillain = World_AIW2.Instance.GameSecond;
                    GameEntityTypeData entityData;
                    PlanetFaction pFaction = BaseInfo.Nemesis.Display.PlanetFaction;
                    entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalVillain");
                    GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, BaseInfo.Nemesis.Display.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, BaseInfo.Nemesis.Display.WorldLocation, Context, "Macrophage-Harvester");
                    newEntity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                    /*
                    AttachedFaction.DoForEntities("UniversalDisaster", delegate (GameEntity_Squad entity)
                    {
                        PlanetFaction plFaction = entity.PlanetFaction;
                        GameEntity_Squad newEntitySecond = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(plFaction, entityData, entity.CurrentMarkLevel,
                        pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Macrophage-Harvester");
                        newEntitySecond.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                        return DelReturn.Continue;
                    });*/
                    AttachedFaction.DoForEntities("UniversalBeacon", delegate (GameEntity_Squad entity)
                    {
                        PlanetFaction plFaction = entity.PlanetFaction;
                        GameEntity_Squad newEntitySecond = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(plFaction, entityData, entity.CurrentMarkLevel,
                        pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Macrophage-Harvester");
                        newEntitySecond.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                        return DelReturn.Continue;
                    });
                }
            }
            if (World_AIW2.Instance.GameSecond - BaseInfo.lastSecondSpawnedHenchman > 30)
            {
                BaseInfo.lastSecondSpawnedHenchman = World_AIW2.Instance.GameSecond;
                GameEntityTypeData entityData;
                PlanetFaction pFaction = BaseInfo.Nemesis.Display.PlanetFaction;
                entityData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalHenchman");
                GameEntity_Squad newEntity = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, entityData, BaseInfo.Nemesis.Display.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, BaseInfo.Nemesis.Display.WorldLocation, Context, "Macrophage-Harvester");
                newEntity.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                /*
                AttachedFaction.DoForEntities("UniversalVillain", delegate (GameEntity_Squad entity)
                {
                    PlanetFaction plFaction = entity.PlanetFaction;
                    GameEntity_Squad newEntitySecond = GameEntity_Squad.CreateNew_ReturnNullIfMPClient(plFaction, entityData, entity.CurrentMarkLevel,
                    pFaction.FleetUsedAtPlanet, 0, entity.WorldLocation, Context, "Macrophage-Harvester");
                    newEntitySecond.Orders.SetBehaviorDirectlyInSim(EntityBehaviorType.Attacker_Full);
                    return DelReturn.Continue;
                });*/
            }
        }

        public readonly List<SafeSquadWrapper> UniversalCataclysmLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed(1500, "UniversalNemesisFactionDeepInfo-UniversalCataclysmLRP");
        public readonly List<SafeSquadWrapper> UniversalCatastropheLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed(1500, "UniversalNemesisFactionDeepInfo-UniversalCatastropheLRP");
        public readonly List<SafeSquadWrapper> UniversalDisasterLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed(1500, "UniversalNemesisFactionDeepInfo-UniversalDisasterLRP");
        public readonly List<SafeSquadWrapper> UniversalVillainLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed(1500, "UniversalNemesisFactionDeepInfo-UniversalVillainLRP");
        public readonly List<SafeSquadWrapper> UniversalHenchmanLRP = List<SafeSquadWrapper>.Create_WillNeverBeGCed(15000, "UniversalNemesisFactionDeepInfo-UniversalHenchmanLRP");

        public override void DoLongRangePlanning_OnBackgroundNonSimThread_Subclass(ArcenLongTermIntermittentPlanningContext Context)
        {
            PerFactionPathCache pathingCacheData = PerFactionPathCache.GetCacheForTemporaryUse_MustReturnToPoolAfterUseOrLeaksMemory();
            try
            {
                UniversalCataclysmLRP.Clear();
                UniversalCatastropheLRP.Clear();
                UniversalDisasterLRP.Clear();
                UniversalVillainLRP.Clear();
                UniversalHenchmanLRP.Clear();
                AttachedFaction.DoForEntities(delegate (GameEntity_Squad entity)
                {
                    //debugCode = 200;
                    if (entity == null)
                        return DelReturn.Continue;

                    var factionData = entity.Planet.GetStanceDataForFaction(AttachedFaction);
                    if (factionData[FactionStance.Hostile].TotalStrength < 2000)
                    {
                        GameEntityTypeData beaconTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalBeacon");

                        if (entity.Planet.GetFirstMatching(beaconTypeData, false, false) == null)
                        {
                            PlanetFaction pFaction = entity.PlanetFaction;
                            //hopefully this is dead center
                            ArcenHostOnlySimContext hostctx = Context.GetHostOnlyContext();
                            ArcenPoint spawnLocation = entity.Planet.GetSafePlacementPointAroundPlanetCenter(hostctx, beaconTypeData, FInt.FromParts(0, 000), FInt.FromParts(0, 000));
                            GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, beaconTypeData, 1, pFaction.FleetUsedAtPlanet, 0, spawnLocation, hostctx, "UniversalBeacon");
                        }
                    }
                        

                    if (entity.TypeData.GetHasTag("UniversalCataclysm"))
                    {
                        //debugCode = 300;
                        UniversalCataclysmLRP.Add(entity);
                        /*
                        if (!BaseInfo.nemesisRecuperating)
                        {
                            AttackPlanetDecisionMaking(entity, Context, pathingCacheData);
                        }*/
                        return DelReturn.Continue;
                    }
                    if (entity.TypeData.GetHasTag("UniversalCatastrophe"))
                    {
                        //debugCode = 300;
                        UniversalCatastropheLRP.Add(entity);
                        /*
                        if (!BaseInfo.nemesisRecuperating)
                        {
                            AttackPlanetDecisionMaking(entity, Context, pathingCacheData);
                        }*/
                        return DelReturn.Continue;
                    }
                    if (entity.TypeData.GetHasTag("UniversalDisaster"))
                    {
                        //debugCode = 300;
                        UniversalDisasterLRP.Add(entity);
                        /*
                        if (!BaseInfo.nemesisRecuperating)
                        {
                            AttackPlanetDecisionMaking(entity, Context, pathingCacheData);
                        }*/
                        return DelReturn.Continue;
                    }
                    if (entity.TypeData.GetHasTag("UniversalVillain"))
                    {
                        //debugCode = 300;
                        UniversalVillainLRP.Add(entity);
                        /*
                        if (!BaseInfo.nemesisRecuperating)
                        {
                            AttackPlanetDecisionMaking(entity, Context, pathingCacheData);
                        }*/
                        return DelReturn.Continue;
                    }
                    if (entity.TypeData.GetHasTag("UniversalHenchman"))
                    {
                        //debugCode = 300;
                        UniversalHenchmanLRP.Add(entity);
                        //here we do not check for whether or not the nemesis is regening, since we don't care
                        //AttackPlanetDecisionMaking(entity, Context, pathingCacheData);
                        return DelReturn.Continue;
                    }
                    if (!entity.TypeData.IsMobileCombatant)
                        return DelReturn.Continue;
                    return DelReturn.Continue;
                });
                //PlanetsToAttack(2);
                if (BaseInfo.nemesisRecuperating)
                {
                    HandleUniversalGenericLRPNemesisCover(Context, pathingCacheData, 1, UniversalCataclysmLRP);
                    HandleUniversalGenericLRPNemesisCover(Context, pathingCacheData, 2, UniversalCatastropheLRP);
                    HandleUniversalGenericLRPNemesisCover(Context, pathingCacheData, 2, UniversalDisasterLRP);
                    HandleUniversalGenericLRPNemesisCover(Context, pathingCacheData, 3, UniversalVillainLRP);
                }
                else
                {
                    AttackPlanetDecisionMakingList(UniversalCataclysmLRP, Context, pathingCacheData);
                    AttackPlanetDecisionMakingList(UniversalCatastropheLRP, Context, pathingCacheData);
                    AttackPlanetDecisionMakingList(UniversalDisasterLRP, Context, pathingCacheData);
                    AttackPlanetDecisionMakingList(UniversalVillainLRP, Context, pathingCacheData);
                }
                //henchmen always attack
                AttackPlanetDecisionMakingList(UniversalHenchmanLRP, Context, pathingCacheData);
            }
            catch
            {

            }
            finally
            {
                FactionUtilityMethods.Instance.FlushUnitsFromReinforcementPointsOnAllRelevantPlanets(AttachedFaction, Context, 5f);
                pathingCacheData.ReturnToPool();
            }
        }
        
        
        List<Planet> planetsToCover = Planet.GetTemporaryPlanetList("UniversalNemesis-planetsToCover", 10f);

        public void HandleUniversalGenericLRPNemesisCover(ArcenLongTermIntermittentPlanningContext Context, PerFactionPathCache PathCacheData, short hops, List<SafeSquadWrapper> listContainingStuff)
        {
            planetsToCover.Clear();
            try
            {
                BaseInfo.Nemesis.Display.Planet.DoForPlanetsWithinXHops(hops, delegate (Planet planet, Int16 Distance)
                {
                    if (!planet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction))
                    {
                        planetsToCover.Add(planet);
                    }
                    return DelReturn.Continue;
                }, delegate (Planet secondaryPlanet)
                {
                    //debugCode = 1700;
                    //don't path through hostile planets.
                    if (secondaryPlanet.GetControllingOrInfluencingFaction().GetIsHostileTowards(AttachedFaction))
                        return PropogationEvaluation.SelfButNotNeighbors;
                    return PropogationEvaluation.Yes;
                });
                for (int i = 0; i < listContainingStuff.Count; i++)
                {
                    var factionData = listContainingStuff[i].GetSquad().Planet.GetStanceDataForFaction(AttachedFaction);
                    if (factionData[FactionStance.Hostile].TotalStrength < 2000 && !listContainingStuff[i].GetSquad().HasExplicitOrders())
                    {
                        Planet planetToSend = planetsToCover[Context.RandomToUse.Next(planetsToCover.Count)];
                        SendShipToPlanet(listContainingStuff[i].GetSquad(), planetToSend, Context, PathCacheData);
                    }
                }
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleUniversalGenericLRPNemesisCover debugCode " + e.ToString(), Verbosity.DoNotShow);
            }
            Planet.ReleaseTemporaryPlanetList(planetsToCover);
        }

        public override void DoOnAnyDeathLogic_FromCentralLoop_NotJustMyOwnShips_HostOnly(ref int debugStage, GameEntity_Squad entity, DamageSource Damage, EntitySystem FiringSystemOrNull,
              Faction factionThatKilledEntity, Faction entityOwningFaction, int numExtraStacksKilled, ArcenHostOnlySimContext Context)
        {
            if (this.BaseInfo.Nemesis.Display == null)
            {
                return;
            }
            try
            {
                //WE DO NOT CHECK FOR MULTIPLE NEMESIS FACTIONS, MAKE SURE THERE IS ONLY ONE EVER

                if (FiringSystemOrNull != null)
                {
                    debugStage = 5900;
                    //if the Macrophage is enabled and the dying unit is a ship
                    GameEntity_Squad EntityThatKilledTarget = FiringSystemOrNull.ParentEntity;
                    //So now we have some shenanigans when stuff is killed because players can also contribute
                    if (EntityThatKilledTarget != null)
                    {
                        debugStage = 6000;
                        if (EntityThatKilledTarget.TypeData.GetHasTag("LeagueOfEvilUnit"))
                        {
                            debugStage = 6100;
                            
                            int gains = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                            gains /= 10; //i suppose this might lower a bit low values (ie small strikecraft), but should be fine
                            gains *= BaseInfo.gain_multiplier_per_intensity * BaseInfo.Intensity;
                            //this is what lies inside of the Base Info, for generic information.
                            //then when this gets used, it is divided based on the ship tier that gets augmented.
                            BaseInfo.bonusHull += gains;
                            BaseInfo.bonusShield += gains;
                        }
                    }
                }

                //We are checking for our own units dying here
                //so uh as a sidenote, make sure this tag can't be gained by any units other than the one specifically in the faction
                //unless you want weird stuff like get unit but feed the nemesis, which i guess is interesting
                //Technically this also feeds on beacons, which can be put a rather large amount of times, but the strength value is low so it should be fine even if chain killing a few
                //in fact there aren't even that much
                if (entity.TypeData.GetHasTag("LeagueOfEvilUnit"))
                {
                    int gains = entity.GetStrengthPerSquad() + entity.GetStrengthPerSquad() * numExtraStacksKilled;
                    gains /= 10; //i suppose this might lower a bit low values (ie small strikecraft), but should be fine
                    gains *= BaseInfo.gain_multiplier_per_intensity * BaseInfo.Intensity;
                    //this is what lies inside of the Base Info, for generic information.
                    //then when this gets used, it is divided based on the ship tier that gets augmented.
                    BaseInfo.bonusHull += gains;
                    BaseInfo.bonusShield += gains;
                }
            }
            catch
            {
                //Exceptions here
            }
        }

        //TODO
        public override void UpdatePlanetInfluence_HostOnly(ArcenHostOnlySimContext Context)
        {
            //reset the faction Influences for this one
            List<Planet> planetsInfluenced = Planet.GetTemporaryPlanetList("UniversalNemesis-UpdatePlanetInfluence_HostOnly-planetsInfluenced", 10f);

            World_AIW2.Instance.DoForPlanetsSingleThread(false, delegate (Planet planet)
            {
                //Beacons give influence
                GameEntityTypeData beaconTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalBeacon");
                if (planet.GetFirstMatching(beaconTypeData, false, false) != null)
                {
                    planetsInfluenced.AddIfNotAlreadyIn(planet);
                }
                //player is a nono, in case i plan to spawn waves
                /*
                if (planet.GetControllingFactionType() == FactionType.Player)
                    return DelReturn.Continue;
                var factionData = planet.GetStanceDataForFaction(AttachedFaction);
                if (factionData[FactionStance.Self].TotalStrength > 10000)
                {
                    planetsInfluenced.AddIfNotAlreadyIn(planet);
                }
                */
                return DelReturn.Continue;
            });

            //Every influenced planet adds 20 AIP
            //i feel I'd rather spawn huntard or smth
            AttachedFaction.SetInfluenceForPlanetsToList(planetsInfluenced);
            Planet.ReleaseTemporaryPlanetList(planetsInfluenced);
        }

        //Some utility functions
        //List<Planet> planetsToAttack = Planet.GetTemporaryPlanetList("UniversalNemesis-planetsToAttack", 10f);
        /*
        public void PlanetsToAttack(short hops)
        {
            planetsToAttack.Clear();
            BaseInfo.Nemesis.Display.Planet.DoForPlanetsWithinXHops(hops, delegate (Planet planet, Int16 Distance)
            {
                planetsToCover.Add(planet);
                return DelReturn.Continue;
            });
            //Choose a planet to attack based on the nemesis' position (might be not too good due to beacon villain spawn but we shall see
            
                        BaseInfo.Nemesis.Display.Planet.DoForPlanetsWithinXHops(2, delegate (Planet planet, Int16 Distance)
                        {
                            //debugCode = 1600;
                            //perhaps we are looking at the AIs stance against us?
                            var otherPlanetFactionData = planet.GetStanceDataForFaction(AttachedFaction);
                            if (otherPlanetFactionData[FactionStance.Hostile].TotalStrength > 2000)
                            {
                                planetsToAttack.Add(planet);
                            }
                            return DelReturn.Continue;
                        });
                        if (planetsToAttack.Count == 0)
                        {
                            //no restrictions, as a fallback so we keep moving (or we made a sea of neutral planets) around the nemesis
                            BaseInfo.Nemesis.Display.Planet.DoForPlanetsWithinXHops(1, delegate (Planet planet, Int16 Distance)
                            {
                                //debugCode = 1600;
                                planetsToAttack.Add(planet);
                                return DelReturn.Continue;
                            });
                        }
        Planet.ReleaseTemporaryPlanetList(planetsToAttack);
        }*/
        List<Planet> planetsToAttack = Planet.GetTemporaryPlanetList("UniversalNemesis-planetsToAttack", 10f);
        public void AttackPlanetDecisionMakingList(List<SafeSquadWrapper> listContainingStuff, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData)
        {
            planetsToAttack.Clear(); //idk
            BaseInfo.Nemesis.Display.Planet.DoForLinkedNeighborsAndSelf(true, delegate (Planet planet)
            {
                planetsToAttack.Add(planet);
                return DelReturn.Continue;
            });
            for (int i = 0; i < listContainingStuff.Count; i++)
            {
                var factionData = listContainingStuff[i].GetSquad().Planet.GetStanceDataForFaction(AttachedFaction);
                if (factionData[FactionStance.Hostile].TotalStrength < 2000 && !listContainingStuff[i].GetSquad().HasExplicitOrders())
                {
                    Planet planetToSend = planetsToAttack[Context.RandomToUse.Next(planetsToAttack.Count)];
                    SendShipToPlanet(listContainingStuff[i].GetSquad(), planetToSend, Context, PathCacheData);
                }
                //path through stuff with ennemies, hit them instead
                else if (factionData[FactionStance.Hostile].TotalStrength > 2000 && listContainingStuff[i].GetSquad().HasExplicitOrders())
                {
                    SendShipToLocation(listContainingStuff[i].GetSquad(), listContainingStuff[i].GetSquad().WorldLocation, Context);
                }
            }
            Planet.ReleaseTemporaryPlanetList(planetsToAttack);
        }
        /*
        public void AttackPlanetDecisionMakingList(List<SafeSquadWrapper> listContainingStuff, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData)
        {
            try
            {
                int i = 0;

                for (int i = 0; i < listContainingStuff.Count; i++)
                {
                    var factionData = listContainingStuff[i].GetSquad().Planet.GetStanceDataForFaction(AttachedFaction);
                    if (factionData[FactionStance.Hostile].TotalStrength < 2000 && !listContainingStuff[i].GetSquad().HasExplicitOrders())
                    {
                        Planet planetToSend = planetsToCover[Context.RandomToUse.Next(planetsToCover.Count)];
                        SendShipToPlanet(listContainingStuff[i].GetSquad(), planetToSend, Context, PathCacheData);
                    }
                    //we stumbled upon ennemies while we were transiting, dispatch them instead of pursuing the order
                    else if (factionData[FactionStance.Hostile].TotalStrength > 2000 && listContainingStuff[i].GetSquad().HasExplicitOrders())
                    {
                        SendShipToLocation(listContainingStuff[i].GetSquad(), listContainingStuff[i].GetSquad().WorldLocation, Context);
                    }
                }
            }
            catch (Exception e)
            {
                ArcenDebugging.ArcenDebugLogSingleLine("Hit exception in HandleUniversalGenericLRPNemesisCover debugCode " + e.ToString(), Verbosity.DoNotShow);
            }
            Planet.ReleaseTemporaryPlanetList(planetsToCover);
        }
        */
        /*
        public void AttackPlanetDecisionMakingList(List<SafeSquadWrapper> listContainingStuff, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData)
        {
            //iterate through all the ships in the list, and have them choose a target if their planet is considered empty
            for (int j = 0; j < listContainingStuff.Count; j++)
            {
                var factionData = listContainingStuff[j].GetSquad().Planet.GetStanceDataForFaction(AttachedFaction);
                //we will suddenly stop pathing if there are ennemies on our planet
                if (factionData[FactionStance.Hostile].TotalStrength < 2000 && !listContainingStuff[j].GetSquad().HasExplicitOrders())
                {
                    //create a beacon if we don't have one
                    GameEntityTypeData beaconTypeData = GameEntityTypeDataTable.Instance.GetRandomRowWithTag(Context, "UniversalBeacon");
                    if (listContainingStuff[j].GetSquad().Planet.GetFirstMatching(beaconTypeData, false, false) == null)
                    {
                        PlanetFaction pFaction = listContainingStuff[j].GetSquad().PlanetFaction;
                        //hopefully this is dead center
                        ArcenHostOnlySimContext hostctx = Context.GetHostOnlyContext();
                        ArcenPoint spawnLocation = listContainingStuff[j].GetSquad().Planet.GetSafePlacementPointAroundPlanetCenter(hostctx, beaconTypeData, FInt.FromParts(0, 000), FInt.FromParts(0, 000));
                        GameEntity_Squad.CreateNew_ReturnNullIfMPClient(pFaction, beaconTypeData, BaseInfo.Nemesis.Display.CurrentMarkLevel, pFaction.FleetUsedAtPlanet, 0, spawnLocation, hostctx, "UniversalBeacon");
                    }
                    //choose a planet among our choices of planets to attack
                    Planet target = null;
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
                    SendShipToPlanet(listContainingStuff[j].GetSquad(), target, Context, PathCacheData);
                }
                //we stumbled upon ennemies while we were transiting, dispatch them instead of pursuing the order
                else if (factionData[FactionStance.Hostile].TotalStrength > 2000 && listContainingStuff[j].GetSquad().HasExplicitOrders())
                {
                    SendShipToLocation(listContainingStuff[j].GetSquad(), listContainingStuff[j].GetSquad().WorldLocation, Context);
                }
                    
            }
            
        }*/

        public void SendShipToPlanet(GameEntity_Squad entity, Planet destination, ArcenSimContextAnyStatus Context, PerFactionPathCache PathCacheData)
        {
            PathBetweenPlanetsForFaction pathCache = PathingHelper.FindPathFreshOrFromCache(entity.PlanetFaction.Faction, "CityShipsSendShipToPlanet", entity.Planet, destination, PathingMode.Safest, Context, PathCacheData);
            if (pathCache != null && pathCache.PathToReadOnly.Count > 0)
            {
                GameCommand command = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.SetWormholePath_NPCSingleUnit], GameCommandSource.AnythingElse);
                command.RelatedString = "CityShips_Dest";
                command.RelatedEntityIDs.Add(entity.PrimaryKeyID);
                for (int k = 0; k < pathCache.PathToReadOnly.Count; k++)
                    command.RelatedIntegers.Add(pathCache.PathToReadOnly[k].Index);
                World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, command, false);
            }
        }
        public void SendShipToLocation(GameEntity_Squad entity, ArcenPoint dest, ArcenSimContextAnyStatus Context)
        {
            GameCommand moveCommand = GameCommand.Create(BaseGameCommand.CommandsByCode[BaseGameCommand.Code.MoveManyToOnePoint_NPCVisitTargetOnPlanet], GameCommandSource.AnythingElse);
            moveCommand.PlanetOrderWasIssuedFrom = entity.Planet.Index;
            moveCommand.RelatedPoints.Add(dest);
            moveCommand.RelatedEntityIDs.Add(entity.PrimaryKeyID);
            World_AIW2.Instance.QueueGameCommand(this.AttachedFaction, moveCommand, false);
        }
    }
}
